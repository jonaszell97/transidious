using System;
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

#if DEBUG
            panel.AddClickableItem("DistanceToNext", "Distance to Next", Color.white, () =>
            {
                GameController.instance.input.MoveTowards(vehicle.Next.transform.position, 0f, () =>
                    {
                        vehicle.Next.ActivateModal();
                    });
            });
#endif
        }

        public void UpdateAll()
        {
            this.panel.SetValue("Passengers", $"{vehicle.PassengerCount} / {vehicle.Capacity}");
            
            var nextStop = this.panel.GetValue("NextStop");
            nextStop.text = vehicle.NextStop.name;

            var link = nextStop.GetComponent<UILocationLink>();
            link.SetLocation(vehicle.NextStop.Location);
            link.postMoveListener = () =>
            {
                vehicle.NextStop.ActivateModal();
            };
        }

        public void SetVehicle(TransitVehicle vehicle)
        {
            this.vehicle = vehicle;

            this.modal.SetTitle($"Vehicle on line {vehicle.line.name}");
            UpdateAll();

            GameController.instance.input.FollowObject(vehicle.gameObject, InputController.FollowingMode.Center);
        }

#if DEBUG
        private void Update()
        {
            if (vehicle != null)
            {
                panel.SetValue("DistanceToNext", $"{vehicle.DistanceToNext.TotalMinutes:n2} min");
            }
        }
#endif
    }
}