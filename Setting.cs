using System;
using System.Linq;
using System.Xml.Serialization;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using UnityEngine;

namespace AutoNighttimeLightExtension
{
    [FileLocation("ModsSettings/AutoNighttimeLightExtension/AutoNighttimeLightExtensionSettings")]
    [SettingsUIGroupOrder(kAdaptiveGroup, kNightGroup, kResetGroup)]
    [SettingsUIShowGroupName(kAdaptiveGroup, kNightGroup, kResetGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kAdaptiveGroup = "AdaptiveGroup";
        public const string kNightGroup = "NightGroup";
        public const string kResetGroup = "ResetGroup";

        // 自适应LOD动态缩放参数
        private bool m_EnableAdaptiveLod = false;
        private int m_AdaptiveResponseSpeed = 1;
        private int m_TargetFps = 30;
        private float m_MinAdaptiveLod = 15.0f;
        private float m_MaxAdaptiveLod = 100.0f;
        private bool m_EnableStillCameraStabilization = true;

        // 夜间 LOD 视距倍率 最大灯光数
        private bool m_EnableNightLodMultiplier = true;
        private float m_NightLodMultiplier = 400.0f;
        private int m_NightMaxLightCount = 65535;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        //Group 1: 自适应LOD动态缩放
        [SettingsUISection(kSection, kAdaptiveGroup)]
        public bool EnableAdaptiveLod
        {
            get => m_EnableAdaptiveLod;
            set => m_EnableAdaptiveLod = value;
        }

        [SettingsUISection(kSection, kAdaptiveGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAdaptiveDisabled))]
        [SettingsUISlider(min = 1, max = 4, step = 1, unit = "integer")]
        public int AdaptiveResponseSpeed
        {
            get => m_AdaptiveResponseSpeed;
            set => m_AdaptiveResponseSpeed = Mathf.Clamp(value, 1, 4);
        }

        [SettingsUISection(kSection, kAdaptiveGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAdaptiveDisabled))]
        // 期望目标帧数
        [SettingsUISlider(min = 15, max = 120, step = 5, unit = "integer")]
        public int TargetFps
        {
            get => m_TargetFps;
            set => m_TargetFps = Mathf.Clamp(value, 15, 120);
        }

        [SettingsUISection(kSection, kAdaptiveGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAdaptiveDisabled))]
        // 最小LOD
        [SettingsUISlider(min = 10f, max = 100f, step = 5f, unit = "percentage")]
        public float MinAdaptiveLod
        {
            get => m_MinAdaptiveLod <= 1.0f ? Mathf.Clamp(m_MinAdaptiveLod * 100f, 10f, 100f) : m_MinAdaptiveLod;
            set => m_MinAdaptiveLod = value;
        }

        [SettingsUISection(kSection, kAdaptiveGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAdaptiveDisabled))]


        // 最大LOD
        [SettingsUISlider(min = 20f, max = 100f, step = 5f, unit = "percentage")]
        public float MaxAdaptiveLod
        {
            get => m_MaxAdaptiveLod <= 4.0f ? Mathf.Clamp(m_MaxAdaptiveLod * 100f, 20f, 100f) : m_MaxAdaptiveLod;
            set => m_MaxAdaptiveLod = value;
        }

        [SettingsUISection(kSection, kAdaptiveGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsAdaptiveDisabled))]
        public bool EnableStillCameraStabilization
        {
            get => m_EnableStillCameraStabilization;
            set => m_EnableStillCameraStabilization = value;
        }

        public bool IsAdaptiveDisabled() => !m_EnableAdaptiveLod;

        //Group 2: 夜间LOD视距
        [SettingsUISection(kSection, kNightGroup)]
        public bool EnableNightLodMultiplier
        {
            get => m_EnableNightLodMultiplier;
            set => m_EnableNightLodMultiplier = value;
        }

        [SettingsUISection(kSection, kNightGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNightLodDisabled))]
        [SettingsUISlider(min = 100f, max = 800f, step = 100f, unit = "percentage")]
        public float NightLodMultiplier
        {
            get => m_NightLodMultiplier <= 8.0f ? Mathf.Clamp(m_NightLodMultiplier * 100f, 100f, 800f) : m_NightLodMultiplier;
            set => m_NightLodMultiplier = value;
        }

        public bool IsNightLodDisabled() => !m_EnableNightLodMultiplier;

        [SettingsUISection(kSection, kNightGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsNightLodDisabled))]
        [SettingsUISlider(min = 4096, max = 65535, step = 2048, unit = "integer")]
        public int NightMaxLightCount
        {
            get => m_NightMaxLightCount;
            set => m_NightMaxLightCount = Mathf.Clamp(value, 4096, 65535);
        }

        //Group 3: 重置参数
        [XmlIgnore]
        [SettingsUIButton]
        [SettingsUISection(kSection, kResetGroup)]
        [SettingsUIConfirmation(null, null)]
        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        public override void SetDefaults()
        {
            m_EnableAdaptiveLod = false;
            m_AdaptiveResponseSpeed = 3;
            m_TargetFps = 30;
            m_MinAdaptiveLod = 25.0f;
            m_MaxAdaptiveLod = 100.0f;
            m_EnableStillCameraStabilization = true;

            m_EnableNightLodMultiplier = true;
            m_NightLodMultiplier = 400.0f;

            m_NightMaxLightCount = 65535;
        }
    }
}

