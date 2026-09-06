using System.Collections.Generic;
using Colossal;
using Game.Settings;

namespace AutoNighttimeLightExtension
{
    public class Localization : IDictionarySource
    {
        private readonly Setting setting;

        public Localization(Setting setting)
        {
            this.setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { setting.GetSettingsLocaleID(), "自动夜间照明扩展" },

                // Groups
                { setting.GetOptionGroupLocaleID(Setting.kAdaptiveGroup), "自适应 LOD 动态缩放（实验性）" },
                { setting.GetOptionGroupLocaleID(Setting.kNightGroup), "夜间 LOD 视距" },
                { setting.GetOptionGroupLocaleID(Setting.kResetGroup), "重置设置" },

                // Group 1: 自适应 LOD 动态缩放
                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableAdaptiveLod)), "自适应 LOD 动态缩放" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableAdaptiveLod)), "通过动态调整 LOD 达到优化帧数效果,仅在GPU 瓶颈（暂停模拟）时良好。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.AdaptiveResponseSpeed)), "响应调节" },
                { setting.GetOptionDescLocaleID(nameof(Setting.AdaptiveResponseSpeed)), "设置自适应调节的收敛速度，较低数值变化更平缓。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.TargetFps)), "期望目标帧率" },
                { setting.GetOptionDescLocaleID(nameof(Setting.TargetFps)), "自适应调节的目标帧率。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.MinAdaptiveLod)), "最小 LOD 视距" },
                { setting.GetOptionDescLocaleID(nameof(Setting.MinAdaptiveLod)), "自适应调节允许的最小 LOD 视距比例。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.MaxAdaptiveLod)), "最大 LOD 视距" },
                { setting.GetOptionDescLocaleID(nameof(Setting.MaxAdaptiveLod)), "自适应调节允许的最大 LOD 视距比例。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableStillCameraStabilization)), "静止视角画质稳定" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableStillCameraStabilization)), "当摄像机静止且帧率收敛至目标基准附近时，削减LOD 动态缩放。" },

                // Group 2: 夜间 LOD 视距
                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableNightLodMultiplier)), "启用夜间照明扩展" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableNightLodMultiplier)), "在夜间启用独立的 LOD 视距倍率设置，在游戏时间18:00~6:00生效。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.NightLodMultiplier)), "夜间 LOD 视距倍率" },
                { setting.GetOptionDescLocaleID(nameof(Setting.NightLodMultiplier)), "游戏内夜间的 LOD 视距倍率。" },

                { setting.GetOptionLabelLocaleID(nameof(Setting.NightMaxLightCount)), "夜间全局最大光源容量上限（实验性）" },
                { setting.GetOptionDescLocaleID(nameof(Setting.NightMaxLightCount)), "游戏内夜间的光源容量上限。" },

                // Group 3: 重置设置
                { setting.GetOptionLabelLocaleID(nameof(Setting.ResetModSettings)), "恢复默认设置" },
                { setting.GetOptionDescLocaleID(nameof(Setting.ResetModSettings)), "恢复所有设置为默认值。" },
                { setting.GetOptionWarningLocaleID(nameof(Setting.ResetModSettings)), "确定恢复默认设置？" },
            };
        }

        public static void RegisterTranslations(Setting setting)
        {
            // 安全空判断
            var locManager = Game.SceneFlow.GameManager.instance?.localizationManager;
            if (locManager != null)
            {
                locManager.AddSource("zh-HANS", new Localization(setting));
                locManager.AddSource("en-US", new LocaleEN(setting));
            }
        }

        public void Unload()
        {
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting setting;

        public LocaleEN(Setting setting)
        {
            this.setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { setting.GetSettingsLocaleID(), "Auto Night LOD Extender" },

                // Groups
                { setting.GetOptionGroupLocaleID(Setting.kAdaptiveGroup), "Adaptive LOD Scaling(Experimental)." },
                { setting.GetOptionGroupLocaleID(Setting.kNightGroup), "Nighttime LOD Distance" },
                { setting.GetOptionGroupLocaleID(Setting.kResetGroup), "Reset Settings" },

                // Group 1: Adaptive Group
                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableAdaptiveLod)), "Adaptive LOD Scaling" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableAdaptiveLod)), "Dynamically adjusts LOD to maintain framerate.It only performs well when the GPU is bottlenecked (the simulation is paused)." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.AdaptiveResponseSpeed)), "Response Speed" },
                { setting.GetOptionDescLocaleID(nameof(Setting.AdaptiveResponseSpeed)), "Adjusts convergence rate; lower values produce smoother transitions." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.TargetFps)), "Target Framerate" },
                { setting.GetOptionDescLocaleID(nameof(Setting.TargetFps)), "Target framerate for adaptive adjustment." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.MinAdaptiveLod)), "Minimum LOD Distance" },
                { setting.GetOptionDescLocaleID(nameof(Setting.MinAdaptiveLod)), "Minimum allowed LOD distance scale during adaptive adjustment." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.MaxAdaptiveLod)), "Maximum LOD Distance" },
                { setting.GetOptionDescLocaleID(nameof(Setting.MaxAdaptiveLod)), "Maximum allowed LOD distance scale during adaptive adjustment." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableStillCameraStabilization)), "Still View Stabilization" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableStillCameraStabilization)), "Reduces LOD dynamic scaling when the camera is stationary and framerate converges near the target." },

                // Group 2: Nighttime Group
                { setting.GetOptionLabelLocaleID(nameof(Setting.EnableNightLodMultiplier)), "Enable automatic night lighting extension" },
                { setting.GetOptionDescLocaleID(nameof(Setting.EnableNightLodMultiplier)), "Enables LOD extension during nighttime.Effective from 6pm to 6am game time." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.NightLodMultiplier)), "Nighttime LOD Multiplier" },
                { setting.GetOptionDescLocaleID(nameof(Setting.NightLodMultiplier)), "LOD distance multiplier during nighttime." },

                { setting.GetOptionLabelLocaleID(nameof(Setting.NightMaxLightCount)), "Nighttime Max Light Count(Experimental)." },
                { setting.GetOptionDescLocaleID(nameof(Setting.NightMaxLightCount)), "Maximum light capacity during nighttime." },

                // Group 3: Reset Group
                { setting.GetOptionLabelLocaleID(nameof(Setting.ResetModSettings)), "Reset to Defaults" },
                { setting.GetOptionDescLocaleID(nameof(Setting.ResetModSettings)), "Resets all settings to default values." },
                { setting.GetOptionWarningLocaleID(nameof(Setting.ResetModSettings)), "Do you want to reset all settings to defaults?" },
            };
        }

        public void Unload()
        {
        }
    }
}
