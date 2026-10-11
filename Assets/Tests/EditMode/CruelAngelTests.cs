using System;
using System.Reflection;
using BopBeat.Beatmaps;
using BopBeat.Gameplay;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BopBeat.Tests
{
    public sealed class CruelAngelTests
    {
        [Test]
        public void SavedChartMatchesImportedSongAndEverySecondBeatInBothSections()
        {
            var song = AssetDatabase.LoadAssetAtPath<SongDefinition>("Assets/Data/Songs/CruelAngelThesis.asset");
            Assert.IsNotNull(song);
            Assert.IsTrue(song.IsPlayable);
            song.ValidateTempo();
            var map = new BeatmapData();
            JsonUtility.FromJsonOverwrite(song.Beatmap.text, map);
            var session = new BeatmapSession(map, song.Music.length);
            int slowCount = 0, fastCount = 0;
            var lanes = new bool[5];
            foreach (var note in map.notes)
            {
                lanes[note.lane] = true;
                Assert.AreEqual("tap", note.type);
                if (note.time < 16) Assert.AreEqual(slowCount++ * 1.5, note.time, 1e-8);
                else Assert.AreEqual(16 + fastCount++ * 0.9375, note.time, 1e-8);
                session.Press(note.lane, note.time);
                session.Release(note.lane, note.time);
            }
            Assert.AreEqual(11, slowCount);
            Assert.AreEqual(Math.Ceiling((song.Music.length - 16) / 0.9375), fastCount);
            CollectionAssert.AreEqual(new[] { true, true, true, true, true }, lanes);
            Assert.AreEqual(map.notes.Length, session.PerfectCount);
            Assert.AreEqual(1, map.speed);
            Assert.AreEqual(80, song.TempoAt(15.99, out double first));
            Assert.AreEqual(0, first);
            Assert.AreEqual(128, song.TempoAt(16, out first));
            Assert.AreEqual(16, first);
            Assert.AreEqual(1.08f, BeatPulse.Scale(16, 128, first, false), 1e-5);
            Assert.AreEqual(1.08f, BeatPulse.Scale(16.9375, 128, first, false), 1e-5);
        }

        [Test]
        public void InvalidOrUnsortedTempoChangesAreRejected()
        {
            var song = ScriptableObject.CreateInstance<SongDefinition>();
            try
            {
                var field = typeof(SongDefinition).GetField("tempoChanges", BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(song, new[] { new SongDefinition.TempoChange { time = 16, bpm = 128 }, new SongDefinition.TempoChange { time = 8, bpm = 80 } });
                Assert.Throws<ArgumentException>(song.ValidateTempo);
                field.SetValue(song, new[] { new SongDefinition.TempoChange { time = double.NaN, bpm = 128 } });
                Assert.Throws<ArgumentException>(song.ValidateTempo);
                field.SetValue(song, new[] { new SongDefinition.TempoChange { time = 16, bpm = 0 } });
                Assert.Throws<ArgumentException>(song.ValidateTempo);
            }
            finally { UnityEngine.Object.DestroyImmediate(song); }
        }
    }
}
