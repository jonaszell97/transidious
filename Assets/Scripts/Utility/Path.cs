using System.Collections;
using System.Collections.Generic;
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

            /// A cubic bezier curve.
            CubicBezier,
        }

        [System.Serializable]
        public struct SerializablePathSegment
        {
            public Kind kind;
            public SerializableVector3 p0, p1, p2, p3;
            public float length;
        }

        public Kind kind;
        public Vector3 p0, p1, p2, p3;
        public float length;

        public float Angle
        {
            get
            {
                return Math.toDegrees((float)System.Math.Atan2(p3.y - p0.y, p3.x - p0.x));
            }
        }

        public Vector3 Direction
        {
            get
            {
                return p3 - p0;
            }
        }

        public bool IsVertical
        {
            get
            {
                if (kind != Kind.StraightLine)
                {
                    return false;
                }

                return Direction.x.Equals(0);
            }
        }

        public bool IsHorizontal
        {
            get
            {
                if (kind != Kind.StraightLine)
                {
                    return false;
                }

                return Direction.y.Equals(0);
            }
        }

        public PathSegment(PathSegment seg)
        {
            this.kind = seg.kind;
            this.p0 = seg.p0;
            this.p1 = seg.p1;
            this.p2 = seg.p2;
            this.p3 = seg.p3;
            this.length = seg.length;
        }

        public PathSegment(Vector3 begin, Vector3 end)
        {
            this.kind = Kind.StraightLine;
            this.p0 = begin;
            this.p1 = new Vector3();
            this.p2 = new Vector3();
            this.p3 = end;
            this.length = (end - begin).magnitude;
        }

        public PathSegment(Vector3 begin, Vector3 cp1, Vector3 cp2, Vector3 end)
        {
            this.kind = Kind.CubicBezier;
            this.p0 = begin;
            this.p1 = cp1;
            this.p2 = cp2;
            this.p3 = end;
            this.length = Math.cubicBezierLength(begin, cp1, cp2, end);
        }

        public SerializablePathSegment Serialize()
        {
            return new SerializablePathSegment
            {
                kind = kind,
                p0 = new SerializableVector3(p0),
                p1 = new SerializableVector3(p1),
                p2 = new SerializableVector3(p2),
                p3 = new SerializableVector3(p3),
                length = length
            };
        }

        public static PathSegment Deserialize(SerializablePathSegment seg)
        {
            return new PathSegment
            {
                kind = seg.kind,
                p0 = seg.p0.ToVector(),
                p1 = seg.p1.ToVector(),
                p2 = seg.p2.ToVector(),
                p3 = seg.p3.ToVector(),
                length = seg.length
            };
        }
    }

    public class Path
    {
        [System.Serializable]
        public struct SerializedPath
        {
            public List<PathSegment.SerializablePathSegment> segments;
            public float length;
            public float width;
        }

        public readonly List<PathSegment> segments;
        public readonly float length;
        public float width = 0.05f;

        public Path(Path path) : this(new List<PathSegment>(path.segments))
        {

        }

        public Path(List<PathSegment> segments, float length, float width)
        {
            this.segments = segments;
            this.length = length;
            this.width = width;
        }

        public Path(List<PathSegment> segments)
        {
            this.segments = segments;
            this.length = segments.Aggregate(0.0f, (sum, seg) => sum += seg.length);
        }

        public Path(Vector3 begin, Vector3 end)
        {
            this.segments = new List<PathSegment> { new PathSegment(begin, end) };
            this.length = (end - begin).magnitude;
        }

        public float BeginAngle
        {
            get
            {
                return segments[0].Angle;
            }
        }

        public float EndAngle
        {
            get
            {
                return segments[segments.Count - 1].Angle;
            }
        }

        public Vector3 Start
        {
            get { return segments.First().p0; }
        }

        public Vector3 End
        {
            get { return segments.Last().p3; }
        }

        public Mesh CreateMesh(float z = 0f)
        {
            var positions = new List<Vector3>();
            foreach (PathSegment seg in segments)
            {
                positions.Add(seg.p0);
            }

            positions.Add(segments.Last().p3);
            return MeshBuilder.CreateSmoothLine(positions, width, 10, z);

            /*
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();

            foreach (PathSegment seg in segments)
            {
                switch (seg.kind)
                {
                    case PathSegment.Kind.StraightLine:
                        Vector3 line = seg.p0 - seg.p3;
                        Vector3 normal = new Vector3(-line.y, line.x, 0.0f).normalized;

                        Vector3 bl = seg.p3 - width * normal;
                        Vector3 tl = seg.p3 + width * normal;
                        Vector3 tr = seg.p0 + width * normal;
                        Vector3 br = seg.p0 - width * normal;

                        bl.z = z;
                        tl.z = z;
                        tr.z = z;
                        br.z = z;

                        MeshBuilder.AddQuad(vertices, triangles, normals,
                                            uv, bl, tr, br, tl);

                        break;
                    case PathSegment.Kind.CubicBezier:
                        break;
                }
            }

            return new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                normals = normals.ToArray(),
                uv = uv.ToArray(),
            };*/
        }

        public Mesh CreateJoints()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var first = true;

            foreach (PathSegment seg in segments)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                switch (seg.kind)
                {
                    case PathSegment.Kind.StraightLine:
                        var center = seg.p0;
                        var radius = width;

                        Vector3 bl = new Vector3(center.x - radius, center.y - radius);
                        Vector3 tl = new Vector3(center.x - radius, center.y + radius);
                        Vector3 tr = new Vector3(center.x + radius, center.y + radius);
                        Vector3 br = new Vector3(center.x + radius, center.y - radius);

                        MeshBuilder.AddQuad(vertices, triangles, normals,
                                            uv, bl, tr, br, tl);

                        break;
                    case PathSegment.Kind.CubicBezier:
                        break;
                }
            }

            return new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                normals = normals.ToArray(),
                uv = uv.ToArray(),
            };
        }

        public void AdjustStart(Vector3 start, bool keepHorizontal = true,
                                bool keepVertical = true)
        {
            PathSegment startSegment = segments.First();
            Vector3 diff = start - startSegment.p0;

            if ((keepHorizontal && startSegment.IsHorizontal)
                || (keepVertical && startSegment.IsVertical))
            {
                startSegment.p0 = start;
                startSegment.p3 += diff;
                segments[0] = startSegment;

                // Propagate changes to other segments.
                if (segments.Count > 1)
                {
                    PathSegment seg = segments[1];
                    seg.p0 = startSegment.p3;

                    segments[1] = seg;
                }
            }
            else
            {
                startSegment.p0 = start;
                segments[0] = startSegment;
            }
        }

        public void AdjustEnd(Vector3 end, bool keepHorizontal = true,
                               bool keepVertical = true)
        {
            PathSegment endSegment = segments.Last();
            Vector3 diff = end - endSegment.p3;

            if ((keepHorizontal && endSegment.IsHorizontal)
                || (keepVertical && endSegment.IsVertical))
            {
                endSegment.p3 = end;
                endSegment.p0 += diff;
                segments[segments.Count - 1] = endSegment;

                // Propagate changes to other segments.
                if (segments.Count > 1)
                {
                    PathSegment seg = segments[segments.Count - 2];
                    seg.p0 = endSegment.p3;

                    segments[segments.Count - 2] = seg;
                }
            }
            else
            {
                endSegment.p3 = end;
                segments[segments.Count - 1] = endSegment;
            }
        }

        public void RemoveStartAngle(Vector3 adjustment)
        {
            var first = segments.First();
            if ((first.Angle % 90).Equals(0))
            {
                return;
            }

            PathSegment newStart = new PathSegment(first.p0, first.p0 + adjustment);
            first.p0 = newStart.p3;

            segments.Insert(0, newStart);
            segments[1] = first;
        }

        public void RemoveEndAngle(Vector3 adjustment)
        {
            var last = segments.Last();
            if ((last.Angle % 90).Equals(0))
            {
                return;
            }

            PathSegment newEnd = new PathSegment(last.p3 + adjustment, last.p3);
            last.p3 = newEnd.p0;

            segments.Add(newEnd);
            segments[segments.Count - 2] = last;
        }

        public SerializedPath Serialize()
        {
            return new SerializedPath
            {
                segments = segments.Select(s => s.Serialize()).ToList(),
                length = length,
                width = width,
            };
        }

        public static Path Deserialize(SerializedPath path)
        {
            if (path.segments == null)
            {
                return null;
            }

            return new Path
            (
                path.segments.Select(PathSegment.Deserialize).ToList(),
                path.length,
                path.width
            );
        }
    }
}
