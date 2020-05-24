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

        public void Initialize()
        {
            modal.Initialize();
        }

        public void SetIntersection(StreetIntersection intersection)
        {
            this.intersection = intersection;
            modal.SetTitle($"Intersection {intersection.Id}");

            var existing = _intersectingStreetList.childCount - 1;
            for (var i = existing; i < intersection.intersectingStreets.Count; ++i)
            {
                var inst = Instantiate(_intersectingStreetItemPrefab, _intersectingStreetList, false);
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

            for (var i = 0; i < intersection.intersectingStreets.Count; ++i)
            {
                var txt = _intersectingStreetList.GetChild(i + 1).GetComponent<TMP_Text>();
                var seg = intersection.intersectingStreets[i];
                
                txt.text = $"{intersection.RelativePosition(seg)}: {seg.name}, angle {intersection.GetAngle(seg):n0}°";
            }
        }
    }
}