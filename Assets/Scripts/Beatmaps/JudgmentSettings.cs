using System;

namespace BopBeat.Beatmaps
{
    [Serializable]
    public sealed class TimingSettings
    {
        public double perfect = 0;
        public double good = 0.15;
        public double ok = 0.4;

        public void Validate()
        {
            if (double.IsNaN(perfect) || double.IsInfinity(perfect) || perfect < 0
                || double.IsNaN(good) || double.IsInfinity(good) || good < perfect
                || double.IsNaN(ok) || double.IsInfinity(ok) || ok < good)
                throw new ArgumentException("Timing windows must be finite and ordered: 0 <= Perfect <= Good <= OK.");
        }
    }

    [Serializable]
    public sealed class ScoreSettings
    {
        public int perfect = 1000;
        public int good = 500;
        public int ok = 100;

        public void Validate()
        {
            if (perfect < 0 || good < 0 || ok < 0)
                throw new ArgumentException("Base note points cannot be negative.");
        }
    }
}
