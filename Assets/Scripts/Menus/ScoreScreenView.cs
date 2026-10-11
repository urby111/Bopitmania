using System;
using BopBeat.Beatmaps;
using UnityEngine;
using UnityEngine.UI;

namespace BopBeat.Menus
{
    /// <summary>Simple results UI created once and reused for each finished song.</summary>
    public sealed class ScoreScreenView
    {
        public GameObject Root { get; }
        public Button ReturnButton { get; }
        private readonly Text title, score, maxCombo;
        private readonly Text[] counts = new Text[4];

        public ScoreScreenView(Transform parent, Font sportypo, Texture2D background, Action returnToSongs)
        {
            Root = new GameObject("Score Screen", typeof(RectTransform), typeof(Image));
            var screen = Root.GetComponent<RectTransform>();
            screen.SetParent(parent, false);
            screen.anchorMin = Vector2.zero;
            screen.anchorMax = Vector2.one;
            screen.offsetMin = screen.offsetMax = Vector2.zero;
            Root.GetComponent<Image>().color = new Color32(5, 43, 54, 255);
            var bg = Rect("Pixelated Background", screen, 0, 0, 1280, 720);
            var image = bg.gameObject.AddComponent<RawImage>();
            image.texture = background;
            image.raycastTarget = false;
            image.uvRect = new Rect(0, 0.005f, 1, 0.99f);
            var cover = bg.gameObject.AddComponent<AspectRatioFitter>();
            cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cover.aspectRatio = background != null ? background.width / (background.height * 0.99f) : 16f / 9;

            var layout = Rect("Score Layout", screen, 0, 0, 1280, 720);
            layout.gameObject.AddComponent<SongSelectLayout>();
            var regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font labels = sportypo != null ? sportypo : regular;
            Label("Heading", layout, "score", labels, 72, 140, 40, 1000, 96);
            title = Label("Song Title", layout, "", labels, 30, 140, 153, 1000, 58);
            score = Label("Final Score", layout, "0", regular, 76, 140, 224, 1000, 108);
            score.fontStyle = FontStyle.BoldAndItalic;
            Label("Max Combo Label", layout, "max combo", labels, 30, 320, 352, 380, 54);
            maxCombo = Label("Max Combo Value", layout, "0", regular, 40, 715, 347, 240, 60);
            maxCombo.fontStyle = FontStyle.BoldAndItalic;
            string[] grades = { "perfect", "good", "ok", "miss" };
            for (int i = 0; i < grades.Length; i++)
            {
                Label(grades[i] + " Label", layout, grades[i], labels, 23, 140 + 250 * i, 440, 250, 44);
                counts[i] = Label(grades[i] + " Count", layout, "0", regular, 36, 140 + 250 * i, 489, 250, 50);
            }
            var buttonRect = Rect("Song Select Button", layout, 310, 586, 660, 88);
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color32(5, 61, 75, 245);
            var border = buttonRect.gameObject.AddComponent<Outline>();
            border.effectColor = Color.white;
            border.effectDistance = new Vector2(2, -2);
            ReturnButton = buttonRect.gameObject.AddComponent<Button>();
            ReturnButton.targetGraphic = buttonImage;
            ReturnButton.onClick.AddListener(() => returnToSongs());
            var navigation = ReturnButton.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = navigation.selectOnDown = navigation.selectOnLeft = navigation.selectOnRight = ReturnButton;
            ReturnButton.navigation = navigation;
            Label("Label", buttonRect, "song select", labels, 32, 18, 8, 624, 72);
            Root.SetActive(false);
        }

        public void Show(SongDefinition song, BeatmapSession session)
        {
            title.text = song != null ? song.SongTitle : "";
            score.text = session.Score.ToString("0.##");
            maxCombo.text = session.MaxCombo.ToString();
            int[] values = { session.PerfectCount, session.GoodCount, session.OkCount, session.MissCount };
            for (int i = 0; i < values.Length; i++) counts[i].text = values[i].ToString();
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Text Label(string name, Transform parent, string value, Font font, int size, float x, float y, float width, float height)
        {
            var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = size;
            return label;
        }
    }
}
