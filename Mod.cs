using System;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace AutoNighttimeLightExtension
{
    public class Mod : IMod
    {
        public static ILog Log = LogManager.GetLogger("AutoNighttimeLightExtension").SetShowsErrorsInUI(false);
        public static Mod Instance { get; private set; } = null!;
        public static Setting Settings { get; private set; } = null!;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log.Info("AutoNighttimeLightExtension: OnLoad invoked!");

            try
            {
                Settings = new Setting(this);
                Settings.RegisterInOptionsUI();
                Localization.RegisterTranslations(Settings);
                AssetDatabase.global.LoadSettings("AutoNighttimeLightExtensionSettings", Settings, new Setting(this));
                updateSystem.UpdateAt<LodPerformanceOptimizerSystem>(SystemUpdatePhase.Rendering);
                Log.Info("AutoNighttimeLightExtension: Mod initialized successfully with Continuous Responsive PID Optimizer!");
            }
            catch (Exception ex)
            {
                Log.ErrorFormat(ex, "AutoNighttimeLightExtension: Failed to initialize mod");
                if (Settings != null)
                {
                    try
                    {
                        Settings.UnregisterInOptionsUI();
                    }
                    catch { }
                    Settings = null!;
                }
            }
        }

        public void OnDispose()
        {
            Log.Info("AutoNighttimeLightExtension: OnDispose invoked");
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null!;
            }
            Instance = null!;
        }
    }
}
