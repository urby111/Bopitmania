using System;
using BopBeat.Menus;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BopBeat.Editor
{
    /// <summary>Explicit song-select-only rebuild; title and gameplay objects remain intact.</summary>
    public static class SongSelectMenuSetup
    {
        [Serializable] private class GlyphManifest { public float fontSize, spaceAdvance; public GlyphRecord[] characters; }
        [Serializable] private class GlyphRecord { public string character, file; public float advance, offsetX, offsetY; }
        [MenuItem("Tools/Bop Beat/Apply Reference Song Select")]
        public static void ApplyReferenceMenu()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(scenePath);
            }
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>(FindObjectsInactive.Include);
            if (flow == null) throw new InvalidOperationException("Create the base menus first.");
            var data = new SerializedObject(flow);
            var screen = (GameObject)data.FindProperty("songSelectScreen").objectReferenceValue;
            var song = (SongDefinition)data.FindProperty("sampleSong").objectReferenceValue;
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/menu/pixelated-image.jpg");
            var backArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Menu_Sportypo/Back.png");
            var headingArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/SongSelect_Sportypo/SongSelect.png");
            var cardArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/SongSelect_Sportypo/RoundedSongCard.png");
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/sportypo/Sportypo-Reguler-Demo.ttf");
            if (background == null || backArt == null || headingArt == null || cardArt == null || font == null)
                throw new InvalidOperationException("Song-select art/font is missing.");

            Undo.RegisterFullObjectHierarchyUndo(screen, "Apply Reference Song Select");
            for (int i = screen.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(screen.transform.GetChild(i).gameObject);
            screen.GetComponent<Image>().color = Color.white;

            var bg = Rect("Pixelated Background", screen.transform, 0, 0, 1280, 720);
            var image = bg.gameObject.AddComponent<RawImage>();
            image.texture = background;
            image.raycastTarget = false;
            // Trim the source JPEG's thin white bottom edge without changing the original asset.
            image.uvRect = new Rect(0, 0.005f, 1, 0.99f);
            var aspect = bg.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectRatio = background.width * image.uvRect.width / (background.height * image.uvRect.height);
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            var layout = Rect("Song Select Layout (1280 x 720)", screen.transform, 0, 0, 1280, 720);
            layout.gameObject.AddComponent<SongSelectLayout>();
            var heading = At("Song Select Heading", layout, 310, 26, 660, 82);
            SpriteImage(heading, headingArt);
            var backRect = At("Back Button", layout, 48, 28, 220, 74);
            var backImage = SpriteImage(backRect, backArt);
            backImage.raycastTarget = true;
            var back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.transition = Selectable.Transition.None;

            var card = At("Sample Song Card", layout, 220, 132, 800, 184);
            var cardImage = SpriteImage(card, cardArt);
            cardImage.type = Image.Type.Sliced;
            cardImage.preserveAspect = false;
            cardImage.raycastTarget = true;
            var start = card.gameObject.AddComponent<Button>();
            start.targetGraphic = cardImage;
            start.transition = Selectable.Transition.ColorTint;
            var colors = start.colors;
            colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 1, 1);
            colors.pressedColor = new Color(0.72f, 0.87f, 0.9f);
            start.colors = colors;

            var titleRect = Rect("Song Title", card, 0, 0, 712, 104);
            var title = titleRect.gameObject.AddComponent<SpriteLettering>();
            var manifest = JsonUtility.FromJson<GlyphManifest>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/ClipArt/Sportypo_Characters/manifest.json").text);
            var glyphs = new SpriteLettering.Glyph[manifest.characters.Length];
            for (int i = 0; i < glyphs.Length; i++)
            {
                var record = manifest.characters[i];
                glyphs[i] = new SpriteLettering.Glyph
                {
                    character = record.character,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Sportypo_Characters/" + record.file),
                    advance = record.advance,
                    offset = new Vector2(record.offsetX, record.offsetY)
                };
            }
            title.Configure(font, glyphs, manifest.fontSize, manifest.spaceAdvance);
            title.SetText(song != null ? song.SongTitle : "No song assigned");
            UnityEventTools.AddPersistentListener(back.onClick, flow.ShowTitle);
            UnityEventTools.AddPersistentListener(start.onClick, flow.StartSelectedSong);
            Navigate(back, start);
            Navigate(start, back);
            data.FindProperty("startSongButton").objectReferenceValue = start;
            data.FindProperty("songTitleGlyphs").objectReferenceValue = title;
            data.FindProperty("songTitle").objectReferenceValue = null;
            foreach (var field in new[] { "songArtist", "songDetails", "songCover", "placeholderCover" })
                data.FindProperty(field).objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            flow.ShowTitle();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = screen;
            Debug.Log("Reference song select saved: top-aligned responsive layout, pixelated background, Sportypo heading/back/title, clickable song row.");
        }

        // A file-only scene update does not replace the Hierarchy already loaded in Unity.
        // Refresh an older open song-select once after this code imports, preserving the
        // current title/gameplay. The new layout component marks an already upgraded screen.
        [InitializeOnLoadMethod]
        private static void RefreshOlderOpenMenu()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += RepairOlderOpenMenu;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += RepairOlderOpenMenu;
            };
        }

        private static void RepairOlderOpenMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity") return;
            var flow = UnityEngine.Object.FindAnyObjectByType<MenuFlow>(FindObjectsInactive.Include);
            if (flow == null) return;
            var data = new SerializedObject(flow);
            var screen = (GameObject)data.FindProperty("songSelectScreen").objectReferenceValue;
            if (screen == null || screen.GetComponentInChildren<SongSelectLayout>(true) != null) return;
            // The older loaded scene may also predate the two gameplay art fields added
            // by the previous request. Keep the saved, intended defaults when saving
            // this upgraded screen; preserve any existing custom references.
            var game = (GameObject)data.FindProperty("gameplayScreen").objectReferenceValue;
            var runner = game != null ? game.GetComponent<BopBeat.Gameplay.CountdownPrototype>() : null;
            if (runner != null)
            {
                var gameplay = new SerializedObject(runner);
                var background = gameplay.FindProperty("defaultBackground");
                var logo = gameplay.FindProperty("boopitArt");
                if (background.objectReferenceValue == null)
                    background.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/menu/pixelated-image.jpg");
                if (logo.objectReferenceValue == null)
                    logo.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ClipArt/Logos_Hypik/BoopitBlack.png");
                gameplay.ApplyModifiedPropertiesWithoutUndo();
            }
            ApplyReferenceMenu();
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private static RectTransform At(string name, Transform parent, float left, float top, float width, float height)
            => Rect(name, parent, left + width / 2 - 640, 360 - top - height / 2, width, height);
        private static Image SpriteImage(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
        private static void Navigate(Button from, Button other)
        {
            var navigation = from.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = navigation.selectOnDown = navigation.selectOnLeft = navigation.selectOnRight = other;
            from.navigation = navigation;
        }
    }
}
