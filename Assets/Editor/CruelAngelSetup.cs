using System;
using System.Collections.Generic;
using System.IO;
using BopBeat.Beatmaps;
using BopBeat.Menus;
using UnityEditor;
using UnityEngine;

namespace BopBeat.Editor
{
    /// <summary>Authored random-lane chart using the user's supplied tempo and imported audio duration.</summary>
    public static class CruelAngelSetup
    {
        public const string ChartPath = "Assets/Data/Beatmaps/CruelAngelThesis.json";
        public const string LevelPath = "Assets/Data/Songs/CruelAngelThesis.asset";
        public const string AudioPath = "Assets/Audio/The Cruel Angel's Thesis TV. Size Version NEON GENESIS EVANGELION II (Original Soundtrack).mp3";

        [MenuItem("Tools/Bop Beat/Regenerate Cruel Angel Chart")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before regenerating the chart.");
            var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (audio == null || audio.length <= 16) throw new InvalidOperationException("The supplied Cruel Angel audio is missing or too short.");
            var random = new System.Random(80128);
            var notes = new List<BeatmapNote>();
            // Both sections begin on their own downbeat. Fixed seed keeps the saved chart repeatable.
            AddSection(notes, random, 0, 16, 80);
            AddSection(notes, random, 16, audio.length, 128);
            var map = new BeatmapData { speed = 1, notes = notes.ToArray() };
            BeatmapValidator.ValidateAndSort(map, audio.length);
            File.WriteAllText(ChartPath, JsonUtility.ToJson(map, true) + "\n");
            AssetDatabase.ImportAsset(ChartPath, ImportAssetOptions.ForceSynchronousImport);
            var song = AssetDatabase.LoadAssetAtPath<SongDefinition>(LevelPath);
            if (song == null)
            {
                song = ScriptableObject.CreateInstance<SongDefinition>();
                AssetDatabase.CreateAsset(song, LevelPath);
            }
            var data = new SerializedObject(song);
            data.FindProperty("songTitle").stringValue = "Cruel Angel Thesis";
            data.FindProperty("artist").stringValue = "Yoko Takahashi";
            data.FindProperty("bpm").intValue = 80;
            data.FindProperty("beatOffsetSeconds").floatValue = 0;
            var changes = data.FindProperty("tempoChanges");
            changes.arraySize = 1;
            changes.GetArrayElementAtIndex(0).FindPropertyRelative("time").doubleValue = 16;
            changes.GetArrayElementAtIndex(0).FindPropertyRelative("bpm").intValue = 128;
            data.FindProperty("difficulty").stringValue = "EVERY TWO BEATS";
            data.FindProperty("music").objectReferenceValue = audio;
            data.FindProperty("beatmap").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(ChartPath);
            data.ApplyModifiedPropertiesWithoutUndo();
            song.ValidateTempo();
            EditorUtility.SetDirty(song);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cruel Angel chart: {notes.Count} taps, audio length {audio.length:0.000} s, seed 80128, 80 BPM until 16 s then 128 BPM.");
        }

        private static void AddSection(List<BeatmapNote> notes, System.Random random, double start, double end, double bpm)
        {
            double step = 120 / bpm;
            for (int index = 0; start + index * step < end; index++)
                notes.Add(new BeatmapNote { time = start + index * step, lane = random.Next(5), type = "tap" });
        }
    }
}
