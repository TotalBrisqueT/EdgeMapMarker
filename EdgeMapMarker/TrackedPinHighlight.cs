using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EdgeMapMarker
{
    public enum HighlightStyle
    {
        Badge,
        Ring,
    }

    // Marks every tracked pin on the large map and the minimap, either with a small dot in the pin's
    // top-right corner or with a ring around it.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    internal static class TrackedPinHighlight
    {
        // Where each style sits, as fractions of the pin's own rectangle (0..1 covers the pin exactly).
        private static readonly Vector2 BadgeMin = new Vector2(0.71f, 0.71f);
        private static readonly Vector2 BadgeMax = new Vector2(1.03f, 1.03f);
        private static readonly Vector2 RingMin = new Vector2(-0.12f, -0.12f);
        private static readonly Vector2 RingMax = new Vector2(1.12f, 1.12f);

        private class Highlight
        {
            public Image Image;
            public HighlightStyle Style;
        }

        private static readonly Dictionary<Minimap.PinData, Highlight> Highlights = new Dictionary<Minimap.PinData, Highlight>();
        private static readonly List<Minimap.PinData> Tracked = new List<Minimap.PinData>();
        private static readonly List<Minimap.PinData> Stale = new List<Minimap.PinData>();
        private static Minimap _owner;
        private static Sprite _badgeSprite;
        private static Sprite _ringSprite;

        private static void Postfix(Minimap __instance)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            // A new minimap means a new session: the old highlights went with the old pins.
            if (_owner != __instance)
            {
                Highlights.Clear();
                _owner = __instance;
            }

            if (Plugin.HighlightTracked.Value)
            {
                PinTracker.GetTrackedPins(__instance, Tracked);
            }
            else
            {
                Tracked.Clear();
            }

            HighlightStyle style = Plugin.HighlightStyleSetting.Value;

            // The game recreates a pin's UI when it moves between maps or scrolls out of view, which
            // destroys the highlight with it. Forget those, highlights on pins that are no longer tracked,
            // and highlights drawn in a style that has since been changed.
            Stale.Clear();
            foreach (KeyValuePair<Minimap.PinData, Highlight> highlight in Highlights)
            {
                Image image = highlight.Value.Image;
                if (image == null || image.transform.parent != highlight.Key.m_uiElement
                    || highlight.Value.Style != style || !Tracked.Contains(highlight.Key))
                {
                    Stale.Add(highlight.Key);
                }
            }
            foreach (Minimap.PinData pin in Stale)
            {
                if (Highlights[pin].Image != null)
                {
                    Object.Destroy(Highlights[pin].Image.gameObject);
                }
                Highlights.Remove(pin);
            }

            Color color = Plugin.HighlightColor.Value;
            foreach (Minimap.PinData pin in Tracked)
            {
                if (pin.m_uiElement == null)
                {
                    continue;
                }
                if (!Highlights.TryGetValue(pin, out Highlight highlight))
                {
                    highlight = new Highlight { Image = Create(pin.m_uiElement, style), Style = style };
                    Highlights[pin] = highlight;
                }
                highlight.Image.color = color;
            }
        }

        // A child of the pin, anchored to its edges, so it follows the pin's position and size (including
        // the game's pulsing pins) and draws on top of it.
        private static Image Create(RectTransform pinElement, HighlightStyle style)
        {
            GameObject go = new GameObject("EdgeMapMarkerHighlight", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(pinElement, false);
            rt.anchorMin = style == HighlightStyle.Badge ? BadgeMin : RingMin;
            rt.anchorMax = style == HighlightStyle.Badge ? BadgeMax : RingMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.sprite = style == HighlightStyle.Badge ? GetBadgeSprite() : GetRingSprite();
            image.raycastTarget = false;
            return image;
        }

        // A white dot with a dark outline: tinting colours the dot and leaves the outline dark, so it
        // stands out on any background.
        private static Sprite GetBadgeSprite()
        {
            if (_badgeSprite == null)
            {
                _badgeSprite = CreateCircleSprite(32, 15.5f, 11.5f, filled: true);
            }
            return _badgeSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (_ringSprite == null)
            {
                _ringSprite = CreateCircleSprite(64, 31f, 28.5f, filled: false);
            }
            return _ringSprite;
        }

        // Anti-aliased circle. Filled: white inside `inner`, black from `inner` to `outer`.
        // Not filled: a white ring between `inner` and `outer`.
        private static Sprite CreateCircleSprite(int size, float outer, float inner, bool filled)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float outerAlpha = Mathf.Clamp01(outer - d + 0.5f);
                    float innerCoverage = Mathf.Clamp01(inner - d + 0.5f);
                    Color pixel = filled
                        ? new Color(innerCoverage, innerCoverage, innerCoverage, outerAlpha)
                        : new Color(1f, 1f, 1f, Mathf.Min(outerAlpha, 1f - innerCoverage));
                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
