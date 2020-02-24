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
            var startTime = FrameTimer.instance.stopwatch.ElapsedMilliseconds;
            callback();

            Debug.Log("Timer [" + description + "]: "
                + (FrameTimer.instance.stopwatch.ElapsedMilliseconds - startTime) + "ms");
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

        public static GameObject DrawCircle(this GameObject container,
                                            float radius, float lineWidth,
                                            Color c)
        {
            return Utility.DrawCircle(container.transform.position, radius, lineWidth, c);
        }
    }
}