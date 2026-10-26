using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

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
        internal static ConfigEntry<KeyCode> TrackKey;
        internal static ConfigEntry<bool> TrackHomeByDefault;
        internal static ConfigEntry<bool> ShowPlayerNames;
        internal static ConfigEntry<bool> ColorPlayers;
        internal static ConfigEntry<bool> HighlightTracked;
        internal static ConfigEntry<HighlightStyle> HighlightStyleSetting;
        internal static ConfigEntry<Color> HighlightColor;

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

            TrackKey = Config.Bind("Tracking", "Track key", KeyCode.LeftAlt,
                "Hold this and left-click a pin on the large map to track it. Do it again on the same pin to stop.");
            TrackHomeByDefault = Config.Bind("Tracking", "Track home by default", true,
                "Track your bed (or the world spawn if you have no bed) in worlds where you haven't toggled it. " +
                "Track-key + click near your spawn point on the large map to toggle it per world.");
            ShowPlayerNames = Config.Bind("Players", "Show player names", true,
                "Show a tracked player's name next to the distance on their edge marker.");
            ColorPlayers = Config.Bind("Players", "Color players", true,
                "Give each tracked player's arrow and name a colour picked from their character name, " +
                "so everyone with the mod sees the same player in the same colour.");

            HighlightTracked = Config.Bind("Tracking", "Highlight tracked pins", true,
                "Mark tracked pins on the large map and the minimap.");
            HighlightStyleSetting = Config.Bind("Tracking", "Highlight style", HighlightStyle.Badge,
                "Badge: a small dot in the pin's top-right corner. Ring: a ring around the pin.");
            HighlightColor = Config.Bind("Tracking", "Highlight color", new Color(1f, 0.75f, 0.2f, 0.6f),
                "Color of the tracked-pin highlight. Lower the alpha to make it fainter.");

            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, PluginGuid);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
