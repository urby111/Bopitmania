using UnityEngine;
using UnityEngine.UI;

namespace BopBeat.Menus
{
    /// <summary>Title artwork and a background that covers the screen through a full rotation.</summary>
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private RectTransform rotatingBackground;
        [SerializeField] private Button startButton, settingsButton, quitButton;
        [SerializeField, Min(0)] private float clockwiseDegreesPerSecond = 2;
        [SerializeField, Min(1)] private float backgroundOverscan = 1.05f;
        private Vector2 lastSize;

        public Button StartButton => startButton;
        public Button SettingsButton => settingsButton;
        public Button QuitButton => quitButton;
        public RectTransform RotatingBackground => rotatingBackground;

        private void OnEnable() => ResizeBackground();

        private void Update()
        {
            if (rotatingBackground != null)
            {
                ResizeBackground();
                rotatingBackground.Rotate(0, 0, -clockwiseDegreesPerSecond * Time.unscaledDeltaTime);
            }
        }

        private void ResizeBackground()
        {
            if (rotatingBackground == null || !(transform is RectTransform panel)) return;
            var size = panel.rect.size;
            if (size == lastSize && rotatingBackground.sizeDelta.sqrMagnitude > 0) return;
            lastSize = size;
            var image = rotatingBackground.GetComponent<RawImage>();
            float aspect = image != null && image.texture != null ? (float)image.texture.width / image.texture.height : 2;
            float diagonal = size.magnitude * backgroundOverscan;
            rotatingBackground.sizeDelta = new Vector2(diagonal * Mathf.Max(1, aspect), diagonal * Mathf.Max(1, 1 / aspect));
        }
    }
}
