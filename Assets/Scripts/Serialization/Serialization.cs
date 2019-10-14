using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Transidious
{
    [System.Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
            this.z = vec.z;
        }

        public static implicit operator Vector2(SerializableVector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        public static implicit operator Vector3(SerializableVector3 vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }

        public Vector3 ToVector()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    public struct SerializableVector2
    {
        public float x, y;

        public SerializableVector2(Vector2 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
        }

        public static implicit operator Vector2(SerializableVector2 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        public static implicit operator Vector3(SerializableVector2 vec)
        {
            return new Vector3(vec.x, vec.y, 0f);
        }

        public Vector2 ToVector()
        {
            return new Vector2(x, y);
        }
    }

    [System.Serializable]
    public struct SerializableColor
    {
        public float r, g, b, a;

        public SerializableColor(Color c)
        {
            this.r = c.r;
            this.g = c.g;
            this.b = c.b;
            this.a = c.a;
        }

        public static implicit operator Color(SerializableColor c)
        {
            return new Color(c.r, c.g, c.b, c.a);
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [System.Serializable]
    public struct SerializableList<TElement>
    {
        public TElement[] elements;

        public List<TElement> Deserialize()
        {
            if (elements == null)
            {
                return new List<TElement>();
            }

            return new List<TElement>(elements);
        }

        public static implicit operator List<TElement>(SerializableList<TElement> list)
        {
            return list.Deserialize();
        }
    }

    [System.Serializable]
    public struct SerializableDictionary<TKey, TValue>
    {
        public TKey[] keys;
        public TValue[] values;

        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            if (dict == null)
            {
                keys = null;
                values = null;

                return;
            }

            keys = dict.Keys.ToArray();
            values = dict.Values.ToArray();
        }

        public static implicit operator
        Dictionary<TKey, TValue>(SerializableDictionary<TKey, TValue> dict)
        {
            return dict.Deserialize();
        }

        public Dictionary<TKey, TValue> Deserialize()
        {
            var dict = new Dictionary<TKey, TValue>();
            for (var i = 0; i < (keys?.Length ?? 0); ++i)
            {
                dict.Add(keys[i], values[i]);
            }

            return dict;
        }
    }

    [System.Serializable]
    public struct SerializableClass<TClass>
    {

    }

    public static class SerializationExtensions
    {
        public static Serialization.Vector2 ToProtobuf(this Vector2 vec)
        {
            return new Serialization.Vector2
            {
                X = vec.x,
                Y = vec.y,
            };
        }

        public static Vector2 Deserialize(this Serialization.Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Serialization.Vector3 ToProtobuf(this Vector3 vec)
        {
            return new Serialization.Vector3
            {
                X = vec.x,
                Y = vec.y,
                Z = vec.z,
            };
        }

        public static Vector3 Deserialize(this Serialization.Vector3 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Serialization.Color ToProtobuf(this Color c)
        {
            return new Serialization.Color
            {
                R = c.r,
                G = c.g,
                B = c.b,
                A = c.a,
            };
        }

        public static Color Deserialize(this Serialization.Color c)
        {
            return new Color(c.R, c.G, c.B, c.A);
        }

        public static Serialization.Mesh2D ToProtobuf2D(this Mesh mesh)
        {
            var result = new Serialization.Mesh2D();
            foreach (var vert in mesh.vertices)
            {
                result.Vertices.Add(((Vector2)vert).ToProtobuf());
            }
            foreach (var tri in mesh.triangles)
            {
                result.Triangles.Add((uint)tri);
            }
            foreach (var uv in mesh.uv)
            {
                result.Uv.Add(uv.ToProtobuf());
            }

            return result;
        }

        public static Mesh Deserialize(this Serialization.Mesh2D m, float z = 0f)
        {
            return new Mesh
            {
                vertices = m.Vertices.Select(v => new Vector3(v.X, v.Y, z)).ToArray(),
                triangles = m.Triangles.Select(v => (int)v).ToArray(),
                uv = m.Uv.Select(v => v.Deserialize()).ToArray(),
            };
        }

        public static Serialization.Mesh ToProtobuf(this Mesh mesh)
        {
            var result = new Serialization.Mesh();
            foreach (var vert in mesh.vertices)
            {
                result.Vertices.Add(vert.ToProtobuf());
            }
            foreach (var tri in mesh.triangles)
            {
                result.Triangles.Add((uint)tri);
            }
            foreach (var uv in mesh.uv)
            {
                result.Uv.Add(uv.ToProtobuf());
            }

            return result;
        }

        public static Mesh Deserialize(this Serialization.Mesh m)
        {
            return new Mesh
            {
                vertices = m.Vertices.Select(v => v.Deserialize()).ToArray(),
                triangles = m.Triangles.Select(v => (int)v).ToArray(),
                uv = m.Uv.Select(v => v.Deserialize()).ToArray(),
            };
        }

        public static VersionTriple Deserialize(this Serialization.VersionTriple triple)
        {
            return new VersionTriple((short)triple.Major, (short)triple.Minor, (short)triple.Patch);
        }

        public static SerializableVector3 Serialize(this Vector3 vec)
        {
            return new SerializableVector3(vec);
        }

        public static SerializableVector2 Serialize(this Vector2 vec)
        {
            return new SerializableVector2(vec);
        }

        public static SerializableColor Serialize(this Color c)
        {
            return new SerializableColor(c);
        }

        public static SerializableList<TKey> Serialize<TKey>(this List<TKey> list)
        {
            return new SerializableList<TKey>
            {
                elements = list?.ToArray() ?? null
            };
        }

        public static SerializableDictionary<TKey, TValue> Serialize<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            return new SerializableDictionary<TKey, TValue>(dict);
        }

        public static bool Contains(this Google.Protobuf.Collections.MapField<string, string> map, string Key, string Value)
        {
            if (!map.TryGetValue(Key, out string val))
            {
                return false;
            }

            return Value == val;
        }

        public static string GetValue(this Google.Protobuf.Collections.MapField<string, string> map, string Key)
        {
            if (map.TryGetValue(Key, out string val))
            {
                return val;
            }

            return string.Empty;
        }
    }

    [System.Serializable]
    public struct SerializableMesh
    {
        public SerializableVector3[] vertices;
        public short[] triangles;
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
            this.triangles = mesh.triangles.Select(v=>(short)v).ToArray();
            this.uv = mesh.uv.Select(v => new SerializableVector2(v)).ToArray();
        }

        public static implicit operator Mesh(SerializableMesh mesh)
        {
            return mesh.GetMesh();
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
                triangles = triangles.Select(v=> (int)v).ToArray(),
                uv = uv.Select(v => v.ToVector()).ToArray()
            };
        }
    }

    [System.Serializable]
    public struct SerializableMesh2D
    {
        public SerializableVector2[] vertices;
        public short[] triangles;
        public SerializableVector2[] uv;

        public SerializableMesh2D(Mesh mesh)
        {
            if (mesh == null)
            {
                this.vertices = null;
                this.triangles = null;
                this.uv = null;

                return;
            }

            this.vertices = mesh.vertices.Select(v => new SerializableVector2(v)).ToArray();
            this.triangles = mesh.triangles.Select(t => (short)t).ToArray();
            this.uv = mesh.uv.Select(v => new SerializableVector2(v)).ToArray();
        }

        public static implicit operator Mesh(SerializableMesh2D mesh)
        {
            return mesh.GetMesh();
        }

        public Mesh GetMesh(float z = 0f)
        {
            if (vertices == null)
            {
                return null;
            }

            return new Mesh
            {
                vertices = vertices.Select(v => new Vector3(v.x, v.y, z)).ToArray(),
                triangles = triangles.Select(t => (int)t).ToArray(),
                uv = uv.Select(v => v.ToVector()).ToArray(),
            };
        }
    }
}