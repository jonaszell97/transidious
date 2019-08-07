using System;
using UnityEngine;

namespace Transidious
{
    public abstract class Math
    {
        private static void arcLengthUtil(Vector2 A, Vector2 B,
                                          Vector2 C, Vector2 D,
                                          uint subdiv, ref float L)
        {
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
                                              Vector2 p2, Vector2 p3)
        {
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
            return toDegrees(Mathf.Atan2(v1.x * v2.y - v1.y * v2.x, v1.x * v2.x + v1.y * v2.y));
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

        public static Vector3 NearestPointOnLine(Vector3 p0, Vector3 p1, Vector3 pnt)
        {
            return NearestPointOnLine(new Vector2(p0.x, p0.y),
                                      new Vector2(p1.x, p1.y),
                                      new Vector2(pnt.x, pnt.y));
        }

        public static Vector2 NearestPointOnLine(Vector2 p0, Vector2 p1, Vector2 pnt)
        {
            var dir = p1 - p0;
            var lineDir = dir.normalized;

            var v = pnt - p0;
            var d = Vector2.Dot(v, lineDir);

            if (d < 0)
                return p0;

            if (d > dir.magnitude)
                return p1;

            return p0 + lineDir * d;
        }

        public static float DistanceToLine(Vector3 p0, Vector3 p1, Vector3 pnt)
        {
            pnt.z = 0f;

            var nearestPt = NearestPointOnLine(p0, p1, pnt);
            return (nearestPt - pnt).magnitude;
        }

        public static float SqrDistanceToLine(Vector3 p0, Vector3 p1, Vector3 pnt)
        {
            pnt.z = 0f;

            var nearestPt = NearestPointOnLine(p0, p1, pnt);
            return (nearestPt - pnt).sqrMagnitude;
        }

        static float OuterProduct(Vector3 a, Vector3 b, Vector3 p)
        {
            return (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);
        }

        public enum PointPosition
        {
            Left, Right, OnLine
        }

        public static PointPosition GetPointPosition(Vector3 a, Vector3 b, Vector3 p)
        {
            var d = OuterProduct(a, b, p);
            if (d.Equals(0f))
                return PointPosition.OnLine;

            var perp = Vector2.Perpendicular(new Vector3(b.x - a.x, b.y - a.y));
            var leftPt = new Vector3(a.x + perp.x, a.y + perp.y);
            var leftD = OuterProduct(a, b, leftPt);

            if (d < 0f == leftD < 0f)
            {
                return PointPosition.Left;
            }

            return PointPosition.Right;
        }

        public static Vector3 GetMidPoint(Vector3 a, Vector3 b)
        {
            var diff = b - a;
            var dist = diff.magnitude;

            return a + (diff.normalized * .5f);
        }

        public static Vector2 GetIntersectionPoint(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2,
                                                   out bool found)
        {
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
            if (tmp == 0)
            {
                found = false;
                return Vector2.zero;
            }

            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
            found = true;

            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }

        public static float NormalizeAngle(float angle)
        {
            if (angle < 0f)
            {
                while (angle < 0f)
                    angle += 180f;
            }
            else if (angle >= 180f)
            {
                while (angle >= 180f)
                    angle -= 180f;
            }

            return angle;
        }

        public static bool EquivalentAngles(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2,
                                            float tolerance = 0f)
        {
            var angle1 = NormalizeAngle(PointAngle(A1, A2));
            var angle2 = NormalizeAngle(PointAngle(B1, B2));

            var angleDiff = Mathf.Abs(angle1 - angle2);
            return angleDiff <= tolerance;
        }

        public static Color ApplyTransparency(Color rgb, float a)
        {
            return new Color(1.0f - a * (1.0f - rgb.r),
                             1.0f - a * (1.0f - rgb.g),
                             1.0f - a * (1.0f - rgb.b));
        }

        public static Color ContrastColor(Color color)
        {
            // https://stackoverflow.com/questions/596216/formula-to-determine-brightness-of-rgb-color
            double luminance = (0.299 * color.r + 0.587 * color.g + 0.114 * color.b);
            if (luminance > 0.5)
            {
                return Color.black;
            }

            return Color.white;
        }

        public static Rect GetWorldBoundingRect(RectTransform rectTransform,
                                                RenderMode renderMode = RenderMode.WorldSpace)
        {
            if (renderMode == RenderMode.WorldSpace)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                return new Rect(corners[0].x, corners[0].y, corners[3].x - corners[0].x, corners[2].y - corners[0].y);
            }
            else
            {
                var transformedPos = Camera.main.ScreenToWorldPoint(new Vector2(
                    rectTransform.position.x, rectTransform.position.y - rectTransform.rect.height));
                var baseSize = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
                var transformedSize = Camera.main.ScreenToWorldPoint(
                    new Vector2(rectTransform.rect.width, rectTransform.rect.height));

                return new Rect(transformedPos.x, transformedPos.y,
                                transformedSize.x - baseSize.x,
                                transformedSize.y - baseSize.y);
            }
        }
    }
}