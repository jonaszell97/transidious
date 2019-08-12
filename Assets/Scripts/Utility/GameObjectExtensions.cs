using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrPutDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                           TKey key,
                                                           TValue defaultValue = default(TValue))
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }

            dictionary.Add(key, defaultValue);
            return defaultValue;
        }

        public static TValue GetOrPutDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                           TKey key,
                                                           System.Func<TValue> defaultValueProvider)
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }

            var defaultValue = defaultValueProvider();
            dictionary.Add(key, defaultValue);

            return defaultValue;
        }
    }

    public static class GameObjectExtensions
    {
        static System.Collections.IEnumerator RunNextFrameImpl(this MonoBehaviour behaviour, System.Action callback)
        {
            yield return null;
            callback();
            yield break;
        }

        public static void RunNextFrame(this MonoBehaviour behaviour, System.Action callback)
        {
            behaviour.gameObject.SetActive(true);
            behaviour.StartCoroutine(RunNextFrameImpl(behaviour, callback));
        }

        public static void RemoveAllChildren(this GameObject obj)
        {
            for (int i = obj.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = obj.transform.GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
        }

        public static void DrawCircle(this GameObject container, float radius, float lineWidth, Color c)
        {
            var segments = 360;

            LineRenderer line = container.GetComponent<LineRenderer>();
            if (line == null)
                line = container.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = segments + 1;
            line.material.color = c;

            var pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            var points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, -13f);
            }

            line.SetPositions(points);
        }
    }

    public static class Utility
    {

        public static T RandomElement<T>(System.Collections.Generic.List<T> coll)
        {
            return coll[UnityEngine.Random.Range(0, coll.Count)];
        }

        public static T RandomElement<T>(T[] coll)
        {
            return coll[UnityEngine.Random.Range(0, coll.Length)];
        }

        public static Color RandomColor
        {
            get
            {
                return new Color(
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f),
                    UnityEngine.Random.Range(0f, 1f)
                );
            }
        }
    }
}