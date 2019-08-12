using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Transidious
{
    public interface IPersistable
    {
        object Serialize(SerializationContext context);
        
        object Deserialize(SerializationContext context, object data);
        
        void Finalize(SerializationContext context,
                      object deserializedObject,
                      object data);
    }

    [System.Serializable]
    public class SerializationContext : System.Runtime.Serialization.ISerializable
    {
        object subject;
        Dictionary<int, object> serializedClasses;
        int lastAssignedID;

        public SerializationContext(object subject)
        {
            this.subject = subject;
            this.serializedClasses = new Dictionary<int, object>();
            this.lastAssignedID = 0;
        }

        public int AddClassInstance<T>(T value) where T: class
        {
            var id = lastAssignedID++;
            serializedClasses.Add(id, value);

            return id;
        }

        public T GetClassInstance<T>(int id) where T: class
        {
            if (serializedClasses.TryGetValue(id, out object value))
            {
                return value as T;
            }

            return null;
        }

        protected SerializationContext(SerializationInfo info, StreamingContext context)
        {
            this.subject = info.GetValue("subject", typeof(object));
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("subject", subject);
            info.AddValue("classTable", serializedClasses.Serialize());
        }
    }

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
            return new SerializableList<TKey> {
                elements = list?.ToArray() ?? null
            };
        }

        public static SerializableDictionary<TKey, TValue> Serialize<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            return new SerializableDictionary<TKey, TValue>(dict);
        }
    }
}