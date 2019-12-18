using UnityEngine;
using System;

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
        int currentRoute;

        /// <summary>
        /// The path following component.
        /// </summary>
        PathFollowingObject pathFollow;

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

        public void Initialize(Line line)
        {
            this.line = line;
            this.spriteRenderer.color = line.color;
            this.sim = GameController.instance.sim;
        }

        public void StartDrive(int routeIndex = 0)
        {
            if (routeIndex >= line.routes.Count)
            {
                return;
            }

            this.currentRoute = routeIndex;

            var route = line.routes[currentRoute];
            if (route.positions == null)
            {
                return;
            }

            this.gameObject.SetActive(true);
            this.pathFollow = new PathFollowingObject(sim, this.gameObject, route.positions,
                                                      line.AverageSpeed, 5f, false,
                                                      (PathFollowingObject _) =>
                                                      {
                                                          pathFollow = null;

                                                          if (currentRoute < line.routes.Count - 1)
                                                          {
                                                              var earliestDeparture = sim.GameTime.AddSeconds(line.AverageStopDuration);
                                                              var nextDep = line.routes[currentRoute].endStop.NextDeparture(line, earliestDeparture);
                                                              nextStopTime = nextDep;
                                                          }
                                                          else
                                                          {
                                                              Debug.Log("vehicle at end of line " + line.name);
                                                          }
                                                      });

            // Make sure to update the velocity right away.
            timeSinceLastUpdate = TrafficSimulator.VelocityUpdateInterval;
            timeElapsed = 0f;
        }

        /// The time at which we should continue along the route.
        DateTime? nextStopTime;

        /// Total time elapsed while driving.
        public float timeElapsed;

        /// Elapsed time since the last update of the car's velocity.
        public float timeSinceLastUpdate;

        void UpdateVelocity()
        {
            // SetVelocity(sim.trafficSim.GetCarVelocity(drivingCar));

            // Must be updated after the velocity calculation.
            timeElapsed += Time.deltaTime * sim.SpeedMultiplier;
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
                pathFollow.Update();
                return;
            }

            if (nextStopTime.HasValue && sim.GameTime >= nextStopTime.Value)
            {
                nextStopTime = null;
                StartDrive(currentRoute + 1);
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
    }
}
