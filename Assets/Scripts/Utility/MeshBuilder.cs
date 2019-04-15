using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MeshBuilder
{
    public static Mesh CreateQuad(Vector2 bl, Vector2 tr, Vector2 br, Vector2 tl)
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
                               Vector2 bl, Vector2 tr, Vector2 br, Vector2 tl)
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
}
