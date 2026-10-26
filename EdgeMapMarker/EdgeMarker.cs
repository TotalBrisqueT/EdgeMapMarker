using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EdgeMapMarker
{
    // Draws the target's icon, an outward arrow and the distance on the edge of the small minimap
    // when the target is outside its view.
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

        private static RectTransform _root;
        private static RectTransform _icon;
        private static RectTransform _arrow;
        private static TMP_Text _label;

        private static void Postfix(Minimap __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_mode != Minimap.MapMode.Small)
            {
                SetActive(_root, false);
                return;
            }

            EnsureMarker(__instance);

            Vector3 target = GetHomePosition();
            RawImage map = __instance.m_mapImageSmall;
            Rect rect = map.rectTransform.rect;

            // Same world -> minimap conversion the game uses for its own pins.
            __instance.WorldToMapPoint(target, out float mx, out float my);
            Vector2 pos = __instance.MapPointToLocalGuiPos(mx, my, map.uvRect, rect);

            Vector2 center = new Vector2(rect.width / 2f, rect.height / 2f);
            Vector2 offset = pos - center;

            // Inside the minimap: the game's own pin is visible, so stay hidden.
            if (Mathf.Abs(offset.x) <= center.x && Mathf.Abs(offset.y) <= center.y)
            {
                SetActive(_root, false);
                return;
            }

            SetActive(_root, true);

            // Scaling each element and its gap from the edge together keeps the layout's proportions.
            float scale = Plugin.MarkerScale.Value;
            Vector3 elementScale = new Vector3(scale, scale, 1f);

            SetActive(_icon, Plugin.ShowIcon.Value);
            _icon.localScale = elementScale;
            _icon.anchoredPosition = center + ClampToEdge(offset, center, IconInset * scale);

            SetActive(_arrow, Plugin.ShowArrow.Value);
            _arrow.localScale = elementScale;
            _arrow.anchoredPosition = center + ClampToEdge(offset, center, ArrowInset * scale);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90f);

            SetActive(_label.rectTransform, Plugin.ShowDistance.Value);
            _label.rectTransform.localScale = elementScale;
            _label.rectTransform.anchoredPosition = center + ClampToEdge(offset, center, LabelInset * scale);
            if (Plugin.ShowDistance.Value)
            {
                _label.text = FormatDistance(Utils.DistanceXZ(player.transform.position, target));
            }
        }

        // Bed spawn if the player has one in this world, otherwise the world spawn.
        private static Vector3 GetHomePosition()
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
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

        private static void EnsureMarker(Minimap minimap)
        {
            if (_root != null)
            {
                return;
            }

            // An invisible container covering the same area as the game's pin layer, so everything inside
            // it uses the same coordinates as real pins.
            GameObject root = new GameObject("EdgeMapMarker", typeof(RectTransform));
            _root = (RectTransform)root.transform;
            _root.SetParent(minimap.m_pinRootSmall, false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            RectTransform pinPrefab = (RectTransform)minimap.m_pinPrefab.transform;

            // Clone the game's pin prefab so the icon has the same anchors, size and look as real pins.
            GameObject icon = Object.Instantiate(minimap.m_pinPrefab, _root);
            icon.name = "Icon";
            _icon = (RectTransform)icon.transform;
            _icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minimap.m_pinSizeSmall);
            _icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minimap.m_pinSizeSmall);
            icon.transform.Find("Checked")?.gameObject.SetActive(false);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.sprite = minimap.GetSprite(Minimap.PinType.Bed);
            iconImage.raycastTarget = false;

            GameObject arrow = NewChild("Arrow", _root, pinPrefab);
            Image arrowImage = arrow.AddComponent<Image>();
            arrowImage.sprite = CreateArrowSprite();
            arrowImage.raycastTarget = false;
            _arrow = (RectTransform)arrow.transform;
            _arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);

            GameObject label = NewChild("Distance", _root, pinPrefab);
            _label = label.AddComponent<TextMeshProUGUI>();
            _label.font = minimap.m_biomeNameSmall.font;
            _label.fontSharedMaterial = minimap.m_biomeNameSmall.fontSharedMaterial;
            _label.fontSize = LabelFontSize;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            _label.rectTransform.sizeDelta = new Vector2(80f, 20f);
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
