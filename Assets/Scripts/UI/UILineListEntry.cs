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
        private static UILineListEntry _selectedEntry;

        public void Initialize()
        {
            this.onSelect = new UnityEvent();
            
            var btn = this.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (_selectedEntry == this)
                {
                    return;
                }

                this.Select();
            });
        }

        public void Select()
        {
            _selectedEntry?.Deselect();
            _selectedEntry = this;

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
