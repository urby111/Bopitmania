using System;

namespace BopBeat.Gameplay
{
    /// <summary>Song-time geometry, using screen coordinates that increase downward.</summary>
    public static class NoteTravel
    {
        public static double TravelSeconds(double speed) => 1 / speed;

        public static double Position(double targetTime, double songTime, double speed, double top, double target)
            => target - (targetTime - songTime) * (target - top) * speed;

        public static double HoldLength(double startTime, double endTime, double speed, double distance)
            => Math.Max(0, endTime - startTime) * distance * speed;

        // A tap occupies 40 ms of travel distance. Clamp only its visual thickness.
        public static double BlockHeight(double speed, double distance)
            => Math.Max(8, Math.Min(96, distance * speed * 0.04));
    }
}
