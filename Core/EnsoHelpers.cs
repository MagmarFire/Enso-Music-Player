using System;

namespace EnsoMusicPlayer
{
    public static class EnsoHelpers
    {
        public static T[] Slice<T>(T[] source, int offset, int length)
        {
            T[] destination = new T[length];
            Array.Copy(source, offset, destination, 0, length);
            return destination;
        }

        public static float CalculateEqualPowerCrossfade(float percent, bool fadingIn)
        {
            float t;

            if (fadingIn)
            {
                t = percent;
            }
            else
            {
                t = 1f - percent;
            }

            return (float)Math.Cos(t / 2 * Math.PI);
        }
    }
}
