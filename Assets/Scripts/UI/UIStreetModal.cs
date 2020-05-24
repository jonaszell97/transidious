using Transidious;
using UnityEngine;

namespace UI
{
    public class UIStreetModal : MonoBehaviour
    {
        /// The selected street segment.
        public StreetSegment segment;

        /// The UI modal.
        public UIModal modal;

        /// The info panel.
        [SerializeField] private UIInfoPanel _infoPanel;
        
        /// The info panel.
        [SerializeField] private UIInfoPanel _streetInfoPanel;

        public void Initialize()
        {
            modal.Initialize();
            modal.onClose.AddListener(() => segment.Unhighlight());
            

            _infoPanel.Initialize();
            _infoPanel.AddItem("Type", "Type", "", "Sprites/ui_edit");
            _infoPanel.AddItem("Length", "Length", "", "Sprites/ui_line_partial");
            _infoPanel.AddItem("Bridge", "Bridge", "", "Sprites/ui_line_partial");
            _infoPanel.AddItem("Position", "Position", "", "Sprites/ui_citizen");
            _infoPanel.AddItem("ParkingSpots", "Parking Spots", "", "Sprites/ui_car");
            _infoPanel.AddItem("OppositeEnd", "Opposite End", "", "Sprites/ui_edit");
            _infoPanel.AddItem("OppositeStart", "Opposite Start", "", "Sprites/ui_edit");
            
            _streetInfoPanel.Initialize();
            _streetInfoPanel.AddItem("Name", "Name", "", "Sprites/ui_edit");
            _streetInfoPanel.AddItem("Length", "Length", "", "Sprites/ui_line_partial");
            _streetInfoPanel.AddItem("Lit", "Lit", "", "Sprites/ui_line_partial");
            _streetInfoPanel.AddItem("Lanes", "Lanes", "", "Sprites/ui_line_partial");
            _streetInfoPanel.AddItem("MaxSpeed", "Max Speed", "", "Sprites/ui_line_partial");
            
        }

        public void UpdateFrequentChanges()
        {
            _infoPanel.SetValue("ParkingSpots", $"{segment.ResidentCount} / {segment.Capacity}");
        }

        public void UpdateAll()
        {
            UpdateFrequentChanges();
            _infoPanel.SetValue("Type", segment.street.type.ToString());
            _infoPanel.SetValue("Length", segment.length.ToString("n2") + "m");
            _infoPanel.SetValue("Position", segment.position.ToString());
            _infoPanel.SetValue("Bridge", segment.IsBridge ? "yes" : "no");

            StreetSegment oppositeEnd = null, oppositeStart = null;
            if (segment.startIntersection != null && !segment.IsOneWay)
            {
                oppositeStart = segment.startIntersection.FindPotentialOppositeSegment(segment)?.Item1;
            }
            if (segment.endIntersection != null)
            {
                oppositeEnd = segment.endIntersection.FindPotentialOppositeSegment(segment)?.Item1;
            }

            _infoPanel.SetValue("OppositeStart", oppositeStart?.name ?? "None");
            _infoPanel.SetValue("OppositeEnd", oppositeEnd?.name ?? "None");

            _streetInfoPanel.SetValue("Name", segment.street.DisplayName);
            _streetInfoPanel.SetValue("Length", segment.street.length.ToString("n2"));
            _streetInfoPanel.SetValue("Lit", segment.street.lit ? "yes" : "no");
            _streetInfoPanel.SetValue("Lanes", segment.street.lanes.ToString());
            _streetInfoPanel.SetValue("MaxSpeed", segment.street.maxspeed.RealTimeKPH.ToString("n0") + " kph");
        }

        public void SetStreet(StreetSegment seg)
        {
            this.segment = seg;
            
            modal.SetTitle(seg.name);
            UpdateAll();
        }
    }
}