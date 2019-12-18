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
        public TextAlignmentOptions labelAlignment = TextAlignmentOptions.Left;
        public TextAlignmentOptions valueAlignment = TextAlignmentOptions.Right;
        public int fontSize = -1;

        Dictionary<string, UIText> titles;
        Dictionary<string, TMP_Text> values;

        [SerializeField] GameObject titlePrefab;
        [SerializeField] GameObject valuePrefab;
        [SerializeField] GameObject iconPrefab;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (titles != null)
            {
                return;
            }

            titles = new Dictionary<string, UIText>();
            values = new Dictionary<string, TMP_Text>();

            foreach (var txt in GetComponentsInChildren<UIText>())
            {
                if (fontSize != -1)
                {
                    txt.textMesh.fontSizeMax = fontSize;
                    txt.textMesh.fontSizeMin = System.Math.Min(fontSize, txt.textMesh.fontSizeMin);
                }

                txt.textMesh.alignment = labelAlignment;
                titles.Add(txt.name, txt);
            }

            foreach (var txt in GetComponentsInChildren<TMP_Text>())
            {
                if (txt.name.Contains("Value"))
                {
                    if (fontSize != -1)
                    {
                        txt.fontSizeMax = fontSize;
                        txt.fontSizeMin = System.Math.Min(fontSize, txt.fontSizeMin);
                    }

                    txt.alignment = valueAlignment;
                    values.Add(txt.name.Replace("Value", ""), txt);
                }
            }
        }

        public void HideItem(string name)
        {
            var item = GetItem(name);
            if (item.Item1 != null)
            {
                item.Item1.gameObject.SetActive(false);
                item.Item2.gameObject.SetActive(false);
            }
        }

        public void ShowItem(string name)
        {
            var item = GetItem(name);
            if (item.Item1 != null)
            {
                item.Item1.gameObject.SetActive(true);
                item.Item2.gameObject.SetActive(true);
            }
        }

        public Tuple<UIText, TMP_Text> GetItem(string name)
        {
            return Tuple.Create(GetTitle(name), GetValue(name));
        }

        public bool HasItem(string name)
        {
            return titles.ContainsKey(name);
        }

        public UIText GetTitle(string name)
        {
            if (titles.TryGetValue(name, out UIText txt))
            {
                return txt;
            }

            return null;
        }

        public TMP_Text GetValue(string name)
        {
            if (values.TryGetValue(name, out TMP_Text txt))
            {
                return txt;
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
            public float scale = .75f;
            public float margin = 5;
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

        public void AddItem(string name, string titleKey, string valueText = "", IconSettings icon = null)
        {
            var titleObj = this.InstantiateInactive(titlePrefab);
            var uiTitle = titleObj.GetComponent<UIText>();
            uiTitle.SetKey(titleKey);

            uiTitle.transform.SetParent(this.transform);
            uiTitle.transform.localScale = new Vector3(1f, 1f, 1f);
            uiTitle.transform.position = new Vector3(uiTitle.transform.position.x,
                                                     uiTitle.transform.position.y,
                                                     this.transform.position.z);

            uiTitle.gameObject.SetActive(true);

            if (icon != null)
            {
                var iconObj = Instantiate(iconPrefab);
                var img = iconObj.GetComponent<Image>();
                img.sprite = icon.icon;
                img.preserveAspect = true;
                img.color = icon.color;
                img.transform.SetParent(uiTitle.transform, false);

                var height = GetComponent<GridLayoutGroup>().cellSize.y * icon.scale;
                
                var rc = img.GetComponent<RectTransform>();
                rc.sizeDelta = new Vector2(height, height);
                rc.anchoredPosition = new Vector2(height / 2f + icon.margin, rc.anchoredPosition.y);

                uiTitle.textMesh.margin = new Vector4(height + icon.margin * 2, 0f, 0f, 0f);
            }

            var valueObj = Instantiate(valuePrefab);
            var value = valueObj.GetComponent<TMP_Text>();

            value.transform.SetParent(this.transform);
            value.transform.localScale = new Vector3(1f, 1f, 1f);
            value.transform.position = new Vector3(uiTitle.transform.position.x,
                                                   uiTitle.transform.position.y,
                                                   this.transform.position.z);

            value.text = valueText;

            if (fontSize != -1)
            {
                uiTitle.textMesh.fontSizeMax = fontSize;
                uiTitle.textMesh.fontSizeMin = System.Math.Min(fontSize, uiTitle.textMesh.fontSizeMin);

                value.fontSizeMax = fontSize;
                value.fontSizeMin = System.Math.Min(fontSize, value.fontSizeMin);
            }

            uiTitle.textMesh.alignment = labelAlignment;
            value.alignment = valueAlignment;

            titles.Add(name, uiTitle);
            values.Add(name, value);
        }
    }
}