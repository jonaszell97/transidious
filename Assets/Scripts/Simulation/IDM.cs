using System;
using System.Collections.Generic;
using UnityEngine;
using DrivingCar = Transidious.TrafficSimulator.DrivingCar;

namespace Transidious
{
    /// Intelligent driver model (https://en.wikipedia.org/wiki/Intelligent_driver_model)
    public class IDM
    {
        public class IntersectionStatus
        {
            /// The occupation status. Each bit represents one of the intersection paths, uniquely identified by
            /// StreetIntersection.GetIndexForIntersectionPath.
            public uint OccupationStatus;
        }

        /// Minimum spacing: the minimum desired net distance. A car can't move if 
        /// the distance from the car in the front is not at least s0. (in m)
        private static readonly float s0_car = 1f;
        private static readonly float s0_intersection = 3.5f;

        /// Exponent for free road term.
        private static readonly float delta = 4f;

        /// Velocity update interval (in seconds).
        private static readonly float VelocityUpdateInterval = 0.4f;

        /// Maximum distance a car must be away from an intersection to check for other cars (in meters).
        private static readonly float IntersectionCheckThreshold = 30f;

        /// Max stopping time that is acceptable for emergency braking (in seconds).
        /// (https://www.tandfonline.com/doi/pdf/10.1080/16484142.2007.9638118)
        private static readonly float MaxSafeStoppingTime = 1.5f;

        /// Map of intersection occupation statuses.
        public static Dictionary<StreetIntersection, IntersectionStatus> IntersectionOccupation;

        /// The car this class is modeling.
        private DrivingCar _car;
        
        /// The traffic light we're waiting for.
        private TrafficLight _waitingForTrafficLight;

        /// True if this car is currently blocking an intersection.
        private bool _blockingIntersection;
        public bool BlockingIntersection => _blockingIntersection;

        /// The current velocity.
        private Velocity _currentVelocity;

        /// Time passed since the last velocity update.
        private float _timeSinceLastUpdate = 1000f;

        /// Reference to the car's driving behaviour.
        private Car.DrivingBehaviour _behaviour;

        /// Initialize static values.
        public static void Initialize()
        {
            IntersectionOccupation = new Dictionary<StreetIntersection, IntersectionStatus>();

            foreach (var intersection in GameController.instance.loadedMap.streetIntersections)
            {
                IntersectionOccupation.Add(intersection, new IntersectionStatus());
            }
        }

        /// (Re-)Initialize the IDM state.
        public void Initialize(DrivingCar car, Velocity velocity)
        {
            Debug.Assert(!_blockingIntersection);
            Reset(car, velocity);
        }

        /// Reset the IDM state.
        public void Reset(DrivingCar car, Velocity velocity)
        {
            _car = car;
            _currentVelocity = velocity;
            _timeSinceLastUpdate = 1000f;
            _behaviour = car.Car.Behaviour;
            _waitingForTrafficLight = null;
        }

        /// Update the car's velocity and position if necessary.
        public void Update(SimulationController sim, PathFollower pathFollower)
        {
            var elapsedTime = Time.deltaTime * sim.SpeedMultiplier;
            _timeSinceLastUpdate += elapsedTime;

            float distanceDelta;
            if (_timeSinceLastUpdate >= VelocityUpdateInterval)
            {
                var (vOut, xOut) = CalculateUpdatedParameters(sim, elapsedTime);
                _currentVelocity = vOut;
                distanceDelta = xOut;

                _timeSinceLastUpdate = 0f;
                pathFollower.Velocity = _currentVelocity;
            }
            else
            {
                distanceDelta = elapsedTime * _currentVelocity.RealTimeMPS;
            }

            // Sanity checks.
            if (distanceDelta.Equals(0f)
                || WouldOvertakeIllegally(distanceDelta)
                || WouldProceedIllegally(distanceDelta))
            {
                return;
            }

            // Update the car's position.
            var prevDistance = _car.DistanceFromStart;
            pathFollower.UpdatePosition(distanceDelta);

            // Only update if we didn't complete the driving step.
            if (prevDistance.Equals(_car.DistanceFromStart))
            {
                _car.DistanceFromStart += distanceDelta;
            }
        }

        /// Whether or not the drive can be safely aborted right now without messing up something.
        public bool Abortable => !_blockingIntersection;

        /// Whether or not the car would illegally overtake the next car with this movement.
        private bool WouldOvertakeIllegally(float delta)
        {
            if (_car.Next == null)
            {
                return false;
            }

            return _car.DistanceFromStart + delta >= _car.Next.DistanceFromStart;
        }

        /// Whether or not we're about to illegally cross an intersection.
        private bool WouldProceedIllegally(float delta)
        {
            return _car.NextIntersection != null && !_car.Turning && !_blockingIntersection
                   && delta >= _car.DistanceToIntersection;
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
            if (_car.NextIntersection != null && _car.DistanceToIntersection <= IntersectionCheckThreshold)
            {
                speedLimit = _car.NextSegment.street.MaxSpeed.RealTimeMPS;
            }
            else
            {
                speedLimit = _car.Segment.street.MaxSpeed.RealTimeMPS;
            }

            // The desired velocity on a free road.
            float v0 = Mathf.Min(_car.Car.MaxVelocity.RealTimeMPS, speedLimit) * _behaviour.SpeedLimitFactor;

            // The car's maximum acceleration.
            float a = _car.Car.Acceleration.RealTimeMPS2;

            // Free road term.
            float freeRoadTerm;
            if (v >= v0)
            {
                // Don't decelerate too harshly in case of a new speed limit.
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

        /// Check which intersections will be blocked by the car's next turn.
        private uint GetIntersectionBlockingMask(int offset)
        {
            var result = ~0u;
            // result &= ~(1u << offset);

            return result;
        }

        /// Check and update the occupation status of the intersection we're approaching.
        private void CheckIntersectionStatus(float v, out float s, out float deltaV, out float s0)
        {
            // Don't accidentally block the intersection before the next car does to avoid deadlocks.
            if ((_car.Next?.Path.idm._blockingIntersection ?? true) && IsIntersectionOccupied())
            {
                s = _car.DistanceToIntersection;
                deltaV = v;
                s0 = s0_intersection;

                return;
            }

            // If there is no leading car, pretend there is one really far away.
            s = 1000f;
            deltaV = 0f;

            // No minimum spacing is needed for an unoccupied intersection.
            s0 = 0f;
        }

        /// Check and update the occupation status of the intersection we're approaching.
        private bool IsIntersectionOccupied()
        {
            if (_car.DistanceToIntersection >= IntersectionCheckThreshold)
            {
                return false;
            }

            var intersection = _car.NextIntersection;
            var offset = intersection.GetIndexForIntersectionPath(_car.Segment, _car.NextSegment);
            var status = IntersectionOccupation[intersection];

            // The intersection is blocked.
            if ((status.OccupationStatus & (1 << offset)) != 0)
            {
                return true;
            }

            // The intersection is free again, mark it as blocked for other cars and go.
            var mask = GetIntersectionBlockingMask(offset);
            status.OccupationStatus |= mask;
            _blockingIntersection = true;

            return false;
        }

        /// Notify other cars that we exited an intersection.
        public void UnblockIntersection()
        {
            var offset = _car.NextIntersection.GetIndexForIntersectionPath(_car.Segment, _car.NextSegment);
            var mask = GetIntersectionBlockingMask(offset);
            var status = IntersectionOccupation[_car.NextIntersection];

            // Unblock the intersection.
            status.OccupationStatus &= ~mask;
            _blockingIntersection = false;
        }

        /// Calculate the busy road term for the car.
        private float GetBusyRoadTerm(SimulationController sim, float a, float v)
        {
            if (GameController.instance.mainUI.citizenModal.citizen == _car.Path.citizen)
            {
                int i = 3;
            }

            // Net distance to the next car.
            float s;

            // Velocity difference between this car and the next one.
            float deltaV;

            // Minimum spacing to the next car or intersection.
            float s0;

            // Check if this car is waiting for a traffic light.
            if (_waitingForTrafficLight != null)
            {
                Debug.Assert(!_blockingIntersection, "blocking intersection at a red traffic light!");

                // Check if the traffic light is green again.
                if (_waitingForTrafficLight.CurrentPhase != TrafficLight.Status.Green)
                {
                    s = _car.DistanceToIntersection;
                    deltaV = v;
                    s0 = s0_intersection;
                }
                else
                {
                    _waitingForTrafficLight = null;

                    // The intersection might still be occupied.
                    CheckIntersectionStatus(v, out s, out deltaV, out s0);
                }
            }
            // Check if this car is waiting for an intersection to be unoccupied.
            else if (!_blockingIntersection && _car.NextIntersection != null)
            {
                CheckIntersectionStatus(v, out s, out deltaV, out s0);
            }
            else
            {
                // If there is no leading car, pretend there is one really far away.
                s = 1000f;
                deltaV = 0f;
                s0 = 0f;
            }

            // Check if there is a leading car we need to pay attention to.
            var leadingCar = GetNextCar(sim);
            if (leadingCar != null && leadingCar.Item2 < s)
            {
                // We found a leading car.
                s = leadingCar.Item2;
                deltaV = v - leadingCar.Item1.CurrentVelocity.RealTimeMPS;
                s0 = s0_car;
            }
            // Check if there is a traffic light to pay attention to.
            else if (_car.NextIntersection != null)
            {
                var tl = _car.Backward
                    ? _car.Segment.startTrafficLight
                    : _car.Segment.endTrafficLight;

                // Check if there is a traffic light we must wait for.
                if (!_car.Turning && tl != null && MustStop(v, tl))
                {
                    // Traffic lights are simulated as a zero-length car that is standing still.
                    s = _car.DistanceToIntersection;
                    deltaV = v;
                    s0 = s0_intersection;
                    _waitingForTrafficLight = tl;

                    // If we're blocking an intersection, we need to unblock it.
                    if (_blockingIntersection)
                    {
                        UnblockIntersection();
                    }
                }
            }

            // Sanity checks.
            Debug.Assert(s0 >= 0f, "invalid desired distance");
            Debug.Assert(s >= 0f, "invalid distance to next car");

            return Mathf.Pow(sStar(s0, v, deltaV, _behaviour.T, a, _behaviour.B) / s, 2f);
        }

        /// Return the next car on an intersection or the next segment, along with the distance to it.
        private Tuple<DrivingCar, float> GetNextCar(SimulationController sim)
        {
            if (_car.Next != null)
            {
                // Calculate distance between midpoints.
                var s = _car.Next.DistanceFromStart - _car.DistanceFromStart;

                // Account for car length.
                s = Mathf.Max(0f, s - (_car.Car.Length.Meters * .5f) - (_car.Next.Car.Length.Meters * .5f));

                return Tuple.Create(_car.Next, s);
            }

            if (_car.NextSegment == null)
            {
                return null;
            }

            // Ignore if the intersection is too far away.
            if (_car.DistanceToIntersection >= IntersectionCheckThreshold)
            {
                return null;
            }

            var trafficSim = sim.trafficSim;

            DrivingCar firstCar;
            float distance;

            // Check if there is a car on the intersection we're approaching.
            if (!_car.Turning && _car.NextIntersection != null)
            {
                firstCar = trafficSim.GetFirstCarOnIntersection(_car.NextIntersection, _car.Segment, _car.NextSegment);
                if (firstCar != null)
                {
                    // Account for car length.
                    distance = _car.DistanceToIntersection + firstCar.DistanceFromStart;
                    distance = Mathf.Max(0f, distance
                                             - (_car.Car.Length.Meters * .5f) - (firstCar.Car.Length.Meters * .5f));

                    return Tuple.Create(firstCar, distance);
                }
            }

            // Check if there is a car on the next street segment.
            firstCar = trafficSim.GetDrivingCars(_car.NextSegment, _car.NextLane);
            if (firstCar == null)
            {
                return null;
            }

            var intersectionPath = trafficSim.StreetPathBuilder.GetIntersectionPath(
                _car.NextIntersection, _car.Segment, _car.NextSegment);

            if (_car.Turning)
            {
                distance = _car.DistanceToGoal
                           + firstCar.DistanceFromStart;
            }
            else
            {
                distance = _car.DistanceToIntersection
                           + intersectionPath.Length
                           + firstCar.DistanceFromStart;
            }

            // Account for car length.
            distance = Mathf.Max(0f, distance
                                     - (_car.Car.Length.Meters * .5f) - (firstCar.Car.Length.Meters * .5f));
            
            return Tuple.Create(firstCar, distance);
        }
    }
}