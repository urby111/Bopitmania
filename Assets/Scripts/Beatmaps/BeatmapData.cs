using System;

namespace BopBeat.Beatmaps
{
    public enum NoteKind { Tap, Hold, Release }

    [Serializable]
    public sealed class BeatmapData
    {
        public double speed = 1;
        public BeatmapNote[] notes;
    }

    [Serializable]
    public sealed class BeatmapNote
    {
        public double time;
        public int lane;
        public string type = "tap";
        public double endTime;

        public NoteKind Kind => type == "hold" ? NoteKind.Hold : type == "release" ? NoteKind.Release : NoteKind.Tap;
        public double LastTime => Kind == NoteKind.Hold ? endTime : time;
    }

    public static class BeatmapValidator
    {
        public const int LaneCount = 5;

        public static BeatmapNote[] ValidateAndSort(BeatmapData data, double songDuration)
        {
            if (data == null || data.notes == null || data.notes.Length == 0)
                throw new ArgumentException("Beatmap must contain a non-empty notes array.");
            if (!Finite(data.speed) || data.speed <= 0 || !Finite(1 / data.speed))
                throw new ArgumentException("Beatmap speed must be a finite positive multiplier (1 = one second of travel).");
            if (!Finite(songDuration) || songDuration <= 0)
                throw new ArgumentException("The level needs a song with a positive duration.");

            var result = new BeatmapNote[data.notes.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var source = data.notes[i];
                if (source == null) throw new ArgumentException($"Note {i}: entry is null.");
                string type = (source.type ?? "").Trim().ToLowerInvariant();
                if (type != "tap" && type != "hold" && type != "release")
                    throw new ArgumentException($"Note {i}: type must be tap, hold, or release.");
                if (!Finite(source.time) || source.time < 0 || source.time > songDuration)
                    throw new ArgumentException($"Note {i}: time must be within the song, in seconds.");
                if (source.lane < 0 || source.lane >= LaneCount)
                    throw new ArgumentException($"Note {i}: lane must be 0 through 4.");
                if (type == "hold" && (!Finite(source.endTime) || source.endTime <= source.time || source.endTime > songDuration))
                    throw new ArgumentException($"Note {i}: hold endTime must follow time and be within the song.");
                result[i] = new BeatmapNote { time = source.time, lane = source.lane, type = type, endTime = source.endTime };
            }

            Array.Sort(result, (a, b) => a.time != b.time ? a.time.CompareTo(b.time) : a.lane.CompareTo(b.lane));
            var last = new BeatmapNote[LaneCount];
            foreach (var note in result)
            {
                var previous = last[note.lane];
                if (previous != null && (note.time == previous.time || note.time < previous.LastTime))
                    throw new ArgumentException($"Lane {note.lane}: duplicate or overlapping note at {note.time:0.###} seconds.");
                last[note.lane] = note;
            }
            return result;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
