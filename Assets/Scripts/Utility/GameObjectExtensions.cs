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

#if DEBUG
        public static void RunTimer(this MonoBehaviour behaviour,
                                    string description,
                                    System.Action callback)
        {
            var startTime = FrameTimer.instance.FrameDuration;
            callback();

            Debug.Log("Timer [" + description + "]: "
                + (FrameTimer.instance.FrameDuration - startTime) + "ms");
        }
#endif

        public static void DisableImmediateChildren(this GameObject obj)
        {
            for (int i = obj.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = obj.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }
        }

        public static void EnableImmediateChildren(this GameObject obj)
        {
            for (int i = obj.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = obj.transform.GetChild(i);
                child.gameObject.SetActive(true);
            }
        }

        public static void RemoveAllChildren(this GameObject obj)
        {
            for (int i = obj.transform.childCount - 1; i >= 0; --i)
            {
                Transform child = obj.transform.GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
        }

        public static GameObject InstantiateInactive(this MonoBehaviour obj, GameObject prefab)
        {
            var prevActive = prefab.activeSelf;
            prefab.SetActive(false);

            var inst = GameObject.Instantiate(prefab);
            prefab.SetActive(prevActive);

            return inst;
        }

        public static void SetPositionInLayer(this Transform transform, float x = 0f, float y = 0f)
        {
            transform.position = new Vector3(x, y, transform.position.z);
        }

        public static void SetPositionInLayer(this Transform transform, Vector2 vec)
        {
            transform.position = new Vector3(vec.x, vec.y, transform.position.z);
        }

        public static void SetLayer(this Transform transform, MapLayer layer, int positionInLayer = 0)
        {
            transform.position = new Vector3(transform.position.x,
                                             transform.position.y,
                                             Map.Layer(layer, positionInLayer));
        }

        public static void ScaleBy(this Transform transform, float scale = 1f)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public static GameObject DrawCircle(this GameObject container, float radius, float lineWidth, Color c)
        {
            var segments = 360;
            var obj = new GameObject();
            obj.name = "DebugCircle";
            obj.transform.position = new Vector3(container.transform.position.x, container.transform.position.y, -300);

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = segments + 1;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.GetUnlitMaterial(c);
            line.loop = true;

            var pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            var points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, -13f);
            }

            line.SetPositions(points);
            return obj;
        }

        public static void DrawRect(this GameObject container, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, float lineWidth, Color c)
        {
            var obj = new GameObject();
            obj.name = "DebugRect";
            obj.transform.position = new Vector3(0, 0, Map.Layer(MapLayer.Foreground, 9));

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = 4;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.GetUnlitMaterial(c);
            line.loop = true;

            var points = new Vector3[] { bl, tl, tr, br };
            line.SetPositions(points);
        }

        public static void DrawLine(this GameObject container, Vector3[] points, float lineWidth, Color c, bool loop = false)
        {
            var obj = new GameObject();
            obj.name = "DebugLine";
            obj.transform.position = new Vector3(0, 0, Map.Layer(MapLayer.Foreground, 9));

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = points.Length;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.GetUnlitMaterial(c);
            line.loop = loop;

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

        public static Vector2[] Points(this Rect rect)
        {
            return new Vector2[]
            {
                new Vector2(rect.x, rect.y),
                new Vector2(rect.x, rect.y + rect.height),
                new Vector2(rect.x + rect.width, rect.y + rect.height),
                new Vector2(rect.x + rect.width, rect.y),
            };
        }

        public static Texture2D ResizeNonDestructive(this Texture2D tex, int newWidth, int newHeight)
        {
            Debug.Assert(newWidth <= tex.width);
            Debug.Assert(newHeight <= tex.height);

            var newTex = new Texture2D(newWidth, newHeight);
            for (var x = 0; x < newWidth; ++x)
            {
                for (var y = 0; y < newHeight; ++y)
                {
                    newTex.SetPixel(x, y, tex.GetPixel(x, y));
                }
            }

            return newTex;
        }
    }
}