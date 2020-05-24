using UnityEngine;

namespace Transidious
{
    public static class RNG
    {
        /// The seeded random instance.
        private static System.Random _random;

        /// Initialize the random number generator.
        public static void Reseed(int seed)
        {
            _random = new System.Random(seed);
        }

        /// Generate a random float between 0 and 1.
        public static float value => (float) _random.NextDouble();

        /// Generate a random int.
        public static float intValue => _random.Next();

        /// Generate a float in a range.
        public static float Next(float min, float max)
        {
            return min + value * (max - min);
        }

        /// Generate an integer in a range.
        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// Generate a Vector2 in a range.
        public static Vector2 Vector2(float minX, float maxX,
                                      float minY, float maxY)
        {
            return new Vector2(Next(minX, maxX), Next(minY, maxY));
        }

        /// Generate a Vector3 in a range.
        public static Vector3 Vector3(float minX, float maxX,
                                      float minY, float maxY,
                                      float z = 0f)
        {
            return new Vector3(Next(minX, maxX), Next(minY, maxY), z);
        }

        /// Return a random element from a collection.
        public static T RandomElement<T>(System.Collections.Generic.List<T> coll)
        {
            return coll[Next(0, coll.Count)];
        }

        /// Return a random element from a collection.
        public static T RandomElement<T>(T[] coll)
        {
            return coll[Next(0, coll.Length)];
        }
        
        /// Return a random color.
        public static Color RandomColor => new Color(value, value, value);
    }
}