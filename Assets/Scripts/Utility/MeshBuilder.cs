using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    public abstract class MeshBuilder
    {
#if DEBUG
        public static int quadVerts = 0;
        public static int circleVerts = 0;
#endif

        public static void AddQuad(List<Vector3> vertices,
                                   List<int> triangles,
                                   List<Vector3> normals,
                                   List<Vector2> uvs,
                                   Vector3 bl, Vector3 tr, Vector3 br, Vector3 tl)
        {
            int baseIndex = vertices.Count;

            vertices.Add(bl);
            vertices.Add(tl);
            vertices.Add(tr);
            vertices.Add(br);

            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);
            normals.Add(-Vector3.forward);

            // We need a clockwise winding order.
            triangles.Add(0 + baseIndex);
            triangles.Add(1 + baseIndex);
            triangles.Add(2 + baseIndex);
            triangles.Add(2 + baseIndex);
            triangles.Add(3 + baseIndex);
            triangles.Add(0 + baseIndex);

#if DEBUG
            quadVerts += 6;
#endif

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
        }

        public static void AddQuadraticBezierCurve(List<Vector3> points,
                                                   Vector2 startPt, Vector2 endPt,
                                                   Vector2 controlPt,
                                                   int segments = 15,
                                                   float z = 0f)
        {
            Debug.Assert(segments > 0, "segment count must be positive!");
            points.Add(startPt);

            var tStep = 1f / segments;
            for (float t = tStep; t < 1f; t += tStep)
            {
                var x = (1 - t) * (1 - t) * startPt.x + 2 * (1 - t) * t * controlPt.x
                    + t * t * endPt.x;
                var y = (1 - t) * (1 - t) * startPt.y + 2 * (1 - t) * t * controlPt.y
                    + t * t * endPt.y;

                points.Add(new Vector3(x, y, z));
            }

            points.Add(endPt);
        }

        public static void AddCubicBezierCurve(List<Vector3> points,
                                               Vector2 startPt, Vector2 endPt,
                                               Vector2 controlPt1, Vector2 controlPt2,
                                               int segments = 15, float z = 0f)
        {
            Debug.Assert(segments > 0, "segment count must be positive!");
            points.Add(startPt);

            var tStep = 1f / segments;
            for (float t = tStep; t < 1f; t += tStep)
            {
                var B0_t = Mathf.Pow((1 - t), 3);
                var B1_t = 3 * t * Mathf.Pow((1 - t), 2);
                var B2_t = 3 * Mathf.Pow(t, 2) * (1 - t);
                var B3_t = Mathf.Pow(t, 3);

                var x = (B0_t * startPt.x) + (B1_t * controlPt1.x) + (B2_t * controlPt2.x)
                    + (B3_t * endPt.x);
                var y = (B0_t * startPt.y) + (B1_t * controlPt1.y) + (B2_t * controlPt2.y)
                    + (B3_t * endPt.y);

                points.Add(new Vector3(x, y, z));
            }

            points.Add(endPt);
        }

        public static int AddSmoothIntersection(IReadOnlyList<Vector3> positions,
                                                int i,
                                                List<Vector3> vertices,
                                                List<int> triangles,
                                                List<Vector2> uvs,
                                                float radius,
                                                int segments,
                                                float z,
                                                bool useZ,
                                                int connectionOffset)
        {
            var baseIndex = vertices.Count;
            var p0 = positions[i - 2];
            var p1 = positions[i - 1];
            var p2 = positions[i];

            var angleDeg = Vector2.SignedAngle(p1 - p0, p2 - p1);

            var cmp = angleDeg.CompareTo(0f);
            if (cmp == 0)
            {
                return 0;
            }

            var goesRight = cmp < 0;

            int fromIdx, toIdx;
            if (goesRight)
            {
                // Bottom left of current quad.
                toIdx = baseIndex - 4;

                // Top left of previous quad.
                fromIdx = baseIndex - 4 - connectionOffset - 3;
            }
            else
            {
                // Bottom right of current quad.
                toIdx = baseIndex - 1;

                // Top right of previous quad.
                fromIdx = baseIndex - 4 - connectionOffset - 2;
            }

            var circleSegmentWidth = 2.5f;
            var totalCircumference = 2 * Mathf.PI * radius;
            var angleDiffDeg = Vector2.Angle(p1 - p0, p2 - p1);
            var circumference = totalCircumference * (angleDiffDeg / 360f);
            var neededSegments = (int)Mathf.Floor(circumference / circleSegmentWidth);

            var center = p1;
            var centerIdx = vertices.Count;
            vertices.Add(new Vector3(center.x, center.y, useZ ? z : 0f));
            
            // Just add a straight line, the connection is too short to notice.
            if (neededSegments <= 1)
            {
                // Clockwise
                triangles.Add(centerIdx);

                if (goesRight)
                {
                    triangles.Add(fromIdx);
                    triangles.Add(toIdx);
                }
                else
                {
                    triangles.Add(toIdx);
                    triangles.Add(fromIdx);
                }

#if DEBUG
                circleVerts += 3;
#endif

                return 1;
            }

            var prevAngleRad = Math.toRadians(Vector2.Angle(Vector2.right, p1 - p0));
            var stepDeg = angleDiffDeg / neededSegments;
            var stepRad = Math.toRadians(stepDeg);
            baseIndex = vertices.Count;

            for (var j = 1; j < neededSegments; ++j)
            {
                // x = cx + r * cos(a)
                // y = cy + r * sin(a)
                var nextAngle = prevAngleRad + j * stepRad;
                var x = center.x + radius * Mathf.Cos(nextAngle);
                var y = center.y + radius * Mathf.Sin(nextAngle);

                vertices.Add(new Vector3(x, y, z));
            }

            for (var j = 0; j < neededSegments; ++j)
            {
                int idx0, idx1;
                if (j == 0)
                {
                    idx0 = fromIdx;
                    idx1 = baseIndex + j;
                }
                else if (j == neededSegments - 1)
                {
                    idx0 = baseIndex + j;
                    idx1 = toIdx;
                }
                else
                {
                    idx0 = baseIndex + j;
                    idx1 = baseIndex + j + 1;
                }

                triangles.Add(centerIdx);

                if (goesRight)
                {
                    triangles.Add(idx0);
                    triangles.Add(idx1);
                }
                else
                {
                    triangles.Add(idx1);
                    triangles.Add(idx0);
                }

#if DEBUG
                circleVerts += 3;
#endif
            }

            return neededSegments;
        }

        public static void AddCirclePart(List<Vector3> vertices,
                                         List<int> triangles,
                                         List<Vector2> uvs,
                                         Vector3 center,
                                         float radius,
                                         Vector2 direction,
                                         int segments, float z)
        {
            var centerIdx = vertices.Count;
            vertices.Add(center);

            var left = Vector2.Perpendicular(direction).normalized * radius;
            var right = -left;

            var leftIdx = vertices.Count;
            vertices.Add(center + (Vector3)left);

            var rightIdx = vertices.Count;
            vertices.Add(center + (Vector3)right);

            var beginAngleRad = Math.toRadians(Vector2.Angle(Vector2.right, left));
            var stepRad = Mathf.PI / segments;
            var baseIndex = vertices.Count;

            for (var i = 1; i < segments; ++i)
            {
                // x = cx + r * cos(a)
                // y = cy + r * sin(a)
                var nextAngle = beginAngleRad + i * stepRad;
                var x = center.x + radius * Mathf.Cos(nextAngle);
                var y = center.y + radius * Mathf.Sin(nextAngle);

                vertices.Add(new Vector3(x, y, z));
            }

            for (var i = 0; i < segments; ++i)
            {
                int idx0, idx1;
                if (i == 0)
                {
                    idx0 = leftIdx;
                    idx1 = baseIndex + i;
                }
                else if (i == segments - 1)
                {
                    idx0 = baseIndex + i - 1;
                    idx1 = rightIdx;
                }
                else
                {
                    idx0 = baseIndex + i - 1;
                    idx1 = baseIndex + i;
                }

                triangles.Add(centerIdx);
                triangles.Add(idx1);
                triangles.Add(idx0);

#if DEBUG
                circleVerts += 3;
#endif
            }
        }

        static void CreateSmoothLine_AddQuad(IReadOnlyList<Vector3> positions,
                                            int i,
                                            float startWidth,
                                            float endWidth,
                                            bool startCap,
                                            bool endCap,
                                            List<Vector3> vertices,
                                            List<int> triangles,
                                            List<Vector2> uv,
                                            List<Vector3> normals,
                                            int cornerVertices,
                                            float z,
                                            bool useZ,
                                            PolygonCollider2D collider,
                                            Vector2[] colliderPath,
                                            float offset,
                                            ref int connectionOffset,
                                            ref int quads)
        {
            var p0 = positions[i - 1];
            var p1 = positions[i];

            if (p0.Equals(Vector3.positiveInfinity) || p1.Equals(Vector3.positiveInfinity))
            {
                connectionOffset = 0;
                return;
            }

            if (useZ)
            {
                p0 = new Vector3(p0.x, p0.y, z);
                p1 = new Vector3(p1.x, p1.y, z);
            }

            Vector3 line = p0 - p1;
            Vector3 normal = new Vector3(-line.y, line.x, 0f).normalized;

            Vector3 tr = p1 + endWidth * normal;
            Vector3 tl = p1 - endWidth * normal;
            Vector3 br = p0 + startWidth * normal;
            Vector3 bl = p0 - startWidth * normal;

            if (!offset.Equals(0f))
            {
                p0 += offset * normal;
                p1 += offset * normal;

                bl += offset * normal;
                tl += offset * normal;
                tr += offset * normal;
                br += offset * normal;
            }

            if (colliderPath != null)
            {
                var angle = Math.Angle(Vector2.down, normal);
                if (!angle.Equals(0f))
                {
                    // Add right side to forward path.
                    colliderPath[i - 1] = br;

                    // Add left side to backward path.
                    colliderPath[colliderPath.Length - i] = bl;

                    if (i == positions.Count - 1)
                    {
                        // Add right side to forward path.
                        colliderPath[i] = tr;

                        // Add left side to backward path.
                        colliderPath[i + 1] = tl;
                    }
                }
            }

            if (i == 1 && startCap)
            {
                AddCirclePart(vertices, triangles, uv, p0, startWidth, -line, 10, z);
            }

            AddQuad(vertices, triangles, normals, uv, bl, tr, br, tl);
            ++quads;

            if (i > 1 && i < positions.Count - 1 && quads > 1)
            {
                AddCirclePart(vertices, triangles, uv, p1, endWidth, line, 10, z);
                //connectionOffset = AddSmoothIntersection(positions, i, vertices, triangles, uv,
                //                                         endWidth, cornerVertices,
                //                                         z, useZ, connectionOffset);
            }
            else if (i == positions.Count - 1 && endCap)
            {
                AddCirclePart(vertices, triangles, uv, p1, endWidth, line, 10, z);
            }
        }

        static void CreateSmoothLine_AddQuadNew(IReadOnlyList<Vector3> positions,
                                                int i,
                                                float startWidth,
                                                float endWidth,
                                                bool startCap,
                                                bool endCap,
                                                List<Vector3> vertices,
                                                List<int> triangles,
                                                List<Vector2> uv,
                                                List<Vector3> normals,
                                                int cornerVertices,
                                                float z,
                                                bool useZ,
                                                PolygonCollider2D collider,
                                                Vector2[] colliderPath,
                                                float offset)
        {
            var p0 = positions[i - 1];
            var p1 = positions[i];

            if (p0.Equals(Vector3.positiveInfinity) || p1.Equals(Vector3.positiveInfinity))
            {
                return;
            }

            if (useZ)
            {
                p0 = new Vector3(p0.x, p0.y, z);
                p1 = new Vector3(p1.x, p1.y, z);
            }

            var baseIndex = vertices.Count;
            Vector3 line = p0 - p1;
            Vector3 normal = new Vector3(-line.y, line.x, 0f).normalized;

            Vector3 tr = p1 + endWidth * normal;
            Vector3 tl = p1 - endWidth * normal;

            Vector3 bl, br;
            if (baseIndex == 0)
            {
                br = p0 + startWidth * normal;
                bl = p0 - startWidth * normal;
            }
            else
            {
                br = vertices[baseIndex - 2];
                bl = vertices[baseIndex - 3];
            }

            if (!offset.Equals(0f))
            {
                p0 += offset * normal;
                p1 += offset * normal;

                bl += offset * normal;
                tl += offset * normal;
                tr += offset * normal;
                br += offset * normal;
            }

            if (colliderPath != null)
            {
                var angle = Math.Angle(Vector2.down, normal);
                if (!angle.Equals(0f))
                {
                    // Add right side to forward path.
                    colliderPath[i - 1] = br;

                    // Add left side to backward path.
                    colliderPath[colliderPath.Length - i] = bl;

                    if (i == positions.Count - 1)
                    {
                        // Add right side to forward path.
                        colliderPath[i] = tr;

                        // Add left side to backward path.
                        colliderPath[i + 1] = tl;
                    }
                }
            }

            if (i == 1 && startCap)
            {
                // AddCirclePart(vertices, triangles, uv, p0, startWidth, -line, 10, z);
            }

            AddQuad(vertices, triangles, normals, uv, bl, tr, br, tl);

            if (i == positions.Count - 1 && endCap)
            {
                // AddCirclePart(vertices, triangles, uv, p1, endWidth, line, 10, z);
            }
        }

        public static void CreateSmoothLine(IReadOnlyList<Vector3> positions,
                                            float width,
                                            bool startCap,
                                            bool endCap,
                                            List<Vector3> vertices,
                                            List<int> triangles,
                                            List<Vector2> uv,
                                            int cornerVertices = 5,
                                            float z = 0f,
                                            PolygonCollider2D collider = null,
                                            float offset = 0f)
        {
            var normals = new List<Vector3>();
            var useZ = !z.Equals(float.NaN);

            Vector2[] colliderPath = null;
            if (collider != null)
            {
                colliderPath = Enumerable.Repeat(
                    Vector2.positiveInfinity, positions.Count * 2).ToArray();
            }

            var connectionOffset = 0;
            var quads = 0;

            for (int i = 1; i < positions.Count; ++i)
            {
                CreateSmoothLine_AddQuad(positions, i, width, width,
                                         startCap, endCap, vertices,
                                         triangles, uv, normals, cornerVertices, z,
                                         useZ, collider, colliderPath, offset,
                                         ref connectionOffset, ref quads);
            }

            if (collider != null)
            {
                var prevPathCount = collider.pathCount;
                collider.pathCount = prevPathCount + 1;
                collider.SetPath(prevPathCount,
                    colliderPath.Where(v => !v.Equals(Vector2.positiveInfinity)).ToArray());
            }
        }

        public static void CreateSmoothLine(IReadOnlyList<Vector3> positions,
                                            IReadOnlyList<float> widths,
                                            bool startCap,
                                            bool endCap,
                                            List<Vector3> vertices,
                                            List<int> triangles,
                                            List<Vector2> uv,
                                            int cornerVertices = 5,
                                            float z = 0f,
                                            PolygonCollider2D collider = null,
                                            float offset = 0f)
        {
            Debug.Assert(widths.Count == positions.Count);

            var normals = new List<Vector3>();
            var useZ = !z.Equals(float.NaN);
            z = useZ ? z : 0f;

            Vector2[] colliderPath = null;
            if (collider != null)
            {
                colliderPath = Enumerable.Repeat(
                    Vector2.positiveInfinity, positions.Count * 2).ToArray();
            }

            var connectionOffset = 0;
            var quads = 0;

            for (int i = 1; i < positions.Count; ++i)
            {
                CreateSmoothLine_AddQuad(positions, i, widths[i - 1], widths[i],
                                         startCap, endCap, vertices,
                                         triangles, uv, normals, cornerVertices, z,
                                         useZ, collider, colliderPath, offset,
                                         ref connectionOffset, ref quads);
            }

            if (collider != null)
            {
                var prevPathCount = collider.pathCount;
                collider.pathCount = prevPathCount + 1;
                collider.SetPath(prevPathCount,
                    colliderPath.Where(v => !v.Equals(Vector2.positiveInfinity)).ToArray());
            }
        }

        public static Mesh CreateSmoothLine(IReadOnlyList<Vector3> positions, float width,
                                            int cornerVertices = 5, float z = 0f,
                                            PolygonCollider2D collider = null,
                                            bool startCap = true, bool endCap = true,
                                            float offset = 0f)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            CreateSmoothLine(positions, width, startCap, endCap, vertices,
                             triangles, uv, cornerVertices, z, collider, offset);

            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                // uv = uv.ToArray(),
            };

            mesh.RecalculateNormals();
            return mesh;
        }

        public static Mesh CreateSmoothLine(IReadOnlyList<Vector3> positions,
                                            IReadOnlyList<float> widths,
                                            int cornerVertices = 5, float z = 0f,
                                            PolygonCollider2D collider = null,
                                            bool startCap = true, bool endCap = true,
                                            float offset = 0f)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            CreateSmoothLine(positions, widths, startCap, endCap, vertices,
                             triangles, uv, cornerVertices, z, collider, offset);

            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                // uv = uv.ToArray(),
            };

            mesh.RecalculateNormals();
            return mesh;
        }

        static void AddColliderPart(IReadOnlyList<Vector3> positions,
                                    Vector2[] colliderPath, int i,
                                    float startWidth, float endWidth,
                                    float offset)
        {
            var p0 = positions[i - 1];
            var p1 = positions[i];

            if (p0.Equals(Vector3.positiveInfinity) || p1.Equals(Vector3.positiveInfinity))
            {
                return;
            }

            Vector3 line = p0 - p1;
            Vector3 normal = new Vector3(-line.y, line.x, 0f).normalized;

            var tr = p1 + endWidth * normal;
            var tl = p1 - endWidth * normal;
            var br = p0 + startWidth * normal;
            var bl = p0 - startWidth * normal;

            if (!offset.Equals(0f))
            {
                p0 += offset * normal;
                p1 += offset * normal;

                bl += offset * normal;
                tl += offset * normal;
                tr += offset * normal;
                br += offset * normal;
            }

            var angle = Math.Angle(Vector2.down, normal);
            if (!angle.Equals(0f))
            {
                // Add right side to forward path.
                colliderPath[i - 1] = br;

                // Add left side to backward path.
                colliderPath[colliderPath.Length - i] = bl;

                if (i == positions.Count - 1)
                {
                    // Add right side to forward path.
                    colliderPath[i] = tr;

                    // Add left side to backward path.
                    colliderPath[i + 1] = tl;
                }
            }
        }

        public static void CreateLineCollider(IReadOnlyList<Vector3> positions,
                                              IReadOnlyList<float> widths,
                                              PolygonCollider2D collider,
                                              float offset = 0f)
        {
            var colliderPath = CreateLineCollider(positions, widths, offset);
            var prevPathCount = collider.pathCount;
            collider.pathCount = prevPathCount + 1;
            collider.SetPath(prevPathCount, colliderPath);
        }

        public static Vector2[] CreateLineCollider(IReadOnlyList<Vector3> positions,
                                                   IReadOnlyList<float> widths,
                                                   float offset = 0f)
        {
            var colliderPath = Enumerable.Repeat(
                Vector2.positiveInfinity, positions.Count * 2).ToArray();

            for (int i = 1; i < positions.Count; ++i)
            {
                var startWidth = widths[i - 1];
                var endWidth = widths[i];

                AddColliderPart(positions, colliderPath, i, startWidth, endWidth, offset);
            }

            return colliderPath.Where(v => !v.Equals(Vector2.positiveInfinity)).ToArray();
        }

        public static void CreateLineCollider(IReadOnlyList<Vector3> positions,
                                              float width,
                                              PolygonCollider2D collider,
                                              float offset = 0f)
        {
            var colliderPath = CreateLineCollider(positions, width, offset);
            var prevPathCount = collider.pathCount;
            collider.pathCount = prevPathCount + 1;
            collider.SetPath(prevPathCount, colliderPath);
        }

        public static Vector2[] CreateLineCollider(IReadOnlyList<Vector3> positions,
                                                   float width,
                                                   float offset = 0f)
        {
            var colliderPath = Enumerable.Repeat(
                Vector2.positiveInfinity, positions.Count * 2).ToArray();

            for (int i = 1; i < positions.Count; ++i)
            {
                AddColliderPart(positions, colliderPath, i, width, width, offset);
            }

            return colliderPath.Where(v => !v.Equals(Vector2.positiveInfinity)).ToArray();
        }

        static LineRenderer tmpRenderer;

        public static Mesh CreateBakedLineMesh(IReadOnlyList<Vector3> positions,
                                               float width,
                                               PolygonCollider2D collider = null,
                                               int cornerVerts = 5, int capVerts = 5)
        {
            if (positions == null)
            {
                return null;
            }

            if (tmpRenderer == null)
            {
                var tmpObj = new GameObject();
                tmpRenderer = tmpObj.AddComponent<LineRenderer>();
                tmpRenderer.enabled = false;
            }

            tmpRenderer.positionCount = positions.Count;
            tmpRenderer.SetPositions(positions.ToArray());
            //tmpRenderer.SetPositions(
            //    positions.Select(v => new Vector3(v.x, v.y, Map.Layer(MapLayer.TransitLines))).ToArray());

            tmpRenderer.numCornerVertices = cornerVerts;
            tmpRenderer.numCapVertices = capVerts;
            tmpRenderer.startWidth = width * 2;
            tmpRenderer.endWidth = tmpRenderer.startWidth;

            var mesh = new Mesh();
            tmpRenderer.BakeMesh(mesh);

            if (collider != null)
            {
                var colliderPath = MeshBuilder.CreateLineCollider(positions, width);
                collider.pathCount = 0;
                collider.SetPath(0, colliderPath);
            }

            return mesh;
        }

        public static AnimationCurve GetWidthCurve(IReadOnlyList<float> widths, IReadOnlyList<Vector3> positions)
        {
            var totalDistance = 0f;
            var distances = new float[positions.Count];

            for (var i = 1; i < positions.Count; ++i)
            {
                var p0 = positions[i - 1];
                var p1 = positions[i];

                var dist = (p1 - p0).sqrMagnitude;
                totalDistance += dist;
                distances[i - 1] = dist;
            }

            var curve = new AnimationCurve();

            var time = 0f;
            for (var i = 1; i < positions.Count; ++i)
            {
                curve.AddKey(time, widths[i]);
                time += distances[i - 1] / totalDistance;
            }

            curve.AddKey(1f, widths.Last());
            Debug.Assert(Mathf.Approximately(time, 1f));
            Debug.Assert(curve.length == widths.Count);

            return curve;
        }

        public static AnimationCurve GetWidthCurve(IReadOnlyList<float> widths)
        {
            var curve = new AnimationCurve();
            var timeStep = 1f / (widths.Count - 1);
            var time = 0f;

            for (var i = 0; i < widths.Count; ++i)
            {
                curve.AddKey(time, widths[i]);
                time += timeStep;
            }

            Debug.Assert(widths.Count == 0 || Mathf.Approximately(time, 1f + timeStep));
            Debug.Assert(curve.length == widths.Count);

            return curve;
        }

        public static Mesh CreateBakedLineMesh(IReadOnlyList<Vector3> positions,
                                               IReadOnlyList<float> widths,
                                               PolygonCollider2D collider = null,
                                               int cornerVerts = 5, int capVerts = 5)
        {
            if (tmpRenderer == null)
            {
                var tmpObj = new GameObject();
                tmpRenderer = tmpObj.AddComponent<LineRenderer>();
                tmpRenderer.enabled = false;
            }

            tmpRenderer.positionCount = positions.Count;
            tmpRenderer.SetPositions(
                positions.Select(v => new Vector3(v.x, v.y, Map.Layer(MapLayer.TransitLines))).ToArray());

            tmpRenderer.numCornerVertices = cornerVerts;
            tmpRenderer.numCapVertices = capVerts;
            tmpRenderer.widthCurve = GetWidthCurve(widths, positions);
            tmpRenderer.widthMultiplier = 2f;

            var mesh = new Mesh();
            tmpRenderer.BakeMesh(mesh);

            if (collider != null)
            {
                var colliderPath = MeshBuilder.CreateLineCollider(positions, widths);
                collider.pathCount = 0;
                collider.SetPath(0, colliderPath);
            }

            return mesh;
        }

        static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
                                                       float currentOffsetStart,
                                                       float currentOffsetEnd,
                                                       out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            p0 = p0 + (perpendicular * currentOffsetStart);
            p1 = p1 + (perpendicular * currentOffsetEnd);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }

        static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
                                                       float currentOffsetStart,
                                                       float currentOffsetEnd,
                                                       Vector3 prevPerpendicular,
                                                       out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            var mid = (perpendicular + prevPerpendicular).normalized;
            perpendicular = mid;

            p0 = p0 + (mid * currentOffsetStart);
            p1 = p1 + (mid * currentOffsetEnd);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }

        public static List<Vector3> GetOffsetPath(IReadOnlyList<Vector3> segPositions, float offset)
        {
            Debug.Assert(segPositions.Count != 1, "can't offset a path of length 1!");

            var positions = new List<Vector3>();
            var perpendicular = Vector3.zero;

            for (int j = 1; j < segPositions.Count; ++j)
            {
                Vector3 p0 = segPositions[j - 1];
                Vector3 p1 = segPositions[j];

                if (j == 1)
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, offset, offset, out perpendicular);
                    positions.Add(offsetPoints.Item1);
                    positions.Add(offsetPoints.Item2);
                }
                else
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, offset, offset, perpendicular,
                                                       out perpendicular);

                    positions.Add(offsetPoints.Item2);
                }
            }

            return positions;
        }

        public static Vector2 GetOffsetPoint(Vector2 pt, float offset, Vector2 direction)
        {
            var perpendicular2d = -Vector2.Perpendicular(direction).normalized;
            return pt + (offset * perpendicular2d);
        }

        public static List<Vector3> GetOffsetPath(IReadOnlyList<Vector3> segPositions,
                                                  IReadOnlyList<float> offsets)
        {
            Debug.Assert(segPositions.Count != 1, "can't offset a path of length 1!");
            Debug.Assert(segPositions.Count == offsets.Count, "offset count doesn't match!");

            var positions = new List<Vector3>();
            var perpendicular = Vector3.zero;

            for (int j = 1; j < segPositions.Count; ++j)
            {
                Vector3 p0 = segPositions[j - 1];
                Vector3 p1 = segPositions[j];

                if (j == 1)
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, offsets[j - 1], offsets[j], out perpendicular);
                    positions.Add(offsetPoints.Item1);
                    positions.Add(offsetPoints.Item2);
                }
                else
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, offsets[j - 1], offsets[j], perpendicular,
                                                       out perpendicular);

                    positions.Add(offsetPoints.Item2);
                }
            }

            return positions;
        }

        public static List<Vector3> RemoveSharpAngles(List<Vector3> positions,
                                                      float threshold = 45.0f,
                                                      int segmentSize = 10)
        {
            var newPositions = new List<Vector3>(positions.Count);
            float prevAngle = float.NaN;

            for (int i = 1; i < positions.Count; ++i)
            {
                var p2 = positions[i];
                var p1 = positions[i - 1];

                var angle = Math.PointAngleDeg(p2, p1);
                if (i == 1)
                {
                    prevAngle = angle;
                    continue;
                }

                var diff = prevAngle - angle;
                var p0 = positions[i - 2];
                newPositions.Add(p0);

                if (Mathf.Abs(diff) > threshold)
                {
                    var vec = (p1 - p0).normalized;
                    var len = vec.magnitude / (segmentSize + 1);

                    for (int j = 1; j <= segmentSize; ++j)
                    {
                        newPositions.Add(p0 + vec * (len * j));
                    }
                }

                prevAngle = angle;
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        public static List<Vector3> DistributeEvenly(List<Vector3> positions,
                                                     float maxDistance = -1f)
        {
            if (maxDistance.Equals(-1f))
            {
                maxDistance = 5f * Map.Meters;
            }

            var newPositions = new List<Vector3>(positions.Count);
            newPositions.Add(positions.First());

            for (int i = 2; i < positions.Count; ++i)
            {
                var p1 = positions[i];
                var p0 = positions[i - 1];

                newPositions.Add(p0);

                var mag = (p1 - p0).magnitude;
                if (mag > maxDistance)
                {
                    var dir = (p1 - p0).normalized;
                    var cnt = (int)(mag / maxDistance);
                    var len = mag / cnt;

                    for (int j = 1; j <= cnt; ++j)
                    {
                        newPositions.Add(p0 + dir * (len * j));
                    }
                }
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        static bool IsSharpAngle(Vector3 p0, Vector3 p1, Vector3 p2, float threshold)
        {
            float angle = Vector3.Angle(p1 - p0, p1 - p2);
            return 180f - Mathf.Abs(angle) >= threshold;
        }

        public static List<Vector3> RemoveDetailByAngle(IReadOnlyList<Vector3> positions,
                                                        float thresholdAngle = 5f)
        {
            if (positions.Count <= 3)
            {
                return positions.ToList();
            }

            var newPositions = new List<Vector3>(positions.Count);
            newPositions.Add(positions.First());

            var p2 = positions[2];
            var p1 = positions[1];
            var p0 = positions[0];

            for (int i = 2; i < positions.Count - 1; ++i)
            {
                p2 = positions[i];

                if (IsSharpAngle(p0, p1, p2, thresholdAngle))
                {
                    newPositions.Add(p1);
                    newPositions.Add(p2);

                    ++i;
                    p0 = p2;
                    p1 = positions[i];

                    continue;
                }

                p0 = p1;
                p1 = p2;
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        public static List<Vector2> RemoveDetailByAngle(IReadOnlyList<Vector2> positions,
                                                        float thresholdAngle = 5f)
        {
            if (positions.Count <= 3)
            {
                return positions.ToList();
            }

            var newPositions = new List<Vector2>(positions.Count);
            newPositions.Add(positions.First());

            var p2 = positions[2];
            var p1 = positions[1];
            var p0 = positions[0];

            for (int i = 2; i < positions.Count - 1; ++i)
            {
                p2 = positions[i];

                if (IsSharpAngle(p0, p1, p2, thresholdAngle))
                {
                    newPositions.Add(p1);
                    newPositions.Add(p2);

                    ++i;
                    p0 = p2;
                    p1 = positions[i];

                    continue;
                }

                p0 = p1;
                p1 = p2;
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        public static List<Vector3> RemoveDetailByDistance(IReadOnlyList<Vector3> positions,
                                                           float minDistance = 5f)
        {
            if (positions.Count <= 2)
            {
                return positions.ToList();
            }

            float dist = 0f;
            var newPositions = new List<Vector3>(positions.Count);
            for (int i = 1; i < positions.Count; ++i)
            {
                var p1 = positions[i];
                var p0 = positions[i - 1];

                var currDist = (p1 - p0).magnitude;
                dist += currDist;

                if (dist >= minDistance)
                {
                    newPositions.Add(p0);
                    dist = 0;
                }
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        public static List<Vector2> RemoveDetailByDistance(IReadOnlyList<Vector2> positions,
                                                           float minDistance = 5f)
        {
            if (positions.Count <= 2)
            {
                return positions.ToList();
            }

            float dist = 0f;
            var newPositions = new List<Vector2>(positions.Count);
            for (int i = 1; i < positions.Count; ++i)
            {
                var p1 = positions[i];
                var p0 = positions[i - 1];

                var currDist = (p1 - p0).magnitude;
                dist += currDist;

                if (dist >= minDistance)
                {
                    newPositions.Add(p0);
                    dist = 0;
                }
            }

            newPositions.Add(positions.Last());
            return newPositions;
        }

        public static Mesh PointsToMeshFast(Vector3[] points)
        {
            if (points.Length == 0)
            {
                return new Mesh();
            }

            int pointCount = points.Length;
            if (points.Last() == points.First())
                --pointCount;

            Vector2[] points2d = new Vector2[pointCount];
            for (int j = 0; j < pointCount; j++)
            {
                Vector2 actual = points[j];
                points2d[j] = actual;
            }

            int[] indices = new Triangulator(points2d).Triangulate();
            Mesh msh = new Mesh();
            msh.vertices = points;
            msh.triangles = indices;
            msh.RecalculateNormals();
            msh.RecalculateBounds();

            return msh;
        }

        public static Mesh PointsToMesh(Vector3[] points)
        {
            var pslg = new PSLG();
            pslg.AddOrderedVertices(points);

            return TriangleAPI.CreateMesh(pslg);
        }

        public static Mesh ScaleMesh(Mesh mesh, float scale)
        {
            var vertices = mesh.vertices;

            // Find the mesh bounds.
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;

            float maxX = 0;
            float maxY = 0;

            for (int i = 0; i < vertices.Length; ++i)
            {
                minX = Mathf.Min(minX, vertices[i].x);
                minY = Mathf.Min(minY, vertices[i].y);

                maxX = Mathf.Max(maxX, vertices[i].x);
                maxY = Mathf.Max(maxY, vertices[i].y);
            }

            float midX = (maxX - minX) / 2;
            float midY = (maxY - minY) / 2;

            for (int i = 0; i < vertices.Length; ++i)
            {
                float x = vertices[i].x - midX - minX;
                float y = vertices[i].y - midY - minY;

                vertices[i].x = (x * scale) + (midX + minX);
                vertices[i].y = (y * scale) + (midY + minY);
            }

            var scaledMesh = new Mesh
            {
                vertices = vertices,
                triangles = mesh.triangles,
                normals = mesh.normals,
                uv = mesh.uv,
            };

            scaledMesh.RecalculateNormals();
            return scaledMesh;
        }

        public static void FixWindingOrder(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];

                var cross = Vector3.Cross(b - a, c - a);
                if (cross.z > 0f)
                {
                    int bIndex = triangles[i + 1];
                    triangles[i + 1] = triangles[i + 2];
                    triangles[i + 2] = bIndex;
                }
            }

            mesh.triangles = triangles;
        }

        public static Mesh CombineMeshes(params Mesh[] meshes)
        {
            return CombineMeshes((IReadOnlyList<Mesh>)meshes);
        }

        public static Mesh CombineMeshes(IReadOnlyList<Mesh> meshes)
        {
            Mesh mesh = new Mesh();
            CombineInstance[] combine = new CombineInstance[meshes.Count];

            for (var i = 0; i < meshes.Count; ++i)
            {
                combine[i].mesh = meshes[i];
                combine[i].transform = Matrix4x4.identity;
            }

            mesh.CombineMeshes(combine);
            return mesh;
        }

        public static Rect GetCollisionRect(Mesh mesh)
        {
            var bounds = mesh.bounds;
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        static Vector2 RotateToXAxis(Vector2 v, float angle)
        {
            var newX = v.x * Mathf.Cos(angle) - v.y * Mathf.Sin(angle);
            var newY = v.x * Mathf.Sin(angle) + v.y * Mathf.Cos(angle);

            return new Vector2(newX, newY);
        }

        public static Vector2[] GetSmallestSurroundingRect(Mesh mesh)
        {
            return GetSmallestSurroundingRect(mesh.vertices.Select(v => (Vector2)v).ToList());
        }

        public static Vector2[] GetSmallestSurroundingRect(Mesh mesh, ref float minAngle)
        {
            return GetSmallestSurroundingRect(mesh.vertices.Select(v => (Vector2)v).ToList(), ref minAngle);
        }

        public static Vector2[] GetSmallestSurroundingRect(IList<Vector2> points)
        {
            float minAngle = 0f;
            return GetSmallestSurroundingRect(points, ref minAngle);
        }

        public static Vector2[] GetSmallestSurroundingRect(IList<Vector2> points, ref float minAngle)
        {
            var hull = Math.MakeHull(points);

            // Go through each edge of the convex hull and rotate the bounding rect
            // according to that edge. Return the smallest area found.
            Rect? minRect = null;
            var minArea = float.MaxValue;

            for (var i = 1; i < hull.Count; ++i)
            {
                var p0 = hull[i - 1];
                var p1 = hull[i];
                var edge = p0 - p1;
                var angle = -Mathf.Atan(edge.y / edge.x);

                var maxY = float.MinValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var minX = float.MaxValue;

                foreach (var p in hull)
                {
                    var rotatedPoint = RotateToXAxis(p, angle);

                    maxY = Mathf.Max(maxY, rotatedPoint.y);
                    minY = Mathf.Min(minY, rotatedPoint.y);

                    minX = Mathf.Min(minX, rotatedPoint.x);
                    maxX = Mathf.Max(maxX, rotatedPoint.x);
                }

                var width = maxX - minX;
                var height = maxY - minY;
                var area = width * height;

                if (minRect == null || area < minArea)
                {
                    minRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                    minArea = area;
                    minAngle = angle;
                }
            }

            var rect = minRect.Value;
            return new Vector2[]
            {
                RotateToXAxis(new Vector2(rect.x, rect.y),                            -minAngle),
                RotateToXAxis(new Vector2(rect.x, rect.y + rect.height),              -minAngle),
                RotateToXAxis(new Vector2(rect.x + rect.width, rect.y + rect.height), -minAngle),
                RotateToXAxis(new Vector2(rect.x + rect.width, rect.y),               -minAngle),
            };
        }

        public static Mesh RotateMesh(Mesh mesh, Vector3 centroid, Quaternion rot)
        {
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = rot * (vertices[i] - centroid) + centroid;
            }

            return new Mesh
            {
                vertices = vertices,
                triangles = mesh.triangles,
            };
        }

        public static Color IncreaseBrightness(Color c, float increase)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);

            return Color.HSVToRGB(h, s, Mathf.Clamp(v + increase, 0f, 1f));
        }

        public static Color IncreaseOrDecreaseBrightness(Color c, float increase)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);

            if (v < (1f - increase))
            {
                return Color.HSVToRGB(h, s, v + increase);
            }
            else
            {
                return Color.HSVToRGB(h, s, v - increase);
            }
        }

        public static void MeshCollider(Mesh mesh, PolygonCollider2D polygonCollider)
        {
            // Get triangles and vertices from mesh
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            // Get just the outer edges from the mesh's triangles (ignore or remove any shared edges)
            Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int vert1 = triangles[i + e];
                    int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];
                    string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                    if (edges.ContainsKey(edge))
                    {
                        edges.Remove(edge);
                    }
                    else
                    {
                        edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                    }
                }
            }

            // Create edge lookup (Key is first vertex, Value is second vertex, of each edge)
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> edge in edges.Values)
            {
                if (lookup.ContainsKey(edge.Key) == false)
                {
                    lookup.Add(edge.Key, edge.Value);
                }
            }

            // Create empty polygon collider
            polygonCollider.pathCount = 0;

            // Loop through edge vertices in order
            int startVert = 0;
            int nextVert = startVert;
            int highestVert = startVert;
            List<Vector2> colliderPath = new List<Vector2>();
            while (true)
            {
                // Add vertex to collider path
                colliderPath.Add(vertices[nextVert]);

                // Get next vertex
                nextVert = lookup[nextVert];

                // Store highest vertex (to know what shape to move to next)
                if (nextVert > highestVert)
                {
                    highestVert = nextVert;
                }

                // Shape complete
                if (nextVert == startVert)
                {

                    // Add path to polygon collider
                    polygonCollider.pathCount++;
                    polygonCollider.SetPath(polygonCollider.pathCount - 1, colliderPath.ToArray());
                    colliderPath.Clear();

                    // Go to next shape if one exists
                    if (lookup.ContainsKey(highestVert + 1))
                    {

                        // Set starting and next vertices
                        startVert = highestVert + 1;
                        nextVert = startVert;

                        // Continue to next loop
                        continue;
                    }

                    // No more verts
                    break;
                }
            }
        }
    }
}
