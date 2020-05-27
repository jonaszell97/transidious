using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class TransitVehicle : MonoBehaviour
    {
        /// The line this vehicle belongs to.
        public Line line;

        /// Reference to the simulation controller.
        SimulationController sim;

        /// The sprite renderer component.
        [SerializeField] SpriteRenderer spriteRenderer;

        /// The current route this vehicle is driving on.
        public int CurrentRoute { get; private set; }
        
        /// The path following component.
        private PointsFollower _pathFollow;

        /// The vehicle capacity.
        public int Capacity { get; private set; }

        /// The current passengers.
        public Dictionary<Stop, List<Stop.WaitingCitizen>> Passengers { get; private set; }

        /// The total passenger count.
        public int PassengerCount { get; private set; }

        /// The current velocity
        public Velocity Velocity { get; private set; }

        /// The next transit vehicle on the same line.
        public TransitVehicle Next { get; set; }

        /// The time we should wait until departing to the next stop.
        private TimeSpan? _waitingTime;

        /// The extra distance driven on the last route.
        private float _extraDistanceDriven;

        /// The next stop on the line.
        public Stop NextStop
        {
            get
            {
                if (CurrentRoute == -1)
                {
                    return line.routes.First().endStop;
                }
                if (CurrentRoute >= line.routes.Count)
                {
                    return line.routes.First().beginStop;
                }

                return line.routes[CurrentRoute].endStop;
            }
        }

        /// The distance (in meters) from the start of the line.
        private Distance DistanceFromStartOfLine
        {
            get
            {
                var baseDistance = 0f;
                if (CurrentRoute > 0)
                {
                    baseDistance = line.cumulativeLengths[CurrentRoute - 1];
                }

                return Distance.FromMeters(baseDistance + (_pathFollow?.TotalProgressAbsolute ?? 0f));
            }
        }

        /// The distance (in time) from the start of the line.
        private TimeSpan TimeFromStartOfLine =>
            (DistanceFromStartOfLine / line.AverageSpeed)
            + line.AverageStopDuration.Multiply(CurrentRoute);

        /// The distance (in time) to the next vehicle on the line.
        public TimeSpan DistanceToNext
        {
            get
            {
                var timeFromStart = TimeFromStartOfLine;
                var nextTimeFromStart = Next.TimeFromStartOfLine;

                if (timeFromStart <= nextTimeFromStart)
                {
                    return nextTimeFromStart - timeFromStart;
                }

                return line.TotalTravelTime - timeFromStart + nextTimeFromStart;
            }
        }

        public void Initialize(Line line, TransitVehicle next, int? capacity = null)
        {
            this.line = line;
            this.Next = next;
            this.spriteRenderer.color = line.color;
            this.sim = GameController.instance.sim;
            this.Passengers = new Dictionary<Stop, List<Stop.WaitingCitizen>>();
            this.PassengerCount = 0;
            this.CurrentRoute = 0;

            this.Velocity = Velocity.zero;
            this.Capacity = capacity ?? GetDefaultCapacity(line.type);

            var pos = line.routes[0].positions[0];
            transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.TransitStops, 1));
        }

        private static int GetDefaultCapacity(TransitType type)
        {
            switch (type)
            {
                case TransitType.Bus:
                default:
                    return 30;
                case TransitType.Tram:
                    return 100;
                case TransitType.Subway:
                    return 250;
                case TransitType.LightRail:
                    return 250;
                case TransitType.IntercityRail:
                    return 400;
                case TransitType.Ferry:
                    return 20;
            }
        }

        public void UpdateColor()
        {
            this.spriteRenderer.color = line.color;
        }

        public void SetStartingRoute(int routeIndex)
        {
            CurrentRoute = routeIndex;
        }

        public void StartDrive(int routeIndex = 0, float progress = 0f)
        {
            this.CurrentRoute = routeIndex;
            this.Velocity = line.AverageSpeed;

            var route = line.routes[CurrentRoute];
            var waitingCitizens = route.beginStop.GetWaitingCitizens(line);
            var taken = 0;

            while (PassengerCount + taken < Capacity && taken < waitingCitizens.Count)
            {
                var c = waitingCitizens[taken];
                if (!Passengers.ContainsKey(c.finalStop))
                {
                    Passengers.Add(c.finalStop, new List<Stop.WaitingCitizen>());
                }

                ++PassengerCount;
                ++taken;
                ++line.weeklyPassengers;

                Passengers[c.finalStop].Add(c);
                c.path.transitVehicle = this;
            }

            waitingCitizens.RemoveRange(0, taken);

            this.gameObject.SetActive(true);
            this.transform.SetPositionInLayer(route.positions.First());

            var expectedArrival = route.endStop.NextDeparture(line, sim.GameTime);
            var current = Next;

            while (current.CurrentRoute == CurrentRoute)
            {
                expectedArrival = route.endStop.NextDeparture(line, expectedArrival.AddSeconds(1));
                current = current.Next;
            }

            this._pathFollow = new PointsFollower(
                this.gameObject, route.positions, Velocity,
                () =>
                {
                    var diff = sim.GameTime - expectedArrival;
                    if (diff.TotalSeconds < 0f)
                    {
                        _waitingTime = diff;
                    }
                    else
                    {
                        _extraDistanceDriven = (line.AverageSpeed * diff).Meters;
                    }

                    _pathFollow = null;
                    _waitingTime = line.AverageStopDuration;
                    CurrentRoute = (CurrentRoute + 1) % line.routes.Count;

                    // Check citizens leaving here.
                    var stop = route.endStop;
                    if (Passengers.TryGetValue(stop, out List<Stop.WaitingCitizen> leaving))
                    {
                        foreach (var c in leaving)
                        {
                            c.path.CompleteStep();
                        }

                        PassengerCount -= leaving.Count;
                        leaving.Clear();
                    }

                    // Update modal.
                    if (MainUI.instance.transitVehicleModal.vehicle == this)
                    {
                        MainUI.instance.transitVehicleModal.UpdateAll();
                    }
                });

            if (progress > 0f)
            {
                _pathFollow.SimulateProgressAbsolute(progress);
            }

            if (_extraDistanceDriven > 0f)
            {
                _pathFollow.SimulateProgressAbsolute(_extraDistanceDriven);
                _extraDistanceDriven = 0f;
            }
        }

        void Update()
        {
            if (sim.game.Paused)
            {
                return;
            }

            var speedMultiplier = sim.SpeedMultiplier;
            for (var i = 0; i < speedMultiplier; ++i)
            {
                if (_pathFollow != null)
                {
                    _pathFollow.Update(Time.deltaTime);
                    continue;
                }

                var extraTime = 0f;
                if (_waitingTime.HasValue)
                {
                    var waitingSeconds = (float) _waitingTime.Value.TotalSeconds;
                    waitingSeconds -= Time.deltaTime;

                    if (waitingSeconds > 0f)
                    {
                        _waitingTime = TimeSpan.FromSeconds(waitingSeconds);
                        continue;
                    }

                    extraTime = -waitingSeconds;
                    _waitingTime = null;
                }

                var diff = (float) DistanceToNext.TotalMinutes - line.schedule.dayInterval;
                if (diff < 0f)
                {
                    return;
                }

                StartDrive(CurrentRoute);
                _pathFollow.Update(extraTime);
            }
        }

        public void ActivateModal()
        {
            var modal = MainUI.instance.transitVehicleModal;
            if (modal.vehicle == this)
            {
                modal.modal.Disable();
                return;
            }

            modal.SetVehicle(this);
            modal.modal.Enable();
        }

        public void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }
            
            ActivateModal();
        }
    }
}
