using System;
using System.Collections.Generic;
using TMPro;
using Transidious;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIIntersectionModal : MonoBehaviour
    {
        /// The selected street intersection.
        public StreetIntersection intersection;

        /// The UI modal.
        public UIModal modal;

        /// The info panel.
        [SerializeField] private RectTransform _intersectingStreetList;

        /// Prefab for street items.
        [SerializeField] private GameObject _intersectingStreetItemPrefab;

        /// The info panel.
        [SerializeField] private UIInfoPanel _infoPanel;

        public void Initialize()
        {
            modal.Initialize();

            _infoPanel.Initialize();
            _infoPanel.AddItem("OccupationMask", "Occupation Mask");
            _infoPanel.AddItem("Pattern", "Pattern");
            _infoPanel.AddClickableItem("Paths", "Show Intersection Paths", Color.white, () =>
            {
                intersection.RenderPaths();
            });
        }

        public void SetIntersection(StreetIntersection intersection)
        {
            this.intersection = intersection;
            modal.SetTitle($"Intersection {intersection.Id}");

            var existing = _intersectingStreetList.childCount - 1;
            var i = existing;
            
            for (; i < intersection.IntersectingStreets.Count; ++i)
            {
                var inst = Instantiate(_intersectingStreetItemPrefab, _intersectingStreetList, false);
                inst.name = $"IntersectingStreetItem {i}";
                
                var n = i;
                inst.GetComponent<Button>().onClick.AddListener(() =>
                {
                    var seg = this.intersection.IntersectingStreets[n];
                    var startPos = seg.positions.Count / 2;
                    var endPos = startPos < seg.positions.Count - 1 ? startPos + 1 : startPos - 1;
                    var dir = (seg.positions[endPos] - seg.positions[startPos]);
                    var middlePos = seg.positions[startPos] + dir * .5f;

                    GameController.instance.input.MoveTowards(middlePos, 0f, () =>
                    {
                        seg.ActivateModal();
                    });
                });
            }

            for (i = 0; i < intersection.IntersectingStreets.Count; ++i)
            {
                var txt = _intersectingStreetList.GetChild(i + 1).GetComponent<TMP_Text>();
                txt.gameObject.SetActive(true);

                var seg = intersection.IntersectingStreets[i];
                txt.text = $"{intersection.RelativePosition(seg)}: {seg.name}, angle {intersection.GetAngle(seg):n0}°";
            }

            for (; i < existing; ++i)
            {
                _intersectingStreetList.GetChild(i + 1).gameObject.SetActive(false);
            }

            _infoPanel.SetValue("Pattern", intersection.Pattern?.PatternType.ToString() ?? "-");
            UpdateFrequentChanges();
        }

        private void UpdateFrequentChanges()
        {
            _infoPanel.SetValue("OccupationMask",
                "0b" + Convert.ToString((long)IDM.IntersectionOccupation[intersection].OccupationStatus, 2));
        }

#if DEBUG
        private void Update()
        {
            if (modal.Active)
            {
                UpdateFrequentChanges();
            }
        }
#endif
    }
}