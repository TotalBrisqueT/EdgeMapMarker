using BepInEx;
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

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    // Smoke test: proves Harmony patches reach the game's minimap. Replaced by the real marker in the next step.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
    internal static class MinimapStartPatch
    {
        private static void Postfix(Minimap __instance)
        {
            Plugin.Log.LogInfo($"Minimap ready (small map root: {__instance.m_smallRoot?.name ?? "missing"})");
        }
    }
}
