using System;
using System.Collections.Generic;
using BopBeat.Beatmaps;
using BopBeat.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BopBeat.Gameplay
{
    /// <summary>Simple generated UI. Note positions are recalculated from song time, never integrated per frame.</summary>
    public sealed class FallingNoteView
    {
        public const float TargetY = 510;
        public const float LaneWidth = 82;
        public const float FieldHeight = 546;
        private static readonly Color White = Color.white;
        private static readonly Color Muted = new Color32(201, 196, 212, 255);
        private static readonly Color Purple = new Color32(79, 6, 150, 255);
        private static readonly Color[] LaneColors = {
            new Color32(244, 255, 51, 255), new Color32(61, 77, 255, 255), Color.white,
            new Color32(134, 255, 82, 255), new Color32(220, 133, 255, 255)
        };
        private sealed class Block
        {
            public RectTransform root, head, tail, body;
            public Image headImage, tailImage, bodyImage;
            public Text label;
        }
        private readonly List<Block> blocks = new List<Block>();
        private readonly Image[] targets = new Image[5];
        private readonly Sprite[] judgments;
        private readonly Font regular, sportypo;
        private readonly RectTransform noteLayer;
        private readonly Image background, shade, judgmentArt;
        private readonly RawImage defaultBackground;
        private readonly RectTransform pulseCircle;
        private readonly CanvasGroup feedbackGroup;
        private readonly Text score, combo, comboNumber, status, counts, clock, judgmentText, hint;
        private readonly Texture2D circleTexture;
        private readonly Sprite circleSprite;
        private BeatmapSession session;
        private double feedbackStart;
        private bool feedbackVisible;
        private SongDefinition song;
        public const double FeedbackSeconds = 0.6;
        public int VisibleBlockCount { get; private set; }

        public FallingNoteView(Transform parent, Font scoreFont, Sprite[] judgmentSprites, Key[] keys,
            Texture2D backgroundTexture, Sprite boopitArt, Action back, Action replay)
        {
            regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sportypo = scoreFont != null ? scoreFont : regular;
            judgments = judgmentSprites;
            background = Box("Level Background", parent, 0, 0, 0, 0, White).GetComponent<Image>();
            Stretch(background.rectTransform);
            var pixelated = Rect("Pixelated Background", background.transform, 0, 0, 0, 0);
            Stretch(pixelated);
            defaultBackground = pixelated.gameObject.AddComponent<RawImage>();
            defaultBackground.texture = backgroundTexture;
            defaultBackground.raycastTarget = false;
            defaultBackground.uvRect = new UnityEngine.Rect(0, 0.005f, 1, 0.99f);
            var cover = pixelated.gameObject.AddComponent<AspectRatioFitter>();
            cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cover.aspectRatio = backgroundTexture != null ? backgroundTexture.width / (backgroundTexture.height * 0.99f) : 16f / 9;
            shade = Box("Background Shade", parent, 0, 0, 0, 0, Color.clear).GetComponent<Image>();
            Stretch(shade.rectTransform);
            var content = Rect("Falling Note Layout (1280 x 720)", parent, 0, 0, 1280, 720);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;

            Label("Score Label", content, "score", 28, White, 74, 40, 260, 45, sportypo);
            // The supplied Sportypo demo maps digits to promotional symbols.
            // Use readable italic numerals while keeping its font for labels.
            score = Label("Score Value", content, "0", 45, White, 74, 89, 290, 64, regular);
            score.fontStyle = FontStyle.BoldAndItalic;
            clock = Label("Song Time", content, "", 16, Muted, 76, 173, 275, 36);
            status = Label("Song Status", content, "", 20, White, 890, 46, 314, 88);
            hint = Label("Instructions", content, "", 17, Muted, 890, 160, 314, 174);
            counts = Label("Result Counts", content, "", 18, Muted, 890, 365, 314, 125);

            circleTexture = MakeCircle();
            circleSprite = Sprite.Create(circleTexture, new UnityEngine.Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            var circle = Box("Bop It Placeholder", content, 70, 410, 230, 230, White).GetComponent<Image>();
            circle.sprite = circleSprite;
            pulseCircle = circle.rectTransform;
            // Center the pivot so the beat expands evenly around the badge.
            pulseCircle.pivot = new Vector2(0.5f, 0.5f);
            pulseCircle.anchoredPosition += new Vector2(115, -115);
            if (boopitArt != null)
            {
                var logo = Box("Bop It Label", circle.transform, 28, 84, 174, 62, White).GetComponent<Image>();
                logo.sprite = boopitArt;
                logo.preserveAspect = true;
            }
            else Label("Bop It Label", circle.transform, "BOOPIT", 30, Color.black, 24, 90, 182, 55, regular, TextAnchor.MiddleCenter);

            var board = Box("Five Lanes", content, 410, 0, LaneWidth * 5, FieldHeight, Purple);
            for (int lane = 0; lane < 5; lane++)
                Box("Lane " + lane, board, lane * LaneWidth + 5, 0, LaneWidth - 10, 485, new Color32(13, 0, 65, 255));
            noteLayer = Rect("Note Blocks", board, 0, 0, LaneWidth * 5, FieldHeight);
            noteLayer.gameObject.AddComponent<RectMask2D>();
            for (int lane = 0; lane < 5; lane++)
            {
                targets[lane] = Box("Target " + lane, board, lane * LaneWidth + 8, TargetY - 19, LaneWidth - 16, 38, LaneColors[lane]).GetComponent<Image>();
            }
            // The center of each colored target is the timing line.
            Box("Hit Line", board, 4, TargetY - 1, LaneWidth * 5 - 8, 2, new Color(1, 1, 1, 0.5f));
            var feedback = Rect("Judgment Feedback", content, 415, 216, 400, 114);
            feedbackGroup = feedback.gameObject.AddComponent<CanvasGroup>();
            feedbackGroup.blocksRaycasts = false;
            feedbackGroup.interactable = false;
            feedbackGroup.alpha = 0;
            judgmentArt = Box("Judgment Art", feedback, 0, 0, 400, 82, White).GetComponent<Image>();
            judgmentArt.preserveAspect = true;
            judgmentArt.gameObject.SetActive(false);
            judgmentText = Label("Judgment Detail", feedback, "", 16, White, 0, 84, 400, 30, regular, TextAnchor.MiddleCenter);
            comboNumber = Label("Combo Value", content, "0", 35, White, 410, 667, 92, 48, regular, TextAnchor.MiddleRight);
            comboNumber.fontStyle = FontStyle.BoldAndItalic;
            combo = Label("Combo", content, "x combo", 30, White, 512, 672, 310, 43, sportypo, TextAnchor.MiddleLeft);
            Button("Song Select", content, "back / esc", 30, 668, 240, back);
            Button("Replay", content, "replay / r", 975, 668, 265, replay);
        }

        public void Begin(BeatmapSession newSession, SongDefinition level)
        {
            session = newSession;
            song = level;
            background.sprite = level.Background;
            defaultBackground.gameObject.SetActive(level.Background == null && defaultBackground.texture != null);
            background.color = level.Background != null ? level.BackgroundTint : defaultBackground.texture != null ? White : new Color32(54, 48, 69, 255);
            shade.color = new Color(0, 0, 0, 0.25f);
            hint.text = "Press at the colored targets.\n\nLong block: hold to the tail.\nR block: hold early, release at target.\n\nFocus the Game view.";
            judgmentArt.gameObject.SetActive(false);
            judgmentText.text = "";
            feedbackVisible = false;
            feedbackGroup.alpha = 0;
            pulseCircle.localScale = Vector3.one;
            while (blocks.Count < session.TotalCount) blocks.Add(CreateBlock(blocks.Count));
            foreach (var block in blocks) block.root.gameObject.SetActive(false);
            Render(-3, false);
        }

        public void SetStatus(string text) => status.text = text;
        public void Error(string text)
        {
            session = null;
            feedbackVisible = false;
            feedbackGroup.alpha = 0;
            pulseCircle.localScale = Vector3.one;
            foreach (var block in blocks) block.root.gameObject.SetActive(false);
            status.text = "LEVEL COULD NOT START";
            hint.text = text;
            score.text = "0";
            comboNumber.text = "0";
            counts.text = "";
        }

        public void Feedback(NoteFeedback result)
        {
            int index = result.Judgment == Judgment.Perfect ? 0 : result.Judgment == Judgment.Good ? 1 : result.Judgment == Judgment.Ok ? 2 : -1;
            var sprite = index >= 0 && judgments != null && index < judgments.Length ? judgments[index] : null;
            judgmentArt.sprite = sprite;
            judgmentArt.gameObject.SetActive(sprite != null);
            string name = sprite == null ? result.Judgment.ToString().ToUpperInvariant() + "  " : "";
            judgmentText.text = name + (result.Holding ? "HOLD START  " : "")
                + result.ErrorSeconds.ToString("+0.000;-0.000;0.000") + " s";
            feedbackStart = Time.realtimeSinceStartupAsDouble;
            feedbackVisible = true;
            feedbackGroup.alpha = 1;
        }

        public void Render(double songTime, bool finished)
        {
            if (session == null) return;
            score.text = session.Score.ToString("0.##");
            comboNumber.text = session.Combo.ToString();
            clock.text = "song " + Math.Max(0, songTime).ToString("0.00") + " s  /  " + session.Speed.ToString("0.##") + "x";
            counts.text = $"perfect  {session.PerfectCount}\ngood  {session.GoodCount}\nok  {session.OkCount}\nmiss  {session.MissCount}";
            int bpm = song.TempoAt(songTime, out double beatOffset);
            pulseCircle.localScale = Vector3.one * BeatPulse.Scale(songTime, bpm, beatOffset, finished);
            if (feedbackVisible)
            {
                double age = Time.realtimeSinceStartupAsDouble - feedbackStart;
                feedbackGroup.alpha = Mathf.Clamp01(1 - (float)(age / FeedbackSeconds));
                if (age >= FeedbackSeconds)
                {
                    feedbackVisible = false;
                    judgmentArt.gameObject.SetActive(false);
                    judgmentText.text = "";
                }
            }
            VisibleBlockCount = 0;
            double thickness = NoteTravel.BlockHeight(session.Speed, TargetY);
            for (int i = 0; i < session.TotalCount; i++)
            {
                var note = session.NoteAt(i);
                var block = blocks[i];
                double headY = NoteTravel.Position(note.time, songTime, session.Speed, 0, TargetY);
                double tailY = note.Kind == NoteKind.Hold ? NoteTravel.Position(note.endTime, songTime, session.Speed, 0, TargetY) : headY;
                if (session.IsHolding(i)) headY = TargetY;
                bool visible = !finished && !session.IsComplete(i) && headY >= -thickness / 2 && tailY <= FieldHeight + thickness / 2;
                block.root.gameObject.SetActive(visible);
                if (!visible) continue;
                VisibleBlockCount++;
                float x = note.lane * LaneWidth + 10;
                Place(block.head, x, (float)(headY - thickness / 2), LaneWidth - 20, (float)thickness);
                block.headImage.color = LaneColors[note.lane];
                block.label.text = note.Kind == NoteKind.Release ? "R" : "";
                bool hold = note.Kind == NoteKind.Hold;
                block.body.gameObject.SetActive(hold);
                block.tail.gameObject.SetActive(hold);
                if (hold)
                {
                    // Clip the long body before casting to float; very long holds need no huge UI rectangles.
                    double start = Math.Max(0, tailY - thickness / 2);
                    double end = Math.Min(FieldHeight, headY + thickness / 2);
                    Place(block.body, x, (float)start, LaneWidth - 20, (float)Math.Max(0, end - start));
                    Place(block.tail, x, (float)Math.Max(-thickness, tailY - thickness / 2), LaneWidth - 20, (float)thickness);
                    var color = LaneColors[note.lane];
                    block.tailImage.color = color;
                    block.bodyImage.color = color;
                }
            }
            for (int lane = 0; lane < 5; lane++)
                targets[lane].color = session.IsKeyHeld(lane) ? Color.Lerp(LaneColors[lane], Color.black, 0.45f) : LaneColors[lane];
        }

        private Block CreateBlock(int index)
        {
            var block = new Block();
            block.root = Rect("Note " + index, noteLayer, 0, 0, LaneWidth * 5, FieldHeight);
            block.body = Box("Hold Body", block.root, 0, 0, 0, 0, White);
            block.tail = Box("Hold Tail", block.root, 0, 0, 0, 0, White);
            block.head = Box("Note Head", block.root, 0, 0, 0, 0, White);
            block.bodyImage = block.body.GetComponent<Image>();
            block.tailImage = block.tail.GetComponent<Image>();
            block.headImage = block.head.GetComponent<Image>();
            block.label = Label("Release Marker", block.head, "", 14, Color.black, 0, 0, 0, 0, regular, TextAnchor.MiddleCenter);
            Stretch(block.label.rectTransform);
            return block;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(circleSprite);
            UnityEngine.Object.Destroy(circleTexture);
        }

        private static Texture2D MakeCircle()
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var pixels = new Color32[256 * 256];
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                float distance = new Vector2(x - 127.5f, y - 127.5f).magnitude;
                var color = distance < 113 ? new Color32(247, 248, 249, 255) : new Color32(0, 0, 0, 255);
                color.a = (byte)(Mathf.Clamp01(127.5f - distance) * 255);
                pixels[y * 256 + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            Place(rect, x, y, width, height);
            return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private Text Label(string name, Transform parent, string value, int size, Color color, float x, float y, float width, float height, Font font = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font != null ? font : regular;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }
        private void Button(string name, Transform parent, string caption, float x, float y, float width, Action action)
        {
            var rect = Box(name, parent, x, y, width, 35, new Color(0, 0, 0, 0));
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(() => action());
            Label("Label", rect, caption, 18, White, 0, 0, width, 35, regular, TextAnchor.MiddleCenter);
        }
    }
}
