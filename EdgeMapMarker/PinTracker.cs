using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace EdgeMapMarker
{
    // Which map pins the edge markers point at, plus whether home (the spawn point) is tracked. Saved in the
    // character's custom data, per world. Pins are saved as type and position (they have no ID of their own).
    internal static class PinTracker
    {
        private const string SaveKeyPrefix = "EdgeMapMarker.Tracked.";
        private const string HomeKeyPrefix = "EdgeMapMarker.Home.";

        // How long a removed tracked pin may take to reappear before tracking is dropped. Some mods
        // (e.g. shared-map mods) replace pins by removing and re-adding them.
        private const float RemovalGraceSeconds = 2f;

        public struct Target
        {
            public Vector3 Position;
            public Sprite Icon;
        }

        private class Entry
        {
            public Minimap.PinType Type;
            public Vector3 Position;
            public Minimap.PinData Pin;
            public float RemovedAt = -1f;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static Minimap _loadedFor;

        // Fills `targets` with home (if tracked) and every tracked pin.
        public static void GetTargets(Minimap minimap, List<Target> targets)
        {
            targets.Clear();
            Refresh(minimap);

            if (IsHomeTracked())
            {
                targets.Add(new Target { Position = GetHomePosition(), Icon = minimap.GetSprite(Minimap.PinType.Bed) });
            }

            foreach (Entry entry in Entries)
            {
                if (entry.Pin != null)
                {
                    targets.Add(new Target { Position = entry.Pin.m_pos, Icon = entry.Pin.m_icon });
                }
            }
        }

        // Fills `pins` with the map pins that are tracked, including the bed pin when home is tracked.
        public static void GetTrackedPins(Minimap minimap, List<Minimap.PinData> pins)
        {
            pins.Clear();
            Refresh(minimap);

            if (IsHomeTracked() && minimap.m_spawnPointPin != null)
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
        }

        // Bed spawn if the player has one in this world, otherwise the world spawn.
        private static Vector3 GetHomePosition()
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
        }

        // Home follows the config default until the player toggles it in this world.
        private static bool IsHomeTracked()
        {
            Player player = Player.m_localPlayer;
            if (player != null && player.m_customData.TryGetValue(HomeKey, out string value))
            {
                return value == "1";
            }
            return Plugin.TrackHomeByDefault.Value;
        }

        private static void ToggleHome(Minimap minimap)
        {
            bool tracked = !IsHomeTracked();
            Player.m_localPlayer.m_customData[HomeKey] = tracked ? "1" : "0";
            ShowMessage(tracked ? "Tracking: Home" : "Stopped tracking: Home", minimap.GetSprite(Minimap.PinType.Bed));
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

        private static void Toggle(Minimap minimap, Minimap.PinData pin)
        {
            Refresh(minimap);

            Entry existing = Entries.Find(e => e.Pin == pin);
            if (existing != null)
            {
                Entries.Remove(existing);
                ShowMessage($"Stopped tracking: {PinLabel(pin)}", pin.m_icon);
            }
            else
            {
                Entries.Add(new Entry { Type = pin.m_type, Position = pin.m_pos, Pin = pin });
                ShowMessage($"Tracking: {PinLabel(pin)}", pin.m_icon);
            }
            Save();
        }

        private static string PinLabel(Minimap.PinData pin)
        {
            string name = Localization.instance.Localize(pin.m_name);
            return string.IsNullOrEmpty(name) ? "pin" : name;
        }

        private static void ShowMessage(string text, Sprite icon)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, text, 0, icon);
        }

        private static string SaveKey => SaveKeyPrefix + ZNet.instance.GetWorldUID();
        private static string HomeKey => HomeKeyPrefix + ZNet.instance.GetWorldUID();

        // Format: "type|x|z;type|x|z;..."
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
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}|{1}|{2}", (int)entry.Type, pos.x, pos.z);
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
                if (parts.Length == 3
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int typeId)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                {
                    Entries.Add(new Entry { Type = (Minimap.PinType)typeId, Position = new Vector3(x, 0f, z) });
                }
            }
        }

        // Track-key + left click on the large map starts or stops tracking the pin under the cursor.
        // Clicking near the spawn point toggles home, since the game doesn't let the bed pin be clicked.
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
        private static class TrackClickPatch
        {
            private static bool Prefix(Minimap __instance)
            {
                if (!ZInput.GetKey(Plugin.TrackKey.Value))
                {
                    return true;
                }

                float radius = __instance.PinInteractRadius;
                Vector3 pos = __instance.ScreenToWorldPoint(ZInput.pointerPosition);
                Minimap.PinData pin = __instance.GetClosestPin(pos, radius);
                float homeDistance = Utils.DistanceXZ(pos, GetHomePosition());

                if (homeDistance < radius && (pin == null || homeDistance < Utils.DistanceXZ(pos, pin.m_pos)))
                {
                    ToggleHome(__instance);
                }
                else if (pin != null)
                {
                    Toggle(__instance, pin);
                }

                // Skip the game's click handling, so tracking doesn't also tick the pin's checkmark.
                __instance.m_pinUpdateRequired = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.RemovePin), typeof(Minimap.PinData))]
        private static class RemovePinPatch
        {
            private static void Postfix(Minimap.PinData pin)
            {
                foreach (Entry entry in Entries)
                {
                    if (pin != null && entry.Pin == pin)
                    {
                        entry.Position = pin.m_pos;
                        entry.Pin = null;
                        entry.RemovedAt = Time.time;
                    }
                }
            }
        }
    }
}
