using System;
using System.Collections;
using System.IO;
using BopBeat.Menus;
using BopBeat.Gameplay;
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
    public sealed class TitleMenuTests
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.EditorInputBehaviorInPlayMode previousInputBehavior;
        private InputSettings.BackgroundBehavior previousBackground;
        private bool previousRunInBackground, configured;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (configured)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
                InputSystem.settings.backgroundBehavior = previousBackground;
                Application.runInBackground = previousRunInBackground;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArrowsSpaceMouseHoverAndRotationWorkWithoutChangingSongSelection()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;
            previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousRunInBackground = Application.runInBackground;
            configured = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>();
            var menu = UnityEngine.Object.FindAnyObjectByType<TitleMenuController>();
            Assert.IsNotNull(menu);
            var backgroundTexture = menu.RotatingBackground.GetComponent<RawImage>().texture;
            Assert.AreEqual("pixelated-image", backgroundTexture.name);
            Assert.AreEqual(FilterMode.Point, backgroundTexture.filterMode);
            Assert.AreEqual(menu.StartButton.gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.AreEqual("QuitGame", menu.QuitButton.onClick.GetPersistentMethodName(0));
            Assert.AreEqual(flow, menu.QuitButton.onClick.GetPersistentTarget(0));
            int settingsCalls = 0, startCalls = 0;
            menu.SettingsButton.onClick.AddListener(() => settingsCalls++);
            menu.StartButton.onClick.AddListener(() => startCalls++);
            var visual = menu.SettingsButton.GetComponent<MenuItemHighlight>().Visual;
            Assert.AreEqual(Color.black, visual.GetComponent<Image>().color);
            Assert.AreEqual(Color.white, visual.GetComponent<Shadow>().effectColor);

            yield return KeyPress(Key.DownArrow);
            Assert.AreEqual(menu.SettingsButton.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.Space);
            Assert.AreEqual(MenuFlow.Screen.Title, flow.CurrentScreen, "Settings is intentionally a no-op.");
            Assert.AreEqual(1, settingsCalls, "Space must submit exactly once.");
            yield return KeyPress(Key.Enter);
            Assert.AreEqual(2, settingsCalls, "Enter must submit exactly once.");
            yield return KeyPress(Key.DownArrow);
            Assert.AreEqual(menu.QuitButton.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.DownArrow);
            Assert.AreEqual(menu.StartButton.gameObject, EventSystem.current.currentSelectedGameObject, "Down wraps to Start.");
            yield return KeyPress(Key.UpArrow);
            Assert.AreEqual(menu.QuitButton.gameObject, EventSystem.current.currentSelectedGameObject, "Up wraps to Quit.");
            yield return KeyPress(Key.LeftArrow);
            Assert.AreEqual(menu.SettingsButton.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.RightArrow);
            Assert.AreEqual(menu.QuitButton.gameObject, EventSystem.current.currentSelectedGameObject);

            var point = RectTransformUtility.WorldToScreenPoint(null, visual.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(menu.SettingsButton.gameObject, EventSystem.current.currentSelectedGameObject);
            Assert.Greater(visual.localScale.x, 1.04f, "Mouse hover grows the item.");
            float oldAngle = menu.RotatingBackground.localEulerAngles.z;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.Less(Mathf.DeltaAngle(oldAngle, menu.RotatingBackground.localEulerAngles.z), 0, "Background rotates clockwise.");
            float diagonal = ((RectTransform)menu.transform).rect.size.magnitude;
            Assert.GreaterOrEqual(Mathf.Min(menu.RotatingBackground.rect.width, menu.RotatingBackground.rect.height), diagonal);
            yield return CaptureMenuPreview(menu.gameObject, "BOP_BEAT_TITLE_PREVIEW");
            // Capturing temporarily changes Canvas/camera sizing; restore layout before pointer coordinates.
            yield return null;
            Canvas.ForceUpdateCanvases();

            point = RectTransformUtility.WorldToScreenPoint(null, menu.StartButton.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            Assert.AreEqual(menu.StartButton.gameObject, EventSystem.current.currentSelectedGameObject, "Mouse hover focuses Start before clicking.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen, "Mouse click starts song selection.");
            Assert.AreEqual(1, startCalls);
            flow.ShowTitle();
            yield return null;
            yield return KeyPress(Key.Space);
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen, "Space opens only song selection, not gameplay.");
            Assert.AreEqual(2, startCalls);
            var songRow = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
            Assert.AreEqual("Sample Song Card", songRow.name);
            var songTitle = songRow.transform.Find("Song Title").GetComponent<SpriteLettering>();
            StringAssert.Contains("Sportypo", songTitle.SourceFont.name);
            Assert.AreEqual(flow.SelectedSong.SongTitle, songTitle.Text);
            Assert.AreEqual(Image.Type.Sliced, songRow.GetComponent<Image>().type);
            var songLayout = songRow.transform.parent.GetComponent<RectTransform>();
            Assert.IsNotNull(songLayout.GetComponent<SongSelectLayout>());
            Assert.AreEqual(new Vector2(0.5f, 1), songLayout.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 1), songLayout.pivot);
            yield return CaptureMenuPreview(songRow.gameObject, "BOP_BEAT_SONG_SELECT_PREVIEW");
            yield return CaptureMenuPreview(songRow.gameObject, "BOP_BEAT_SONG_SELECT_TALL_PREVIEW", 1024, 768);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var back = songRow.transform.parent.Find("Back Button").GetComponent<Button>();
            yield return KeyPress(Key.UpArrow);
            Assert.AreEqual(back.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.Enter);
            Assert.AreEqual(MenuFlow.Screen.Title, flow.CurrentScreen, "Enter on Back returns to the title.");
            yield return KeyPress(Key.Enter);
            Assert.AreEqual(MenuFlow.Screen.SongSelect, flow.CurrentScreen, "Enter on Start opens only song select.");
            Assert.AreEqual(3, startCalls);
            yield return KeyPress(Key.RightArrow);
            Assert.AreEqual(back.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.Space);
            Assert.AreEqual(MenuFlow.Screen.Title, flow.CurrentScreen, "Space on Back returns to title.");
            flow.ShowSongSelect();
            yield return KeyPress(Key.LeftArrow);
            Assert.AreEqual(back.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.DownArrow);
            Assert.AreEqual(songRow.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return KeyPress(Key.Space);
            Assert.AreEqual(MenuFlow.Screen.Gameplay, flow.CurrentScreen, "Space on the song row launches gameplay.");
            Assert.IsFalse(EventSystem.current.sendNavigationEvents);
            var runner = UnityEngine.Object.FindAnyObjectByType<CountdownPrototype>();
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (!runner.IsRunning && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            Assert.IsTrue(runner.IsRunning);
            Assert.AreEqual(0, runner.Session.CompletedCount, "Launching with Space must not consume a note.");
            yield return new WaitForSecondsRealtime(0.2f);
            var session = runner.Session;
            var replay = runner.transform.Find("Falling Note Layout (1280 x 720)/Replay").GetComponent<Button>();
            EventSystem.current.SetSelectedGameObject(replay.gameObject);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            Assert.IsTrue(session.IsKeyHeld(2), "Gameplay Space must reach its lane.");
            Assert.AreSame(session, runner.Session, "Gameplay Space must not submit a focused Replay button.");
            Assert.AreEqual(MenuFlow.Screen.Gameplay, flow.CurrentScreen);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            flow.ShowSongSelect();
            Assert.IsTrue(EventSystem.current.sendNavigationEvents);
            yield return KeyPress(Key.Enter);
            Assert.AreEqual(MenuFlow.Screen.Gameplay, flow.CurrentScreen, "Enter on the song row launches gameplay.");
            flow.ShowSongSelect();
            EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            Assert.AreSame(songRow.gameObject, EventSystem.current.currentSelectedGameObject, "Lost menu focus must recover.");
            flow.ShowSongSelect();
            yield return null;
            point = RectTransformUtility.WorldToScreenPoint(null, songRow.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
            Assert.AreEqual(MenuFlow.Screen.Gameplay, flow.CurrentScreen, "Clicking the song row launches gameplay.");
            flow.ShowSongSelect();
            // Never invoke Quit in a test: it intentionally exits the player/editor play session.
        }

        private IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator CaptureMenuPreview(GameObject screen, string environmentVariable, int width = 1280, int height = 720)
        {
            string path = Environment.GetEnvironmentVariable(environmentVariable);
            if (string.IsNullOrEmpty(path)) yield break;
            var canvas = screen.GetComponentInParent<Canvas>();
            var camera = Camera.main;
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldPlane = canvas.planeDistance;
            var target = new RenderTexture(width, height, 24);
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
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldPlane;
                camera.targetTexture = oldTarget;
                RenderTexture.active = null;
                if (image != null) UnityEngine.Object.Destroy(image);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
