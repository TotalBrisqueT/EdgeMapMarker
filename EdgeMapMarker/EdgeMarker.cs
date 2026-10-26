using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EdgeMapMarker
{
    // Draws the target's icon on the edge of the small minimap when the target is outside its view.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    internal static class EdgeMarker
    {
        // Gap between the marker centre and the minimap edge, in minimap UI units.
        private const float EdgeMargin = 12f;

        private static RectTransform _marker;
        private static Image _icon;

        private static void Postfix(Minimap __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_mode != Minimap.MapMode.Small)
            {
                SetVisible(false);
                return;
            }

            EnsureMarker(__instance);

            Vector3 target = GetHomePosition();
            RawImage map = __instance.m_mapImageSmall;
            Rect uv = map.uvRect;
            Rect rect = map.rectTransform.rect;

            // Same world -> minimap conversion the game uses for its own pins.
            __instance.WorldToMapPoint(target, out float mx, out float my);
            Vector2 pos = __instance.MapPointToLocalGuiPos(mx, my, uv, rect);

            Vector2 center = new Vector2(rect.width / 2f, rect.height / 2f);
            Vector2 offset = pos - center;
            float halfW = center.x - EdgeMargin;
            float halfH = center.y - EdgeMargin;

            // Inside the minimap: the game's own pin is visible, so stay hidden.
            if (Mathf.Abs(offset.x) <= halfW && Mathf.Abs(offset.y) <= halfH)
            {
                SetVisible(false);
                return;
            }

            // Shrink the offset until it touches the inset rectangle, keeping its direction.
            float scale = Mathf.Min(
                offset.x != 0f ? halfW / Mathf.Abs(offset.x) : float.MaxValue,
                offset.y != 0f ? halfH / Mathf.Abs(offset.y) : float.MaxValue);
            _marker.anchoredPosition = center + offset * scale;
            SetVisible(true);
        }

        // Bed spawn if the player has one in this world, otherwise the world spawn.
        private static Vector3 GetHomePosition()
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
        }

        private static void EnsureMarker(Minimap minimap)
        {
            if (_marker != null)
            {
                return;
            }

            // Clone the game's pin prefab so the marker uses the same anchors, size and look as real pins.
            GameObject go = Object.Instantiate(minimap.m_pinPrefab, minimap.m_pinRootSmall);
            go.name = "EdgeMapMarker";
            _marker = (RectTransform)go.transform;
            _marker.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minimap.m_pinSizeSmall);
            _marker.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minimap.m_pinSizeSmall);
            go.transform.Find("Checked")?.gameObject.SetActive(false);

            _icon = go.GetComponent<Image>();
            _icon.sprite = minimap.GetSprite(Minimap.PinType.Bed);
            _icon.raycastTarget = false;
        }

        private static void SetVisible(bool visible)
        {
            if (_marker != null && _marker.gameObject.activeSelf != visible)
            {
                _marker.gameObject.SetActive(visible);
            }
        }
    }
}
