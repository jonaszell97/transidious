using mattatz.Triangulation2DSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct SerializableMesh
{
    public SerializableVector3[] vertices;
    public int[] triangles;
    public SerializableVector2[] uv;

    public SerializableMesh(Mesh mesh)
    {
        if (mesh == null)
        {
            this.vertices = null;
            this.triangles = null;
            this.uv = null;

            return;
        }

        this.vertices = mesh.vertices.Select(v => new SerializableVector3(v)).ToArray();
        this.triangles = mesh.triangles;
        this.uv = mesh.uv.Select(v => new SerializableVector2(v)).ToArray();
    }

    public Mesh GetMesh()
    {
        if (vertices == null)
        {
            return null;
        }

        return new Mesh
        {
            vertices = vertices.Select(v => v.ToVector()).ToArray(),
            triangles = triangles,
            uv = uv.Select(v => v.ToVector()).ToArray()
        };
    }
}

public abstract class MeshBuilder
{
    public static Mesh CreateQuad(Vector3 bl, Vector3 tr, Vector3 br, Vector3 tl)
    {
        Mesh mesh = new Mesh
        {
            vertices = new Vector3[] { bl, tr, br, tl },
            normals = new Vector3[] {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            },
            triangles = new int[]
            {
                0, 1, 2, 1, 0, 3
            },
            uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(0, 1)
            }
        };

        return mesh;
    }

    public static void AddQuad(List<Vector3> vertices,
                               List<int> triangles,
                               List<Vector3> normals,
                               List<Vector2> uvs,
                               Vector3 bl, Vector3 tr, Vector3 br, Vector3 tl)
    {
        int baseIndex = vertices.Count;

        vertices.Add(bl);
        vertices.Add(tr);
        vertices.Add(br);
        vertices.Add(tl);

        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);

        triangles.Add(0 + baseIndex);
        triangles.Add(1 + baseIndex);
        triangles.Add(2 + baseIndex);
        triangles.Add(1 + baseIndex);
        triangles.Add(0 + baseIndex);
        triangles.Add(3 + baseIndex);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 1));
    }

    public static void AddQuad(List<Vector3> vertices,
                               List<int> triangles,
                               List<Vector2> uvs,
                               Vector3 bl, int trIndex,
                               int brIndex, Vector3 tl)
    {
        int baseIndex = vertices.Count;

        vertices.Add(bl);
        vertices.Add(tl);

        triangles.Add(0 + baseIndex);
        triangles.Add(trIndex);
        triangles.Add(brIndex);
        triangles.Add(trIndex);
        triangles.Add(0 + baseIndex);
        triangles.Add(1 + baseIndex);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
    }

    public static void AddCircle(List<Vector3> vertices,
                                 List<int> triangles,
                                 List<Vector3> normals,
                                 List<Vector2> uvs,
                                 Vector3 center, float radius)
    {
        Vector3 bl = new Vector3(center.x - radius, center.y - radius, center.z);
        Vector3 tl = new Vector3(center.x - radius, center.y + radius, center.z);
        Vector3 tr = new Vector3(center.x + radius, center.y + radius, center.z);
        Vector3 br = new Vector3(center.x + radius, center.y - radius, center.z);

        MeshBuilder.AddQuad(vertices, triangles,
                            normals, uvs,
                            bl, tr, br, tl);
    }

    public static void CreateLineMesh(List<Vector3> positions,
                                       float width,
                                       bool connectStart,
                                       bool connectEnd,
                                       List<Vector3> vertices,
                                       List<int> triangles,
                                       List<Vector2> uv,
                                       List<Vector3> jointVertices,
                                       List<int> jointTriangles,
                                       List<Vector2> jointUv,
                                       float z = 0f)
    {
        var normals = new List<Vector3>();
        var jointNormals = new List<Vector3>();
        var useZ = !z.Equals(0f);

        for (int i = 1; i < positions.Count; ++i)
        {
            var p0 = positions[i - 1];
            var p1 = positions[i];

            if (useZ)
            {
                p0 = new Vector3(p0.x, p0.y, z);
                p1 = new Vector3(p1.x, p1.y, z);
            }

            if (i == 1 && connectStart)
            {
                AddCircle(jointVertices, jointTriangles, jointNormals, jointUv,
                          p0, width);
            }

            {
                Vector3 line = p0 - p1;
                Vector3 normal = new Vector3(-line.y, line.x, 0.0f).normalized;

                Vector3 bl = p1 - width * normal;
                Vector3 tl = p1 + width * normal;
                Vector3 tr = p0 + width * normal;
                Vector3 br = p0 - width * normal;

                MeshBuilder.AddQuad(vertices, triangles, normals,
                                    uv, bl, tr, br, tl);
            }

            if (i < positions.Count - 1 || connectEnd)
            {
                AddCircle(jointVertices, jointTriangles, jointNormals, jointUv,
                          p1, width);
            }
        }
    }

    public static Tuple<Mesh, Mesh> CreateLineMesh(List<Vector3> positions,
                                                   float width,
                                                   bool connectStart = false,
                                                   bool connectEnd = false)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();

        var jointVertices = new List<Vector3>();
        var jointTriangles = new List<int>();
        var jointUv = new List<Vector2>();

        CreateLineMesh(positions, width, connectStart, connectEnd, vertices,
                      triangles, uv, jointVertices, jointTriangles, jointUv);

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uv.ToArray(),
        };

        var jointMesh = new Mesh
        {
            vertices = jointVertices.ToArray(),
            triangles = jointTriangles.ToArray(),
            uv = jointUv.ToArray(),
        };

        mesh.RecalculateNormals();
        jointMesh.RecalculateNormals();

        return new Tuple<Mesh, Mesh>(mesh, jointMesh);
    }

    public static void AddCirclePart(List<Vector3> vertices,
                                     List<int> triangles,
                                     List<Vector2> uvs,
                                     Vector3 from, Vector3 to,
                                     float radius, Vector3 center,
                                     int segments, float z = 0f)
    {
        /*var fromAngle = Transidious.Math.toRadians(Transidious.Math.Angle(from, center));
        var toAngle = Transidious.Math.toRadians(Transidious.Math.Angle(center, to));

        float step = (toAngle - fromAngle) / segments;
        int steps = (int)((toAngle - fromAngle) / step);*/

        Vector3 p0 = Vector3.zero;
        Vector3 p1 = Vector3.zero;

        var centerIdx = vertices.Count;
        vertices.Add(center);
        uvs.Add(new Vector2(0, 0));

        var lastIdx = 0;

        float twicePI = Mathf.PI * 2f;
        for (int i = 0; i <= segments; ++i)
        {
            var x = center.x + (radius * Mathf.Cos(i * twicePI / segments));
            var y = center.y + (radius * Mathf.Sin(i * twicePI / segments));

            p0 = p1;
            p1 = new Vector3(x, y, z);

            if (i == 0)
            {
                lastIdx = vertices.Count;
                vertices.Add(p1);
                uvs.Add(new Vector2(0, 0));

                continue;
            }

            var nextIdx = vertices.Count;
            vertices.Add(p1);
            uvs.Add(new Vector2(0, 0));

            triangles.Add(centerIdx);
            triangles.Add(nextIdx);
            triangles.Add(lastIdx);

            lastIdx = nextIdx;
        }
    }

    public static void CreateSmoothLine(List<Vector3> positions,
                                        float width,
                                        bool startCap,
                                        bool endCap,
                                        List<Vector3> vertices,
                                        List<int> triangles,
                                        List<Vector2> uv,
                                        int cornerVertices = 5,
                                        float z = 0f)
    {
        var normals = new List<Vector3>();
        var useZ = !z.Equals(0f);

        for (int i = 1; i < positions.Count; ++i)
        {
            var p0 = positions[i - 1];
            var p1 = positions[i];

            if (useZ)
            {
                p0 = new Vector3(p0.x, p0.y, z);
                p1 = new Vector3(p1.x, p1.y, z);
            }

            Vector3 line = p0 - p1;
            Vector3 normal = new Vector3(-line.y, line.x, 0.0f).normalized;

            Vector3 bl = p1 - width * normal;
            Vector3 tl = p1 + width * normal;
            Vector3 tr = p0 + width * normal;
            Vector3 br = p0 - width * normal;

            if (i == 1 && startCap)
            {
                AddCirclePart(vertices, triangles, uv, br, tr, width, p0,
                              cornerVertices, z);
            }

            MeshBuilder.AddQuad(vertices, triangles, normals,
                                uv, bl, tr, br, tl);

            if (i < positions.Count - 1 || endCap)
            {
                AddCirclePart(vertices, triangles, uv, bl, tl, width, p1,
                              cornerVertices, z);
            }
        }
    }

    public static Mesh CreateSmoothLine(List<Vector3> positions, float width,
                                        int cornerVertices = 5, float z = 0f)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv = new List<Vector2>();

        CreateSmoothLine(positions, width, true, true, vertices,
                         triangles, uv, cornerVertices, z);

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uv.ToArray(),
        };

        mesh.RecalculateNormals();
        return mesh;
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

            var angle = Transidious.Math.PointAngle(p2, p1);
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

    public static List<Vector3> DistributeEvenly(List<Vector3> positions, float maxDistance = 0.05f)
    {
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

    public static List<Vector3> RemoveDetail(Vector3[] positions, float thresholdAngle = 5f)
    {
        if (positions.Length <= 3)
        {
            return new List<Vector3>(positions);
        }

        var newPositions = new List<Vector3>(positions.Length);
        newPositions.Add(positions.First());

        var p2 = positions[2];
        var p1 = positions[1];
        var p0 = positions[0];

        for (int i = 2; i < positions.Length - 1; ++i)
        {
            p2 = positions[i];

            if (IsSharpAngle(p0, p1, p2, thresholdAngle))
            {
                newPositions.Add(p1);
                newPositions.Add(p2);
            }

            p0 = p1;
            p1 = p2;
        }

        newPositions.Add(positions.Last());
        return newPositions;
    }

    public static List<Vector3> RemoveDetail(List<Vector3> positions, float minDistance = 0.1f)
    {
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

    public static Mesh _CreateLine(List<Vector3> positions, float width)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        var first = true;
        Vector3 prevNormal = Vector3.zero;
        Vector3 p0 = Vector3.zero;
        Vector3 p1 = Vector3.zero;

        for (var i = 1; i < positions.Count; ++i)
        {
            var pos = positions[i];
            var prev = positions[i - 1];
            var vec = pos - prev;

            var normal = new Vector3(vec.y, -vec.x, 0f).normalized * width;
            Vector3 realNormal;

            if (first)
            {
                p0 = prev + normal;
                p1 = prev - normal;
                realNormal = normal;
            }
            else
            {
                realNormal = (normal.normalized + prevNormal.normalized).normalized * width;
            }

            var p2 = pos + realNormal;
            var p3 = pos - realNormal;

            AddQuad(vertices, triangles, normals, uvs, p3, p0, p1, p2);

            first = false;
            prevNormal = normal;
            p0 = p2;
            p1 = p3;
        }

        return new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            normals = normals.ToArray(),
            uv = uvs.ToArray()
        };
    }

    public static Mesh CreateLine(List<Vector3> m_Points, float width)
    {
        Vector3 localViewPos = new Vector3(0f, 0f, 1f);
        Vector3[] vertices = new Vector3[m_Points.Count * 2];
        Vector3[] normals = new Vector3[m_Points.Count * 2];

        Vector3 oldTangent = Vector3.zero;
        Vector3 oldDir = Vector3.zero;

        for (int i = 0; i < m_Points.Count - 1; i++)
        {
            Vector3 faceNormal = (localViewPos - m_Points[i]).normalized;
            Vector3 dir = (m_Points[i + 1] - m_Points[i]);
            Vector3 tangent = Vector3.Cross(dir, faceNormal).normalized;
            Vector3 offset;
            if (i == 0)
            {
                offset = (oldTangent + tangent).normalized * width / 2.0f;
            }
            else
            {
                float alpha = (Mathf.PI - Mathf.Acos(Vector3.Dot(oldDir.normalized, dir.normalized))) / 2;
                float d = width / 2.0f / Mathf.Sin(alpha);
                d *= -Mathf.Sign(Vector3.Dot(tangent.normalized, oldDir.normalized));
                offset = ((dir.normalized - oldDir.normalized) / 2).normalized * d;
            }
            vertices[i * 2] = m_Points[i] - offset;
            vertices[i * 2 + 1] = m_Points[i] + offset;
            normals[i * 2] = normals[i * 2 + 1] = faceNormal;

            if (i == m_Points.Count - 2)
            {
                // last two points
                vertices[i * 2 + 2] = m_Points[i + 1] - tangent * width / 2.0f;
                vertices[i * 2 + 3] = m_Points[i + 1] + tangent * width / 2.0f;
                normals[i * 2 + 2] = normals[i * 2 + 3] = faceNormal;
            }

            oldDir = dir;
            oldTangent = tangent;
        }

        var m_Indices = new int[m_Points.Count * 2];
        var m_UVs = new Vector2[m_Points.Count * 2];
        for (int i = 0; i < m_Points.Count; i++)
        {
            m_Indices[i * 2] = i * 2;
            m_Indices[i * 2 + 1] = i * 2 + 1;
            m_UVs[i * 2] = m_UVs[i * 2 + 1] = new Vector2((float)i / (m_Points.Count - 1), 0);
            m_UVs[i * 2 + 1].y = 1.0f;
        }

        var m_Mesh = new Mesh
        {
            vertices = vertices,
            normals = normals,
            uv = m_UVs,
            triangles = m_Indices
        };

        m_Mesh.RecalculateBounds();
        return m_Mesh;
    }

    public static Mesh PointsToMesh(Vector3[] points)
    {
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
}
