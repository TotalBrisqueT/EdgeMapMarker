using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EdgeMapMarker
{
    // For each target outside the small minimap's view, draws its icon, an outward arrow and the distance
    // on the minimap edge.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    internal static class EdgeMarker
    {
        // Distances inward from the minimap edge, in minimap UI units. Each element sits on the same
        // line from the map centre toward the target: arrow outermost, then icon, then label.
        private const float ArrowInset = 8f;
        private const float IconInset = 32f;
        private const float LabelInset = 60f;

        private const float ArrowSize = 14f;
        private const float LabelFontSize = 14f;

        // One icon + arrow + label set, reused from frame to frame.
        private class MarkerView
        {
            public RectTransform Icon;
            public Image IconImage;
            public RectTransform Arrow;
            public Image ArrowImage;
            public TMP_Text Label;
        }

        private static RectTransform _root;
        private static Sprite _arrowSprite;
        private static readonly List<MarkerView> Views = new List<MarkerView>();
        private static readonly List<PinTracker.Target> Targets = new List<PinTracker.Target>();

        private static void Postfix(Minimap __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_mode != Minimap.MapMode.Small)
            {
                SetActive(_root, false);
                return;
            }

            EnsureRoot(__instance);
            SetActive(_root, true);
            PinTracker.GetTargets(__instance, Targets);

            RawImage map = __instance.m_mapImageSmall;
            Rect rect = map.rectTransform.rect;
            Vector2 center = new Vector2(rect.width / 2f, rect.height / 2f);

            // Scaling each element and its gap from the edge together keeps the layout's proportions.
            float scale = Plugin.MarkerScale.Value;
            Vector3 elementScale = new Vector3(scale, scale, 1f);

            int used = 0;
            foreach (PinTracker.Target target in Targets)
            {
                // Same world -> minimap conversion the game uses for its own pins.
                __instance.WorldToMapPoint(target.Position, out float mx, out float my);
                Vector2 offset = __instance.MapPointToLocalGuiPos(mx, my, map.uvRect, rect) - center;

                // Inside the minimap: the game's own pin is visible, so no edge marker.
                if (Mathf.Abs(offset.x) <= center.x && Mathf.Abs(offset.y) <= center.y)
                {
                    continue;
                }

                if (used == Views.Count)
                {
                    Views.Add(CreateView(__instance));
                }
                MarkerView view = Views[used++];

                SetActive(view.Icon, Plugin.ShowIcon.Value);
                if (view.IconImage.sprite != target.Icon)
                {
                    view.IconImage.sprite = target.Icon;
                }
                view.Icon.localScale = elementScale;
                view.Icon.anchoredPosition = center + ClampToEdge(offset, center, IconInset * scale);

                // Players get their own colour on the arrow and name, if enabled.
                bool isPlayer = target.PlayerName != null;
                Color color = isPlayer && Plugin.ColorPlayers.Value ? PlayerTracker.ColorFor(target.PlayerName) : Color.white;

                SetActive(view.Arrow, Plugin.ShowArrow.Value);
                view.ArrowImage.color = color;
                view.Arrow.localScale = elementScale;
                view.Arrow.anchoredPosition = center + ClampToEdge(offset, center, ArrowInset * scale);
                view.Arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90f);

                bool showName = isPlayer && Plugin.ShowPlayerNames.Value;
                bool showLabel = showName || Plugin.ShowDistance.Value;
                SetActive(view.Label.rectTransform, showLabel);
                view.Label.rectTransform.localScale = elementScale;
                view.Label.rectTransform.anchoredPosition = center + ClampToEdge(offset, center, LabelInset * scale);
                if (showLabel)
                {
                    string distance = Plugin.ShowDistance.Value
                        ? FormatDistance(Utils.DistanceXZ(player.transform.position, target.Position))
                        : "";
                    view.Label.text = showName ? (target.PlayerName + " " + distance).TrimEnd() : distance;
                    view.Label.color = color;
                }
            }

            // Hide views left over from frames with more off-map targets.
            for (int i = used; i < Views.Count; i++)
            {
                SetActive(Views[i].Icon, false);
                SetActive(Views[i].Arrow, false);
                SetActive(Views[i].Label.rectTransform, false);
            }
        }

        // Shrinks the offset, keeping its direction, until it touches a rectangle `inset` units inside the map edge.
        private static Vector2 ClampToEdge(Vector2 offset, Vector2 halfSize, float inset)
        {
            float halfW = halfSize.x - inset;
            float halfH = halfSize.y - inset;
            float scale = Mathf.Min(
                offset.x != 0f ? halfW / Mathf.Abs(offset.x) : float.MaxValue,
                offset.y != 0f ? halfH / Mathf.Abs(offset.y) : float.MaxValue);
            return offset * scale;
        }

        private static string FormatDistance(float meters)
        {
            return meters < 1000f ? $"{meters:0} m" : $"{meters / 1000f:0.0} km";
        }

        private static void EnsureRoot(Minimap minimap)
        {
            if (_root != null)
            {
                return;
            }

            // The old minimap (and every view under it) is gone, e.g. after logging out.
            Views.Clear();

            // An invisible container covering the same area as the game's pin layer, so everything inside
            // it uses the same coordinates as real pins.
            GameObject root = new GameObject("EdgeMapMarker", typeof(RectTransform));
            _root = (RectTransform)root.transform;
            _root.SetParent(minimap.m_pinRootSmall, false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
        }

        private static MarkerView CreateView(Minimap minimap)
        {
            MarkerView view = new MarkerView();
            RectTransform pinPrefab = (RectTransform)minimap.m_pinPrefab.transform;

            // Clone the game's pin prefab so the icon has the same anchors, size and look as real pins.
            GameObject icon = Object.Instantiate(minimap.m_pinPrefab, _root);
            icon.name = "Icon";
            view.Icon = (RectTransform)icon.transform;
            view.Icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minimap.m_pinSizeSmall);
            view.Icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minimap.m_pinSizeSmall);
            icon.transform.Find("Checked")?.gameObject.SetActive(false);
            view.IconImage = icon.GetComponent<Image>();
            view.IconImage.raycastTarget = false;

            if (_arrowSprite == null)
            {
                _arrowSprite = CreateArrowSprite();
            }
            GameObject arrow = NewChild("Arrow", _root, pinPrefab);
            Image arrowImage = arrow.AddComponent<Image>();
            arrowImage.sprite = _arrowSprite;
            arrowImage.raycastTarget = false;
            view.ArrowImage = arrowImage;
            view.Arrow = (RectTransform)arrow.transform;
            view.Arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);

            GameObject label = NewChild("Distance", _root, pinPrefab);
            view.Label = label.AddComponent<TextMeshProUGUI>();
            view.Label.font = minimap.m_biomeNameSmall.font;
            view.Label.fontSharedMaterial = minimap.m_biomeNameSmall.fontSharedMaterial;
            view.Label.fontSize = LabelFontSize;
            view.Label.alignment = TextAlignmentOptions.Center;
            view.Label.textWrappingMode = TextWrappingModes.NoWrap;
            view.Label.raycastTarget = false;
            view.Label.rectTransform.sizeDelta = new Vector2(160f, 20f);

            return view;
        }

        // A UI object anchored like a real pin, so its anchoredPosition uses the same coordinates.
        private static GameObject NewChild(string name, RectTransform parent, RectTransform anchorsFrom)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorsFrom.anchorMin;
            rt.anchorMax = anchorsFrom.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        // A white upward-pointing triangle, so it can be tinted and rotated freely.
        private static Sprite CreateArrowSprite()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32 clear = new Color32(255, 255, 255, 0);
            for (int y = 0; y < size; y++)
            {
                // Width shrinks linearly from the base (y = 0) to the tip (y = size - 1).
                float halfWidth = (size - 1 - y) / 2f;
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - size / 2f);
                    float alpha = Mathf.Clamp01(halfWidth - d + 0.5f);
                    tex.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : (Color)clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }
    }
}
