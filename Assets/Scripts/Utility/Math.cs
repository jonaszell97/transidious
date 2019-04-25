using System;
using UnityEngine;

namespace Transidious
{
    public abstract class Math
    {
        private static void arcLengthUtil(Vector2 A, Vector2 B,
                                          Vector2 C, Vector2 D,
                                          uint subdiv, ref float L) {
            if (subdiv > 0)
            {
                Vector2 a = A + (B - A) * 0.5f;
                Vector2 b = B + (C - B) * 0.5f;
                Vector2 c = C + (D - C) * 0.5f;
                Vector2 d = a + (b - a) * 0.5f;
                Vector2 e = b + (c - b) * 0.5f;
                Vector2 f = d + (e - d) * 0.5f;

                // left branch
                arcLengthUtil(A, a, d, f, subdiv - 1, ref L);

                // right branch
                arcLengthUtil(f, e, c, D, subdiv - 1, ref L);
            }
            else
            {
                float controlNetLength = (B - A).magnitude + (C - B).magnitude + (D - C).magnitude;
                float chordLength = (D - A).magnitude;
                L += (chordLength + controlNetLength) / 2.0f;
            }
        }

        public static float cubicBezierLength(Vector2 p0, Vector2 p1,
                                              Vector2 p2, Vector2 p3) {
            float len = 0.0f;
            arcLengthUtil(p0, p1, p2, p3, 5, ref len);

            return len;
        }

        public static float toDegrees(float radians)
        {
            return radians * (180f / Mathf.PI);
        }

        public static float toRadians(float degrees)
        {
            return degrees / (180f / Mathf.PI);
        }

        public static float PointAngle(Vector3 p0, Vector3 p3)
        {
            return Math.toDegrees((float)System.Math.Atan2(p3.y - p0.y, p3.x - p0.x));
        }

        public static float Angle(Vector2 v1, Vector2 v2)
        {
            return toDegrees(Mathf.Atan2(v1.x*v2.y - v1.y*v2.x, v1.x*v2.x + v1.y*v2.y));
        }

        public static CardinalDirection ClassifyDirection(float angle)
        {
            if (angle < 0f)
            {
                angle = 360 + angle;
            }

            if (angle >= 315f || angle < 45f)
            {
                return CardinalDirection.East;
            }
            if (angle >= 45f && angle < 135f)
            {
                return CardinalDirection.North;
            }
            if (angle >= 135f && angle < 225f)
            {
                return CardinalDirection.West;
            }

            return CardinalDirection.South;
        }

        public static Vector3 DirectionVector(CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North:
                    return Vector3.up;
                case CardinalDirection.South:
                    return Vector3.down;
                case CardinalDirection.East:
                    return Vector3.right;
                case CardinalDirection.West:
                    return Vector3.left;
                default:
                    throw new System.ArgumentException(string.Format("Illegal enum value {0}", dir));
            }
        }

        public static CardinalDirection Reverse(CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North:
                    return CardinalDirection.South;
                case CardinalDirection.South:
                    return CardinalDirection.North;
                case CardinalDirection.East:
                    return CardinalDirection.West;
                case CardinalDirection.West:
                    return CardinalDirection.East;
                default:
                    throw new System.ArgumentException(string.Format("Illegal enum value {0}", dir));
            }
        }
    }
}