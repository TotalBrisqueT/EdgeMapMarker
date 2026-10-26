using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace EdgeMapMarker
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "SunPheonyx.EdgeMapMarker";
        public const string PluginName = "Edge Map Marker";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ShowIcon;
        internal static ConfigEntry<bool> ShowArrow;
        internal static ConfigEntry<bool> ShowDistance;
        internal static ConfigEntry<float> MarkerScale;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ShowIcon = Config.Bind("Edge marker", "Show icon", true, "Show the target's pin icon on the minimap edge.");
            ShowArrow = Config.Bind("Edge marker", "Show arrow", true, "Show an arrow pointing toward the target.");
            ShowDistance = Config.Bind("Edge marker", "Show distance", true, "Show the distance to the target.");
            MarkerScale = Config.Bind("Edge marker", "Marker size", 0.7f, new ConfigDescription(
                "Size of the icon, arrow and distance text, relative to the game's own pins.",
                new AcceptableValueRange<float>(0.3f, 1.5f)));

            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
