using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious
{
    public class UILineListEntry : MonoBehaviour
    {
        public Line line;
        public UILineLogo lineLogo;
        public TMP_Text lineName;
        public TMP_Text linePassengers;
        public TMP_Text lineEfficiency;
        public Image imgComponent;
        public UnityEvent onSelect;
        public static UILineListEntry selectedEntry;

        private void Awake()
        {
            this.onSelect = new UnityEvent();

            var rc = GetComponent<RectTransform>();
            rc.localScale = new Vector3(1f, 1f, 1f);
            rc.localPosition = new Vector3(rc.localPosition.x, rc.localPosition.y, 0f);
            
            var btn = this.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (selectedEntry == this)
                {
                    return;
                }

                this.Select();
            });
        }

        public void Select()
        {
            selectedEntry?.Deselect();
            selectedEntry = this;

            this.imgComponent.enabled = true;
            onSelect.Invoke();
        }

        public void Deselect()
        {
            this.imgComponent.enabled = false;
        }

        public void SetLine(Line line)
        {
            this.line = line;
            lineLogo.SetLine(line, true);
            lineName.text = line.name;
            linePassengers.text = UnityEngine.Random.Range(100, 10000).ToString();
            lineEfficiency.text = UnityEngine.Random.Range(0, 100).ToString() + "%";
        }
    }
}
