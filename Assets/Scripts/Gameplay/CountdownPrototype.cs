using System;
using System.Collections;
using BopBeat.Beatmaps;
using BopBeat.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace BopBeat.Gameplay
{
    // Retain the component name/GUID so the existing scene keeps its references.
    public sealed class CountdownPrototype : MonoBehaviour
    {
        [SerializeField] private Key[] laneKeys = { Key.D, Key.F, Key.Space, Key.J, Key.K };
        [SerializeField, Range(0, 1)] private float musicVolume = 0.65f;
        [SerializeField] private Font scoreFont;
        [SerializeField] private Sprite perfectArt, goodArt, okArt;
        [SerializeField] private Texture2D defaultBackground;
        [SerializeField] private Sprite boopitArt;
        [SerializeField] private TimingSettings timing = new TimingSettings();
        [SerializeField] private ScoreSettings scoring = new ScoreSettings();
        private InputAction[] inputs;
        private AudioSource audioSource;
        private SongDefinition level;
        private Action onBack;
        private Action<BeatmapSession> onComplete;
        private FallingNoteView view;
        private bool finished;
        private double scheduledStart, finishAt;

        public BeatmapSession Session { get; private set; }
        public bool IsRunning { get; private set; }
        public double SongTime => IsRunning ? AudioSettings.dspTime - scheduledStart : finished ? finishAt : 0;
        public AudioSource MusicSource => audioSource;
        public int VisibleBlockCount => view != null ? view.VisibleBlockCount : 0;
        public Font ScoreFont => scoreFont;
        public Texture2D DefaultBackground => defaultBackground;

        public void BeginLevel(SongDefinition selectedLevel, Action returnToSongs, Action<BeatmapSession> showResults = null)
        {
            StopLevel();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            level = selectedLevel;
            onBack = returnToSongs;
            onComplete = showResults;
            Session = null;
            finished = false;
            if (view == null)
                view = new FallingNoteView(transform, scoreFont, new[] { perfectArt, goodArt, okArt }, laneKeys, defaultBackground, boopitArt,
                    () => onBack?.Invoke(), () => { if (level != null) BeginLevel(level, onBack, onComplete); });
            try
            {
                if (level == null || !level.IsPlayable) throw new ArgumentException("Assign a song and a JSON beatmap on the level asset.");
                level.ValidateTempo();
                if (laneKeys == null || laneKeys.Length != 5) throw new ArgumentException("Assign exactly five keyboard keys.");
                for (int i = 0; i < 5; i++)
                {
                    if (laneKeys[i] == Key.None) throw new ArgumentException("Every lane needs a keyboard key.");
                    for (int j = 0; j < i; j++)
                        if (laneKeys[i] == laneKeys[j]) throw new ArgumentException("Lane keys must be different.");
                }
                // FromJsonOverwrite preserves the default 1x when an older map omits speed.
                var map = new BeatmapData();
                JsonUtility.FromJsonOverwrite(level.Beatmap.text, map);
                Session = new BeatmapSession(map, level.Music.length, timing, scoring);
                Session.Feedback += view.Feedback;
                view.Begin(Session, level);
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0;
                audioSource.pitch = 1;
                audioSource.volume = musicVolume;
                audioSource.clip = level.Music;
                ConfigureInputs();
                view.SetStatus("LOADING SONG...");
                StartCoroutine(StartWhenLoaded());
            }
            catch (Exception exception) { ShowError(exception.Message); }
        }

        private IEnumerator StartWhenLoaded()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            if (level.Music.loadState == AudioDataLoadState.Unloaded) level.Music.LoadAudioData();
            while (level.Music.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            if (level.Music.loadState != AudioDataLoadState.Loaded)
            {
                ShowError("The song could not load. Try a WAV file with Preload Audio Data enabled.");
                yield break;
            }
            scheduledStart = AudioSettings.dspTime + 3;
            finishAt = Math.Max(level.Music.length, Session.LastNoteTime + Session.HitWindow);
            audioSource.PlayScheduled(scheduledStart);
            IsRunning = true;
            foreach (var input in inputs) input.Enable();
            RefreshView(SongTime);
        }

        private void ConfigureInputs()
        {
            if (inputs != null) foreach (var input in inputs) input.Dispose();
            inputs = new InputAction[5];
            for (int lane = 0; lane < 5; lane++)
            {
                int capturedLane = lane;
                var input = new InputAction("Lane " + lane, InputActionType.Button, "<Keyboard>/" + laneKeys[lane].ToString().ToLowerInvariant());
                input.performed += context =>
                {
                    if (IsRunning) Session.Press(capturedLane, InputSongTime(context.time));
                };
                input.canceled += context =>
                {
                    if (IsRunning) Session.Release(capturedLane, InputSongTime(context.time));
                };
                inputs[lane] = input;
            }
        }

        private double InputSongTime(double eventTime)
        {
            // Subtract the event's queue age before comparing with the audio clock.
            double age = Math.Max(0, Time.realtimeSinceStartupAsDouble - eventTime);
            return AudioSettings.dspTime - scheduledStart - age;
        }

        private void Update()
        {
            if (view == null) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && level != null)
            {
                BeginLevel(level, onBack, onComplete);
                return;
            }
            if (!IsRunning)
            {
                if (finished) view.Render(finishAt, true); // Finish feedback still fades after the audio stops.
                return;
            }
            double songTime = SongTime;
            Session.Advance(songTime);
            RefreshView(songTime);
            if (songTime >= finishAt && Session.CompletedCount == Session.TotalCount)
            {
                IsRunning = false;
                finished = true;
                audioSource.Stop();
                foreach (var input in inputs) input.Disable();
                view.Render(finishAt, true);
                view.SetStatus("SONG COMPLETE\nR TO REPLAY");
                onComplete?.Invoke(Session);
            }
        }

        private void RefreshView(double time)
        {
            view.Render(time, false);
            view.SetStatus(time < 0 ? $"GET READY\n{Math.Ceiling(-time):0}" : level.SongTitle);
        }

        public void StopLevel()
        {
            IsRunning = false;
            StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();
            if (inputs != null) foreach (var input in inputs) input.Disable();
        }
        private void OnDisable() => StopLevel();
        private void OnDestroy()
        {
            if (inputs != null) foreach (var input in inputs) input.Dispose();
            view?.Dispose();
        }
        private void ShowError(string message)
        {
            StopLevel();
            view.Error(message);
            Debug.LogError("Bop Beat level: " + message, this);
        }
    }
}
