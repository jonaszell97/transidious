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

        void Start()
        {
            modal.titleInput.interactable = false;
            modal.onClose.AddListener(() =>
            {
                this.vehicle = null;
                GameController.instance.input.StopFollowing();
            });
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