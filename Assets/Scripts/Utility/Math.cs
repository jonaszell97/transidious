using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;
using UnityEngine;

namespace Transidious
{
    public abstract class Math
    {
        public static readonly float Kph2Mps = 1f / 3.6f;
        public static readonly float Mps2Kph = 3.6f;

        public static readonly float TwoPI = 2f * Mathf.PI;
        public static readonly float HalfPI = .5f * Mathf.PI;
        public static readonly float ThreeHalvesPI = 1.5f * Mathf.PI;

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

        public static float CubicBezierLength(Vector2 p0, Vector2 p1,
                                              Vector2 p2, Vector2 p3)
        {
            float len = 0.0f;
            arcLengthUtil(p0, p1, p2, p3, 5, ref len);

            return len;
        }

        public static Vector2 CubicBezierPoint(float t,
                                               Vector2 startPt, Vector2 controlPt1,
                                               Vector2 controlPt2, Vector2 endPt)
        {
            var B0_t = Mathf.Pow((1 - t), 3);
            var B1_t = 3 * t * Mathf.Pow((1 - t), 2);
            var B2_t = 3 * Mathf.Pow(t, 2) * (1 - t);
            var B3_t = Mathf.Pow(t, 3);

            var x = (B0_t * startPt.x) + (B1_t * controlPt1.x) + (B2_t * controlPt2.x)
                    + (B3_t * endPt.x);
            var y = (B0_t * startPt.y) + (B1_t * controlPt1.y) + (B2_t * controlPt2.y)
                    + (B3_t * endPt.y);
            
            return new Vector2(x, y);
        }

        public static float QuadraticBezierLength(Vector2 startPt, Vector2 controlPt, Vector2 endPt)
        {
            const float step = 1f / 8f;
            var prev = startPt;
            var length = 0f;

            for (var t = step; t <= 1f; t += step)
            {
                var pt = QuadraticBezierPoint(t, startPt, controlPt, endPt);
                length += (pt - prev).magnitude;

                prev = pt;
            }
            
            Debug.Assert(prev.Equals(endPt));
            return length;
        }

        public static Vector2 QuadraticBezierPoint(float t, Vector2 startPt,
                                                   Vector2 controlPt, Vector2 endPt)
        {
            var x = (1 - t) * (1 - t) * startPt.x + 2 * (1 - t) * t * controlPt.x
                                                  + t * t * endPt.x;
            var y = (1 - t) * (1 - t) * startPt.y + 2 * (1 - t) * t * controlPt.y
                                                  + t * t * endPt.y;
            
            return new Vector2(x, y);
        }

        public static float MaxAbs(float v1, float v2)
        {
            return Mathf.Abs(v1) >= Mathf.Abs(v2) ? v1 : v2;
        }

        public static float MinAbs(float v1, float v2)
        {
            return Mathf.Abs(v1) <= Mathf.Abs(v2) ? v1 : v2;
        }

        public static float PointAngleDeg(Vector3 p0, Vector3 p3)
        {
            return PointAngleRad(p0, p3) * Mathf.Rad2Deg;
        }

        public static float PointAngleRad(Vector3 p0, Vector3 p3)
        {
            return Mathf.Atan2(p3.y - p0.y, p3.x - p0.x);
        }

        public static float AngleFromHorizontalAxis(Vector2 v)
        {
            var signedAngle = Vector2.SignedAngle(Vector2.right, v);
            return (signedAngle + 360) % 360;
        }

        public static float DirectionalAngleDeg(Vector2 v1, Vector2 v2)
        {
            return DirectionalAngleRad(v1, v2) * Mathf.Rad2Deg;
        }

        public static float DirectionalAngleRad(Vector2 v1, Vector2 v2)
        {
            // angle = atan2(vector2.y, vector2.x) - atan2(vector1.y, vector1.x);
            var angle = Mathf.Atan2(v2.y, v2.x) - Mathf.Atan2(v1.y, v1.x);
            if (angle < 0f)
                angle += 2f * Mathf.PI;

            return angle;
        }

        public static bool IsStraightAngleRad(Vector2 v1, Vector2 v2, float tolerance)
        {
            var angle = DirectionalAngleRad(v1, v2);
            return Mathf.Abs(Mathf.PI - angle) <= tolerance;
        }
        
        public static bool IsStraightAngleDeg(Vector2 v1, Vector2 v2, float tolerance)
        {
            var angle = DirectionalAngleDeg(v1, v2);
            return Mathf.Abs(180f - angle) <= tolerance;
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

        public static bool PointOnLine(Vector2 a, Vector2 b, Vector2 pt)
        {
            return Mathf.Approximately((pt - a).magnitude + (pt - b).magnitude, (a - b).magnitude);
        }

        public static Vector2 GetPointOnCircleClockwiseRad(Vector2 center, float radius, float angleRad)
        {
            var x = radius * Mathf.Sin(angleRad);
            var y = radius * Mathf.Cos(angleRad);

            return center + new Vector2(x, y);
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

        public static PointPosition GetPointPosition(Vector3 p, PointOnStreet pos)
        {
            if (pos.street.IsOneWay)
                return PointPosition.Right;

            var a = pos.street.drivablePositions[pos.prevIdx];
            var b = pos.street.drivablePositions[pos.prevIdx + 1];

            return GetPointPosition(a, b, p);
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

        public static Vector2 GetIntersectionPoint(Ray2D ray, Vector2 a, Vector2 b, out bool found)
        {
            var o = ray.origin;
            var d = ray.direction;

            var v1 = o - a;
            var v2 = b - a;
            var v3 = new Vector2(-d.y, d.x);

            var dot = Vector2.Dot(v2, v3);
            if (Mathf.Abs(dot) < 0.000001)
            {
                found = false;
                return Vector2.zero;
            }

            var t1 = Vector3.Cross(v2, v1).magnitude / dot;
            var t2 = Vector2.Dot(v1, v3) / dot;

            found = t1 >= 0f && 0f <= t2 && t2 <= 1f;
            return o + d * t1;
        }

        public static float PathLength(IReadOnlyList<Vector3> path)
        {
            float length = 0f;
            for (var i = 1; i < path.Count; ++i)
                length += (path[i] - path[i - 1]).magnitude;
            
            return length;
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
            var angle1 = NormalizeAngle(PointAngleDeg(A1, A2));
            var angle2 = NormalizeAngle(PointAngleDeg(B1, B2));

            var angleDiff = Mathf.Abs(angle1 - angle2);
            return angleDiff <= tolerance || 180f - angleDiff <= tolerance;
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

        public static Color DarkenColor(Color c, float factor)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);

            v = Mathf.Max(0f, v - factor);
            return Color.HSVToRGB(h, s, v);
        }

        public static Color BrightenColor(Color c, float factor)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);

            v = Mathf.Min(1f, v + factor);
            return Color.HSVToRGB(h, s, v);
        }

        public static Rect GetWorldBoundingRect(RectTransform rectTransform,
                                                RenderMode renderMode = RenderMode.WorldSpace)
        {
            if (renderMode == RenderMode.WorldSpace)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
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

        public static Rect GetBoundingRect(IReadOnlyList<Vector2> points)
        {
            Debug.Assert(points.Count >= 2);

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var pt in points)
            {
                minX = Mathf.Min(minX, pt.x);
                maxX = Mathf.Max(maxX, pt.x);
                minY = Mathf.Min(minY, pt.y);
                maxY = Mathf.Max(maxY, pt.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        // Returns a new list of points representing the convex hull of
        // the given set of points. The convex hull excludes collinear points.
        // This algorithm runs in O(n log n) time.
        public static IList<Vector2> MakeHull(IList<Vector2> points)
        {
            List<Vector2> newVector2s = new List<Vector2>(points);
            newVector2s.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : (a.x > b.x ? 1 : -1));

            return MakeHullPresorted(newVector2s);
        }

        public static IList<Vector2> MakeHull(IList<Vector3> points)
        {
            List<Vector2> newVector2s = points.Select(v => (Vector2)v).ToList();
            newVector2s.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : (a.x > b.x ? 1 : -1));

            return MakeHullPresorted(newVector2s);
        }


        // Returns the convex hull, assuming that each points[i] <= points[i + 1]. Runs in O(n) time.
        public static IList<Vector2> MakeHullPresorted(IList<Vector2> points)
        {
            if (points.Count <= 1)
                return new List<Vector2>(points);

            // Andrew's monotone chain algorithm. Positive y coordinates correspond to "up"
            // as per the mathematical convention, instead of "down" as per the computer
            // graphics convention. This doesn't affect the correctness of the result.

            List<Vector2> upperHull = new List<Vector2>();
            foreach (Vector2 p in points)
            {
                while (upperHull.Count >= 2)
                {
                    Vector2 q = upperHull[upperHull.Count - 1];
                    Vector2 r = upperHull[upperHull.Count - 2];
                    if ((q.x - r.x) * (p.y - r.y) >= (q.y - r.y) * (p.x - r.x))
                        upperHull.RemoveAt(upperHull.Count - 1);
                    else
                        break;
                }
                upperHull.Add(p);
            }
            upperHull.RemoveAt(upperHull.Count - 1);

            IList<Vector2> lowerHull = new List<Vector2>();
            for (int i = points.Count - 1; i >= 0; i--)
            {
                Vector2 p = points[i];
                while (lowerHull.Count >= 2)
                {
                    Vector2 q = lowerHull[lowerHull.Count - 1];
                    Vector2 r = lowerHull[lowerHull.Count - 2];
                    if ((q.x - r.x) * (p.y - r.y) >= (q.y - r.y) * (p.x - r.x))
                        lowerHull.RemoveAt(lowerHull.Count - 1);
                    else
                        break;
                }
                lowerHull.Add(p);
            }

            lowerHull.RemoveAt(lowerHull.Count - 1);

            if (!(upperHull.Count == 1 && Enumerable.SequenceEqual(upperHull, lowerHull)))
                upperHull.AddRange(lowerHull);

            return upperHull;
        }

        public static bool IsPointInPolygon(Vector2 pt, Vector2[][] polys)
        {
            foreach (var poly in polys)
            {
                if (IsPointInPolygon(pt, poly))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPointInPolygon(Vector2 pt, IReadOnlyList<Vector2> poly)
        {
#if DEBUG
            if (poly.Count < 3)
            {
                Debug.LogWarning("invalid polygon");
                return false;
            }
#endif

            var inside = false;
            var j = poly.Count - 1;
            for (int i = 0; i < poly.Count; j = i++)
            {
                // what the fuck
                inside ^= poly[i].y > pt.y ^ poly[j].y > pt.y
                    && pt.x < (poly[j].x - poly[i].x) * (pt.y - poly[i].y) / (poly[j].y - poly[i].y)
                        + poly[i].x;
            }

            return inside;
        }

        public static bool IsPointInPolygon(Vector2 pt, Vector2[] poly)
        {
#if DEBUG
            if (poly.Length < 3)
            {
                Debug.LogWarning("invalid polygon");
                return false;
            }
#endif

            var inside = false;
            var j = poly.Length - 1;
            for (int i = 0; i < poly.Length; j = i++)
            {
                // what the fuck
                inside ^= poly[i].y > pt.y ^ poly[j].y > pt.y
                    && pt.x < (poly[j].x - poly[i].x) * (pt.y - poly[i].y) / (poly[j].y - poly[i].y)
                        + poly[i].x;
            }

            return inside;
        }

        enum CrossingType
        {
            Upward,
            Downward,
            None,
        }

        static CrossingType GetCrossing(Ray2D ray, Vector2 e1, Vector2 e2,
                                        out bool strictlyRight, out bool strictlyLeft)
        {
            strictlyRight = false;
            strictlyLeft = false;

            // Rule #3: horizontal edges are excluded
            if (e1.y.Equals(e2.y))
            {
                return CrossingType.None;
            }

            var pt = GetIntersectionPoint(ray, e1, e2, out bool found);
            if (!found)
            {
                return CrossingType.None;
            }

            strictlyRight = pt.x > ray.origin.x;
            strictlyLeft = pt.x < ray.origin.x;

            // Rule #1: an upward edge includes its starting endpoint, and excludes its final endpoint
            if (e2.y > e1.y)
            {
               if (pt.Equals(e2))
               {
                   return CrossingType.None;
               }

                return CrossingType.Upward;
            }

            // Rule #2: a downward edge excludes its starting endpoint, and includes its final endpoint
            Debug.Assert(e2.y < e1.y);

            if (pt.Equals(e1))
            {
                return CrossingType.None;
            }

            return CrossingType.Downward;
        }

        // See http://geomalgorithms.com/a03-_inclusion.html
        public static int WindingNumber(Vector2 pt, IReadOnlyList<Vector3> poly)
        {
            if (!poly.Last().Equals(poly.First()))
            {
                var tmp = poly.ToList();
                tmp.Add(poly.First());
                poly = tmp;
            }

            var wn = 0;
            var ray = new Ray2D(pt, Vector2.right);

            //    for (each edge E[i]:V[i]V[i + 1] of the polygon)
            for (var i = 1; i < poly.Count; ++i)
            {
                var e1 = poly[i - 1];
                var e2 = poly[i];
                var crossing = GetCrossing(ray, e1, e2, out bool strictlyRight, out bool strictlyLeft);

                //    if (E[i] crosses upward ala Rule #1)  
                if (crossing == CrossingType.Upward)
                {
                    //    if (P is strictly left of E[i])    // Rule #4
                    if (strictlyLeft)
                    {
                        ++wn;   // a valid up intersect right of P.x
                    }
                }

                //    else if (E[i] crosses downward ala Rule  #2)
                else if (crossing == CrossingType.Downward)
                {
                    //    if (P is  strictly right of E[i])   // Rule #4
                    if (strictlyRight)
                    {
                        --wn;   // a valid down intersect right of P.x
                    }
                }
            }

            return wn; // =0 <=> P is outside the polygon
        }

        public static int WindingNumber(Vector2 pt, IReadOnlyList<Vector2> poly)
        {
            return WindingNumber(pt, poly.Select(v => (Vector2)v).ToArray());
        }

        public static float GetAreaOfPolygon(IReadOnlyList<Vector2> poly)
        {
            return GetAreaOfPolygon(poly, out bool _);
        }

        public static float GetAreaOfPolygon(IReadOnlyList<Vector2> poly, out bool isCounterClockwise)
        {
            var sum = 0f;
            for (var i = 1; i <= poly.Count; ++i)
            {
                var p0 = poly[i - 1];
                Vector2 p1;
                if (i == poly.Count)
                {
                    p1 = poly[0];
                }
                else
                {
                    p1 = poly[i];
                }

                sum += (p0.x * p1.y - p0.y * p1.x);
            }

            if (sum < 0f)
            {
                isCounterClockwise = true;
                sum = -sum;
            }
            else
            {
                isCounterClockwise = false;
            }

            return sum * .5f;
        }

        public static Vector2 GetCentroid(IReadOnlyList<Vector2> poly)
        {
            var accumulatedArea = 0.0f;
            var centerX = 0.0f;
            var centerY = 0.0f;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
                accumulatedArea += temp;
                centerX += (poly[i].x + poly[j].x) * temp;
                centerY += (poly[i].y + poly[j].y) * temp;
            }

            if (Mathf.Abs(accumulatedArea) < 1E-7f)
                return Vector2.zero;

            accumulatedArea *= 3f;
            return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        public static Vector2 GetCentroid(IReadOnlyList<Vector3> poly)
        {
            var accumulatedArea = 0.0f;
            var centerX = 0.0f;
            var centerY = 0.0f;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
                accumulatedArea += temp;
                centerX += (poly[i].x + poly[j].x) * temp;
                centerY += (poly[i].y + poly[j].y) * temp;
            }

            if (Mathf.Abs(accumulatedArea) < 1E-7f)
                return Vector2.zero;

            accumulatedArea *= 3f;
            return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
        }

        public static Vector2 RandomPointInPolygon(IReadOnlyList<Vector3> poly)
        {
            return RandomPointInPolygon(poly.Select(v => (Vector2)v).ToArray());
        }

        public static Vector2 RandomPointInPolygon(IReadOnlyList<Vector2> poly)
        {
            var boundingBox = GetBoundingRect(poly);
            var tries = 0;

            while (true)
            {
                var x = RNG.Next(boundingBox.x, boundingBox.x + boundingBox.width);
                var y = RNG.Next(boundingBox.y, boundingBox.y + boundingBox.height);
                var pt = new Vector2(x, y);

                if (IsPointInPolygon(pt, poly))
                {
                    return pt;
                }

                if (++tries > 1000)
                {
                    Debug.LogError("could not find point in polygon!");
                    return Vector2.zero;
                }
            }
        }

        static void DBSCAN_RangeQuery(ref List<Vector2> neighbors,
                                      IList<Vector2> points,
                                      Vector2 Q,
                                      float eps)
        {
            // RangeQuery(DB, distFunc, Q, eps) {
            //     Neighbors = empty list
            //     for each point P in database DB {   /* Scan all points in the database */
            //         if distFunc(Q, P) ≤ eps then {  /* Compute distance and check epsilon */
            //             Neighbors = Neighbors ∪ {P} /* Add to result */
            //         }
            //     }
            //     return Neighbors
            // }

            foreach (var P in points)
            {
                var magnitude = (P - Q).magnitude;
                if (magnitude > 0f && magnitude <= eps)
                {
                    neighbors.Add(P);
                }
            }
        }

        public static List<List<Vector2>> Cluster_DBSCAN(IList<Vector2> points, float eps, int minPts)
        {
            //  DBSCAN(DB, distFunc, eps, minPts) {
            // C = 0 /* Cluster counter */
            var C = 0;
            var labelMap = new Dictionary<Vector2, int>();
            var neighbors = new List<Vector2>();
            var innerNeighbors = new List<Vector2>();

            // for each point P in database DB {
            foreach (var P in points)
            {
                //  if label(P) ≠ undefined then continue /* Previously processed in inner loop */
                if (labelMap.ContainsKey(P))
                {
                    continue;
                }

                // Neighbors N = RangeQuery(DB, distFunc, P, eps) /* Find neighbors */
                neighbors.Clear();
                DBSCAN_RangeQuery(ref neighbors, points, P, eps);

                // if |N| < minPts then { /* Density check */
                //   label(P) = Noise /* Label as Noise */
                //   continue
                //  }
                if (neighbors.Count + 1 < minPts)
                {
                    labelMap.Add(P, -1);
                    continue;
                }

                // C = C + 1 /* next cluster label */
                ++C;

                // label(P) = C /* Label initial point */
                labelMap.Add(P, C);

                // Seed set S = N \ {P} /* Neighbors to expand */
                // (implicitly done)

                // for each point Q in S { /* Process every seed point */
                for (var i = 0; i < neighbors.Count; ++i)
                {
                    var Q = neighbors[i];

                    // if label(Q) = Noise then label(Q) = C /* Change Noise to border point */
                    var hasKey = labelMap.ContainsKey(Q);
                    if (hasKey && labelMap[Q] == -1)
                    {
                        labelMap[Q] = C;
                    }
                    // if label(Q) ≠ undefined then continue /* Previously processed */
                    else if (hasKey)
                    {
                        continue;
                    }
                    else
                    {
                        // label(Q) = C /* Label neighbor */
                        labelMap.Add(Q, C);
                    }

                    // Neighbors N = RangeQuery(DB, distFunc, Q, eps)   /* Find neighbors */
                    innerNeighbors.Clear();
                    DBSCAN_RangeQuery(ref innerNeighbors, points, Q, eps);

                    // if |N| ≥ minPts then {                          /* Density check */
                    //   S = S ∪ N                                    /* Add new neighbors to seed set */
                    // }
                    if (innerNeighbors.Count >= minPts)
                    {
                        neighbors.AddRange(innerNeighbors);
                    }
                }
            }

            var clusters = new List<List<Vector2>>();
            for (var i = 0; i < C; ++i)
            {
                clusters.Add(new List<Vector2>());
            }

            foreach (var P in points)
            {
                var cluster = labelMap[P];
                if (cluster == -1)
                {
                    clusters.Add(new List<Vector2> { P });
                    continue;
                }

                clusters[C - 1].Add(P);
            }

            return clusters;
        }

        static readonly int ClipPrecision = 1000;

        static ClipperLib.IntPoint GetIntPoint(Vector2 v)
        {
            return new ClipperLib.IntPoint(v.x * ClipPrecision, v.y * ClipPrecision);
        }

        static List<ClipperLib.IntPoint> GetPath(PSLG pslg)
        {
            var list = new List<ClipperLib.IntPoint>();
            foreach (var pt in pslg.vertices)
            {
                list.Add(GetIntPoint(pt));
            }

            return list;
        }

        static List<ClipperLib.IntPoint> GetPath(Vector2[] poly)
        {
            var list = new List<ClipperLib.IntPoint>();
            foreach (var pt in poly)
            {
                list.Add(GetIntPoint(pt));
            }

            return list;
        }

        static Vector2[] GetUnityPath(List<ClipperLib.IntPoint> path)
        {
            return path.Select(p => new Vector2((float)p.X / (float)ClipPrecision, (float)p.Y / (float)ClipPrecision)).ToArray();
        }

        static void PopulateResultPSLG(PSLG pslg, ClipperLib.PolyNode node, bool hole)
        {
            if (node.IsOpen)
            {
                Debug.LogWarning("FOUND OPEN NODE");
            }

            if (hole)
            {
                pslg.AddHole(GetUnityPath(node.Contour));
            }
            else
            {
                pslg.AddOrderedVertices(GetUnityPath(node.Contour));
            }

            foreach (var child in node.Childs)
            {
                PopulateResultPSLG(pslg, child, !hole);
            }
        }

        public static PSLG PolygonUnion(IReadOnlyList<PSLG> pslgs)
        {
            var clipper = new ClipperLib.Clipper();
            foreach (var pslg in pslgs)
            {
                clipper.AddPath(GetPath(pslg), ClipperLib.PolyType.ptSubject, true);

                foreach (var hole in pslg.holes)
                {
                    clipper.AddPath(GetPath(hole), ClipperLib.PolyType.ptSubject, true);
                }
            }

            var solution = new ClipperLib.PolyTree();
            if (!clipper.Execute(ClipperLib.ClipType.ctUnion, solution))
            {
                return null;
            }

            var result = new PSLG();
            foreach (var node in solution.Childs)
            {
                PopulateResultPSLG(result, node, false);
            }

            return result;
        }

        public static PSLG PolygonUnion(IEnumerable<Vector2[]> polys)
        {
            var clipper = new ClipperLib.Clipper();
            foreach (var poly in polys)
            {
                clipper.AddPath(GetPath(poly), ClipperLib.PolyType.ptSubject, true);
            }

            var solution = new ClipperLib.PolyTree();
            if (!clipper.Execute(ClipperLib.ClipType.ctUnion, solution))
            {
                return null;
            }

            var result = new PSLG();
            foreach (var node in solution.Childs)
            {
                PopulateResultPSLG(result, node, false);
            }

            return result;
        }

        public static PSLG PolygonDiff(PSLG subject, PSLG clip)
        {
            var clipper = new ClipperLib.Clipper();

            clipper.AddPath(GetPath(subject), ClipperLib.PolyType.ptSubject, true);
            foreach (var hole in subject.holes)
            {
                clipper.AddPath(GetPath(hole), ClipperLib.PolyType.ptSubject, true);
            }

            clipper.AddPath(GetPath(clip), ClipperLib.PolyType.ptClip, true);
            foreach (var hole in clip.holes)
            {
                clipper.AddPath(GetPath(hole), ClipperLib.PolyType.ptClip, true);
            }
            
            var solution = new ClipperLib.PolyTree();
            if (!clipper.Execute(ClipperLib.ClipType.ctDifference, solution))
            {
                return null;
            }

            var result = new PSLG(subject.z);
            foreach (var node in solution.Childs)
            {
                PopulateResultPSLG(result, node, false);
            }

            return result;
        }
    }
}