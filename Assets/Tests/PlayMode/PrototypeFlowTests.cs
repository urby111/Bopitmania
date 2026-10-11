using System;
using System.Collections;
using System.IO;
using BopBeat.Gameplay;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BopBeat.Tests
{
    public sealed class PrototypeFlowTests
    {
        private Keyboard keyboard;
        private InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private bool previousRunInBackground, configuredInput;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>();
            if (flow != null) flow.ShowTitle();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (configuredInput)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                Application.runInBackground = previousRunInBackground;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuStartsAudioAndKeyboardJudgesThenReplayResetsAndBackStopsAudio()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>();
            Assert.AreEqual(MenuFlow.Screen.Title, flow.CurrentScreen);
#if UNITY_EDITOR
            // Keep the original timing fixture independent of the song currently featured in the menu.
            typeof(MenuFlow).GetField("sampleSong", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(flow, UnityEditor.AssetDatabase.LoadAssetAtPath<SongDefinition>("Assets/Data/Songs/SampleSong.asset"));
#endif
            // Batch tests have no focused Game view. Route simulated keyboard events
            // into the player for this test, and restore the user's settings afterward.
            previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            previousRunInBackground = Application.runInBackground;
            configuredInput = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            flow.ShowSongSelect();
            flow.StartSelectedSong();
            var prototype = UnityEngine.Object.FindAnyObjectByType<CountdownPrototype>();
            Assert.AreEqual(MenuFlow.Screen.Gameplay, flow.CurrentScreen);
            Assert.IsNotNull(prototype);
            var savedTiming = (BopBeat.Beatmaps.TimingSettings)typeof(CountdownPrototype)
                .GetField("timing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(prototype);
            Assert.AreEqual(0, savedTiming.perfect);
            Assert.AreEqual(0.15, savedTiming.good);
            Assert.AreEqual(0.4, savedTiming.ok);
            yield return WaitUntil(() => prototype.IsRunning, 10);
            Assert.Less(prototype.SongTime, 0);
            yield return WaitUntil(() => prototype.SongTime >= 1.2, 8);
            Assert.Greater(prototype.VisibleBlockCount, 0);
            var layout = prototype.transform.Find("Falling Note Layout (1280 x 720)");
            Assert.IsNotNull(layout);
            var scoreLabel = layout.Find("Score Label").GetComponent<Text>();
            StringAssert.Contains("Sportypo", scoreLabel.font.name);
            StringAssert.Contains("Sportypo", layout.Find("Combo").GetComponent<Text>().font.name);
            var head = layout.Find("Five Lanes/Note Blocks/Note 0/Note Head").GetComponent<RectTransform>();
            Assert.AreEqual(NoteTravel.Position(1.5, prototype.SongTime, 1, 0, FallingNoteView.TargetY),
                -head.anchoredPosition.y + head.sizeDelta.y / 2, 20, "Block position should follow song time.");
            yield return WaitUntil(() => prototype.SongTime >= 1.55, 8);
            Assert.IsTrue(prototype.MusicSource.isPlaying);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D), Time.realtimeSinceStartupAsDouble);
            yield return null;
            Assert.AreEqual(1, prototype.Session.GoodCount, $"D state={keyboard.dKey.isPressed}; held={prototype.Session.IsKeyHeld(0)}; song={prototype.SongTime:0.000}; good={prototype.Session.GoodCount}; miss={prototype.Session.MissCount}");
            Assert.AreEqual(0, prototype.Session.PerfectCount);
            Assert.AreEqual(500, prototype.Session.Score);
            Assert.AreEqual(0, prototype.Session.Combo);
            Assert.AreEqual("500", layout.Find("Score Value").GetComponent<Text>().text);
            Assert.AreEqual("0", layout.Find("Combo Value").GetComponent<Text>().text);
            Assert.IsNotNull(layout.Find("Judgment Feedback/Judgment Art").GetComponent<Image>().sprite);
            Assert.IsFalse(layout.Find("Five Lanes/Note Blocks/Note 0").gameObject.activeSelf);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), Time.realtimeSinceStartupAsDouble);
            yield return null;
            yield return WaitUntil(() => prototype.SongTime >= 2.075, 3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F), Time.realtimeSinceStartupAsDouble);
            yield return null;
            Assert.AreEqual(2, prototype.Session.GoodCount);
            Assert.AreEqual(0, prototype.Session.Combo);
            Assert.AreEqual(1000, prototype.Session.Score);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), Time.realtimeSinceStartupAsDouble);
            yield return null;
            yield return WaitUntil(() => prototype.SongTime > 3.05, 3);
            Assert.GreaterOrEqual(prototype.Session.MissCount, 1);
            yield return WaitUntil(() => prototype.SongTime >= 3.8, 3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.K), Time.realtimeSinceStartupAsDouble);
            yield return null;
            Assert.AreEqual(1, prototype.Session.OkCount);
            Assert.AreEqual(1100, prototype.Session.Score);
            // Warm up screenshot rendering before the later, brief hold-start feedback.
            yield return CapturePreviewIfRequested();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), Time.realtimeSinceStartupAsDouble);
            yield return null;
            yield return WaitUntil(() => prototype.SongTime >= 6.5, 5);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F), Time.realtimeSinceStartupAsDouble);
            yield return null;
            Assert.IsTrue(prototype.Session.IsHolding(8));
            yield return CapturePreviewIfRequested();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), Time.realtimeSinceStartupAsDouble);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R), Time.realtimeSinceStartupAsDouble);
            yield return null;
            yield return WaitUntil(() => prototype.IsRunning, 10);
            Assert.AreEqual(0, prototype.Session.CompletedCount);
            Assert.AreEqual(0, prototype.Session.Score);
            Assert.AreEqual(0, prototype.Session.Combo);
            Assert.Less(prototype.SongTime, 0);
            Assert.AreEqual(3, prototype.transform.childCount, "Replay must reuse the UI.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), Time.realtimeSinceStartupAsDouble);
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape), Time.realtimeSinceStartupAsDouble);
            yield return null;
            Assert.IsFalse(prototype.IsRunning);
            Assert.IsFalse(prototype.MusicSource.isPlaying);
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, double timeout)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeout;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(condition(), "Timed out waiting for the playable song clock.");
        }

        private static IEnumerator CapturePreviewIfRequested()
        {
            string path = Environment.GetEnvironmentVariable("BOP_BEAT_PREVIEW");
            if (string.IsNullOrEmpty(path)) yield break;
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var camera = Camera.main;
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldPlane = canvas.planeDistance;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                foreach (var text in UnityEngine.Object.FindObjectsByType<Text>()) text.SetAllDirty();
                // Allow dynamic font atlases/meshes to settle at the preview resolution.
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldPlane;
                camera.targetTexture = oldTarget;
                RenderTexture.active = null;
                if (texture != null) UnityEngine.Object.Destroy(texture);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
