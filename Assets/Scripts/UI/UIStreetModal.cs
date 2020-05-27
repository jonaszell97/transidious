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
        [SerializeField] private UIInfoPanel infoPanel;
        
        /// The info panel.
        [SerializeField] private UIInfoPanel streetInfoPanel;

        public void Initialize()
        {
            modal.Initialize();
            modal.onClose.AddListener(() => segment.Unhighlight());

            infoPanel.Initialize();
            infoPanel.AddItem("Type", "Type", "", "Sprites/ui_edit");
            infoPanel.AddItem("Length", "Length", "", "Sprites/ui_line_partial");
            infoPanel.AddItem("Bridge", "Bridge", "", "Sprites/ui_line_partial");
            infoPanel.AddItem("Position", "Position", "", "Sprites/ui_citizen");
            infoPanel.AddItem("ParkingSpots", "Parking Spots", "", "Sprites/ui_car");
            
            infoPanel.AddClickableItem("Start Intersection", "Start Intersection", Color.white, () =>
            {
                GameController.instance.input.MoveTowards(segment.startIntersection.position, 0f, () =>
                {
                    segment.startIntersection.ActivateModal();
                });
            });
            
            infoPanel.AddClickableItem("End Intersection", "End Intersection", Color.white, () =>
            {
                GameController.instance.input.MoveTowards(segment.endIntersection.position, 0f, () =>
                {
                    segment.endIntersection.ActivateModal();
                });
            });
            
            infoPanel.AddClickableItem("Points", "Show Points", Color.white, () =>
            {
                foreach (var pos in segment.positions)
                {
                    Utility.DrawCircle(pos, 2f, 2f, Color.red);
                }
            });
            
            infoPanel.AddClickableItem("Paths", "Show Paths", Color.white, () =>
            {
                var builder = GameController.instance.sim.trafficSim.StreetPathBuilder;
                var points = new System.Collections.Generic.List<Vector2>();

                for (var i = 0; i < segment.street.lanes; ++i)
                {
                    var path = builder.GetPath(segment, i);
                    path.AddPoints(points, 5);

                    var color = RNG.RandomColor;
                    foreach (var pt in points)
                    {
                        Utility.DrawCircle(pt, 1f, 1f, color);
                    }

                    Utility.DrawLine(points.ToArray(), 1f, color, Map.Layer(MapLayer.Foreground), false, true);
                    points.Clear();
                }
            });
            
            streetInfoPanel.Initialize();
            streetInfoPanel.AddItem("Name", "Name", "", "Sprites/ui_edit");
            streetInfoPanel.AddItem("Length", "Length", "", "Sprites/ui_line_partial");
            streetInfoPanel.AddItem("Lit", "Lit", "", "Sprites/ui_line_partial");
            streetInfoPanel.AddItem("Lanes", "Lanes", "", "Sprites/ui_line_partial");
            streetInfoPanel.AddItem("MaxSpeed", "Max Speed", "", "Sprites/ui_line_partial");
            
        }

        public void UpdateFrequentChanges()
        {
            infoPanel.SetValue("ParkingSpots", 
                $"{segment.GetOccupancyCount(OccupancyKind.ParkingCitizen)} / {segment.Capacity}");
        }

        public void UpdateAll()
        {
            UpdateFrequentChanges();
            infoPanel.SetValue("Type", segment.street.type.ToString());
            infoPanel.SetValue("Length", segment.length.ToString("n2") + "m");
            infoPanel.SetValue("Position", segment.position.ToString());
            infoPanel.SetValue("Bridge", segment.IsBridge ? "yes" : "no");

            streetInfoPanel.SetValue("Name", segment.street.DisplayName);
            streetInfoPanel.SetValue("Length", segment.street.length.ToString("n2"));
            streetInfoPanel.SetValue("Lit", segment.street.lit ? "yes" : "no");
            streetInfoPanel.SetValue("Lanes", segment.street.lanes.ToString());
            streetInfoPanel.SetValue("MaxSpeed", segment.street.maxspeed.RealTimeKPH.ToString("n0") + " kph");
        }

        public void SetStreet(StreetSegment seg)
        {
            this.segment = seg;
            
            modal.SetTitle(seg.name);
            UpdateAll();
        }
    }
}