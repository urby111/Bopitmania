using System.Collections;
using System.Reflection;
using BopBeat.Beatmaps;
using BopBeat.Gameplay;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BopBeat.Tests
{
    public sealed class GameplayVisualTests
    {
        private GameObject root;
        private FallingNoteView view;
        private SongDefinition level;
        private Texture2D customTexture;
        private Sprite customSprite;

        private static T Field<T>(object instance, string name) => (T)instance.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            view?.Dispose();
            if (root != null) Object.Destroy(root);
            if (level != null) Object.Destroy(level);
            if (customSprite != null) Object.Destroy(customSprite);
            if (customTexture != null) Object.Destroy(customTexture);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RectangularHoldsCenteredFadeBeatPulseAndBackgroundOverridesWork()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var flow = Object.FindAnyObjectByType<MenuFlow>();
            flow.ShowSongSelect();
            level = Object.Instantiate(flow.SelectedSong);
            // This geometry/pulse fixture uses 120 BPM regardless of the menu's featured song.
            typeof(SongDefinition).GetField("bpm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(level, 120);
            typeof(SongDefinition).GetField("tempoChanges", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(level, System.Array.Empty<SongDefinition.TempoChange>());
            var runner = Object.FindAnyObjectByType<CountdownPrototype>(FindObjectsInactive.Include);
            var texture = Field<Texture2D>(runner, "defaultBackground");
            var logo = Field<Sprite>(runner, "boopitArt");
            Assert.IsNotNull(texture);
            Assert.AreEqual("pixelated-image", texture.name);
            Assert.AreEqual(FilterMode.Point, texture.filterMode);
            Assert.IsNotNull(logo);
            root = new GameObject("Visual Test Root", typeof(RectTransform));
            root.transform.SetParent(Object.FindAnyObjectByType<Canvas>().transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            view = new FallingNoteView(root.transform, Field<Font>(runner, "scoreFont"),
                new[] { Field<Sprite>(runner, "perfectArt"), Field<Sprite>(runner, "goodArt"), Field<Sprite>(runner, "okArt") },
                new[] { Key.D, Key.F, Key.Space, Key.J, Key.K }, texture, logo, () => { }, () => { });
            var session = new BeatmapSession(new BeatmapData { notes = new[] {
                new BeatmapNote { time = 1, endTime = 1.4, lane = 1, type = "hold" }
            } }, 3);
            view.Begin(session, level);
            var layout = root.transform.Find("Falling Note Layout (1280 x 720)");
            for (int lane = 0; lane < 5; lane++) Assert.IsNull(layout.Find("Key " + lane), "Lane keyboard labels were removed.");
            var pixelated = root.transform.Find("Level Background/Pixelated Background").GetComponent<RawImage>();
            Assert.AreSame(texture, pixelated.texture);
            Assert.IsTrue(pixelated.gameObject.activeSelf);
            Assert.AreSame(logo, layout.Find("Bop It Placeholder/Bop It Label").GetComponent<Image>().sprite);
            view.Render(0.9, false);
            var block = layout.Find("Five Lanes/Note Blocks/Note 0");
            var head = block.Find("Note Head").GetComponent<Image>();
            var body = block.Find("Hold Body").GetComponent<Image>();
            var tail = block.Find("Hold Tail").GetComponent<Image>();
            Assert.IsTrue(block.gameObject.activeSelf);
            Assert.AreEqual(head.rectTransform.sizeDelta.x, body.rectTransform.sizeDelta.x);
            Assert.AreEqual(head.rectTransform.anchoredPosition.x, body.rectTransform.anchoredPosition.x);
            Assert.AreEqual(head.color, body.color);
            Assert.AreEqual(head.color, tail.color);
            Assert.AreEqual(1, body.color.a);
            Assert.AreEqual(224.4, body.rectTransform.sizeDelta.y, 0.01, "Duration body must join both equal-width endpoints.");
            var badge = layout.Find("Bop It Placeholder");
            view.Render(0.5, false);
            Assert.AreEqual(1.08f, badge.localScale.x, 0.00001);
            view.Render(0.75, false);
            Assert.AreEqual(Vector3.one, badge.localScale);
            var feedback = layout.Find("Judgment Feedback").GetComponent<CanvasGroup>();
            Assert.AreEqual(new Vector2(415, -216), feedback.GetComponent<RectTransform>().anchoredPosition);
            Assert.IsFalse(feedback.blocksRaycasts);
            view.Feedback(new NoteFeedback(1, Judgment.Perfect, 0));
            Assert.AreEqual(1, feedback.alpha);
            yield return new WaitForSecondsRealtime(0.25f);
            view.Render(0.9, false);
            Assert.Greater(feedback.alpha, 0);
            Assert.Less(feedback.alpha, 0.9);
            view.Feedback(new NoteFeedback(1, Judgment.Good, 0.15));
            Assert.AreEqual(1, feedback.alpha, "New feedback restarts the fade.");
            yield return new WaitForSecondsRealtime(0.65f);
            view.Render(3, true);
            Assert.AreEqual(0, feedback.alpha);
            Assert.IsFalse(feedback.transform.Find("Judgment Art").gameObject.activeSelf);
            Assert.AreEqual(Vector3.one, badge.localScale);
            view.Feedback(new NoteFeedback(1, Judgment.Miss, 0.41));
            StringAssert.Contains("MISS", feedback.transform.Find("Judgment Detail").GetComponent<Text>().text);
            view.Begin(session, level);
            Assert.AreEqual(0, feedback.alpha, "Replay clears old feedback.");
            customTexture = new Texture2D(8, 8);
            customSprite = Sprite.Create(customTexture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f);
            typeof(SongDefinition).GetField("background", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(level, customSprite);
            view.Begin(session, level);
            Assert.IsFalse(pixelated.gameObject.activeSelf);
            Assert.AreSame(customSprite, root.transform.Find("Level Background").GetComponent<Image>().sprite);
        }
    }
}
