using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BopBeat.Gameplay;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BopBeat.Tests
{
    public sealed class ScoreScreenTests
    {
        private SongDefinition level;
        private AudioClip music;
        private TextAsset chart;
        private Keyboard keyboard;
        private InputSettings.EditorInputBehaviorInPlayMode previousInput;
        private InputSettings.BackgroundBehavior previousBackground;
        private bool previousRunInBackground, configured;
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>();
            if (flow != null) flow.ShowTitle();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (configured)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInput;
                InputSystem.settings.backgroundBehavior = previousBackground;
                Application.runInBackground = previousRunInBackground;
            }
            if (level != null) UnityEngine.Object.Destroy(level);
            if (music != null) UnityEngine.Object.Destroy(music);
            if (chart != null) UnityEngine.Object.Destroy(chart);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SongCompletionShowsAccurateResultsAndReturnWorksWithoutReusingOldScores()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            previousInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousRunInBackground = Application.runInBackground;
            configured = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>();
            flow.ShowSongSelect();
            Assert.AreEqual("Cruel Angel Thesis", flow.SelectedSong.SongTitle);
            level = UnityEngine.Object.Instantiate(flow.SelectedSong);
            music = AudioClip.Create("Short Results Test", 33075, 1, 44100, false);
            chart = new TextAsset("{\"notes\":[{\"time\":0,\"lane\":0,\"type\":\"tap\"},{\"time\":0.25,\"lane\":1,\"type\":\"tap\"},{\"time\":0.4,\"lane\":2,\"type\":\"tap\"},{\"time\":0.55,\"lane\":3,\"type\":\"tap\"}]}");
            Set(level, "music", music);
            Set(level, "beatmap", chart);
            Set(level, "tempoChanges", Array.Empty<SongDefinition.TempoChange>());
            Set(flow, "sampleSong", level);
            flow.ShowSongSelect();
            flow.StartSelectedSong();
            var runner = UnityEngine.Object.FindAnyObjectByType<CountdownPrototype>();
            yield return Until(() => runner.IsRunning);
            runner.Session.Press(0, 0); runner.Session.Release(0, 0);
            runner.Session.Press(1, 0.4); runner.Session.Release(1, 0.4);
            runner.Session.Press(2, 0.7); runner.Session.Release(2, 0.7);
            yield return Until(() => flow.CurrentScreen == MenuFlow.Screen.Results);
            Assert.IsFalse(runner.IsRunning);
            Assert.IsFalse(runner.MusicSource.isPlaying);
            var root = GameObject.Find("Score Screen");
            var layout = root.transform.Find("Score Layout");
            Assert.AreEqual("1640", layout.Find("Final Score").GetComponent<Text>().text);
            Assert.AreEqual("1", layout.Find("Max Combo Value").GetComponent<Text>().text);
            foreach (string grade in new[] { "perfect", "good", "ok", "miss" })
                Assert.AreEqual("1", layout.Find(grade + " Count").GetComponent<Text>().text);
            StringAssert.Contains("Sportypo", layout.Find("Heading").GetComponent<Text>().font.name);
            var texture = root.transform.Find("Pixelated Background").GetComponent<RawImage>().texture;
            Assert.AreEqual("pixelated-image", texture.name);
            Assert.AreEqual(FilterMode.Point, texture.filterMode);
            var button = layout.Find("Song Select Button").GetComponent<Button>();
            Assert.AreSame(button.gameObject, EventSystem.current.currentSelectedGameObject);
            foreach (var arrow in new[] { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow })
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(arrow));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.AreSame(button.gameObject, EventSystem.current.currentSelectedGameObject, "Results arrows must keep the return button focused.");
            }
            yield return Capture(root);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen);
            Assert.IsFalse(root.activeSelf);
            flow.StartSelectedSong();
            yield return Until(() => runner.IsRunning);
            runner.Session.Advance(2);
            flow.ShowResults(runner.Session);
            Assert.AreEqual("0", layout.Find("Final Score").GetComponent<Text>().text);
            Assert.AreEqual("4", layout.Find("miss Count").GetComponent<Text>().text);
            Assert.AreSame(root, GameObject.Find("Score Screen"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen, "Space on results must return to song select without launching it.");
            flow.StartSelectedSong();
            runner.Session.Advance(2);
            flow.ShowResults(runner.Session);
            button.onClick.Invoke();
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen);
            flow.StartSelectedSong();
            flow.ShowSongSelect();
            Assert.IsFalse(root.activeSelf, "Aborting a song must not show results.");
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(condition(), "Timed out waiting for the song/results flow.");
        }

        private static IEnumerator Capture(GameObject root)
        {
            string path = Environment.GetEnvironmentVariable("BOP_BEAT_RESULTS_PREVIEW");
            if (string.IsNullOrEmpty(path)) yield break;
            var camera = Camera.main;
            var canvas = root.GetComponentInParent<Canvas>();
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldTarget = camera.targetTexture;
            float oldPlane = canvas.planeDistance;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                camera.targetTexture = oldTarget; RenderTexture.active = null;
                if (image != null) UnityEngine.Object.Destroy(image);
                target.Release(); UnityEngine.Object.Destroy(target);
            }
        }
    }
}
