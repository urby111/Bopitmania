using System;

namespace BopBeat.Beatmaps
{
    public enum Judgment { None, Perfect, Good, Ok, Miss }

    public readonly struct NoteFeedback
    {
        public readonly int Lane;
        public readonly Judgment Judgment;
        public readonly double ErrorSeconds;
        public readonly bool Holding;

        public NoteFeedback(int lane, Judgment judgment, double errorSeconds, bool holding = false)
        {
            Lane = lane;
            Judgment = judgment;
            ErrorSeconds = errorSeconds;
            Holding = holding;
        }
    }

    /// <summary>Pure timing logic; times are always seconds from the start of the song.</summary>
    public sealed class BeatmapSession
    {
        public const double PerfectWindow = 0;
        public const double GoodWindow = 0.15;
        public const double OkWindow = 0.4;
        private const double Epsilon = 1e-9;
        private enum State { Waiting, Holding, Complete }
        private readonly BeatmapNote[] notes;
        private readonly State[] states;
        private readonly Judgment[] holdStartGrades;
        private readonly int[] activeHolds = { -1, -1, -1, -1, -1 };
        private readonly bool[] held = new bool[BeatmapValidator.LaneCount];
        private readonly TimingSettings timing;
        private readonly ScoreSettings scoring;

        public event Action<NoteFeedback> Feedback;
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int OkCount { get; private set; }
        public int MissCount { get; private set; }
        public int CompletedCount => PerfectCount + GoodCount + OkCount + MissCount;
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public double Score { get; private set; }
        public double HitWindow => timing.ok;
        public int TotalCount => notes.Length;
        public double LastNoteTime { get; }
        public double Speed { get; }

        public BeatmapNote NoteAt(int index) => notes[index];
        public bool IsComplete(int index) => states[index] == State.Complete;
        public bool IsHolding(int index) => states[index] == State.Holding;

        public BeatmapSession(BeatmapData data, double songDuration, TimingSettings timingSettings = null, ScoreSettings scoreSettings = null)
        {
            var windows = timingSettings ?? new TimingSettings();
            var points = scoreSettings ?? new ScoreSettings();
            windows.Validate();
            points.Validate();
            // Copy settings so Inspector changes cannot alter a session mid-song.
            timing = new TimingSettings { perfect = windows.perfect, good = windows.good, ok = windows.ok };
            scoring = new ScoreSettings { perfect = points.perfect, good = points.good, ok = points.ok };
            notes = BeatmapValidator.ValidateAndSort(data, songDuration);
            Speed = data.speed;
            states = new State[notes.Length];
            holdStartGrades = new Judgment[notes.Length];
            foreach (var note in notes) LastNoteTime = Math.Max(LastNoteTime, note.LastTime);
        }

        public static Judgment Grade(double errorSeconds)
            => Grade(errorSeconds, new TimingSettings());

        public static Judgment Grade(double errorSeconds, TimingSettings timing)
        {
            double error = Math.Abs(errorSeconds);
            // A zero Perfect window means exact timing; never widen it with the decimal-boundary tolerance.
            if (error == 0 || (timing.perfect > 0 && error <= timing.perfect + Epsilon)) return Judgment.Perfect;
            if (error <= timing.good + Epsilon) return Judgment.Good;
            if (error <= timing.ok + Epsilon) return Judgment.Ok;
            return Judgment.Miss;
        }

        public Judgment Press(int lane, double songTime)
        {
            CheckLane(lane);
            if (held[lane]) return Judgment.None;
            Advance(songTime);
            held[lane] = true;
            int index = Nearest(lane, songTime, false);
            if (index < 0) return Judgment.None;
            var note = notes[index];
            var grade = Grade(songTime - note.time, timing);
            if (note.Kind == NoteKind.Hold)
            {
                states[index] = State.Holding;
                activeHolds[lane] = index;
                holdStartGrades[index] = grade;
                Feedback?.Invoke(new NoteFeedback(lane, grade, songTime - note.time, true));
            }
            else Finish(index, grade, songTime - note.time);
            return grade;
        }

        public Judgment Release(int lane, double songTime)
        {
            CheckLane(lane);
            if (!held[lane]) return Judgment.None;
            held[lane] = false;
            int index = activeHolds[lane];
            if (index >= 0)
            {
                double error = songTime - notes[index].endTime;
                var grade = Grade(error, timing);
                if ((int)holdStartGrades[index] > (int)grade) grade = holdStartGrades[index];
                Finish(index, grade, error);
                return grade;
            }
            Advance(songTime);
            index = Nearest(lane, songTime, true);
            if (index < 0) return Judgment.None;
            var releaseGrade = Grade(songTime - notes[index].time, timing);
            Finish(index, releaseGrade, songTime - notes[index].time);
            return releaseGrade;
        }

        public void Advance(double songTime)
        {
            if (songTime < 0) return;
            for (int i = 0; i < notes.Length; i++)
            {
                if (states[i] == State.Waiting && songTime > notes[i].time + HitWindow + Epsilon)
                    Finish(i, Judgment.Miss, songTime - notes[i].time);
                else if (states[i] == State.Holding && songTime > notes[i].endTime + HitWindow + Epsilon)
                    Finish(i, Judgment.Miss, songTime - notes[i].endTime);
            }
        }

        public BeatmapNote Upcoming(int lane)
        {
            CheckLane(lane);
            int active = activeHolds[lane];
            if (active >= 0) return notes[active];
            for (int i = 0; i < notes.Length; i++)
                if (notes[i].lane == lane && states[i] == State.Waiting) return notes[i];
            return null;
        }

        public bool IsHoldingNote(int lane) => activeHolds[lane] >= 0;
        public bool IsKeyHeld(int lane) => held[lane];

        private int Nearest(int lane, double songTime, bool release)
        {
            if (activeHolds[lane] >= 0) return -1;
            int result = -1;
            double best = HitWindow + Epsilon;
            for (int i = 0; i < notes.Length; i++)
            {
                var note = notes[i];
                if (states[i] != State.Waiting || note.lane != lane || (note.Kind == NoteKind.Release) != release) continue;
                double error = Math.Abs(songTime - note.time);
                if (error <= HitWindow + Epsilon && (result < 0 || error < best - Epsilon))
                {
                    result = i;
                    best = error;
                }
            }
            return result;
        }

        private void Finish(int index, Judgment grade, double error)
        {
            if (states[index] == State.Complete) return;
            int lane = notes[index].lane;
            if (activeHolds[lane] == index) activeHolds[lane] = -1;
            states[index] = State.Complete;
            if (grade == Judgment.Perfect) PerfectCount++;
            else if (grade == Judgment.Good) GoodCount++;
            else if (grade == Judgment.Ok) OkCount++;
            else MissCount++;
            // Use the combo after this judgment: Perfect increments; every other result resets.
            Combo = grade == Judgment.Perfect ? Combo + 1 : 0;
            MaxCombo = Math.Max(MaxCombo, Combo);
            int hitValue = grade == Judgment.Perfect ? scoring.perfect : grade == Judgment.Good ? scoring.good : grade == Judgment.Ok ? scoring.ok : 0;
            Score += hitValue + hitValue * (Combo / 25.0);
            Feedback?.Invoke(new NoteFeedback(lane, grade, error));
        }

        private static void CheckLane(int lane)
        {
            if (lane < 0 || lane >= BeatmapValidator.LaneCount) throw new ArgumentOutOfRangeException(nameof(lane));
        }
    }
}
