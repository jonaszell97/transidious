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
        int currentRoute = -1;

        /// <summary>
        /// The path following component.
        /// </summary>
        PathFollowingObject pathFollow;

        float velocity;

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

        public void Initialize(Line line, float? velocity = null)
        {
            this.line = line;
            this.spriteRenderer.color = line.color;
            this.sim = GameController.instance.sim;

            if (velocity != null)
            {
                this.velocity = velocity.Value;
            }
            else
            {
                this.velocity = line.AverageSpeed / 3.6f;
            }
        }

        static System.Collections.Generic.Dictionary<Stop, DateTime> lastStopAtStation
            = new System.Collections.Generic.Dictionary<Stop, DateTime>();

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
            if (route.positions == null)
            {
                return;
            }

            this.gameObject.SetActive(true);
            this.transform.position = new Vector3(route.positions[0].x, route.positions[0].y, transform.position.z);
            this.pathFollow = new PathFollowingObject(sim, this.gameObject, route.positions,
                                                      velocity, 0f, false,
                                                      (PathFollowingObject _) =>
                                                      {
                                                          if (currentRoute < line.routes.Count - 1)
                                                          {
                                                              nextStopTime = sim.GameTime.AddSeconds(line.stopDuration);
                                                          }
                                                          else
                                                          {
                                                              nextStopTime = sim.GameTime.AddSeconds(line.stopDuration + line.endOfLineWaitTime);

                                                              var estimateSeconds = (line.length / (line.AverageSpeed / 3.6f)) * sim.BaseSpeedMultiplier;
                                                              estimateSeconds += line.stops.Count * line.stopDuration;
                                                              var realSeconds = (sim.GameTime - _startTime).TotalSeconds;

                                                              Debug.Log($"estimate {estimateSeconds}s <-> real {realSeconds}s, diff {realSeconds - estimateSeconds}s");
                                                          }

                                                          pathFollow = null;

                                                          var stop = route.endStop;
                                                          if (lastStopAtStation.ContainsKey(stop))
                                                          {
                                                              Debug.Log($"time between stops: {(sim.GameTime - lastStopAtStation[stop]).TotalMinutes}m");
                                                              lastStopAtStation[stop] = sim.GameTime;
                                                          }
                                                          else
                                                          {
                                                              lastStopAtStation.Add(stop, sim.GameTime);
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

        void FixedUpdate()
        {
            if (sim.game.Paused)
            {
                return;
            }

            if (pathFollow != null)
            {
                pathFollow.FixedUpdate();
                return;
            }

            if (nextStopTime.HasValue && sim.GameTime >= nextStopTime.Value)
            {
                //Debug.Assert(sim.GameTime == nextStopTime.Value);
                nextStopTime = null;

                if (currentRoute + 1 >= line.routes.Count)
                {
                    StartDrive();
                }
                else
                {
                    StartDrive(currentRoute + 1);
                }

                pathFollow.FixedUpdate();
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
