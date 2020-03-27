using System;
using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public static class Colors
    {
        [Serializable]
        struct ColorItem
        {
            public string key;
            public string value;

            public void Deconstruct(out string key, out string value)
            {
                key = this.key;
                value = this.value;
            }
        }

        [Serializable]
        struct ColorProfile
        {
            public ColorItem[] colors;
        }

        /// The loaded color dictionary.
        private static Dictionary<string, Color> colors;

        /// Loads the colors from disk.
        private static void LoadColors()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            colors = new Dictionary<string, Color>();

            var file = Resources.Load("Config/ColorProfile") as TextAsset;
            Debug.Assert(file != null, "color profile not found");

            var obj = JsonUtility.FromJson<ColorProfile>(file.text);
            foreach (var (key, value) in obj.colors)
            {
                ColorUtility.TryParseHtmlString(value, out Color c);
                colors.Add(key, c);
            }
        }

        /// Get a color by key.
        public static Color GetColor(string key)
        {
            if (colors == null)
            {
                LoadColors();
            }

            return colors[key];
        }

        public static Color GetDefaultSystemColor(TransitType system)
        {
            return GetColor($"transit.default{system}");
        }
    }
}