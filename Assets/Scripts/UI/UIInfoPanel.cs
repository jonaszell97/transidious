using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Transidious
{
    public class UIInfoPanel : MonoBehaviour
    {
        Dictionary<string, UIText> titles;
        Dictionary<string, TMP_Text> values;

        [SerializeField] GameObject titlePrefab;
        [SerializeField] GameObject valuePrefab;

        void Awake()
        {
            titles = new Dictionary<string, UIText>();
            values = new Dictionary<string, TMP_Text>();

            foreach (var txt in GetComponentsInChildren<UIText>())
            {
                titles.Add(txt.name, txt);
            }

            foreach (var txt in GetComponentsInChildren<TMP_Text>())
            {
                if (txt.name.Contains("Value"))
                {
                    values.Add(txt.name.Replace("Value", ""), txt);
                }
            }
        }

        public Tuple<UIText, TMP_Text> GetItem(string name)
        {
            return Tuple.Create(GetTitle(name), GetValue(name));
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

        public void AddItem(string name, string titleKey, string valueText = "")
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

            var valueObj = Instantiate(valuePrefab);
            var value = valueObj.GetComponent<TMP_Text>();

            value.transform.SetParent(this.transform);
            value.transform.localScale = new Vector3(1f, 1f, 1f);
            value.transform.position = new Vector3(uiTitle.transform.position.x,
                                                   uiTitle.transform.position.y,
                                                   this.transform.position.z);

            value.text = valueText;

            titles.Add(name, uiTitle);
            values.Add(name, value);
        }
    }
}