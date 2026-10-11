using System;
using UnityEngine;

namespace BopBeat.Menus
{
    /// <summary>A customizable level: song, JSON beatmap, background, and menu metadata.</summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "Bop Beat/Level")]
    public sealed class SongDefinition : ScriptableObject
    {
        [Serializable]
        public struct TempoChange
        {
            public double time;
            public int bpm;
        }
        [SerializeField] private string songTitle = "Sample Song";
        [SerializeField] private string artist = "Hackathon Team";
        [Tooltip("Tempo used by the beat pulse. Note hit times still come from the JSON chart.")]
        [SerializeField, Min(1)] private int bpm = 120;
        [Tooltip("Song time of the first pulse beat. Use this for silence or an intro before the first beat.")]
        [SerializeField, Min(0)] private float beatOffsetSeconds;
        [Tooltip("Optional tempo changes in ascending song seconds. Each starts a new pulse beat; chart times stay unchanged.")]
        [SerializeField] private TempoChange[] tempoChanges = Array.Empty<TempoChange>();
        [SerializeField] private string difficulty = "DEMO";
        [SerializeField] private Sprite coverArt;
        [Header("Playable level")]
        [SerializeField] private AudioClip music;
        [SerializeField] private TextAsset beatmap;
        [SerializeField] private Sprite background;
        [SerializeField] private Color backgroundTint = Color.white;

        public string SongTitle => songTitle;
        public string Artist => artist;
        public int Bpm => bpm;
        public float BeatOffsetSeconds => beatOffsetSeconds;
        public string Difficulty => difficulty;
        public Sprite CoverArt => coverArt;
        public AudioClip Music => music;
        public TextAsset Beatmap => beatmap;
        public Sprite Background => background;
        public Color BackgroundTint => backgroundTint;
        public bool IsPlayable => music != null && beatmap != null;

        public int TempoAt(double songTime, out double firstBeat)
        {
            int tempo = bpm;
            firstBeat = beatOffsetSeconds;
            if (tempoChanges != null)
                foreach (var change in tempoChanges)
                {
                    if (songTime < change.time) break;
                    tempo = change.bpm;
                    firstBeat = change.time;
                }
            return tempo;
        }

        public void ValidateTempo()
        {
            if (bpm <= 0 || float.IsNaN(beatOffsetSeconds) || float.IsInfinity(beatOffsetSeconds) || beatOffsetSeconds < 0)
                throw new ArgumentException("BPM must be positive and Beat Offset Seconds must be finite and nonnegative.");
            double previous = beatOffsetSeconds;
            if (tempoChanges == null) return;
            foreach (var change in tempoChanges)
            {
                if (double.IsNaN(change.time) || double.IsInfinity(change.time) || change.time <= previous
                    || change.bpm <= 0 || (music != null && change.time >= music.length))
                    throw new ArgumentException("Tempo changes need positive BPM and increasing times after the first beat and before the song ends.");
                previous = change.time;
            }
        }
    }
}
