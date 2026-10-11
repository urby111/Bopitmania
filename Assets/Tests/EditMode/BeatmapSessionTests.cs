using System;
using System.Collections.Generic;
using BopBeat.Beatmaps;
using BopBeat.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BopBeat.Tests
{
    public sealed class BeatmapSessionTests
    {
        private static BeatmapNote Tap(double time = 1.5, int lane = 0) => new BeatmapNote { time = time, lane = lane, type = "tap" };
        private static BeatmapSession Session(params BeatmapNote[] notes) => new BeatmapSession(new BeatmapData { notes = notes }, 30);

        [TestCase(0, Judgment.Perfect)]
        [TestCase(1e-12, Judgment.Good)]
        [TestCase(-1e-12, Judgment.Good)]
        [TestCase(0.1, Judgment.Good)]
        [TestCase(-0.1, Judgment.Good)]
        [TestCase(0.15, Judgment.Good)]
        [TestCase(-0.15, Judgment.Good)]
        [TestCase(0.15001, Judgment.Ok)]
        [TestCase(-0.15001, Judgment.Ok)]
        [TestCase(0.4, Judgment.Ok)]
        [TestCase(-0.4, Judgment.Ok)]
        [TestCase(0.40001, Judgment.Miss)]
        [TestCase(-0.40001, Judgment.Miss)]
        public void TimingWindowsAreInclusiveAndSymmetric(double error, Judgment expected)
            => Assert.AreEqual(expected, BeatmapSession.Grade(error));

        [Test]
        public void DecimalTimestampBoundaryRemainsGood()
            => Assert.AreEqual(Judgment.Good, Session(Tap()).Press(0, 1.65));

        [Test]
        public void PreRollSupportsEarlyTimeZeroHitsAndReleasePreparation()
        {
            Assert.AreEqual(Judgment.Perfect, Session(Tap(0)).Press(0, 0));
            Assert.AreEqual(Judgment.Good, Session(Tap(0)).Press(0, -0.15));
            Assert.AreEqual(Judgment.Ok, Session(Tap(0)).Press(0, -0.4));
            var release = Session(new BeatmapNote { time = 0, lane = 1, type = "release" });
            Assert.AreEqual(Judgment.None, release.Press(1, -1));
            Assert.AreEqual(Judgment.Perfect, release.Release(1, 0));
        }

        [Test]
        public void WrongLaneAndRepeatedPressCannotConsumeTheNote()
        {
            var session = Session(Tap());
            Assert.AreEqual(Judgment.None, session.Press(1, 1.5));
            Assert.AreEqual(Judgment.Perfect, session.Press(0, 1.5));
            Assert.AreEqual(Judgment.None, session.Press(0, 1.5));
            session.Release(0, 1.51);
            Assert.AreEqual(Judgment.None, session.Press(0, 1.52));
            Assert.AreEqual(1, session.PerfectCount);
        }

        [Test]
        public void HoldingAnEarlyKeyDoesNotBecomeATapWithoutANewPress()
        {
            var session = Session(Tap());
            Assert.AreEqual(Judgment.None, session.Press(0, 0.8));
            Assert.AreEqual(Judgment.None, session.Press(0, 1.5));
            session.Release(0, 1.51);
            Assert.AreEqual(Judgment.Good, session.Press(0, 1.52));
        }

        [Test]
        public void NearestNoteWinsAndEqualDistanceChoosesEarlierNote()
        {
            var session = Session(Tap(1.5), Tap(2.1));
            Assert.AreEqual(Judgment.Ok, session.Press(0, 1.8));
            Assert.AreEqual(2.1, session.Upcoming(0).time);
            session.Release(0, 1.81);
            Assert.AreEqual(Judgment.Perfect, session.Press(0, 2.1));
        }

        [Test]
        public void HoldCountsOnceAndUsesTheWorseEndpointGrade()
        {
            var session = Session(new BeatmapNote { time = 1.5, lane = 2, type = "hold", endTime = 3 });
            Assert.AreEqual(Judgment.Good, session.Press(2, 1.65));
            Assert.IsTrue(session.IsHoldingNote(2));
            Assert.AreEqual(0, session.CompletedCount);
            Assert.AreEqual(Judgment.Good, session.Release(2, 3.0));
            Assert.AreEqual(1, session.GoodCount);
            session.Advance(20);
            Assert.AreEqual(1, session.CompletedCount);
        }

        [Test]
        public void EarlyHoldReleaseAndNeverReleasedHoldEachMissOnce()
        {
            var hold = new BeatmapNote { time = 1.5, lane = 0, type = "hold", endTime = 3 };
            var early = Session(hold);
            early.Press(0, 1.5);
            Assert.AreEqual(Judgment.Miss, early.Release(0, 2));
            early.Advance(20);
            Assert.AreEqual(1, early.MissCount);
            var late = Session(hold);
            late.Press(0, 1.5);
            late.Advance(3.4);
            Assert.AreEqual(0, late.MissCount);
            late.Advance(3.4001);
            late.Release(0, 4);
            Assert.AreEqual(1, late.MissCount);
        }

        [Test]
        public void StandaloneReleaseRequiresTheKeyToBeHeldFirst()
        {
            var session = Session(new BeatmapNote { time = 2, lane = 1, type = "release" });
            Assert.AreEqual(Judgment.None, session.Release(1, 2));
            Assert.AreEqual(Judgment.None, session.Press(1, 1));
            Assert.AreEqual(Judgment.Perfect, session.Release(1, 2));
        }

        [Test]
        public void ChordsAcrossLanesAreIndependentAndUnplayedNotesMissOnce()
        {
            var session = Session(Tap(1.5, 0), Tap(1.5, 4), Tap(2.5, 2));
            session.Press(0, 1.5);
            session.Press(4, 1.5);
            session.Advance(3.0001);
            session.Advance(25);
            Assert.AreEqual(2, session.PerfectCount);
            Assert.AreEqual(1, session.MissCount);
        }

        [Test]
        public void InvalidNotesAndOverlappingHoldsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => Session(Tap(-1)));
            Assert.Throws<ArgumentException>(() => Session(Tap(double.NaN)));
            Assert.Throws<ArgumentException>(() => Session(Tap(31)));
            Assert.Throws<ArgumentException>(() => Session(Tap(1, 5)));
            Assert.Throws<ArgumentException>(() => Session(new BeatmapNote { time = 1, type = "unknown" }));
            Assert.Throws<ArgumentException>(() => Session(new BeatmapNote { time = 2, type = "hold", endTime = 1 }));
            Assert.Throws<ArgumentException>(() => Session(Tap(), Tap()));
            Assert.Throws<ArgumentException>(() => Session(new BeatmapNote { time = 1, type = "hold", endTime = 3 }, Tap(2)));
        }

        [Test]
        public void SampleLevelCanBePlayedPerfectlyFromItsActualJson()
        {
            var level = AssetDatabase.LoadAssetAtPath<SongDefinition>("Assets/Data/Songs/SampleSong.asset");
            Assert.IsNotNull(level.Music);
            Assert.IsNotNull(level.Beatmap);
            // The sample deliberately uses the plain replaceable background.
            var data = JsonUtility.FromJson<BeatmapData>(level.Beatmap.text);
            var session = new BeatmapSession(data, level.Music.length);
            var events = new List<(double time, int lane, bool press)>();
            foreach (var note in data.notes)
            {
                if (note.Kind == NoteKind.Release) events.Add((note.time - 0.75, note.lane, true));
                else events.Add((note.time, note.lane, true));
                events.Add((note.Kind == NoteKind.Hold ? note.endTime : note.Kind == NoteKind.Release ? note.time : note.time + 0.01, note.lane, false));
            }
            events.Sort((a, b) => a.time.CompareTo(b.time));
            foreach (var input in events)
            {
                if (input.press) session.Press(input.lane, input.time);
                else session.Release(input.lane, input.time);
            }
            session.Advance(level.Music.length + 1);
            Assert.AreEqual(data.notes.Length, session.PerfectCount);
            Assert.AreEqual(0, session.GoodCount);
            Assert.AreEqual(0, session.OkCount);
            Assert.AreEqual(0, session.MissCount);
            Assert.AreEqual(32, session.Combo);
            Assert.AreEqual(53120, session.Score, 0.001);
        }
    }
}
