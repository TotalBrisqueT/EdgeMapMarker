using System.Collections.Generic;
using UnityEngine;

namespace EdgeMapMarker
{
    // Which other players the edge markers point at, by character name. Saved in the character's custom
    // data, per world. Only players visible on the map (vanilla "Visible on map") can be found; others stay
    // tracked and reappear when they become visible.
    internal static class PlayerTracker
    {
        private const string SaveKeyPrefix = "EdgeMapMarker.Players.";

        // Distinct colours that read well on the map. A player's colour is picked from this by their name,
        // so every client shows the same player in the same colour without syncing anything.
        private static readonly Color[] Palette =
        {
            new Color(1f, 0.38f, 0.38f),   // red
            new Color(0.38f, 0.7f, 1f),    // blue
            new Color(0.45f, 0.9f, 0.4f),  // green
            new Color(1f, 0.85f, 0.3f),    // yellow
            new Color(0.8f, 0.5f, 1f),     // purple
            new Color(0.3f, 0.92f, 0.88f), // cyan
            new Color(1f, 0.6f, 0.2f),     // orange
            new Color(1f, 0.55f, 0.82f),   // pink
            new Color(0.75f, 0.95f, 0.3f), // lime
            new Color(0.62f, 0.62f, 1f),   // lavender
        };

        private static readonly HashSet<string> Names = new HashSet<string>();
        private static readonly List<ZNet.PlayerInfo> Players = new List<ZNet.PlayerInfo>();
        private static Minimap _loadedFor;

        public static void AddTargets(Minimap minimap, List<PinTracker.Target> targets)
        {
            if (!Refresh(minimap))
            {
                return;
            }

            Sprite icon = minimap.GetSprite(Minimap.PinType.Player);
            foreach (ZNet.PlayerInfo player in Players)
            {
                if (Names.Contains(player.m_name))
                {
                    targets.Add(new PinTracker.Target
                    {
                        Position = player.m_position,
                        Icon = icon,
                        Label = player.m_name,
                        PlayerName = player.m_name,
                    });
                }
            }
        }

        // The game keeps one map pin per visible player, in the same order as ZNet's list of them.
        public static void AddTrackedPins(Minimap minimap, List<Minimap.PinData> pins)
        {
            if (!Refresh(minimap))
            {
                return;
            }

            int count = Mathf.Min(Players.Count, minimap.m_playerPins.Count);
            for (int i = 0; i < count; i++)
            {
                if (Names.Contains(Players[i].m_name))
                {
                    pins.Add(minimap.m_playerPins[i]);
                }
            }
        }

        // The visible player closest to `pos` within `radius`, for map clicks.
        public static bool TryFindClosest(Vector3 pos, float radius, out string name, out float distance)
        {
            name = null;
            distance = radius;
            Players.Clear();
            ZNet.instance.GetOtherPublicPlayers(Players);
            foreach (ZNet.PlayerInfo player in Players)
            {
                float d = Utils.DistanceXZ(pos, player.m_position);
                if (d < distance)
                {
                    name = player.m_name;
                    distance = d;
                }
            }
            return name != null;
        }

        public static void Toggle(Minimap minimap, string name)
        {
            Refresh(minimap);
            bool tracked = Names.Add(name);
            if (!tracked)
            {
                Names.Remove(name);
            }
            Save();
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft,
                (tracked ? "Tracking: " : "Stopped tracking: ") + name, 0, minimap.GetSprite(Minimap.PinType.Player));
        }

        // FNV-1a hash of the lower-cased name: stable across machines and game runs, unlike string.GetHashCode.
        public static Color ColorFor(string name)
        {
            uint hash = 2166136261;
            foreach (char c in name.ToLowerInvariant())
            {
                hash = (hash ^ c) * 16777619;
            }
            return Palette[hash % (uint)Palette.Length];
        }

        // Loads the saved names for a new session and reads which players are visible this frame.
        // Returns false when nothing is tracked.
        private static bool Refresh(Minimap minimap)
        {
            if (_loadedFor != minimap && Player.m_localPlayer != null)
            {
                Load();
                _loadedFor = minimap;
            }
            if (Names.Count == 0)
            {
                return false;
            }

            Players.Clear();
            ZNet.instance.GetOtherPublicPlayers(Players);
            return true;
        }

        private static string SaveKey => SaveKeyPrefix + ZNet.instance.GetWorldUID();

        // Format: names separated by '\n' (character names can't contain line breaks).
        private static void Save()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            if (Names.Count == 0)
            {
                player.m_customData.Remove(SaveKey);
            }
            else
            {
                player.m_customData[SaveKey] = string.Join("\n", Names);
            }
        }

        private static void Load()
        {
            Names.Clear();
            if (Player.m_localPlayer.m_customData.TryGetValue(SaveKey, out string value))
            {
                foreach (string name in value.Split('\n'))
                {
                    if (name.Length > 0)
                    {
                        Names.Add(name);
                    }
                }
            }
        }
    }
}
