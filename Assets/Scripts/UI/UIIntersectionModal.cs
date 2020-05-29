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
            _infoPanel.AddItem("WaitingCars", "Waiting Cars");
            _infoPanel.AddItem("PassedCars", "Passed Cars");
            _infoPanel.AddClickableItem("Paths", "Show Intersection Paths", Color.white, () =>
            {
                var builder = GameController.instance.sim.trafficSim.StreetPathBuilder;
                var points = new List<Vector2>();

                foreach (var incoming in intersection.IncomingStreets)
                {
                    foreach (var outgoing in intersection.OutgoingStreets)
                    {
                        var path = builder.GetIntersectionPath(intersection, incoming, outgoing);
                        path.AddPoints(points, 5);

                        var color = RNG.RandomColor;
                        Utility.DrawLine(points.ToArray(), 1f, color, Map.Layer(MapLayer.Foreground), false, true);

                        points.Clear();
                    }
                }
            });
        }

        public void SetIntersection(StreetIntersection intersection)
        {
            this.intersection = intersection;
            modal.SetTitle($"Intersection {intersection.Id}");

            var existing = _intersectingStreetList.childCount - 1;
            var i = existing;
            
            for (; i < intersection.intersectingStreets.Count; ++i)
            {
                var inst = Instantiate(_intersectingStreetItemPrefab, _intersectingStreetList, false);
                inst.name = $"IntersectingStreetItem {i}";
                
                var n = i;
                inst.GetComponent<Button>().onClick.AddListener(() =>
                {
                    var seg = this.intersection.intersectingStreets[n];
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

            for (i = 0; i < intersection.intersectingStreets.Count; ++i)
            {
                var txt = _intersectingStreetList.GetChild(i + 1).GetComponent<TMP_Text>();
                txt.gameObject.SetActive(true);

                var seg = intersection.intersectingStreets[i];
                txt.text = $"{intersection.RelativePosition(seg)}: {seg.name}, angle {intersection.GetAngle(seg):n0}°";
            }

            for (; i < existing; ++i)
            {
                _intersectingStreetList.GetChild(i + 1).gameObject.SetActive(false);
            }
            
            UpdateFrequentChanges();
        }

        private void UpdateFrequentChanges()
        {
            _infoPanel.SetValue("OccupationMask",
                "0b" + Convert.ToString(IDM.IntersectionOccupation[intersection].OccupationStatus, 2));
            _infoPanel.SetValue("WaitingCars", IDM.IntersectionOccupation[intersection].WaitingCars.ToString());
            _infoPanel.SetValue("PassedCars", IDM.IntersectionOccupation[intersection].PassedCars.ToString());
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