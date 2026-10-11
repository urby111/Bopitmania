using System;
using BopBeat.Beatmaps;
using BopBeat.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BopBeat.Tests
{
    public sealed class FallingNoteTests
    {
        private static BeatmapNote Tap(double time, int lane = 0) => new BeatmapNote { time = time, lane = lane, type = "tap" };

        [Test]
        public void SongTimeDeterminesTravelWithoutAccumulatedFrameMovement()
        {
            Assert.AreEqual(1, NoteTravel.TravelSeconds(1));
            Assert.AreEqual(0, NoteTravel.Position(10, 9, 1, 0, 100));
            Assert.AreEqual(20, NoteTravel.Position(10, 9.2, 1, 0, 100), 1e-9);
            Assert.AreEqual(50, NoteTravel.Position(10, 9.5, 1, 0, 100));
            Assert.AreEqual(100, NoteTravel.Position(10, 10, 1, 0, 100));
            Assert.AreEqual(110, NoteTravel.Position(10, 10.1, 1, 0, 100), 1e-9);
        }

        [Test]
        public void DoubleSpeedHalvesTravelAndDoublesHoldLengthAndTapThickness()
        {
            Assert.AreEqual(0.5, NoteTravel.TravelSeconds(2));
            Assert.AreEqual(0, NoteTravel.Position(10, 9.5, 2, 0, 510));
            Assert.AreEqual(255, NoteTravel.Position(10, 9.75, 2, 0, 510));
            Assert.AreEqual(510, NoteTravel.Position(10, 10, 2, 0, 510));
            Assert.AreEqual(765, NoteTravel.HoldLength(6.5, 8, 1, 510));
            Assert.AreEqual(1530, NoteTravel.HoldLength(6.5, 8, 2, 510));
            Assert.AreEqual(20.4, NoteTravel.BlockHeight(1, 510), 1e-9);
            Assert.AreEqual(40.8, NoteTravel.BlockHeight(2, 510), 1e-9);
        }

        [Test]
        public void OlderJsonDefaultsToOneSpeedAndExplicitSpeedIsLoaded()
        {
            var data = new BeatmapData();
            JsonUtility.FromJsonOverwrite("{\"notes\":[{\"time\":1,\"lane\":0,\"type\":\"tap\"}]}", data);
            Assert.AreEqual(1, new BeatmapSession(data, 10).Speed);
            JsonUtility.FromJsonOverwrite("{\"speed\":2,\"notes\":[{\"time\":1,\"lane\":0,\"type\":\"tap\"}]}", data);
            Assert.AreEqual(2, new BeatmapSession(data, 10).Speed);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidSpeedIsRejected(double speed)
            => Assert.Throws<ArgumentException>(() => new BeatmapSession(new BeatmapData { speed = speed, notes = new[] { Tap(1) } }, 10));

        [Test]
        public void PerfectComboBoostsScoreAndGoodOkMissResetIt()
        {
            var session = new BeatmapSession(new BeatmapData { notes = new[] { Tap(1), Tap(2), Tap(3), Tap(4), Tap(5), Tap(6), Tap(7) } }, 10);
            session.Press(0, 1);
            Assert.AreEqual(1040, session.Score);
            Assert.AreEqual(1, session.Combo);
            session.Release(0, 1.01);
            session.Press(0, 2);
            Assert.AreEqual(2120, session.Score);
            Assert.AreEqual(2, session.Combo);
            session.Release(0, 2.01);
            Assert.AreEqual(Judgment.Good, session.Press(0, 3.15));
            Assert.AreEqual(0, session.Combo);
            Assert.AreEqual(2620, session.Score);
            session.Release(0, 3.16);
            session.Press(0, 4);
            session.Release(0, 4.01);
            Assert.AreEqual(Judgment.Ok, session.Press(0, 5.3));
            Assert.AreEqual(0, session.Combo);
            session.Release(0, 5.31);
            session.Press(0, 6);
            session.Release(0, 6.01);
            session.Advance(7.41);
            Assert.AreEqual(0, session.Combo);
            Assert.AreEqual(2, session.MaxCombo);
            Assert.AreEqual(4800, session.Score);
            Assert.AreEqual(7, session.CompletedCount);
            session.Advance(20);
            Assert.AreEqual(4800, session.Score, "Completed notes must not score again.");
        }

        [Test]
        public void DivisionIsFractionalAndTwentyFifthPerfectDoublesItsBasePoints()
        {
            var notes = new BeatmapNote[25];
            for (int i = 0; i < notes.Length; i++) notes[i] = Tap(i + 1);
            var session = new BeatmapSession(new BeatmapData { notes = notes }, 30, scoreSettings: new ScoreSettings { perfect = 101 });
            session.Press(0, 1);
            Assert.AreEqual(105.04, session.Score, 1e-9);
            session.Release(0, 1.01);
            for (int i = 1; i < 24; i++) { session.Press(0, i + 1); session.Release(0, i + 1.01); }
            double before = session.Score;
            session.Press(0, 25);
            Assert.AreEqual(202, session.Score - before, 1e-9);
        }

        [Test]
        public void HoldScoresOnlyOnceAtTheWorseEndpointAndReleaseCanEarnOk()
        {
            var session = new BeatmapSession(new BeatmapData { notes = new[] {
                new BeatmapNote { time = 1, lane = 0, type = "hold", endTime = 3 },
                new BeatmapNote { time = 5, lane = 1, type = "release" }
            } }, 10);
            session.Press(0, 1.3);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.Combo);
            Assert.AreEqual(Judgment.Ok, session.Release(0, 3));
            Assert.AreEqual(100, session.Score);
            session.Press(1, 4);
            Assert.AreEqual(Judgment.Ok, session.Release(1, 5.4));
            Assert.AreEqual(200, session.Score);
            Assert.AreEqual(2, session.OkCount);
        }

        [Test]
        public void SpeedDoesNotChangeJudgmentTimesAndSettingsAreValidated()
        {
            var data = new BeatmapData { speed = 2, notes = new[] { Tap(1) } };
            var session = new BeatmapSession(data, 10);
            Assert.AreEqual(Judgment.Good, session.Press(0, 1.05));
            Assert.Throws<ArgumentException>(() => new BeatmapSession(data, 10, new TimingSettings { perfect = 0.1, good = 0.05 }));
            Assert.Throws<ArgumentException>(() => new BeatmapSession(data, 10, scoreSettings: new ScoreSettings { ok = -1 }));
        }
    }
}
