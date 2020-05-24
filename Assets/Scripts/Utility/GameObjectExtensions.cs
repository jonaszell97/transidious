using System;
using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public static class DictionaryExtensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
        
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

        public static TValue SecondToLast<TValue>(this IReadOnlyList<TValue> list)
        {
            return list[list.Count - 2];
        }

        public static TValue TryGet<TValue>(this IReadOnlyList<TValue> list, int index)
        {
            return list.Count > index ? list[index] : default(TValue);
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

        public class Timer : IDisposable
        {
            private readonly long _startTime;
            private readonly string _description;
            private readonly int _iterations;

            public Timer(string description, int iterations)
            {
                _description = description;
                _startTime = DateTime.Now.Ticks;
                _iterations = iterations;

                Debug.Log($"Starting '{description}' at {DateTime.Now.ToShortTimeString()}");
            }

            public void Dispose()
            {
                var t = TimeSpan.FromTicks(DateTime.Now.Ticks - _startTime).TotalMilliseconds;
                var msg = $"[{_description}] {t:n0}ms";

                if (_iterations > 1)
                {
                    msg += $" (avg {t / _iterations:n2}ms)";
                }
                
                Debug.Log(msg);
            }
        }

        public static Timer CreateTimer(this System.Object obj, string description = null, int iterations = 1) 
        {
            return new Timer(description ?? "<Unnamed>", iterations);
        }

        public static int MinuteOfDay(this DateTime dt)
        {
            return dt.Hour * 60 + dt.Minute;
        }

        public static Weekday GetWeekday(this DateTime dt)
        {
            var dayOfWeek = dt.DayOfWeek;
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return Weekday.Monday;
                case DayOfWeek.Tuesday:
                    return Weekday.Tuesday;
                case DayOfWeek.Wednesday:
                    return Weekday.Wednesday;
                case DayOfWeek.Thursday:
                    return Weekday.Thursday;
                case DayOfWeek.Friday:
                    return Weekday.Friday;
                case DayOfWeek.Saturday:
                    return Weekday.Saturday;
                case DayOfWeek.Sunday:
                    return Weekday.Sunday;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Weekday AddDays(this Weekday wd, int days)
        {
            days %= 7;

            var mask = 0b1111111;
            var result = (int)wd << days;

            if ((result & mask) == 0)
            {
                return (Weekday) ((int) wd >> (7 - days));
            }

            return (Weekday) result;
        }

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

        public static Vector2 WithX(this Vector2 v, float x)
        {
            return new Vector2(x, v.y);
        }
        
        public static Vector2 WithY(this Vector2 v, float y)
        {
            return new Vector2(v.x, y);
        }
        
        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }
        
        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        public static Vector3 WithZ(this Vector2 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        public static GameObject DrawCircle(this GameObject container,
                                            float radius, float lineWidth,
                                            Color c)
        {
            return Utility.DrawCircle(container.transform.position, radius, lineWidth, c);
        }
    }
}