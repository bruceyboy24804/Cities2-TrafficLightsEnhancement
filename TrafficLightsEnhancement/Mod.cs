using Colossal.IO.AssetDatabase;
using Game.Net;

namespace C2VM.TrafficLightsEnhancement
{
    using System.Reflection;
    using C2VM.TrafficLightsEnhancement.Extensions;
    using C2VM.TrafficLightsEnhancement.Systems.Serialization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.Rendering;
    using Game.SceneFlow;
    using Game.Settings;
    using Unity.Collections;
    using Unity.Entities;

    public class Mod : IMod
    {
        public static readonly string m_Id = typeof(Mod).Assembly.GetName().Name;

        public static ILog log = LogManager.GetLogger($"{m_Id}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        public static string Version =>
            Assembly.GetExecutingAssembly().GetName().Version.ToString(4);
        public static string InformationalVersion =>
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

        public static Settings m_Setting;

        public static World m_World;

        public static string modName => "C2VM.TrafficLightsEnhancement";

        private static TrafficLightInitializationSystem m_TrafficLightInitializationSystem;

        private static Game.Simulation.TrafficLightSystem m_TrafficLightSystem;

        private static TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem m_PatchedTrafficLightInitializationSystem;

        private static TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem m_PatchedTrafficLightSystem;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Settings(this);
            m_Setting.RegisterInOptionsUI();
            m_Setting.RegisterKeyBindings();
            foreach (var item in new LocaleHelper(modName + ".Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            m_World = updateSystem.World;

            m_TrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<Game.Net.TrafficLightInitializationSystem>();
            m_TrafficLightSystem = m_World.GetOrCreateSystemManaged<Game.Simulation.TrafficLightSystem>();
            m_PatchedTrafficLightInitializationSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem>();
            m_PatchedTrafficLightSystem = m_World.GetOrCreateSystemManaged<C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem>();

            m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>();

            var noneList = new NativeList<ComponentType>(1, Allocator.Temp);
            noneList.Add(ComponentType.ReadOnly<Components.CustomTrafficLights>());
            Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightInitializationSystem, "m_TrafficLightsQuery", noneList);
            Utils.EntityQueryUtils.UpdateEntityQuery(m_TrafficLightSystem, "m_TrafficLightQuery", noneList);

            updateSystem.UpdateBefore<TLEDataMigrationSystem, Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem>(SystemUpdatePhase.Modification4B);
            updateSystem.UpdateBefore<Systems.TrafficLightSystems.Initialisation.PatchedTrafficLightInitializationSystem, Game.Net.TrafficLightInitializationSystem>(SystemUpdatePhase.Modification4B);
            updateSystem.UpdateBefore<Systems.TrafficLightSystems.Simulation.PatchedTrafficLightSystem, Game.Simulation.TrafficLightSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<Systems.TrafficGroupSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<Systems.UI.TooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<Systems.UI.UISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<Systems.Tool.ToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<Systems.Update.ModificationUpdateSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<Systems.Update.SimulationUpdateSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<Systems.Overlay.TrafficLightsOverlaySystem, AreaRenderSystem>(SystemUpdatePhase.Rendering);

            SetCompatibilityMode(m_Setting != null && m_Setting.m_CompatibilityMode);

            string netToolSystemToolID = m_World.GetOrCreateSystemManaged<Game.Tools.NetToolSystem>().toolID;
            Assert(netToolSystemToolID == "Net Tool", $"netToolSystemToolID: {netToolSystemToolID}");
            // NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
            AssetDatabase.global.LoadSettings(nameof(Settings), m_Setting);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }

        public static void SetCompatibilityMode(bool enable)
        {
            m_TrafficLightInitializationSystem.Enabled = enable;
            m_TrafficLightSystem.Enabled = enable;

            m_PatchedTrafficLightInitializationSystem.SetCompatibilityMode(enable);
            m_PatchedTrafficLightSystem.SetCompatibilityMode(enable);

            log.Info($"Compatibility mode is set to {enable}.");
        }

        public static bool IsBeta()
        {
#if SHOW_CANARY_BUILD_WARNING
            return true;
#else
            return false;
#endif
        }

        public static void Assert(bool condition, string message = "", bool showInUI = false, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string expression = "")
        {
            if (condition == true)
            {
                return;
            }
            bool showsErrorsInUI = log.showsErrorsInUI;
            log.SetShowsErrorsInUI(showInUI);
            log.Error($"Assertion failed!\n{message}\nExpression: {expression}");
            log.SetShowsErrorsInUI(showsErrorsInUI);
        }
    }
}