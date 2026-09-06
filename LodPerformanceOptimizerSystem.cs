using System;
using Colossal.Logging;
using Game;
using Game.Rendering;
using Game.Settings;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AutoNighttimeLightExtension
{
    public partial class LodPerformanceOptimizerSystem : GameSystemBase
    {
        public static LodPerformanceOptimizerSystem? Instance { get; private set; }

        private RenderingSystem? m_RenderingSystem;
        private PlanetarySystem? m_PlanetarySystem;
        private LightingSystem? m_LightingSystem;
        private CameraUpdateSystem? m_CameraUpdateSystem;
        private int m_OfficialMaxLightCount = -1;
        private float m_OfficialLevelOfDetail = -1f;
        private bool m_OfficialCrossFade = true;
        private bool m_OfficialSettingsCaptured = false;
        private const int MEDIAN_WINDOW_SIZE = 5;
        private readonly float[] m_RawHistory = new float[MEDIAN_WINDOW_SIZE] { 33.333f, 33.333f, 33.333f, 33.333f, 33.333f };
        private readonly float[] m_SortBuffer = new float[MEDIAN_WINDOW_SIZE] { 33.333f, 33.333f, 33.333f, 33.333f, 33.333f };
        private int m_HistoryIndex = 0;
        private float m_EnvelopeFrameTimeMs = 33.333f;
                private float m_DropStress = 0.0f;

        private float3 m_PrevCamPos;
        private float3 m_PrevCamDir;
        private bool m_HasCamHistory = false;
        private float m_CamStillDuration = 0.0f;
        private bool m_IsStillStabilized = false;

        // 当前管线实际 LOD 与平滑量
        private float m_CurrentLod = 1.0f;
        private bool m_WasAdaptiveEnabled = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            try
            {
                m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
                m_PlanetarySystem = World.GetOrCreateSystemManaged<PlanetarySystem>();
                m_LightingSystem = World.GetOrCreateSystemManaged<LightingSystem>();
                m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();

                Mod.Log.Info("AutoNighttimeLightExtension: LodPerformanceOptimizerSystem created successfully.");
            }
            catch (Exception ex)
            {
                Mod.Log.ErrorFormat(ex, "AutoNighttimeLightExtension: Failed to initialize LodPerformanceOptimizerSystem dependencies");
            }
        }

        protected override void OnDestroy()
        {
            if (m_RenderingSystem != null)
            {
                var curLod = GetLodQualitySettings();
                float restoreLod = (curLod != null && curLod.levelOfDetail > 0.05f) ? curLod.levelOfDetail : m_OfficialLevelOfDetail;
                int restoreLights = (curLod != null && curLod.maxLightCount > 0) ? curLod.maxLightCount : m_OfficialMaxLightCount;
                bool restoreCrossFade = (curLod != null) ? curLod.lodCrossFade : m_OfficialCrossFade;

                if (restoreLod > 0.05f && Mathf.Abs(m_RenderingSystem.levelOfDetail - restoreLod) > 0.001f)
                {
                    m_RenderingSystem.levelOfDetail = restoreLod;
                }
                if (restoreLights > 0 && m_RenderingSystem.maxLightCount != restoreLights)
                {
                    m_RenderingSystem.maxLightCount = restoreLights;
                }
                if (m_RenderingSystem.lodCrossFade != restoreCrossFade)
                {
                    m_RenderingSystem.lodCrossFade = restoreCrossFade;
                }
            }

            Instance = null;
            base.OnDestroy();
        }

        private static LevelOfDetailQualitySettings? GetLodQualitySettings()
        {
            var list = SharedSettings.instance?.graphics?.qualitySettings;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is LevelOfDetailQualitySettings lod)
                {
                    return lod;
                }
            }
            return null;
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || m_RenderingSystem == null) return;
            // 动态同步最新设置，避免首帧旧快照锁死
            var curLodQuality = GetLodQualitySettings();
            if (curLodQuality != null)
            {
                if (curLodQuality.maxLightCount > 0) m_OfficialMaxLightCount = curLodQuality.maxLightCount;
                if (curLodQuality.levelOfDetail > 0.05f) m_OfficialLevelOfDetail = curLodQuality.levelOfDetail;
                m_OfficialCrossFade = curLodQuality.lodCrossFade;
                m_OfficialSettingsCaptured = true;
            }
            else if (!m_OfficialSettingsCaptured)
            {
                m_OfficialMaxLightCount = m_RenderingSystem.maxLightCount > 0 ? m_RenderingSystem.maxLightCount : 4096;
                m_OfficialLevelOfDetail = m_RenderingSystem.levelOfDetail;
                m_OfficialCrossFade = m_RenderingSystem.lodCrossFade;
                m_OfficialSettingsCaptured = true;
            }

            // LOD 交叉淡入淡出管理：必需强制开启交叉淡入淡，否则LOD 波动时物体会闪烁
            bool targetCrossFade = Mod.Settings.EnableAdaptiveLod ? true : m_OfficialCrossFade;
            if (m_RenderingSystem.lodCrossFade != targetCrossFade)
            {
                m_RenderingSystem.lodCrossFade = targetCrossFade;
            }

            // 昼夜状态判定 对齐游戏内路灯及建筑亮灯时机
            bool isNight;
            if (m_LightingSystem != null && m_LightingSystem.state != LightingSystem.State.Invalid)
            {
                isNight = (m_LightingSystem.state == LightingSystem.State.Night ||
                           m_LightingSystem.state == LightingSystem.State.Dusk ||
                           m_LightingSystem.dayLightBrightness < 0.1f);
            }
            else
            {
                // 无 LightingSystem 时 fallback 退回
                float timeOfDay = m_PlanetarySystem != null ? m_PlanetarySystem.time : 12.0f;
                isNight = (timeOfDay >= 18.0f || timeOfDay < 6.0f);
            }

            // 昼夜光源容量调度 夜间按 Mod 设定扩展，白天由用户设置
            bool applyDedicatedNight = isNight && Mod.Settings.EnableNightLodMultiplier;
            int targetLights = applyDedicatedNight ? Mod.Settings.NightMaxLightCount : m_OfficialMaxLightCount;
            if (m_RenderingSystem.maxLightCount != targetLights)
            {
                m_RenderingSystem.maxLightCount = targetLights;
            }

            float officialGameLod = m_OfficialLevelOfDetail > 0.05f ? m_OfficialLevelOfDetail : 1.0f;

            // LOD 自适应算法
            float finalLod;
            bool applyDedicatedNightLod = isNight && Mod.Settings.EnableNightLodMultiplier;

            if (applyDedicatedNightLod)
            {
                m_WasAdaptiveEnabled = false;
                m_CurrentLod = Mod.Settings.NightLodMultiplier / 100.0f;
                finalLod = m_CurrentLod;
            }
            else if (Mod.Settings.EnableAdaptiveLod)
            {
                float maxLod = Mathf.Clamp(Mod.Settings.MaxAdaptiveLod / 100.0f, 0.2f, 1.0f);
                float minLod = Mathf.Clamp(Mod.Settings.MinAdaptiveLod / 100.0f, 0.1f, maxLod);

                float rawDt = UnityEngine.Time.unscaledDeltaTime;
                if (rawDt > 0.0001f)
                {
                    float dt = Mathf.Clamp(rawDt, 0.002f, 0.250f);
                    float rawFrameTimeMs = dt * 1000.0f;
                    if (!m_WasAdaptiveEnabled)
                    {
                        m_EnvelopeFrameTimeMs = rawFrameTimeMs;
                        for (int i = 0; i < MEDIAN_WINDOW_SIZE; i++)
                        {
                            m_RawHistory[i] = rawFrameTimeMs;
                            m_SortBuffer[i] = rawFrameTimeMs;
                        }
                        m_DropStress = 0.0f;
                        m_CurrentLod = maxLod;
                        m_WasAdaptiveEnabled = true;
                        m_CamStillDuration = 0.0f;
                        m_IsStillStabilized = false;
                        m_HasCamHistory = false;
                    }

                    // 摄像机位移与视角旋转检测
                    bool isCameraMovingThisFrame = false;
                    if (m_CameraUpdateSystem != null)
                    {
                        float3 curPos = m_CameraUpdateSystem.position;
                        float3 curDir = m_CameraUpdateSystem.direction;

                        if (m_HasCamHistory)
                        {
                            float posDeltaSq = math.lengthsq(curPos - m_PrevCamPos);
                            float dirDot = math.dot(curDir, m_PrevCamDir);

                            if (posDeltaSq > 0.0001f || dirDot < 0.9999f)
                            {
                                isCameraMovingThisFrame = true;
                            }
                        }
                        else
                        {
                            m_HasCamHistory = true;
                        }
                        m_PrevCamPos = curPos;
                        m_PrevCamDir = curDir;
                        if (m_CameraUpdateSystem.gamePlayController != null && m_CameraUpdateSystem.gamePlayController.moving)
                        {
                            isCameraMovingThisFrame = true;
                        }
                    }

                    if (isCameraMovingThisFrame)
                    {
                        m_CamStillDuration = 0.0f;
                        m_IsStillStabilized = false; // 仅依赖手动移动退出***
                    }
                    else
                    {
                        m_CamStillDuration += dt;
                    }

                    m_RawHistory[m_HistoryIndex] = rawFrameTimeMs;
                    m_HistoryIndex = (m_HistoryIndex + 1) % MEDIAN_WINDOW_SIZE;
                    Array.Copy(m_RawHistory, m_SortBuffer, MEDIAN_WINDOW_SIZE);
                    Array.Sort(m_SortBuffer);
                    float medianMs = m_SortBuffer[2];

                    const float TAU_ATTACK = 0.100f;
                    const float TAU_DECAY  = 0.850f;

                    float currentTau = (medianMs > m_EnvelopeFrameTimeMs) ? TAU_ATTACK : TAU_DECAY;
                    float alpha = 1.0f - Mathf.Exp(-dt / currentTau);
                    m_EnvelopeFrameTimeMs = Mathf.Lerp(m_EnvelopeFrameTimeMs, medianMs, alpha);

                    // 基准目标与阈值
                    float targetFps = Mathf.Max(10.0f, (float)Mod.Settings.TargetFps);
                    float targetFrameTimeMs = 1000.0f / targetFps;
                    int speedLevel = Mathf.Clamp(Mod.Settings.AdaptiveResponseSpeed, 1, 4);
                    float baseDescentRate;  // 下探基准速率
                    float maxDescentRate;   // 突发卡顿时下探速率
                    float baseAscentRate;   // 爬升速率

                    switch (speedLevel)
                    {
                        case 1: // 慢
                            baseDescentRate = 1.00f; maxDescentRate = 1.00f; baseAscentRate = 0.060f;
                            break;
                        case 4: // 超快
                            baseDescentRate = 1.0f; maxDescentRate = 2.00f; baseAscentRate = 0.400f;
                            break;
                        case 3: // 快
                            baseDescentRate = 1.0f; maxDescentRate = 1.75f; baseAscentRate = 0.180f;
                            break;
                        case 2: // 中
                        default:
                            baseDescentRate = 1.0f; maxDescentRate = 1.50f; baseAscentRate = 0.090f; 
                            break;
                    }

                    // 非对称死区定义
                    float dropDeadbandMs = targetFrameTimeMs * 0.04f;
                    float riseDeadbandMs = targetFrameTimeMs * 0.080f;
                    if (!m_IsStillStabilized && m_CamStillDuration >= 2.5f)
                    {
                        bool isWindowFocused = Application.isFocused;
                        bool isFrameConverged = m_EnvelopeFrameTimeMs <= (targetFrameTimeMs + dropDeadbandMs * 1.5f) || m_CurrentLod >= (maxLod - 0.01f);

                        if (isWindowFocused && isFrameConverged)
                        {
                            m_IsStillStabilized = true;
                        }
                    }
                    if (Mod.Settings.EnableStillCameraStabilization && m_IsStillStabilized)
                    {
                        baseDescentRate = baseAscentRate;
                        maxDescentRate = baseAscentRate;
                    }
                    if (m_EnvelopeFrameTimeMs > (targetFrameTimeMs + dropDeadbandMs))
                    {
                        float severity = (m_EnvelopeFrameTimeMs - (targetFrameTimeMs + dropDeadbandMs)) / (targetFrameTimeMs * 0.20f);
                        m_DropStress = Mathf.Clamp01(m_DropStress + dt * (3.5f + severity * 4.0f));
                    }
                    else
                    {
                        m_DropStress = Mathf.Clamp01(m_DropStress - dt * 5.0f);
                    }

                    float lodDelta = 0.0f;
                    if (m_EnvelopeFrameTimeMs > (targetFrameTimeMs + dropDeadbandMs) && m_DropStress > 0.25f)
                    {
                        float excessMs = m_EnvelopeFrameTimeMs - (targetFrameTimeMs + dropDeadbandMs);
                        float overloadRatio = Mathf.Clamp01(excessMs / (targetFrameTimeMs * 0.25f));
                        float effectiveRate = Mathf.Lerp(baseDescentRate, maxDescentRate, overloadRatio * overloadRatio);
                        lodDelta = -effectiveRate * m_DropStress * dt;
                    }
                    else if (m_EnvelopeFrameTimeMs < (targetFrameTimeMs - riseDeadbandMs) && m_DropStress < 0.10f)
                    {
                        float headroomMs = (targetFrameTimeMs - riseDeadbandMs) - m_EnvelopeFrameTimeMs;
                        float headroomRatio = Mathf.Clamp01(headroomMs / (targetFrameTimeMs * 0.15f));
                        float currentAscentRate = baseAscentRate * (0.35f + 0.65f * headroomRatio);
                        lodDelta = currentAscentRate * dt;
                    }

                    m_CurrentLod = Mathf.Clamp(m_CurrentLod + lodDelta, minLod, maxLod);
                    finalLod = Mathf.Round(m_CurrentLod * 1000.0f) / 1000.0f;
                }
                else
                {
                    finalLod = m_CurrentLod;
                }
            }
            else
            {
                m_WasAdaptiveEnabled = false;
                m_CurrentLod = officialGameLod;
                finalLod = officialGameLod;
            }

            // 按需写入：仅当目标 finalLod 与当前渲染系统的 levelOfDetail 存在实质差异时才写入 避免覆写
            if (Mathf.Abs(m_RenderingSystem.levelOfDetail - finalLod) > 0.001f)
            {
                m_RenderingSystem.levelOfDetail = finalLod;
            }
            // 夜景时LOD 压力大,防止手欠或者其它MOD 打开disableLodModels 造成死机(职责越界)
            // if (m_RenderingSystem.disableLodModels)
            // {
            //     m_RenderingSystem.disableLodModels = false;
            // }
        }
    }
}