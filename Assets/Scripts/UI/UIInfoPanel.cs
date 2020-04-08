using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UIInfoPanel : MonoBehaviour
    {
        Dictionary<string, Tuple<GameObject, Image, UIText, TMP_Text>> items;
        [SerializeField] private GameObject itemPrefab;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (items != null)
            {
                return;
            }

            items = new Dictionary<string, Tuple<GameObject, Image, UIText, TMP_Text>>();

            var tf = transform;
            var childCount = tf.childCount;

            for (var i = 0; i < childCount; ++i)
            {
                var item = tf.GetChild(i);
                var icon = item.GetChild(0).GetComponent<Image>();
                var key = item.GetChild(1).GetComponent<UIText>();
                var value = item.GetChild(2).GetComponent<TMP_Text>();
                
                items.Add(item.name, Tuple.Create(item.gameObject, icon, key, value));
            }
        }

        public void HideItem(string name)
        {
            var item = GetItem(name);
            item.Item1.SetActive(false);
        }

        public void ShowItem(string name)
        {
            var item = GetItem(name);
            item.Item1.SetActive(true);
        }

        public Tuple<GameObject, Image, UIText, TMP_Text> GetItem(string name)
        {
            return items[name];
        }

        public bool HasItem(string name)
        {
            return items.ContainsKey(name);
        }

        public UIText GetTitle(string name)
        {
            if (items.TryGetValue(name, out var item))
            {
                return item.Item3;
            }

            return null;
        }

        public TMP_Text GetValue(string name)
        {
            if (items.TryGetValue(name, out var item))
            {
                return item.Item4;
            }

            return null;
        }

        public void SetTitle(string name, string key)
        {
            var titleObj = GetTitle(name);
            if (titleObj != null)
            {
                titleObj.SetKey(key);
            }
        }

        public void SetValue(string name, string value)
        {
            var valueObj = GetValue(name);
            if (valueObj != null)
            {
                valueObj.text = value;
            }
        }

        public class IconSettings
        {
            public Sprite icon;
            public Color color = Color.white;
        }

        public void AddClickableItem(string name, string titleKey, Color c, UnityAction callback)
        {
            AddItem(name, titleKey);

            var item = GetTitle(name);
            item.textMesh.color = c;

            var btn = item.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(callback);
        }

        public Tuple<GameObject, Image, UIText, TMP_Text> AddItem(string name, string titleKey, string valueText, string iconName)
        {
            return AddItem(name, titleKey, valueText, new IconSettings
            {
                icon = SpriteManager.GetSprite(iconName),
            });
        }

        public Tuple<GameObject, Image, UIText, TMP_Text> AddItem(string name, string titleKey, string valueText = "", IconSettings iconInfo = null)
        {
            var item = Instantiate(itemPrefab, this.transform, false).transform;
            var icon = item.GetChild(0).GetComponent<Image>();
            var key = item.GetChild(1).GetComponent<UIText>();
            var value = item.GetChild(2).GetComponent<TMP_Text>();

            key.SetKey(titleKey);
            
            if (iconInfo != null)
            {
                icon.sprite = iconInfo.icon;
                icon.preserveAspect = true;
                icon.color = iconInfo.color;
            }
            else
            {
                icon.enabled = false;
            }

            value.text = valueText;

            var result = Tuple.Create(item.gameObject, icon, key, value);
            items.Add(name, result);

            return result;
        }
    }
}