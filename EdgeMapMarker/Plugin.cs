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
}
