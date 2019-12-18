﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class MultiMesh : MonoBehaviour
    {
        [System.Serializable]
        public struct SerializableMultiMeshData
        {
            public SerializableMesh2D[] meshes;
            public SerializableColor color;
            public RenderingDistance distance;
        }

        [System.Serializable]
        public struct SerializableMultiMesh
        {
            public SerializableMultiMeshData[] meshes;
        }

        class MutableMesh
        {
            internal List<Vector3> positions = new List<Vector3>();
            internal List<Vector3> vertices = new List<Vector3>();
            internal List<int> triangles = new List<int>();
            internal List<Vector2> uv = new List<Vector2>();
            internal Mesh[] meshes;
        }

        class MeshData
        {
            internal Dictionary<Color, MutableMesh> meshMap =
                new Dictionary<Color, MutableMesh>();
        }

        /// <summary>
        ///  Reference to the map.
        /// </summary>
        public Map map;
        Dictionary<RenderingDistance, MeshData> meshData;

        public List<GameObject> renderingObjects;
        private static GameObject prefab;

        public static MultiMesh Create(Map map, string name, Transform parent = null)
        {
            if (prefab == null)
            {
                prefab = Resources.Load("Prefabs/MultiMesh") as GameObject;
            }

            var multiMesh = Instantiate(prefab);
            multiMesh.name = name;
            multiMesh.transform.SetParent(parent);

            var mm = multiMesh.GetComponent<MultiMesh>();
            mm.map = map;

            return mm;
        }

        public void AddMesh(Color c, Mesh mesh, float z = 0f)
        {
            AddMesh(c, mesh, RenderingDistance.Near, z);
        }

        public void AddMesh(Color c, Mesh mesh, RenderingDistance dist, float z = 0f)
        {
            if (!meshData.TryGetValue(dist, out MeshData data))
            {
                data = new MeshData();
                meshData.Add(dist, data);
            }
            if (!data.meshMap.TryGetValue(c, out MutableMesh mutableMesh))
            {
                mutableMesh = new MutableMesh();
                data.meshMap.Add(c, mutableMesh);
            }

            var baseIdx = mutableMesh.vertices.Count;
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                mutableMesh.vertices.Add(
                    new Vector3(mesh.vertices[i].x, mesh.vertices[i].y, z));
            }

            mutableMesh.triangles.AddRange(mesh.triangles.Select(idx => idx + baseIdx));
            mutableMesh.uv.AddRange(mesh.uv);
        }

        public static readonly int maxMeshVertices = 65532;

        public void CreateMeshes()
        {
            foreach (var data in meshData)
            {
                foreach (var entry in data.Value.meshMap)
                {
                    if (entry.Value.meshes != null)
                    {
                        continue;
                    }

                    var meshes = new List<Mesh>();

                    var triangles = entry.Value.triangles;
                    var vertices = entry.Value.vertices;
                    var uv = entry.Value.uv;

                    int totalTriangles = triangles.Count;
                    int leftoverTriangles = totalTriangles;
                    int triangleIdx = 0;

                    while (leftoverTriangles != 0)
                    {
                        int numTriangles = triangleIdx;
                        int min = triangles[numTriangles];
                        int max = triangles[numTriangles];

                        while (numTriangles < totalTriangles)
                        {
                            var val = triangles[numTriangles];
                            if (val < min)
                            {
                                min = val;
                                if (max - min >= maxMeshVertices)
                                {
                                    break;
                                }
                            }
                            else if (val > max)
                            {
                                max = val;
                                if (max - min >= maxMeshVertices)
                                {
                                    break;
                                }
                            }

                            ++numTriangles;
                        }

                        while (numTriangles % 3 != 0)
                        {
                            --numTriangles;
                        }

                        var usedTriangles = numTriangles - triangleIdx;
                        var triangleRange = triangles.GetRange(triangleIdx, usedTriangles);
                        var minIdx = triangleRange.Min();
                        var maxIdx = triangleRange.Max();

                        var vertexRange = vertices.GetRange(minIdx, maxIdx - minIdx + 1);
                        var adjustedTriangleRange = triangleRange.Select(i => i - minIdx).ToArray();

                        Vector2[] uvRange = null;
                        if (uv.Count > 0)
                        {
                            uvRange = uv.GetRange(minIdx, maxIdx - minIdx + 1).ToArray();
                        }

                        Mesh mesh;
                        if (uv.Count > 0)
                        {
                            mesh = new Mesh
                            {
                                vertices = vertexRange.ToArray(),
                                triangles = adjustedTriangleRange,
                                uv = uvRange,
                            };
                        }
                        else
                        {
                            mesh = new Mesh
                            {
                                vertices = vertexRange.ToArray(),
                                triangles = adjustedTriangleRange,
                            };
                        }

                        meshes.Add(mesh);

                        leftoverTriangles -= usedTriangles;
                        triangleIdx += usedTriangles;
                    }

                    entry.Value.meshes = meshes.ToArray();
                }
            }

            renderingObjects = new List<GameObject>();
        }

        void LoadMeshes(RenderingDistance distance, ref int i)
        {
            if (meshData.TryGetValue(distance, out MeshData data))
            {
                foreach (var entry in data.meshMap)
                {
                    if (entry.Value.meshes == null)
                    {
                        continue;
                    }

                    foreach (var mesh in entry.Value.meshes)
                    {
                        if (i >= renderingObjects.Count)
                        {
                            var nextObj = Instantiate(map.meshPrefab);
                            nextObj.transform.SetParent(this.transform);

                            renderingObjects.Add(nextObj);
                        }

                        var obj = renderingObjects[i];
                        var meshFilter = obj.GetComponent<MeshFilter>();
                        var meshRenderer = obj.GetComponent<MeshRenderer>();

                        meshFilter.mesh = mesh;
                        meshRenderer.material = GameController.GetUnlitMaterial(entry.Key);

                        ++i;
                    }
                }
            }
        }

        public void RemoveMesh(Mesh meshToRemove)
        {
            var vertsToRemove = meshToRemove.vertices;
            foreach (var obj in renderingObjects)
            {
                var meshFilter = obj.GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;
                var verts = mesh.vertices;

                var newTriangles = mesh.triangles.Where(idx =>
                {
                    var vert = verts[idx];
                    foreach (Vector2 v in vertsToRemove)
                    {
                        if (v.Equals(vert))
                        {
                            return false;
                        }
                    }

                    return true;
                }).ToArray();

                meshFilter.sharedMesh = new Mesh
                {
                    vertices = verts,
                    triangles = newTriangles,
                };
            }
        }

        public void UpdateScale(RenderingDistance distance)
        {
            var i = 0;
            switch (distance)
            {
                case RenderingDistance.Near:
                    LoadMeshes(RenderingDistance.Near, ref i);
                    LoadMeshes(RenderingDistance.Far, ref i);
                    LoadMeshes(RenderingDistance.VeryFar, ref i);
                    LoadMeshes(RenderingDistance.Farthest, ref i);

                    break;
                case RenderingDistance.Far:
                    LoadMeshes(RenderingDistance.Far, ref i);
                    LoadMeshes(RenderingDistance.VeryFar, ref i);
                    LoadMeshes(RenderingDistance.Farthest, ref i);

                    break;
                case RenderingDistance.VeryFar:
                    LoadMeshes(RenderingDistance.VeryFar, ref i);
                    LoadMeshes(RenderingDistance.Farthest, ref i);

                    break;
                case RenderingDistance.Farthest:
                    LoadMeshes(RenderingDistance.Farthest, ref i);

                    break;
            }

            while (i < renderingObjects.Count)
            {
                var obj = renderingObjects[i];
                var meshFilter = obj.GetComponent<MeshFilter>();
                meshFilter.mesh = null;

                ++i;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            if (meshData == null)
                return;

            foreach (var entry in meshData)
            {
                foreach (var data in entry.Value.meshMap)
                {
                    if (data.Value.positions == null)
                        continue;

                    foreach (var pt in data.Value.positions)
                    {
                        Gizmos.DrawSphere(pt, 0.005f);
                    }
                }
            }
        }

        public SerializableMultiMesh Serialize()
        {
            var serializedMeshData = new List<SerializableMultiMeshData>();
            foreach (var data in meshData)
            {
                foreach (var entry in data.Value.meshMap)
                {
                    var meshes = new SerializableMesh2D[entry.Value.meshes.Length];

                    int i = 0;
                    foreach (var mesh in entry.Value.meshes)
                    {
                        meshes[i++] = new SerializableMesh2D(mesh);
                    }

                    serializedMeshData.Add(new SerializableMultiMeshData
                    {
                        meshes = meshes,
                        color = new SerializableColor(entry.Key),
                        distance = data.Key,
                    });
                }
            }

            return new SerializableMultiMesh { meshes = serializedMeshData.ToArray() };
        }

        public void Deserialize(SerializableMultiMesh meshes)
        {
            foreach (var m in meshes.meshes)
            {
                if (!meshData.TryGetValue(m.distance, out MeshData data))
                {
                    data = new MeshData();
                    meshData.Add(m.distance, data);
                }

                var c = m.color.ToColor();
                if (!data.meshMap.TryGetValue(c, out MutableMesh mutableMesh))
                {
                    mutableMesh = new MutableMesh();
                    data.meshMap.Add(c, mutableMesh);
                }

                mutableMesh.meshes = new Mesh[m.meshes.Length];

                int i = 0;
                foreach (var mesh in m.meshes)
                {
                    mutableMesh.meshes[i++] = mesh.GetMesh();
                }
            }
        }

        void Awake()
        {
            this.meshData = new Dictionary<RenderingDistance, MeshData>();
        }
    }
}