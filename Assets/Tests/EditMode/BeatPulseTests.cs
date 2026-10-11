using BopBeat.Gameplay;
using NUnit.Framework;

namespace BopBeat.Tests
{
    public sealed class BeatPulseTests
    {
        [Test]
        public void TempoAndOffsetFollowAbsoluteSongTimeWithoutDrift()
        {
            Assert.AreEqual(1, BeatPulse.Scale(-0.1, 120, 0, false));
            Assert.AreEqual(1.08f, BeatPulse.Scale(0, 120, 0, false), 0.00001);
            Assert.AreEqual(1.08f, BeatPulse.Scale(0.5, 120, 0, false), 0.00001);
            Assert.AreEqual(1, BeatPulse.Scale(0.25, 120, 0, false));
            Assert.AreEqual(1.08f, BeatPulse.Scale(600.5, 120, 0, false), 0.00001);
            Assert.AreEqual(1.08f, BeatPulse.Scale(0.25, 240, 0, false), 0.00001);
            Assert.AreEqual(1, BeatPulse.Scale(1.9, 120, 2, false));
            Assert.AreEqual(1.08f, BeatPulse.Scale(2.5, 120, 2, false), 0.00001);
            Assert.AreEqual(1, BeatPulse.Scale(2.5, 120, 2, true));
        }

        [Test]
        public void PulseReturnsSmoothlyToNormalAndInvalidTempoCannotBreakTheView()
        {
            Assert.AreEqual(1.02f, BeatPulse.Scale(0.09, 120, 0, false), 0.00001);
            Assert.AreEqual(1, BeatPulse.Scale(0.18, 120, 0, false));
            Assert.AreEqual(1, BeatPulse.Scale(0, 0, 0, false));
            Assert.AreEqual(1, BeatPulse.Scale(0, double.NaN, 0, false));
            Assert.AreEqual(1, BeatPulse.Scale(0, double.PositiveInfinity, 0, false));
        }
    }
}
