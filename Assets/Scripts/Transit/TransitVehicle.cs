using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class TransitVehicle : MonoBehaviour
    {
        /// <summary>
        /// The line this vehicle belongs to.
        /// </summary>
        public Line line;

        /// <summary>
        /// Reference to the simulation controller.
        /// </summary>
        SimulationController sim;

        /// <summary>
        /// The sprite renderer component.
        /// </summary>
        [SerializeField] SpriteRenderer spriteRenderer;

        /// <summary>
        /// The current route this vehicle is driving on.
        /// </summary>
        int currentRoute = 0;

        /// <summary>
        /// The path following component.
        /// </summary>
        PathFollowingObject pathFollow;

        /// The vehicle capacity.
        public int capacity;

        /// <summary>
        /// The current passengers.
        /// </summary>
        public Dictionary<Stop, List<Stop.WaitingCitizen>> passengers;

        public int passengerCount;
        Velocity velocity;

        public Stop NextStop
        {
            get
            {
                if (currentRoute >= line.routes.Count)
                {
                    return null;
                }

                return line.routes[currentRoute].endStop;
            }
        }

        public void Initialize(Line line, Velocity? velocity = null, int? capacity = null)
        {
            this.line = line;
            this.spriteRenderer.color = line.color;
            this.sim = GameController.instance.sim;
            this.passengers = new Dictionary<Stop, List<Stop.WaitingCitizen>>();
            this.passengerCount = 0;
            this.currentRoute = line.routes.Count;

            this.velocity = velocity ?? line.AverageSpeed;
            this.capacity = capacity ?? GetDefaultCapacity(line.type);
        }

        public static int GetDefaultCapacity(TransitType type)
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

        static System.Collections.Generic.Dictionary<Tuple<Line, Stop>, DateTime> lastStopAtStation
            = new System.Collections.Generic.Dictionary<Tuple<Line, Stop>, DateTime>();

        DateTime _startTime;
        float _length;

        public void StartDrive(int routeIndex = 0)
        {
            if (routeIndex >= line.routes.Count)
            {
                return;
            }

            if (routeIndex == 0)
            {
                _length = 0f;
                _startTime = sim.GameTime;
            }

            this.currentRoute = routeIndex;

            var route = line.routes[currentRoute];
            if ((route.positions?.Count ?? 0) == 0)
            {
                return;
            }

            var trafficSim = sim.trafficSim;
            var waitingCitizens = route.beginStop.GetWaitingCitizens(line);
            var taken = 0;

            while (passengerCount + taken < capacity && taken < waitingCitizens.Count)
            {
                var c = waitingCitizens[taken];
                if (!passengers.ContainsKey(c.finalStop))
                {
                    passengers.Add(c.finalStop, new List<Stop.WaitingCitizen>());
                }

                ++passengerCount;
                ++taken;
                ++line.weeklyPassengers;

                passengers[c.finalStop].Add(c);
                c.path.transitVehicle = this;
            }

            waitingCitizens.RemoveRange(0, taken);

            this.gameObject.SetActive(true);
            this.transform.SetPositionInLayer(route.positions[0]);

            this.pathFollow = new PathFollowingObject(
                sim, this.gameObject, route.positions,
                velocity,
                () =>
                {
                  var nextDep = route.endStop.NextDeparture(line, sim.GameTime - TimeSpan.FromMinutes(5));
                  if (currentRoute < line.routes.Count - 1)
                  {
                      nextStopTime = nextDep.Add(line.stopDuration);

                      var diff = sim.GameTime - nextDep;
                      if (System.Math.Abs(diff.TotalSeconds) >= 100)
                      {
                          // GameController.instance.EnterPause();
                          GameController.instance.input.MoveTowards(transform.position, 100f);
                          Debug.LogError($"Diff: {diff.TotalSeconds:n2}sec, next dep {nextDep.ToLongTimeString()} <-> actual {sim.GameTime.ToLongTimeString()}");
                      }
                      else
                      {
                          Debug.Log($"Diff: {diff.TotalSeconds:n2}sec");
                      }
                  }
                  else
                  {
                      nextStopTime = nextDep.Add(line.stopDuration + line.endOfLineWaitTime);
                      Debug.Log($"line done");
                  }

                  pathFollow = null;

                  // Check citizens leaving here.
                  var stop = route.endStop;
                  if (passengers.TryGetValue(stop, out List<Stop.WaitingCitizen> leaving))
                  {
                      foreach (var c in leaving)
                      {
                          c.path.CompleteStep();
                      }

                      passengerCount -= leaving.Count;
                      leaving.Clear();
                  }

                  var key = Tuple.Create(line, stop);
                  if (lastStopAtStation.ContainsKey(key))
                  {
                      var diff = (nextDep - lastStopAtStation[key]).TotalMinutes;
                      if ((diff - line.schedule.dayInterval) >= .5)
                      {
                          Debug.LogError($"diff: {(diff - line.schedule.dayInterval):n2}min, time between stops: {(sim.GameTime - lastStopAtStation[key]).TotalMinutes}m");
                      }
                      else
                      {
                          Debug.Log($"time between stops: {(sim.GameTime - lastStopAtStation[key]).TotalMinutes}m");
                      }

                      lastStopAtStation[key] = sim.GameTime;
                  }
                  else
                  {
                      lastStopAtStation.Add(key, sim.GameTime);
                  }
                });

            // Make sure to update the velocity right away.
            timeSinceLastUpdate = TrafficSimulator.VelocityUpdateInterval;
            timeElapsed = 0f;
        }

        /// The time at which we should continue along the route.
        public DateTime? nextStopTime;

        /// Total time elapsed while driving.
        public float timeElapsed;

        /// Elapsed time since the last update of the car's velocity.
        public float timeSinceLastUpdate;

        void UpdateVelocity()
        {
            // SetVelocity(sim.trafficSim.GetCarVelocity(drivingCar));

            // Must be updated after the velocity calculation.
            timeElapsed += Time.fixedDeltaTime * sim.SpeedMultiplier;
            timeSinceLastUpdate = 0f;
        }

        void Update()
        {
            if (sim.game.Paused)
            {
                return;
            }

            if (pathFollow != null)
            {
                pathFollow.Update(Time.deltaTime * sim.SpeedMultiplier);
                return;
            }

            if (nextStopTime.HasValue && sim.GameTime >= nextStopTime.Value)
            {
                var diff = SimulationController.GameTimeToRealTime(sim.GameTime - nextStopTime.Value);
                Debug.Log($"diff: {diff}");

                nextStopTime = null;

                if (currentRoute + 1 >= line.routes.Count)
                {
                    StartDrive();
                }
                else
                {
                    StartDrive(currentRoute + 1);
                }

                pathFollow?.Update((Time.deltaTime + (float)diff.TotalSeconds) * sim.SpeedMultiplier);
            }

            //if (!sim.game.Paused && pathFollow != null)
            //{
            //    var elapsedTime = Time.deltaTime * sim.SpeedMultiplier;
            //    timeSinceLastUpdate += elapsedTime;

            //    if (timeSinceLastUpdate < TrafficSimulator.VelocityUpdateInterval)
            //    {
            //        timeElapsed += elapsedTime;
            //    }
            //    else
            //    {
            //        UpdateVelocity();
            //    }

            //    if (drivingCar.waitingForTrafficLight != null)
            //    {
            //        if (drivingCar.waitingForTrafficLight.MustStop)
            //        {
            //            return;
            //        }

            //        drivingCar.waitingForTrafficLight = null;
            //    }

            //    pathFollow.Update();
            //}
        }

        public void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = GameController.instance.sim.transitVehicleModal;
            modal.SetVehicle(this);

            modal.modal.PositionAt(transform.position);
            modal.modal.Enable();
        }
    }
}
