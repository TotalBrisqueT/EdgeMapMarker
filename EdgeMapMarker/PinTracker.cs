using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace EdgeMapMarker
{
    // Where a tracked target is shown: on the minimap edge, as a HUD marker, or both.
    [Flags]
    public enum MarkerViews
    {
        None = 0,
        Minimap = 1,
        Hud = 2,
        Both = Minimap | Hud,
    }

    internal static class MarkerViewsExtensions
    {
        // Track-key + right-click order: Both -> Minimap only -> HUD only -> Both.
        public static MarkerViews Next(this MarkerViews views)
        {
            switch (views)
            {
                case MarkerViews.Both: return MarkerViews.Minimap;
                case MarkerViews.Minimap: return MarkerViews.Hud;
                default: return MarkerViews.Both;
            }
        }

        public static string Describe(this MarkerViews views)
        {
            switch (views)
            {
                case MarkerViews.Minimap: return "minimap only";
                case MarkerViews.Hud: return "HUD only";
                default: return "minimap + HUD";
            }
        }

        // Saved values are 1..3; anything else (old saves, typos) means both.
        public static MarkerViews Parse(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 1 && n <= 3
                ? (MarkerViews)n
                : MarkerViews.Both;
        }
    }

    // Which map pins are tracked and how each is shown, plus home (the spawn point). Saved in the character's
    // custom data, per world. Pins are saved as type and position (they have no ID of their own).
    internal static class PinTracker
    {
        private const string SaveKeyPrefix = "EdgeMapMarker.Tracked.";
        private const string HomeKeyPrefix = "EdgeMapMarker.HomeViews.";

        // Before per-target views, home was saved as "0"/"1" under this key. Still read so old saves carry over.
        private const string LegacyHomeKeyPrefix = "EdgeMapMarker.Home.";

        // How long a pin the player deleted may take to reappear before tracking is dropped. Some mods
        // (e.g. SharedMap) replace pins by removing and re-adding them.
        private const float RemovalGraceSeconds = 2f;

        // True while the game handles the player deleting a pin from the map (right-click). Pins removed
        // any other way (e.g. SharedMap hiding public markers) stay tracked and relink when they return.
        private static bool _userRemoving;

        public struct Target
        {
            public Vector3 Position;
            public Sprite Icon;
            public MarkerViews Views;

            // Shown on the HUD marker: "Home", the pin's name (may be empty) or the player's name.
            public string Label;

            // Set when the target is another player.
            public string PlayerName;
        }

        private class Entry
        {
            public Minimap.PinType Type;
            public Vector3 Position;
            public MarkerViews Views = MarkerViews.Both;
            public Minimap.PinData Pin;
            public float RemovedAt = -1f;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static Minimap _loadedFor;

        // Fills `targets` with home (if tracked), every tracked pin and every tracked player who is visible.
        public static void GetTargets(Minimap minimap, List<Target> targets)
        {
            targets.Clear();
            Refresh(minimap);

            MarkerViews homeViews = GetHomeViews();
            if (homeViews != MarkerViews.None)
            {
                targets.Add(new Target
                {
                    Position = GetHomePosition(),
                    Icon = minimap.GetSprite(Minimap.PinType.Bed),
                    Views = homeViews,
                    Label = "Home",
                });
            }

            foreach (Entry entry in Entries)
            {
                if (entry.Pin != null)
                {
                    targets.Add(new Target
                    {
                        Position = entry.Pin.m_pos,
                        Icon = entry.Pin.m_icon,
                        Views = entry.Views,
                        Label = Localization.instance.Localize(entry.Pin.m_name),
                    });
                }
            }

            PlayerTracker.AddTargets(minimap, targets);
        }

        // Fills `pins` with the map pins that are tracked, including the bed pin when home is tracked and
        // the pins of tracked players.
        public static void GetTrackedPins(Minimap minimap, List<Minimap.PinData> pins)
        {
            pins.Clear();
            Refresh(minimap);

            if (GetHomeViews() != MarkerViews.None && minimap.m_spawnPointPin != null)
            {
                pins.Add(minimap.m_spawnPointPin);
            }

            foreach (Entry entry in Entries)
            {
                if (entry.Pin != null)
                {
                    pins.Add(entry.Pin);
                }
            }

            PlayerTracker.AddTrackedPins(minimap, pins);
        }

        // Bed spawn if the player has one in this world, otherwise the world spawn.
        private static Vector3 GetHomePosition()
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
        }

        // Home follows the config default until the player tracks or untracks it in this world.
        private static MarkerViews GetHomeViews()
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                if (player.m_customData.TryGetValue(HomeKey, out string value))
                {
                    return value == "0" ? MarkerViews.None : MarkerViewsExtensions.Parse(value);
                }
                if (player.m_customData.TryGetValue(LegacyHomeKey, out string legacy))
                {
                    return legacy == "1" ? MarkerViews.Both : MarkerViews.None;
                }
            }
            return Plugin.TrackHomeByDefault.Value ? MarkerViews.Both : MarkerViews.None;
        }

        private static void SetHomeViews(MarkerViews views)
        {
            Player player = Player.m_localPlayer;
            player.m_customData[HomeKey] = ((int)views).ToString(CultureInfo.InvariantCulture);
            player.m_customData.Remove(LegacyHomeKey);
        }

        // Loads the saved list for a new session, links entries to the minimap's pins, and drops entries
        // whose pin was removed and didn't come back.
        private static void Refresh(Minimap minimap)
        {
            if (_loadedFor != minimap && Player.m_localPlayer != null)
            {
                Load();
                _loadedFor = minimap;
            }

            bool changed = false;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry entry = Entries[i];
                if (entry.Pin != null)
                {
                    continue;
                }

                entry.Pin = FindPin(minimap, entry.Type, entry.Position);
                if (entry.Pin != null)
                {
                    entry.RemovedAt = -1f;
                }
                else if (entry.RemovedAt >= 0f && Time.time - entry.RemovedAt > RemovalGraceSeconds)
                {
                    Entries.RemoveAt(i);
                    changed = true;
                    ShowMessage("Stopped tracking: pin removed", minimap.GetSprite(entry.Type));
                }
            }

            if (changed)
            {
                Save();
            }
        }

        private static Minimap.PinData FindPin(Minimap minimap, Minimap.PinType type, Vector3 pos)
        {
            foreach (Minimap.PinData pin in minimap.m_pins)
            {
                if (pin.m_type == type && Utils.DistanceXZ(pin.m_pos, pos) < 1f)
                {
                    return pin;
                }
            }
            return null;
        }

        private static string PinLabel(Minimap.PinData pin)
        {
            string name = Localization.instance.Localize(pin.m_name);
            return string.IsNullOrEmpty(name) ? "pin" : name;
        }

        internal static void ShowMessage(string text, Sprite icon)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, text, 0, icon);
        }

        private static string SaveKey => SaveKeyPrefix + ZNet.instance.GetWorldUID();
        private static string HomeKey => HomeKeyPrefix + ZNet.instance.GetWorldUID();
        private static string LegacyHomeKey => LegacyHomeKeyPrefix + ZNet.instance.GetWorldUID();

        // Format: "type|x|z|views;..." (views was added later; entries without it show both).
        private static void Save()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            if (Entries.Count == 0)
            {
                player.m_customData.Remove(SaveKey);
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (Entry entry in Entries)
            {
                Vector3 pos = entry.Pin != null ? entry.Pin.m_pos : entry.Position;
                if (sb.Length > 0)
                {
                    sb.Append(';');
                }
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", (int)entry.Type, pos.x, pos.z, (int)entry.Views);
            }
            player.m_customData[SaveKey] = sb.ToString();
        }

        private static void Load()
        {
            Entries.Clear();
            if (!Player.m_localPlayer.m_customData.TryGetValue(SaveKey, out string value))
            {
                return;
            }

            foreach (string item in value.Split(';'))
            {
                string[] parts = item.Split('|');
                if ((parts.Length == 3 || parts.Length == 4)
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int typeId)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                {
                    Entries.Add(new Entry
                    {
                        Type = (Minimap.PinType)typeId,
                        Position = new Vector3(x, 0f, z),
                        Views = parts.Length == 4 ? MarkerViewsExtensions.Parse(parts[3]) : MarkerViews.Both,
                    });
                }
            }
        }

        // What a track-key click on the large map lands on: the closest of a pin, another player, or home
        // (the spawn point). The game doesn't let the bed pin or player pins be clicked, so those two are found
        // by distance here.
        private enum ClickKind { Nothing, Pin, Player, Home }

        private static ClickKind FindClickTarget(Minimap minimap, out Minimap.PinData pin, out string playerName)
        {
            float radius = minimap.PinInteractRadius;
            Vector3 pos = minimap.ScreenToWorldPoint(ZInput.pointerPosition);

            pin = minimap.GetClosestPin(pos, radius);
            float pinDistance = pin != null ? Utils.DistanceXZ(pos, pin.m_pos) : float.MaxValue;
            float homeDistance = Utils.DistanceXZ(pos, GetHomePosition());
            if (homeDistance >= radius)
            {
                homeDistance = float.MaxValue;
            }
            if (!PlayerTracker.TryFindClosest(pos, radius, out playerName, out float playerDistance))
            {
                playerDistance = float.MaxValue;
            }

            if (playerDistance < pinDistance && playerDistance < homeDistance)
            {
                return ClickKind.Player;
            }
            if (homeDistance < pinDistance)
            {
                return ClickKind.Home;
            }
            return pin != null ? ClickKind.Pin : ClickKind.Nothing;
        }

        // Track-key + left click: start tracking (shown on the minimap and the HUD), or stop tracking.
        private static void ToggleTracked(Minimap minimap)
        {
            Refresh(minimap);
            switch (FindClickTarget(minimap, out Minimap.PinData pin, out string playerName))
            {
                case ClickKind.Player:
                    PlayerTracker.Toggle(minimap, playerName);
                    break;

                case ClickKind.Home:
                {
                    bool track = GetHomeViews() == MarkerViews.None;
                    SetHomeViews(track ? MarkerViews.Both : MarkerViews.None);
                    ShowMessage(track ? $"Tracking: Home ({MarkerViews.Both.Describe()})" : "Stopped tracking: Home",
                        minimap.GetSprite(Minimap.PinType.Bed));
                    break;
                }

                case ClickKind.Pin:
                {
                    Entry existing = Entries.Find(e => e.Pin == pin);
                    if (existing != null)
                    {
                        Entries.Remove(existing);
                        ShowMessage($"Stopped tracking: {PinLabel(pin)}", pin.m_icon);
                    }
                    else
                    {
                        Entries.Add(new Entry { Type = pin.m_type, Position = pin.m_pos, Pin = pin });
                        ShowMessage($"Tracking: {PinLabel(pin)} ({MarkerViews.Both.Describe()})", pin.m_icon);
                    }
                    Save();
                    break;
                }
            }
        }

        // Track-key + right click on a tracked target: cycle where it's shown. Does nothing on untracked targets.
        private static void CycleViews(Minimap minimap)
        {
            Refresh(minimap);
            switch (FindClickTarget(minimap, out Minimap.PinData pin, out string playerName))
            {
                case ClickKind.Player:
                    PlayerTracker.CycleViews(minimap, playerName);
                    break;

                case ClickKind.Home:
                {
                    MarkerViews views = GetHomeViews();
                    if (views != MarkerViews.None)
                    {
                        views = views.Next();
                        SetHomeViews(views);
                        ShowMessage($"Home: {views.Describe()}", minimap.GetSprite(Minimap.PinType.Bed));
                    }
                    break;
                }

                case ClickKind.Pin:
                {
                    Entry existing = Entries.Find(e => e.Pin == pin);
                    if (existing != null)
                    {
                        existing.Views = existing.Views.Next();
                        ShowMessage($"{PinLabel(pin)}: {existing.Views.Describe()}", pin.m_icon);
                        Save();
                    }
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
        private static class TrackClickPatch
        {
            private static bool Prefix(Minimap __instance)
            {
                if (!ZInput.GetKey(Plugin.TrackKey.Value))
                {
                    return true;
                }

                ToggleTracked(__instance);

                // Skip the game's click handling, so tracking doesn't also tick the pin's checkmark.
                __instance.m_pinUpdateRequired = true;
                return false;
            }
        }

        // Right-click on the map normally deletes the pin under the cursor. With the track key held it cycles
        // the target's views instead, and never deletes.
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePinUnderPointer))]
        private static class CycleClickPatch
        {
            private static bool Prefix(Minimap __instance)
            {
                if (!ZInput.GetKey(Plugin.TrackKey.Value))
                {
                    return true;
                }

                CycleViews(__instance);
                __instance.m_pinUpdateRequired = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePin), typeof(Minimap.PinData))]
        private static class RemovePinPatch
        {
            // __runOriginal is false when another mod's prefix blocked the removal (SharedMap does this for
            // public markers you aren't allowed to delete), so the pin is still there.
            private static void Postfix(Minimap.PinData pin, bool __runOriginal)
            {
                if (pin == null || !__runOriginal)
                {
                    return;
                }
                foreach (Entry entry in Entries)
                {
                    if (entry.Pin == pin)
                    {
                        entry.Position = pin.m_pos;
                        entry.Pin = null;
                        entry.RemovedAt = _userRemoving ? Time.time : -1f;
                    }
                }
            }
        }

        // The player's right-click delete goes through this overload; other removals call RemovePin(PinData) directly.
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePin), typeof(Vector3), typeof(float))]
        private static class UserRemovePinPatch
        {
            private static void Prefix()
            {
                _userRemoving = true;
            }

            private static void Finalizer()
            {
                _userRemoving = false;
            }
        }
    }
}
