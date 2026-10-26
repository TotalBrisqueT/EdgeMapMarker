using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EdgeMapMarker
{
    // Ping-style markers on the HUD: each tracked target gets a floating label with its icon, name and
    // distance at its position in the world, pinned to the left or right screen edge when it's off-screen.
    // Built from the game's own ping text template so it looks the same.
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class HudMarkers
    {
        private const float IconSize = 26f;

        // How far above the ground a pin's marker floats, in metres.
        private const float HeightAboveGround = 2f;

        private class View
        {
            public GameObject Root;
            public RectTransform Rect;
            public TMP_Text Text;
            public Image Icon;
        }

        private static readonly List<View> Views = new List<View>();
        private static readonly List<PinTracker.Target> Targets = new List<PinTracker.Target>();
        private static Chat _owner;

        // Runs after the camera has moved this frame, so markers don't trail it when turning.
        private static void Postfix()
        {
            Player player = Player.m_localPlayer;
            Minimap minimap = Minimap.instance;
            Chat chat = Chat.instance;
            Camera camera = Utils.GetMainCamera();

            // The chat UI (and every view under it) is recreated with each session.
            if (_owner != chat)
            {
                Views.Clear();
                _owner = chat;
            }

            int used = 0;
            if (Plugin.ShowHudMarkers.Value && player != null && minimap != null && chat != null && camera != null
                && minimap.m_mode != Minimap.MapMode.Large && !Hud.IsUserHidden())
            {
                PinTracker.GetTargets(minimap, Targets);
                foreach (PinTracker.Target target in Targets)
                {
                    float distance = Utils.DistanceXZ(player.transform.position, target.Position);
                    if (distance < Plugin.HudHideWithin.Value)
                    {
                        continue;
                    }

                    if (used == Views.Count)
                    {
                        Views.Add(CreateView(chat));
                    }
                    View view = Views[used++];
                    if (!view.Root.activeSelf)
                    {
                        view.Root.SetActive(true);
                    }

                    string distanceText = EdgeMarker.FormatDistance(distance);
                    view.Text.text = string.IsNullOrEmpty(target.Label) ? distanceText : $"{target.Label} ({distanceText})";
                    view.Text.color = target.PlayerName != null && Plugin.ColorPlayers.Value
                        ? PlayerTracker.ColorFor(target.PlayerName)
                        : Color.white;

                    bool showIcon = Plugin.HudShowIcon.Value && target.Icon != null;
                    if (view.Icon.gameObject.activeSelf != showIcon)
                    {
                        view.Icon.gameObject.SetActive(showIcon);
                    }
                    if (view.Icon.sprite != target.Icon)
                    {
                        view.Icon.sprite = target.Icon;
                    }

                    Vector3 screen = ToScreen(camera, WorldPoint(target));
                    screen = chat.ClampToScreenEdge(screen, view.Rect, isChatMessage: true);
                    screen.z = Mathf.Min(screen.z, 100f);
                    view.Root.transform.position = screen;
                }
            }

            for (int i = used; i < Views.Count; i++)
            {
                if (Views[i].Root != null && Views[i].Root.activeSelf)
                {
                    Views[i].Root.SetActive(false);
                }
            }
        }

        // Pins placed on the map have no height (y = 0), so float those above the ground instead.
        // Players and the spawn point have a real height.
        private static Vector3 WorldPoint(PinTracker.Target target)
        {
            Vector3 pos = target.Position;
            if (target.PlayerName == null && Mathf.Approximately(pos.y, 0f))
            {
                pos.y = ZoneSystem.instance.GetGroundHeight(pos, out float ground)
                    ? ground
                    : Mathf.Max(WorldGenerator.instance.GetHeight(pos.x, pos.z), ZoneSystem.instance.m_waterLevel);
            }
            pos.y += HeightAboveGround;
            return pos;
        }

        // Same as the game's ping: project onto the screen, and if that lands off-screen or behind the camera,
        // put it on the left or right edge at the height it would have if you turned toward it.
        private static Vector3 ToScreen(Camera camera, Vector3 world)
        {
            Vector3 screen = camera.WorldToScreenPointScaled(world);
            if (screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height || screen.z < 0f)
            {
                Vector3 toTarget = world - camera.transform.position;
                bool onLeft = Vector3.Dot(camera.transform.right, toTarget) < 0f;
                Vector3 flat = toTarget;
                flat.y = 0f;
                Vector3 forward = camera.transform.forward;
                forward.y = 0f;
                forward = forward.normalized * flat.magnitude;
                screen = camera.WorldToScreenPointScaled(camera.transform.position + forward + Vector3.up * toTarget.y);
                screen.x = onLeft ? 0f : Screen.width;
            }
            return screen;
        }

        private static View CreateView(Chat chat)
        {
            GameObject root = Object.Instantiate(chat.m_worldTextBase, chat.transform);
            root.name = "EdgeMapMarkerHud";
            root.SetActive(true);

            View view = new View
            {
                Root = root,
                Rect = (RectTransform)root.transform,
                Text = root.transform.Find("Text").GetComponent<TMP_Text>(),
            };

            // Icon centred above the text.
            GameObject icon = new GameObject("Icon", typeof(RectTransform));
            RectTransform rt = (RectTransform)icon.transform;
            rt.SetParent(root.transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(IconSize, IconSize);
            rt.anchoredPosition = new Vector2(0f, 2f);
            view.Icon = icon.AddComponent<Image>();
            view.Icon.preserveAspect = true;
            view.Icon.raycastTarget = false;

            return view;
        }
    }
}
