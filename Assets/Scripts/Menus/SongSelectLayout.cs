using UnityEngine;

namespace BopBeat.Menus
{
    /// <summary>Keep the reference menu at the top at any Game view aspect ratio.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SongSelectLayout : MonoBehaviour
    {
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            var frame = (RectTransform)transform;
            if (!(frame.parent is RectTransform screen)) return;
            if (screen.rect.width <= 0) return;
            // All reference positions are authored in 1280 x 720 design pixels.
            // Fit the width and keep the top edge fixed rather than centering vertically.
            var top = new Vector2(0.5f, 1);
            var scale = Vector3.one * (screen.rect.width / 1280f);
            if (frame.anchorMin != top) frame.anchorMin = top;
            if (frame.anchorMax != top) frame.anchorMax = top;
            if (frame.pivot != top) frame.pivot = top;
            if (frame.anchoredPosition != Vector2.zero) frame.anchoredPosition = Vector2.zero;
            if (frame.sizeDelta != new Vector2(1280, 720)) frame.sizeDelta = new Vector2(1280, 720);
            if (frame.localScale != scale) frame.localScale = scale;
        }
    }
}
