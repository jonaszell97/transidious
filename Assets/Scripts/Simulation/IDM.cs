using System;
using UnityEngine;
using DrivingCar = Transidious.TrafficSimulator.DrivingCar;

namespace Transidious
{
    /// Intelligent driver model (https://en.wikipedia.org/wiki/Intelligent_driver_model)
    public class IDM
    {
        /// Minimum spacing: the minimum desired net distance. A car can't move if 
        /// the distance from the car in the front is not at least s0. (in m)
        private static readonly float s0_car = 1f;
        private static readonly float s0_intersection = 3.5f;

        /// Exponent.
        private static readonly float delta = 4f;

        /// Velocity update interval in seconds.
        private static readonly float VelocityUpdateInterval = 0.4f;

        /// Maximum distance a car must be away from an intersection to check for other cars.
        private static readonly float IntersectionCheckThreshold = 30f;

        /// Max stopping time that is acceptable for emergency braking.
        /// (https://www.tandfonline.com/doi/pdf/10.1080/16484142.2007.9638118)
        private static readonly float MaxSafeStoppingTime = 1.5f;

        /// The car this class is modeling.
        private DrivingCar _car;

        /// The current velocity.
        private Velocity _currentVelocity;

        /// Time passed since the last velocity update.
        private float _timeSinceLastUpdate = 1000f;

        /// Reference to the car's driving behaviour.
        private Car.DrivingBehaviour _behaviour;

        /// Reset the IDM state.
        public void Reset(DrivingCar car, Velocity velocity)
        {
            _car = car;
            _currentVelocity = velocity;
            _timeSinceLastUpdate = 1000f;
            _behaviour = car.car.Behaviour;
        }

        /// Update the car's velocity and position if necessary.
        public void Update(SimulationController sim, PathFollower pathFollower)
        {
            var elapsedTime = Time.deltaTime * sim.SpeedMultiplier;
            _timeSinceLastUpdate += elapsedTime;

            float delta;
            if (_timeSinceLastUpdate >= VelocityUpdateInterval)
            {
                var newParams = CalculateUpdatedParameters(sim, elapsedTime);
                _currentVelocity = newParams.Item1;
                delta = newParams.Item2;

                _timeSinceLastUpdate = 0f;
                pathFollower.Velocity = _currentVelocity;
            }
            else
            {
                delta = elapsedTime * _currentVelocity.RealTimeMPS;
            }

            if (delta.Equals(0f) || WouldOvertakeIllegally(delta))
            {
                return;
            }

            // Update the car's position.
            var prevDistance = _car.distanceFromStart;
            pathFollower.UpdatePosition(delta);

            // Only update if we didn't complete the driving step.
            if (prevDistance.Equals(_car.distanceFromStart))
            {
                _car.distanceFromStart += delta;
            }
        }

        /// Whether or not the car would illegally overtake the next car with this movement.
        private bool WouldOvertakeIllegally(float delta)
        {
            if (_car.next == null)
            {
                return false;
            }

            return _car.distanceFromStart + delta >= _car.next.distanceFromStart;
        }

        /// Calculate the car's new velocity and position.
        private Tuple<Velocity, float> CalculateUpdatedParameters(SimulationController sim, float deltaTime)
        {
            // The actual current velocity.
            Velocity v = _currentVelocity;

            // Calculate the safe acceleration.
            Acceleration dv_dt = GetSafeAcceleration(sim, v.RealTimeMPS);
            
            // New speed = current speed + acceleration * deltaTime
            Velocity v_new = v + (dv_dt * TimeSpan.FromSeconds(VelocityUpdateInterval));

            // The new velocity.
            Velocity vOut;

            // The new position delta.
            float xOut;

            // For vehicles approaching an already stopped vehicle or a red traffic light, the ballistic update
            // method as described above will lead to negative speeds whenever the end of a time integration interval
            // is not exactly equal to the true stopping time (of course, there is always a mismatch).
            // Then, the ballistic method has to be generalized to simulate following approximate dynamics: 
            // If the true stopping time is within an update time interval, decelerate at constant deceleration (dv/dt)
            // to a complete stop and remain at standstill until this interval has ended.
            // (https://traffic-simulation.de/info/info_IDM.html)
            if (v_new.RealTimeMPS < 0f)
            {
                // v(t+Δt) = 0
                vOut = Velocity.zero;

                // x(t+Δt) = x(t) − 1/2 v^2(t) / (dv/dt)
                xOut = -.5f * (Mathf.Pow(v.RealTimeMPS, 2f) / dv_dt.RealTimeMPS2);
            }
            else
            {
                // v(t+Δt) = v(t) + (dv/dt) Δt
                vOut = v_new;

                // x(t+Δt) = x(t) + v(t)Δt + 1/2 (dv/dt) (Δt)^2
                xOut = (v.RealTimeMPS * deltaTime) + .5f * dv_dt.RealTimeMPS2 * Mathf.Pow(deltaTime, 2f);
            }

            return Tuple.Create(vOut, xOut);
        }

        /// Calculate the safe acceleration for the car.
        private Acceleration GetSafeAcceleration(SimulationController sim, float v)
        {
            // The current speed limit.
            float speedLimit;
            if (_car.nextIntersection != null && _car.DistanceToIntersection <= IntersectionCheckThreshold)
            {
                speedLimit = _car.nextSegment.street.MaxSpeed.RealTimeMPS;
            }
            else
            {
                speedLimit = _car.segment.street.MaxSpeed.RealTimeMPS;
            }

            // The desired velocity on a free road.
            float v0 = Mathf.Min(_car.car.MaxVelocity.RealTimeMPS, speedLimit) * _behaviour.SpeedLimitFactor;

            // The car's maximum acceleration.
            float a = _car.car.Acceleration.RealTimeMPS2;

            // Free road term.
            float freeRoadTerm;
            if (v >= v0)
            {
                freeRoadTerm = v / v0;
            }
            else
            {
                freeRoadTerm = Mathf.Pow(v / v0, delta);
            }

            // Busy road term.
            float busyRoadTerm = GetBusyRoadTerm(sim, a, v);

            // Maximum safe acceleration.
            return Acceleration.FromRealTimeMPS2(a * (1f - freeRoadTerm - busyRoadTerm));
        }

        /// Desired dynamical distance s*.
        private static float sStar(float s0, float v, float deltaV, float T, float a, float b)
        {
            var factor = (v * deltaV) / (2f * Mathf.Sqrt(a * b));
            return s0 + Mathf.Max(0f, v * T + factor);
        }

        /// Whether or not a car needs to stop at a traffic light.
        private bool MustStop(float v, TrafficLight tl)
        {
            switch (tl.CurrentPhase)
            {
                default:
                    return true;
                case TrafficLight.Status.Green:
                    return false;
                case TrafficLight.Status.Yellow:
                case TrafficLight.Status.Red:
                {
                    // Only continue if stopping would be too dangerous.
                    var distanceToIntersection = _car.DistanceToIntersection;
                    var stoppingTime = distanceToIntersection / v;

                    return stoppingTime >= MaxSafeStoppingTime;
                }
            }
        }

        /// Calculate the busy road term for the car.
        private float GetBusyRoadTerm(SimulationController sim, float a, float v)
        {
            // Net distance to the next car.
            float s;

            // Velocity difference between this car and the next one.
            float deltaV;

            // Minimum spacing to the next car or intersection.
            float s0;

            // Check if this car is waiting for a traffic light.
            if (_car.waitingForTrafficLight != null)
            {
                // Check if the traffic light is green again.
                if (_car.waitingForTrafficLight.CurrentPhase != TrafficLight.Status.Green)
                {
                    s = _car.DistanceToIntersection;
                    deltaV = v;
                    s0 = s0_intersection;
                }
                else
                {
                    _car.waitingForTrafficLight = null;
                    s = 1000f;
                    deltaV = 0f;
                    s0 = 0f;
                }
            }
            // Check if there is a next car on the road.
            else if (_car.next != null)
            {
                // Calculate distance between midpoints.
                s = _car.next.distanceFromStart - _car.distanceFromStart;

                // Account for car length.
                s = Mathf.Max(0f, s - (_car.car.Length.Meters * .5f) - (_car.next.car.Length.Meters * .5f));

                // Velocity difference.
                deltaV = v - _car.next.CurrentVelocity.RealTimeMPS;
                
                // Minimum spacing.
                s0 = s0_car;
            }
            // Check if there is an intersection we need to wait for.
            else if (_car.nextIntersection != null)
            {
                var tl = _car.backward
                    ? _car.segment.startTrafficLight
                    : _car.segment.endTrafficLight;

                // Check if there is a traffic light we must wait for.
                if (!_car.Turning && tl != null && MustStop(v, tl))
                {
                    // Traffic lights are simulated as a zero-length car that is standing still.
                    s = _car.DistanceToIntersection;
                    deltaV = v;
                    s0 = s0_intersection;
                    _car.waitingForTrafficLight = tl;
                }
                // TODO Check if another car is approaching the intersection and has the right of way.
                // Otherwise, check for a leading car on the next intersection or segment.
                else
                {
                    // Check if there is a leading car on the next segment after the intersection.
                    var leadingCar = GetNextCar(sim);
                    if (leadingCar != null)
                    {
                        // We found a leading car.
                        s = leadingCar.Item2;
                        deltaV = v - leadingCar.Item1.CurrentVelocity.RealTimeMPS;
                        s0 = s0_car;
                    }
                    else
                    {
                        // If there is no leading car, pretend there is one really far away.
                        s = 1000f;
                        deltaV = 0f;

                        // No minimum spacing is needed for an unoccupied intersection.
                        s0 = 0f;
                    }
                }
            }
            else
            {
                // If there is no leading car, pretend there is one really far away.
                s = 1000f;
                deltaV = 0f;
                s0 = 0f;
            }

            // Sanity checks.
            Debug.Assert(s0 >= 0f, "invalid desired distance");
            Debug.Assert(s >= 0f, "invalid distance to next car");

            return Mathf.Pow(sStar(s0, v, deltaV, _behaviour.T, a, _behaviour.B) / s, 2f);
        }

        /// Return the next car on an intersection or the next segment, along with the distance to it.
        private Tuple<DrivingCar, float> GetNextCar(SimulationController sim)
        {
            // Ignore if the intersection is too far away.
            if (_car.DistanceToIntersection >= IntersectionCheckThreshold)
            {
                return null;
            }

            var trafficSim = sim.trafficSim;

            DrivingCar firstCar;
            float distance;

            // Check if there is a car on the intersection we're approaching.
            if (!_car.Turning && _car.nextIntersection != null)
            {
                firstCar = trafficSim.GetFirstCarOnIntersection(_car.nextIntersection, _car.segment, _car.nextSegment);
                if (firstCar != null)
                {
                    distance = _car.DistanceToIntersection + firstCar.distanceFromStart;
                    return Tuple.Create(firstCar, distance);
                }
            }

            // Check if there is a car on the next street segment.
            if (_car.nextSegment == null)
            {
                return null;
            }

            firstCar = trafficSim.GetDrivingCars(_car.nextSegment, _car.nextLane);
            if (firstCar == null)
            {
                return null;
            }

            var intersectionPath = trafficSim.StreetPathBuilder.GetIntersectionPath(
                _car.nextIntersection, _car.segment, _car.nextSegment);

            distance = _car.DistanceToIntersection
                           + intersectionPath.Length
                           + firstCar.distanceFromStart;
            
            return Tuple.Create(firstCar, distance);
        }
    }
}