using System;
using BopBeat.Menus;
using BopBeat.Gameplay;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BopBeat.Editor
{
    /// <summary>One-time setup. The resulting UI is saved in the scene and is editable in Unity.</summary>
    public static class MenuSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SongPath = "Assets/Data/Songs/SampleSong.asset";
        private static readonly Color Background = new Color32(12, 16, 29, 255);
        private static readonly Color Panel = new Color32(22, 29, 47, 255);
        private static readonly Color Cyan = new Color32(72, 230, 224, 255);
        private static readonly Color Pink = new Color32(255, 92, 155, 255);
        private static readonly Color White = new Color32(239, 244, 255, 255);
        private static readonly Color Muted = new Color32(161, 175, 199, 255);

        [MenuItem("Tools/Bop Beat/Create Menu UI")]
        public static void CreateMenuUI()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Never overwrite an existing menu or the user's later customizations.
            var existing = UnityEngine.Object.FindAnyObjectByType<MenuFlow>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("Menu UI already exists. Edit its saved scene objects in the Hierarchy.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Songs")) AssetDatabase.CreateFolder("Assets/Data", "Songs");
            var song = AssetDatabase.LoadAssetAtPath<SongDefinition>(SongPath);
            if (song == null)
            {
                song = ScriptableObject.CreateInstance<SongDefinition>();
                AssetDatabase.CreateAsset(song, SongPath);
            }

            var root = new GameObject("Menu Flow");
            var flow = root.AddComponent<MenuFlow>();
            var canvasObject = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(root.transform, false);
            }

            var title = FullPanel("Title Screen", canvasObject.transform, Background);
            var songs = FullPanel("Song Select Screen", canvasObject.transform, Background);
            var blank = FullPanel("Gameplay Prototype Screen", canvasObject.transform, Color.black);
            var gameplay = new SerializedObject(blank.AddComponent<CountdownPrototype>());
            Assign(gameplay, "scoreFont", AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf"));
            Assign(gameplay, "perfectArt", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Judgments_Graffonti/Perfect.png"));
            Assign(gameplay, "goodArt", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Judgments_Graffonti/Good.png"));
            Assign(gameplay, "okArt", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Judgments_Graffonti/Ok.png"));
            Assign(gameplay, "defaultBackground", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/menu/pixelated-image.jpg"));
            Assign(gameplay, "boopitArt", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Logos_Hypik/BoopitBlack.png"));
            gameplay.ApplyModifiedPropertiesWithoutUndo();
            var titleContent = Content(title.transform);
            var songContent = Content(songs.transform);

            Header(titleContent, "HACKATHON / RHYTHM PROJECT", "01 / TITLE");
            Label("Project Label", titleContent, "BOP BEAT", 26, Cyan, 80, 175, 600, 45, FontStyle.Bold);
            Label("Title", titleContent, "FIND YOUR\nRHYTHM.", 76, White, 75, 235, 680, 195, FontStyle.Bold);
            Label("Subtitle", titleContent, "A Bop It rhythm experiment.", 23, Muted, 80, 442, 630, 42);
            var play = ActionButton("Play Button", titleContent, "PLAY", 80, 525, 280, 66, Cyan, Background);
            UnityEventTools.AddPersistentListener(play.onClick, flow.ShowSongSelect);
            Label("Title Controls", titleContent, "CLICK PLAY OR PRESS ENTER", 15, Muted, 80, 613, 660, 30);
            DecorativeLanes(titleContent);
            Footer(titleContent, "TIMING PROTOTYPE", "KEYBOARD + MOUSE / GAMEPAD MENUS");

            Header(songContent, "BOP BEAT / SONG SELECT", "02 / SELECT");
            Label("Heading", songContent, "CHOOSE YOUR TRACK", 48, White, 80, 140, 1120, 70, FontStyle.Bold);
            Label("Subheading", songContent, "One sample track. Ready for your first test.", 21, Muted, 80, 215, 1120, 45);
            var card = Box("Sample Song Card", songContent, 80, 295, 1120, 240, Panel);
            Box("Selection Stripe", card, 0, 0, 5, 240, Cyan);
            var cover = Box("Placeholder Cover", card, 30, 30, 180, 180, Background);
            Label("Cover Icon", cover, "B / B", 44, Cyan, 12, 40, 156, 70, FontStyle.Bold, TextAnchor.MiddleCenter);
            Label("Cover Caption", cover, "SAMPLE", 14, Muted, 12, 127, 156, 25, FontStyle.Normal, TextAnchor.MiddleCenter);
            var coverImage = Box("Song Cover Art", card, 30, 30, 180, 180, Color.white).GetComponent<Image>();
            coverImage.preserveAspect = true;
            coverImage.gameObject.SetActive(false);
            Label("Selected Badge", card, "SELECTED / 01", 15, Cyan, 250, 24, 800, 30, FontStyle.Bold);
            var songTitle = Label("Song Title", card, song.SongTitle, 38, White, 250, 64, 810, 54, FontStyle.Bold);
            var artist = Label("Artist", card, song.Artist, 21, Muted, 250, 120, 810, 35);
            var details = Label("Track Details", card, $"{song.Bpm} BPM  /  {song.Difficulty}", 18, Cyan, 250, 171, 810, 33);
            Label("Prototype Notice", songContent, "Falling-note demo / D, F, Space, J, K / Perfect exact / Good 0.15 s / OK 0.4 s", 17, Muted, 80, 550, 1120, 35);
            var back = ActionButton("Back Button", songContent, "BACK", 80, 610, 190, 60, Panel, White);
            var start = ActionButton("Start Song Button", songContent, "START SONG", 880, 610, 320, 60, Cyan, Background);
            UnityEventTools.AddPersistentListener(back.onClick, flow.ShowTitle);
            UnityEventTools.AddPersistentListener(start.onClick, flow.StartSelectedSong);
            Label("Select Controls", songContent, "ARROWS: MOVE  /  ENTER: CONFIRM  /  ESC: BACK", 14, Muted, 295, 624, 560, 32);

            // Explicit navigation keeps focus on the two real actions.
            LinkNavigation(back, start);
            LinkNavigation(start, back);
            var playNavigation = play.navigation;
            playNavigation.mode = Navigation.Mode.None;
            play.navigation = playNavigation;

            var serialized = new SerializedObject(flow);
            Assign(serialized, "sampleSong", song);
            Assign(serialized, "titleScreen", title);
            Assign(serialized, "songSelectScreen", songs);
            Assign(serialized, "gameplayScreen", blank);
            Assign(serialized, "playButton", play);
            Assign(serialized, "startSongButton", start);
            Assign(serialized, "songTitle", songTitle);
            Assign(serialized, "songArtist", artist);
            Assign(serialized, "songDetails", details);
            Assign(serialized, "songCover", coverImage);
            Assign(serialized, "placeholderCover", cover.gameObject);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Check actual view transitions and restore the saved entry state.
            // Persistent UI callbacks normally run only in Play Mode. Enable them for
            // this setup check, then restore their normal runtime-only setting.
            play.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            start.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            back.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            flow.ShowTitle();
            Require(title.activeSelf && !songs.activeSelf && !blank.activeSelf, "Title visibility");
            play.onClick.Invoke();
            Require(songs.activeSelf && flow.SelectedSong == song, "Play opens song select");
            if (song.IsPlayable)
            {
                start.onClick.Invoke();
                Require(blank.activeSelf && !title.activeSelf && !songs.activeSelf, "Start opens prototype screen");
                Require(blank.GetComponent<CountdownPrototype>() != null, "Gameplay has a prototype runner");
            }
            flow.ShowSongSelect();
            back.onClick.Invoke();
            Require(title.activeSelf && flow.SelectedSong == null, "Back returns to title");
            flow.ShowTitle();

            play.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
            start.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
            back.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("BOP BEAT: Menu scene saved. Title, song select, prototype screen and back transitions verified.");
        }

        private static void Require(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Menu validation failed: " + description);
        }

        private static void Assign(SerializedObject target, string property, UnityEngine.Object value)
        {
            target.FindProperty(property).objectReferenceValue = value;
        }

        private static void LinkNavigation(Button from, Button to)
        {
            var navigation = from.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = navigation.selectOnDown = navigation.selectOnLeft = navigation.selectOnRight = to;
            from.navigation = navigation;
        }

        private static GameObject FullPanel(string name, Transform parent, Color color)
        {
            var rect = Box(name, parent, 0, 0, 0, 0, color);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect.gameObject;
        }

        private static RectTransform Content(Transform parent)
        {
            var rect = Rect("Layout (1280 x 720)", parent, 0, 0, 1280, 720);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static Text Label(string name, Transform parent, string value, int size, Color color,
            float x, float y, float width, float height, FontStyle style = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Rect(name, parent, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button ActionButton(string name, Transform parent, string caption,
            float x, float y, float width, float height, Color fill, Color ink)
        {
            var rect = Box(name, parent, x, y, width, height, fill);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 0.88f, 1);
            colors.selectedColor = new Color(0.78f, 0.88f, 1);
            colors.pressedColor = new Color(0.58f, 0.7f, 0.85f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f);
            button.colors = colors;
            Label("Label", rect, caption, 23, ink, 0, 0, width, height, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }

        private static void Header(Transform parent, string left, string right)
        {
            Label("Header", parent, left, 16, Muted, 80, 38, 820, 35, FontStyle.Bold);
            Label("Step", parent, right, 15, Cyan, 960, 38, 240, 35, FontStyle.Normal, TextAnchor.MiddleRight);
            Box("Header Divider", parent, 80, 91, 1120, 2, Panel);
        }

        private static void Footer(Transform parent, string left, string right)
        {
            Label("Footer", parent, left, 13, Muted, 80, 676, 520, 25);
            Label("Footer Right", parent, right, 13, Muted, 680, 676, 520, 25, FontStyle.Normal, TextAnchor.MiddleRight);
        }

        private static void DecorativeLanes(Transform parent)
        {
            var art = Box("Placeholder Lane Artwork (static)", parent, 835, 162, 365, 450, Panel);
            Label("Artwork Caption", art, "BOP / TWIST / PULL", 14, Muted, 20, 20, 325, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            for (int lane = 0; lane < 3; lane++)
            {
                Box("Lane " + (lane + 1), art, 28 + lane * 107, 70, 95, 304, Background);
                Box("Static Note " + (lane + 1), art, 36 + lane * 107, 116 + lane * 66, 79, 18, lane == 1 ? Pink : Cyan);
                Box("Key " + (lane + 1), art, 28 + lane * 107, 397, 95, 30, lane == 1 ? Pink : Cyan);
            }
            Box("Artwork Hit Line", art, 20, 369, 325, 3, White);
        }
    }
}
