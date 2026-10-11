using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Serialization;
using BopBeat.Gameplay;
using BopBeat.Beatmaps;

namespace BopBeat.Menus
{
    /// <summary>Controls the title, song select, and playable falling-note prototype.</summary>
    public sealed class MenuFlow : MonoBehaviour
    {
        public enum Screen { Title, SongSelect, Gameplay, Results }

        [Header("Song metadata")]
        [SerializeField] private SongDefinition sampleSong;
        [Header("Screen roots")]
        [SerializeField] private GameObject titleScreen;
        [SerializeField] private GameObject songSelectScreen;
        [FormerlySerializedAs("blankGameplayScreen")]
        [SerializeField] private GameObject gameplayScreen;
        [Header("Initial keyboard / gamepad focus")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button startSongButton;
        [Header("Song card")]
        [SerializeField] private Text songTitle;
        [SerializeField] private SpriteLettering songTitleGlyphs;
        [SerializeField] private Text songArtist;
        [SerializeField] private Text songDetails;
        [SerializeField] private Image songCover;
        [SerializeField] private GameObject placeholderCover;

        public Screen CurrentScreen { get; private set; }
        public SongDefinition SelectedSong { get; private set; }
        private ScoreScreenView results;
        private InputSystemUIInputModule uiModule;
        private InputActionAsset menuInput;
        private InputActionReference menuSubmit, previousSubmit;
        private EventSystem menuEvents;
        private bool previousNavigationEvents;

        private void Awake()
        {
            ConfigureMenuInput();
            CompleteArrowNavigation(titleScreen);
            RefreshSongCard();
            ShowTitle();
        }

        private void Start()
        {
            // EventSystem may enable after Awake; set focus again when all objects are ready.
            Focus(playButton);
        }

        private void Update()
        {
            RecoverMenuFocus();
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            back |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (!back) return;

            if (CurrentScreen == Screen.Gameplay || CurrentScreen == Screen.Results) ShowSongSelect();
            else if (CurrentScreen == Screen.SongSelect) ShowTitle();
        }

        public void ShowTitle()
        {
            SelectedSong = null;
            SetScreen(Screen.Title, playButton);
        }

        public void ShowSongSelect()
        {
            // The one available sample is selected automatically.
            SelectedSong = sampleSong;
            RefreshSongCard();
            SetScreen(Screen.SongSelect, startSongButton);
        }

        // Intentionally a placeholder until the user defines settings.
        public void ShowSettings() { }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void StartSelectedSong()
        {
            if (CurrentScreen != Screen.SongSelect || SelectedSong == null || !SelectedSong.IsPlayable) return;
            SetScreen(Screen.Gameplay, null);
            if (Application.isPlaying)
            {
                var prototype = gameplayScreen.GetComponent<CountdownPrototype>();
                if (prototype == null) prototype = gameplayScreen.AddComponent<CountdownPrototype>();
                prototype.BeginLevel(SelectedSong, ShowSongSelect, ShowResults);
            }
        }

        public void ShowResults(BeatmapSession session)
        {
            if (CurrentScreen != Screen.Gameplay || session == null || session.CompletedCount != session.TotalCount) return;
            if (results == null)
            {
                var runner = gameplayScreen.GetComponent<CountdownPrototype>();
                results = new ScoreScreenView(gameplayScreen.transform.parent, runner.ScoreFont, runner.DefaultBackground, ShowSongSelect);
            }
            results.Show(SelectedSong, session);
            SetScreen(Screen.Results, results.ReturnButton);
        }

        private void SetScreen(Screen screen, Button initialFocus)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            CurrentScreen = screen;
            titleScreen.SetActive(screen == Screen.Title);
            songSelectScreen.SetActive(screen == Screen.SongSelect);
            gameplayScreen.SetActive(screen == Screen.Gameplay);
            if (results != null) results.Root.SetActive(screen == Screen.Results);
            // Space is a gameplay lane key. Menu submit/move must never trigger HUD buttons during a song.
            if (menuEvents != null) menuEvents.sendNavigationEvents = screen != Screen.Gameplay;
            Focus(initialFocus);
        }

        private void ConfigureMenuInput()
        {
            uiModule = FindAnyObjectByType<InputSystemUIInputModule>(FindObjectsInactive.Include);
            if (uiModule == null) return;
            menuEvents = uiModule.GetComponent<EventSystem>();
            previousNavigationEvents = menuEvents.sendNavigationEvents;
            previousSubmit = uiModule.submit;
            // A private asset leaves the project's/package's input assets unchanged.
            menuInput = ScriptableObject.CreateInstance<InputActionAsset>();
            var action = menuInput.AddActionMap("Menus").AddAction("Submit", InputActionType.Button);
            action.AddBinding("*/{Submit}"); // Enter and existing controller confirmation.
            action.AddBinding("<Keyboard>/numpadEnter");
            action.AddBinding("<Keyboard>/space");
            menuSubmit = InputActionReference.Create(action);
            uiModule.submit = menuSubmit;
        }

        private static void CompleteArrowNavigation(GameObject root)
        {
            if (root == null) return;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var nav = button.navigation;
                if (nav.mode != Navigation.Mode.Explicit) continue;
                if (nav.selectOnLeft == null) nav.selectOnLeft = nav.selectOnUp;
                if (nav.selectOnRight == null) nav.selectOnRight = nav.selectOnDown;
                button.navigation = nav;
            }
        }

        private void RecoverMenuFocus()
        {
            if (CurrentScreen == Screen.Gameplay || EventSystem.current == null) return;
            var root = CurrentScreen == Screen.Title ? titleScreen : CurrentScreen == Screen.SongSelect ? songSelectScreen : results?.Root;
            if (root == null) return;
            var selected = EventSystem.current.currentSelectedGameObject;
            var button = selected != null ? selected.GetComponent<Button>() : null;
            if (button != null && button.isActiveAndEnabled && button.interactable && button.transform.IsChildOf(root.transform)) return;
            var preferred = CurrentScreen == Screen.Title ? playButton : CurrentScreen == Screen.SongSelect ? startSongButton : results.ReturnButton;
            if (preferred != null && preferred.isActiveAndEnabled && preferred.interactable) { Focus(preferred); return; }
            foreach (var candidate in root.GetComponentsInChildren<Button>())
                if (candidate.isActiveAndEnabled && candidate.interactable) { Focus(candidate); return; }
        }

        private void OnDestroy()
        {
            if (uiModule != null && uiModule.submit == menuSubmit) uiModule.submit = previousSubmit;
            if (menuEvents != null) menuEvents.sendNavigationEvents = previousNavigationEvents;
            if (menuInput != null) menuInput.Disable();
            if (menuSubmit != null) Destroy(menuSubmit);
            if (menuInput != null) Destroy(menuInput);
        }

        private static void Focus(Button button)
        {
            if (button != null && EventSystem.current != null && button.interactable)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private void RefreshSongCard()
        {
            bool available = sampleSong != null;
            if (songTitle != null) songTitle.text = available ? sampleSong.SongTitle : "No song assigned";
            if (songTitleGlyphs != null) songTitleGlyphs.SetText(available ? sampleSong.SongTitle : "No song assigned");
            if (songArtist != null) songArtist.text = available ? sampleSong.Artist : "Assign a Song asset on Menu Flow.";
            if (songDetails != null) songDetails.text = available ? $"{sampleSong.Bpm} BPM  /  {sampleSong.Difficulty}" : "";
            startSongButton.interactable = available && sampleSong.IsPlayable;
            bool hasArt = available && sampleSong.CoverArt != null;
            if (songCover != null)
            {
                songCover.sprite = hasArt ? sampleSong.CoverArt : null;
                songCover.gameObject.SetActive(hasArt);
            }
            if (placeholderCover != null) placeholderCover.SetActive(!hasArt);
        }
    }
}
