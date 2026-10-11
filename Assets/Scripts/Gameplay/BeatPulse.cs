using System;

namespace BopBeat.Gameplay
{
    /// <summary>Beat decoration derived from the audio clock; never changes note timing.</summary>
    public static class BeatPulse
    {
        public static float Scale(double songTime, double bpm, double firstBeatSeconds, bool finished)
        {
            if (finished || songTime < 0 || songTime < firstBeatSeconds || bpm <= 0
                || double.IsNaN(bpm) || double.IsInfinity(bpm)) return 1;
            double interval = 60.0 / bpm;
            double phase = (songTime - firstBeatSeconds) % interval;
            double duration = Math.Min(0.18, interval * 0.5);
            double strength = Math.Max(0, 1 - phase / duration);
            return (float)(1 + 0.08 * strength * strength);
        }
    }
}
