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
    /// <summary>Explicit title-only replacement; song selection and gameplay objects remain intact.</summary>
    public static class TitleMenuSetup
    {
        [MenuItem("Tools/Bop Beat/Apply Reference Title Menu")]
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
            var serialized = new SerializedObject(flow);
            var title = (GameObject)serialized.FindProperty("titleScreen").objectReferenceValue;
            var logoMask = RequireSprite("Assets/Art/menu/BoopitmaniaWhiteMask.png");
            var startArt = RequireSprite("Assets/Art/ClipArt/Menu_Sportypo/Start.png");
            var settingsArt = RequireSprite("Assets/Art/ClipArt/Menu_Sportypo/Settings.png");
            var quitArt = RequireSprite("Assets/Art/ClipArt/Menu_Sportypo/Quit.png");
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/menu/pixelated-image.jpg");
            if (background == null) throw new InvalidOperationException("Menu background image is missing.");

            Undo.RegisterFullObjectHierarchyUndo(title, "Apply Reference Title Menu");
            for (int i = title.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(title.transform.GetChild(i).gameObject);
            var oldController = title.GetComponent<TitleMenuController>();
            if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);
            title.GetComponent<Image>().color = new Color32(3, 19, 27, 255);

            var bg = CenterRect("Rotating Background", title.transform, 0, 0, 3200, 1600);
            var rawImage = bg.gameObject.AddComponent<RawImage>();
            rawImage.texture = background;
            rawImage.raycastTarget = false;

            var layout = CenterRect("Title Layout (1280 x 720)", title.transform, 0, 0, 1280, 720);
            SpriteImage("Logo Purple Offset Copy", layout, logoMask, new Color32(167, 0, 204, 255), 605, 144, 942, 144);
            SpriteImage("Boopitmania Logo", layout, logoMask, new Color32(214, 80, 255, 255), 613, 138, 942, 144);
            var start = Item("Start Button", layout, startArt, 0, 300, 83);
            var settings = Item("Settings Button", layout, settingsArt, 1, 490, 83);
            var quit = Item("Quit Button", layout, quitArt, 2, 231, 83);
            UnityEventTools.AddPersistentListener(start.onClick, flow.ShowSongSelect);
            UnityEventTools.AddPersistentListener(settings.onClick, flow.ShowSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, flow.QuitGame);
            Navigate(start, quit, settings);
            Navigate(settings, start, quit);
            Navigate(quit, settings, start);
            var controller = title.AddComponent<TitleMenuController>();
            var controllerData = new SerializedObject(controller);
            Assign(controllerData, "rotatingBackground", bg);
            Assign(controllerData, "startButton", start);
            Assign(controllerData, "settingsButton", settings);
            Assign(controllerData, "quitButton", quit);
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            Assign(serialized, "playButton", start);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            flow.ShowTitle();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = title;
            Debug.Log("Reference title menu saved: rotating background, Start/Settings/Quit, keyboard and mouse.");
        }

        private static Sprite RequireSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Missing menu sprite: " + path);
            return sprite;
        }
        private static void Assign(SerializedObject data, string property, UnityEngine.Object value)
            => data.FindProperty(property).objectReferenceValue = value;

        private static RectTransform CenterRect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private static RectTransform SpriteImage(string name, Transform parent, Sprite sprite, Color color, float centerX, float centerY, float width, float height)
        {
            var rect = CenterRect(name, parent, centerX - 640, 360 - centerY, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }
        private static void OffsetCopy(GameObject obj)
        {
            // Shadow renders one identical glyph copy beneath the original, with a flat offset.
            var copy = obj.AddComponent<Shadow>();
            copy.effectColor = Color.white;
            copy.effectDistance = new Vector2(-5, -5);
            copy.useGraphicAlpha = true;
        }
        private static Button Item(string name, Transform parent, Sprite art, int index, float width, float height)
        {
            const float left = 155;
            float centerY = 259 + index * 96;
            var root = CenterRect(name, parent, left + width / 2 - 640, 360 - centerY, width + 24, height + 12);
            var image = root.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var visual = CenterRect("Black Text + White Backdrop", root, 0, 0, width, height);
            var textImage = visual.gameObject.AddComponent<Image>();
            textImage.sprite = art;
            textImage.color = Color.black;
            textImage.preserveAspect = true;
            textImage.raycastTarget = false;
            OffsetCopy(visual.gameObject);
            var highlight = new SerializedObject(root.gameObject.AddComponent<MenuItemHighlight>());
            Assign(highlight, "visual", visual);
            highlight.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }
        private static void Navigate(Button from, Button up, Button down)
        {
            var navigation = from.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = up;
            navigation.selectOnRight = down;
            from.navigation = navigation;
        }
    }
}
