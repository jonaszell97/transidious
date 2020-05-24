using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Transidious.UI;

namespace Transidious
{
    public class UIInfoPanel : MonoBehaviour
    {
        public struct Item
        {
            /// The item's game object.
            public GameObject GameObject;

            /// The item's icon (or null).
            public Image Icon;

            /// The item's key text.
            public UIText Key;

            /// The item's value text.
            public TMP_Text Value;

            /// The item's progress bar (or null).
            public UIProgressBar ProgressBar;
        }

        Dictionary<string, Item> items;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject progressItemPrefab;

        /// The default color gradient for progress bars.
        private Gradient _defaultGradient;
        private Gradient DefaultGradient
        {
            get
            {
                if (_defaultGradient == null)
                {
                    _defaultGradient = new Gradient();
                    var keys = new GradientColorKey[3];
                    keys[0].color = Colors.GetColor("ui.happinessLow");
                    keys[0].time = 0f;
                    keys[1].color = Colors.GetColor("ui.happinessMedium");
                    keys[1].time = .5f;
                    keys[2].color = Colors.GetColor("ui.happinessHigh");
                    keys[2].time = .8f;
                    
                    var alphaKeys = new GradientAlphaKey[1];
                    alphaKeys[0].alpha = 1f;
                    alphaKeys[0].time = 0f;

                    _defaultGradient.SetKeys(keys, alphaKeys);
                }
                
                return _defaultGradient;
            }
        }

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

            items = new Dictionary<string, Item>();

            var tf = transform;
            var childCount = tf.childCount;

            for (var i = 0; i < childCount; ++i)
            {
                var item = tf.GetChild(i);
                var icon = item.GetChild(0).GetComponent<Image>();
                var key = item.GetChild(1).GetComponent<UIText>();
                var value = item.GetChild(2).GetComponent<TMP_Text>();

                items.Add(item.name, new Item() {
                    GameObject = item.gameObject,
                    Icon = icon, 
                    Key = key,
                    Value = value,
                    ProgressBar = null,
                });
            }
        }

        public void HideItem(string name)
        {
            var item = GetItem(name);
            item.GameObject.SetActive(false);
        }

        public void ShowItem(string name)
        {
            var item = GetItem(name);
            item.GameObject.SetActive(true);
        }

        public Item GetItem(string name)
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
                return item.Key;
            }

            return null;
        }

        public TMP_Text GetValue(string name)
        {
            if (items.TryGetValue(name, out var item))
            {
                return item.Value;
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

        public void SetProgress(string name, float progress)
        {
            var item = GetItem(name);
            Debug.Assert(item.ProgressBar != null, "item is not a progress bar!");

            item.ProgressBar.SetProgress(progress);
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

        public Item AddItem(string name, string titleKey, string valueText, string iconName)
        {
            return AddItem(name, titleKey, valueText, new IconSettings
            {
                icon = SpriteManager.GetSprite(iconName),
            });
        }

        public Item AddItem(string name, string titleKey, 
                            string valueText = "",  IconSettings iconInfo = null)
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

            var newItem = new Item
            {
                GameObject = item.gameObject,
                Icon = icon,
                Key = key,
                Value = value,
            };

            items.Add(name, newItem);
            return newItem;
        }

        public Item AddProgressItem(string name, string titleKey,
                                    string iconName,
                                    Gradient gradient = null)
        {
            return AddProgressItem(name, titleKey, gradient, new IconSettings
            {
                icon = SpriteManager.GetSprite(iconName),
            });
        }

        public Item AddProgressItem(string name, string titleKey,
                                    Gradient gradient = null,
                                    IconSettings iconInfo = null)
        {
            var item = Instantiate(progressItemPrefab, this.transform, false).transform;
            var icon = item.GetChild(0).GetComponent<Image>();
            var key = item.GetChild(1).GetComponent<UIText>();
            var value = item.GetChild(2).GetComponent<TMP_Text>();

            var progressBar = value.transform.GetChild(1).GetComponent<UIProgressBar>();
            progressBar.Gradient = gradient ?? DefaultGradient;

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

            var newItem = new Item
            {
                GameObject = item.gameObject,
                Icon = icon,
                Key = key,
                Value = value,
                ProgressBar = progressBar,
            };

            items.Add(name, newItem);
            return newItem;
        }
    }
}