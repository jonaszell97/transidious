using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UITransitVehicleModal : MonoBehaviour
    {
        /// The transit vehicle currently displayed by the modal.
        public TransitVehicle vehicle;

        /// Reference to the modal component.
        public UIModal modal;

        /// The info panel.
        public UIInfoPanel panel;

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            
            modal.titleInput.interactable = false;
            modal.onClose.AddListener(() =>
            {
                this.vehicle = null;
                GameController.instance.input.StopFollowing();
            });

            panel.AddItem("Passengers", "ui:transit:passengers", "", "Sprites/ui_citizen_head");
            
            var ns = panel.AddItem("NextStop", "ui:transit:next_stop", 
                                                            "", "Sprites/stop_ring");
            ns.Item4.gameObject.AddComponent<UILocationLink>();
        }

        public void SetVehicle(TransitVehicle vehicle)
        {
            this.vehicle = vehicle;

            this.modal.SetTitle($"Vehicle on line {vehicle.line.name}");
            this.panel.SetValue("Passengers", $"{vehicle.passengerCount} / {vehicle.capacity}");

            var nextStop = this.panel.GetValue("NextStop");
            nextStop.text = vehicle.NextStop.name;

            var link = nextStop.GetComponent<UILocationLink>();
            link.SetLocation(vehicle.NextStop.Location);
            link.postMoveListener = () =>
            {
                vehicle.NextStop.ActivateModal();
            };

            GameController.instance.input.FollowObject(
                vehicle.gameObject, InputController.FollowingMode.Visible);
        }
    }
}