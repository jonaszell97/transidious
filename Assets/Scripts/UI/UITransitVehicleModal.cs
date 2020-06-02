using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Transidious.UI;

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

        /// The passenger list.
        public UICitizenList passengerList;

        public void Initialize()
        {
            modal.Initialize();
            panel.Initialize();
            passengerList.Initialize("ui:transit:passengers");
            
            modal.titleInput.interactable = false;
            modal.onClose.AddListener(() =>
            {
                this.vehicle = null;
                GameController.instance.input.StopFollowing();
            });

            panel.AddItem("Passengers", "ui:transit:passengers", "", "Sprites/ui_citizen_head");
            
            var ns = panel.AddItem("NextStop", "ui:transit:next_stop", 
                                                            "", "Sprites/stop_ring");
            ns.Value.gameObject.AddComponent<UILocationLink>();

#if DEBUG
            panel.AddClickableItem("DistanceToNext", "Distance to Next", Color.white, () =>
            {
                GameController.instance.input.MoveTowards(vehicle.Next.transform.position, 0f, () =>
                    {
                        vehicle.Next.ActivateModal();
                    });
            });

            panel.AddItem("DistanceFromStart", "Distance From Start");
            panel.AddItem("DistanceToNextStop", "Distance To Next Stop");
            panel.AddItem("TimeToNextStop", "Time To Next Stop");
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

            UpdatePassengers();
        }

        private void UpdatePassengers()
        {
            if (vehicle.Passengers.Count == 0)
            {
                passengerList.gameObject.SetActive(false);
                return;
            }

            var passengers = new List<Citizen>();
            var destinations = new Dictionary<Citizen, Stop>();

            foreach (var pair in vehicle.Passengers)
            {
                foreach (var wc in pair.Value)
                {
                    passengers.Add(wc.path.citizen);
                    destinations.Add(wc.path.citizen, pair.Key);
                }
            }

            if (passengers.Count == 0)
            {
                passengerList.gameObject.SetActive(false);
                return;
            }

            passengerList.gameObject.SetActive(true);
            passengerList.SetCitizens(passengers, c => destinations[c].Name);
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
                panel.SetValue("TimeToNextStop", $"{vehicle.TimeToNextStop.TotalMinutes:n2} min");
                panel.SetValue("DistanceFromStart", $"{vehicle.DistanceFromStartOfLine.Meters:n2} m");
                panel.SetValue("DistanceToNextStop", $"{vehicle.DistanceFromNextStop.Meters:n2} m");
            }
        }
#endif
    }
}