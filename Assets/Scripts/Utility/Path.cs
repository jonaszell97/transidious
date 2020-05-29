using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Transidious;

namespace Transidious
{
    public enum CardinalDirection
    {
        North, South, West, East
    }

    public static class CardinalDirectionExtensions
    {
        public static bool IsHorizontal(this CardinalDirection dir)
        {
            return dir == CardinalDirection.East || dir == CardinalDirection.West;
        }

        public static CardinalDirection RotatedRight(this CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.East;
                case CardinalDirection.South: return CardinalDirection.West;
                case CardinalDirection.East: return CardinalDirection.South;
                case CardinalDirection.West: return CardinalDirection.North;
                default:
                    throw new System.ArgumentException(string.Format("Illegal enum value {0}", dir));
            }
        }

        public static CardinalDirection RotatedLeft(this CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.West;
                case CardinalDirection.South: return CardinalDirection.East;
                case CardinalDirection.East: return CardinalDirection.North;
                case CardinalDirection.West: return CardinalDirection.South;
                default:
                    throw new System.ArgumentException(string.Format("Illegal enum value {0}", dir));
            }
        }
    }

    public struct PathSegment
    {
        public enum Kind
        {
            /// A straight line segment.
            StraightLine,

            /// A line segment with several points.
            Line,

            /// A cubic bezier curve.
            CubicBezier,

            /// A quadratic bezier curve.
            QuadraticBezier,
        }

        public Kind kind;
        public readonly Vector2[] Points;
        public float Length;

        public float Angle => Math.PointAngleDeg(Points.First(), Points.Last());

        public Vector2 Direction => Points.Last() - Points.First();

        public Vector2 StartDirection => (PointAt(.01f) - Points.First()).normalized;

        public bool IsVertical => kind == Kind.StraightLine && Direction.x.Equals(0);

        public bool IsHorizontal => kind == Kind.StraightLine && Direction.y.Equals(0);

        public PathSegment(PathSegment seg)
        {
            this.kind = seg.kind;
            this.Points = seg.Points;
            this.Length = seg.Length;
        }

        public PathSegment(Vector2 begin, Vector2 end)
        {
            this.kind = Kind.StraightLine;
            this.Points = new [] { begin, end };
            this.Length = (end - begin).magnitude;
        }

        public PathSegment(IReadOnlyList<Vector2> points)
        {
            if (points.Count == 2)
            {
                this.kind = Kind.StraightLine;
                this.Points = new [] { points[0], points[1] };
                this.Length = (points[1] - points[0]).magnitude;
                
                return;
            }

            this.kind = Kind.Line;
            this.Points = points.ToArray();
            this.Length = 0f;

            for (int i = 1; i < points.Count; ++i)
            {
                this.Length += (points[i - 1] - points[i]).magnitude;
            }
        }

        public PathSegment(Vector2[] points, float length = 0f)
        {
            this.kind = Kind.Line;
            this.Points = points;
            this.Length = length;

            if (length.Equals(0f))
            {
                for (int i = 1; i < points.Length; ++i)
                {
                    this.Length += (points[i - 1] - points[i]).magnitude;
                }
            }
        }

        public PathSegment(Vector2 begin, Vector2 cp1, Vector2 cp2, Vector2 end)
        {
            this.kind = Kind.CubicBezier;
            this.Points = new[] { begin, cp1, cp2, end };
            this.Length = Math.CubicBezierLength(begin, cp1, cp2, end);
        }

        public PathSegment(Vector2 begin, Vector2 cp, Vector2 end)
        {
            this.kind = Kind.QuadraticBezier;
            this.Points = new[] { begin, cp, end };
            this.Length = Math.QuadraticBezierLength(begin, cp, end);
        }

        /// A point at the specified offset on the path [0..1].
        public Vector2 PointAt(float offset)
        {
            if (offset >= 1f)
            {
                return Points.Last();
            }

            switch (kind)
            {
                case Kind.StraightLine:
                    return Points[0] + (Points[1] - Points[0]) * offset;
                case Kind.Line:
                {
                    var neededLen = offset * Length;
                    var sum = 0f;

                    for (var i = 1; i < Points.Length; ++i)
                    {
                        var dir = (Points[i] - Points[i - 1]);
                        var len = dir.magnitude;
                        if (sum + len > neededLen)
                        {
                            return Points[i - 1] + (dir.normalized * (neededLen - sum));
                        }

                        sum += len;
                    }

                    Debug.LogError("point is not on line");
                    return Points.Last();
                }
                case Kind.CubicBezier:
                    return Math.CubicBezierPoint(offset, Points[0], Points[1], Points[2], Points[3]);
                case Kind.QuadraticBezier:
                    return Math.QuadraticBezierPoint(offset, Points[0], Points[1], Points[2]);
                default:
                    Debug.LogError("invalid path kind");
                    return default;
            }
        }

        public float GetDistanceFromStart(Vector2 pt)
        {
            switch (kind)
            {
                case Kind.StraightLine:
                    return (pt - Points[0]).magnitude;
                case Kind.Line:
                {
                    var sum = 0f;
                    for (var i = 1; i < Points.Length; ++i)
                    {
                        if (!Math.PointOnLine(Points[i - 1], Points[i], pt))
                        {
                            sum += (Points[i - 1] - Points[i]).magnitude;
                            continue;
                        }

                        return sum + (pt - Points[i - 1]).magnitude;
                    }

                    Debug.LogError("point is not on line");
                    return default;
                }
                case Kind.CubicBezier:
                case Kind.QuadraticBezier:
                    Debug.LogError("not supported for bezier curves");
                    return default;
                default:
                    Debug.LogError("invalid path kind");
                    return default;
            }
        }

        public void AddPoints(List<Vector2> result, int bezierSegments)
        {
            switch (kind)
            {
                case Kind.StraightLine:
                case Kind.Line:
                    result.AddRange(Points);
                    break;
                case Kind.CubicBezier:
                {
                    var step = 1f / bezierSegments;
                    for (var t = 0f; t <= 1f; t += step)
                    {
                        result.Add(Math.CubicBezierPoint(t, Points[0], Points[1], Points[2], Points[3]));
                    }
                    
                    return;
                }
                case Kind.QuadraticBezier:
                {
                    var step = 1f / bezierSegments;
                    for (var t = 0f; t <= 1f; t += step)
                    {
                        result.Add(Math.QuadraticBezierPoint(t, Points[0], Points[1], Points[2]));
                    }
                    
                    return;
                }
                default:
                    Debug.LogError("invalid path kind");
                    return;
            }
        }
    }

    public class Path
    {
        public readonly List<PathSegment> Segments;
        public readonly float Length;

        public Path(Path path) : this(new List<PathSegment>(path.Segments))
        {

        }

        public Path(List<PathSegment> segments, float length)
        {
            this.Segments = segments;
            this.Length = length;
        }

        public Path(PathSegment singleSegment)
        {
            this.Segments = new List<PathSegment> { singleSegment };
            this.Length = singleSegment.Length;
        }

        public Path(List<PathSegment> segments)
        {
            this.Segments = segments;
            this.Length = segments.Aggregate(0.0f, (sum, seg) => sum += seg.Length);
        }

        public Path(List<Vector2> points)
        {
            this.Segments = new List<PathSegment>
            {
                new PathSegment(points)
            };

            this.Length = this.Segments[0].Length;
        }

        public Path(Vector2 begin, Vector2 end)
        {
            this.Segments = new List<PathSegment> { new PathSegment(begin, end) };
            this.Length = (end - begin).magnitude;
        }

        public List<Vector2> GetPoints(int bezierSegments = 5)
        {
            var result = new List<Vector2>();
            foreach (var seg in Segments)
            {
                seg.AddPoints(result, bezierSegments);
            }

            return result;
        }

        public float BeginAngle => Segments[0].Angle;

        public float EndAngle => Segments[Segments.Count - 1].Angle;

        public Vector2 Start => Segments.First().Points[0];

        public Vector2 End => Segments.Last().Points.Last();
    }
}
