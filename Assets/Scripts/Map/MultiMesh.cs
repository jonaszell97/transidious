using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MultiMesh : MonoBehaviour
{
    [System.Serializable]
    public struct SerializableMultiMeshData
    {
        public SerializableMesh[] meshes;
        public SerializableColor color;
        public InputController.RenderingDistance distance;
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
        internal Dictionary<Color, MutableMesh> meshMap = new Dictionary<Color, MutableMesh>();
    }

    /// <summary>
    ///  Reference to the map.
    /// </summary>
    public Map map;
    Dictionary<InputController.RenderingDistance, MeshData> meshData;

    public GameObject[] renderingObjects;

    public void AddStreetSegment(InputController.RenderingDistance renderingDistance,
                                 List<Vector3> positions,
                                 float width, float borderWidth,
                                 Color color, Color borderColor,
                                 bool connectStart, bool connectEnd,
                                 float meshZ, float outlineZ)
    {
        if (!meshData.TryGetValue(renderingDistance, out MeshData data))
        {
            data = new MeshData();
            meshData.Add(renderingDistance, data);
        }

        if (!data.meshMap.TryGetValue(color, out MutableMesh streetMesh))
        {
            streetMesh = new MutableMesh();
            data.meshMap.Add(color, streetMesh);
        }
        if (!data.meshMap.TryGetValue(borderColor, out MutableMesh outlineMesh))
        {
            outlineMesh = new MutableMesh();
            data.meshMap.Add(borderColor, outlineMesh);
        }

        MeshBuilder.CreateSmoothLine(positions, width, connectStart, connectEnd,
                                     streetMesh.vertices, streetMesh.triangles,
                                     streetMesh.uv, 10, meshZ);

        MeshBuilder.CreateSmoothLine(positions, width + borderWidth, connectStart, connectEnd,
                                     outlineMesh.vertices, outlineMesh.triangles,
                                     outlineMesh.uv, 10, outlineZ);

        streetMesh.positions.AddRange(positions);
    }

    public void AddMesh(Color c, Mesh mesh, float z = 0f)
    {
        if (!meshData.TryGetValue(InputController.RenderingDistance.Near, out MeshData data))
        {
            data = new MeshData();
            meshData.Add(InputController.RenderingDistance.Near, data);
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
        int neededObjects = 0;

        foreach (var data in meshData)
        {
            int currNeededObjects = 0;

            foreach (var entry in data.Value.meshMap)
            {
                if (entry.Value.meshes != null)
                {
                    currNeededObjects = entry.Value.meshes.Length;
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

                    Mesh mesh;
                    if (uv.Count > 0)
                    {
                        mesh = new Mesh
                        {
                            vertices = vertexRange.ToArray(),
                            triangles = adjustedTriangleRange,
                            uv = uv.GetRange(minIdx, maxIdx - minIdx + 1).ToArray()
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
                currNeededObjects += meshes.Count;
            }

            neededObjects = System.Math.Max(neededObjects, currNeededObjects);
        }

        renderingObjects = new GameObject[neededObjects];

        for (int i = 0; i < neededObjects; ++i)
        {
            var obj = Instantiate(map.meshPrefab);
            obj.transform.SetParent(this.transform);

            var meshFilter = obj.GetComponent<MeshFilter>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();

            renderingObjects[i] = obj;
        }
    }

    public void CopyData(InputController.RenderingDistance fromDistance,
                         InputController.RenderingDistance toDistance)
    {
        meshData.Add(toDistance, meshData[fromDistance]);
    }

    public void UpdateScale(InputController.RenderingDistance distance)
    {
        int i = 0;
        if (meshData.TryGetValue(distance, out MeshData data))
        {
            foreach (var entry in data.meshMap)
            {
                foreach (var mesh in entry.Value.meshes)
                {
                    var obj = renderingObjects[i];
                    var meshFilter = obj.GetComponent<MeshFilter>();
                    var meshRenderer = obj.GetComponent<MeshRenderer>();

                    meshFilter.mesh = mesh;
                    meshRenderer.material = map.GetUnlitMaterial(entry.Key);

                    ++i;
                }
            }
        }

        while (i < renderingObjects.Length)
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
                var meshes = new SerializableMesh[entry.Value.meshes.Length];

                int i = 0;
                foreach (var mesh in entry.Value.meshes)
                {
                    meshes[i++] = new SerializableMesh(mesh);
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
        this.meshData = new Dictionary<InputController.RenderingDistance, MeshData>();
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
