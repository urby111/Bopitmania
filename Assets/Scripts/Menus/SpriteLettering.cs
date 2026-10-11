using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BopBeat.Menus
{
    /// <summary>One-line, size-to-fit lettering made from the exported font glyph sprites.</summary>
    public sealed class SpriteLettering : MonoBehaviour
    {
        [Serializable]
        public sealed class Glyph
        {
            public string character;
            public Sprite sprite;
            public float advance;
            public Vector2 offset;
        }
        [SerializeField] private Font sourceFont;
        [SerializeField] private Glyph[] glyphs = Array.Empty<Glyph>();
        [SerializeField] private float designSize = 256, fontSize = 64, spaceAdvance = 64;
        [SerializeField] private string text = "";
        private readonly Dictionary<char, Glyph> lookup = new Dictionary<char, Glyph>();
        private readonly List<Image> letters = new List<Image>();
        private string drawnText;
        private Vector2 drawnSize;
        private Text fallback;
        public string Text => text;
        public Font SourceFont => sourceFont;

        public void Configure(Font font, Glyph[] characters, float sourceSize, float space)
        {
            sourceFont = font;
            glyphs = characters;
            designSize = sourceSize;
            spaceAdvance = space;
            lookup.Clear();
            drawnText = null;
        }
        public void SetText(string value)
        {
            text = value ?? "";
            Draw();
        }
        private void OnEnable() { drawnText = null; Draw(); }
        private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) Draw(); }

        private void Draw()
        {
            if (glyphs.Length == 0 || !(transform is RectTransform rect)) return;
            var size = rect.rect.size;
            if (drawnText == text && drawnSize == size) return;
            drawnText = text;
            drawnSize = size;
            if (lookup.Count == 0)
            {
                foreach (var glyph in glyphs)
                    if (!string.IsNullOrEmpty(glyph.character) && glyph.sprite != null)
                        lookup[glyph.character[0]] = glyph;
                foreach (Transform child in transform)
                {
                    var image = child.GetComponent<Image>();
                    if (image != null && !letters.Contains(image)) letters.Add(image);
                }
            }
            string value = text.ToLowerInvariant();
            bool supported = true;
            foreach (char c in value) if (!char.IsWhiteSpace(c) && !lookup.ContainsKey(c)) supported = false;
            if (!supported)
            {
                // Preserve punctuation/non-Latin titles through the source font rather than dropping characters.
                foreach (var image in letters) image.gameObject.SetActive(false);
                if (fallback == null)
                {
                    var obj = new GameObject("Font Fallback", typeof(RectTransform), typeof(Text));
                    obj.transform.SetParent(transform, false);
                    var frame = obj.GetComponent<RectTransform>();
                    frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.offsetMin = frame.offsetMax = Vector2.zero;
                    fallback = obj.GetComponent<Text>();
                    fallback.font = sourceFont;
                    fallback.color = Color.white;
                    fallback.raycastTarget = false;
                    fallback.alignment = TextAnchor.MiddleLeft;
                    fallback.resizeTextForBestFit = true;
                    fallback.resizeTextMinSize = 12;
                    fallback.resizeTextMaxSize = (int)fontSize;
                }
                fallback.gameObject.SetActive(true);
                fallback.text = text;
                return;
            }
            if (fallback != null) fallback.gameObject.SetActive(false);
            float cursor = 0, minX = 0, maxX = 0, minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c)) { cursor += spaceAdvance; continue; }
                var glyph = lookup[c];
                minX = Mathf.Min(minX, cursor + glyph.offset.x);
                maxX = Mathf.Max(maxX, cursor + glyph.offset.x + glyph.sprite.rect.width);
                minY = Mathf.Min(minY, glyph.offset.y);
                maxY = Mathf.Max(maxY, glyph.offset.y + glyph.sprite.rect.height);
                cursor += glyph.advance;
            }
            int count = 0;
            if (float.IsPositiveInfinity(minY))
            {
                foreach (var image in letters) image.gameObject.SetActive(false);
                return;
            }
            float scale = Mathf.Min(fontSize / designSize, size.x / Mathf.Max(1, maxX - minX), size.y / Mathf.Max(1, maxY - minY));
            float originX = -minX * scale;
            float originY = (size.y - (maxY - minY) * scale) / 2 - minY * scale;
            cursor = 0;
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c)) { cursor += spaceAdvance; continue; }
                var glyph = lookup[c];
                if (count == letters.Count)
                {
                    var obj = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
                    obj.transform.SetParent(transform, false);
                    letters.Add(obj.GetComponent<Image>());
                }
                var image = letters[count++];
                image.gameObject.SetActive(true);
                image.name = "Glyph " + count + " (" + c + ")";
                image.sprite = glyph.sprite;
                image.color = Color.white;
                image.raycastTarget = false;
                var frame = image.rectTransform;
                frame.anchorMin = frame.anchorMax = new Vector2(0, 1);
                frame.pivot = new Vector2(0, 1);
                frame.anchoredPosition = new Vector2(originX + (cursor + glyph.offset.x) * scale, -(originY + glyph.offset.y * scale));
                frame.sizeDelta = glyph.sprite.rect.size * scale;
                cursor += glyph.advance;
            }
            for (int i = count; i < letters.Count; i++) letters[i].gameObject.SetActive(false);
        }
    }
}
