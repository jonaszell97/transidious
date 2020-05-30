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
            /// Base mask that adds one to every path.
            private static readonly ulong _baseMask
                = 0b0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001_0001;

            /// Cached masks.
            private static Dictionary<Tuple<StreetSegment, StreetSegment>, ulong> _maskCache;

            /// Masks for a two-way by two-way intersection.
            private static ulong[][] _twoWayByTwoWayMasks;

            /// Initialize the cache.
            public static void Initialize()
            {
                _maskCache = new Dictionary<Tuple<StreetSegment, StreetSegment>, ulong>();
                InitTwoWayByTwoWayMasks();
            }

            /// The occupation status. Every four bits represent one of the intersection paths, uniquely identified by
            /// StreetIntersection.GetIndexForIntersectionPath.
            public ulong OccupationStatus { get; private set; }

            /// Number of occupying cars.
            private int _occupyingCars;

            /// Calculate the occupation mask for a two-way by two-way intersection.
            private static void InitTwoWayByTwoWayMasks()
            {
                int GetOffsetFor(int from, int to)
                {
                    return from * 4 + to;
                }

                _twoWayByTwoWayMasks = new ulong[4][];

                for (var from = 0; from < 4; ++from)
                {
                    _twoWayByTwoWayMasks[from] = new ulong[4];
                    for (var to = 0; to < 4; ++to)
                    {
                        var mask = _baseMask;

                        // Never block the path itself.
                        mask &= ~(1ul << (GetOffsetFor(from, to) * 4));

                        int straight, left, right;
                        switch (from)
                        {
                            case 0:
                                straight = 2;
                                left = 3;
                                right = 1;
                                break;
                            case 2:
                                straight = 0;
                                left = 1;
                                right = 3;
                                break;
                            case 1:
                                straight = 3;
                                left = 0;
                                right = 2;
                                break;
                            default:
                                Debug.Assert(@from == 3);
                                straight = 1;
                                left = 2;
                                right = 0;
                                break;
                        }

                        // Never block any turns from the same street.
                        mask &= ~(1ul << (GetOffsetFor(from, right) * 4));
                        mask &= ~(1ul << (GetOffsetFor(from, left) * 4));
                        mask &= ~(1ul << (GetOffsetFor(from, from) * 4));

                        // Car is going straight
                        if (to == straight)
                        {
                            // Don't block the straight path in the other direction.
                            mask &= ~(1ul << (GetOffsetFor(to, from) * 4));

                            // Don't block right turns from the other direction.
                            mask &= ~(1ul << (GetOffsetFor(straight, left) * 4));

                            // Don't block U-turns from the side directions.
                            mask &= ~(1ul << (GetOffsetFor(left, left) * 4));
                            mask &= ~(1ul << (GetOffsetFor(right, right) * 4));
                        }
                        // Car is turning right
                        else if (to == right)
                        {
                            // Don't block the straight path in the other direction.
                            mask &= ~(1ul << (GetOffsetFor(to, from) * 4));

                            // Don't block right turns from the straight and left directions.
                            mask &= ~(1ul << (GetOffsetFor(straight, left) * 4));
                            mask &= ~(1ul << (GetOffsetFor(left, from) * 4));

                            // Don't block U-turns from the left and straight directions.
                            mask &= ~(1ul << (GetOffsetFor(left, left) * 4));
                            mask &= ~(1ul << (GetOffsetFor(straight, straight) * 4));
                        }
                        // Car is turning left
                        else if (to == left)
                        {
                            // Don't block right turn from the left side.
                            mask &= ~(1ul << (GetOffsetFor(left, from) * 4));

                            // Don't block U-turns from the right and straight directions.
                            mask &= ~(1ul << (GetOffsetFor(right, right) * 4));
                            mask &= ~(1ul << (GetOffsetFor(straight, straight) * 4));
                        }
                        // Car is u-turning
                        else
                        {
                            Debug.Assert(to == from);
                            
                            // Don't block the straight path in the side directions.
                            mask &= ~(1ul << (GetOffsetFor(left, right) * 4));
                            mask &= ~(1ul << (GetOffsetFor(right, left) * 4));

                            // Don't block right turns from the right direction.
                            mask &= ~(1ul << (GetOffsetFor(right, straight) * 4));
                            
                            // Don't block left turns from the left direction.
                            mask &= ~(1ul << (GetOffsetFor(left, straight) * 4));

                            // Don't block U-turns from any directions.
                            mask &= ~(1ul << (GetOffsetFor(left, left) * 4));
                            mask &= ~(1ul << (GetOffsetFor(right, right) * 4));
                            mask &= ~(1ul << (GetOffsetFor(straight, straight) * 4));
                        }

                        _twoWayByTwoWayMasks[from][to] = mask;
                    }
                }
            }

            /// Calculate the opaque mask for the paths blocked by an intersection crossing.
            private static ulong GetMask(StreetIntersection intersection, StreetSegment from, StreetSegment to)
            {
                if (intersection.IntersectingStreets.Count > 4)
                {
                    return ~0ul;
                }

                var key = Tuple.Create(from, to);
                if (_maskCache.TryGetValue(key, out var mask))
                {
                    return mask;
                }

                if (intersection.Pattern?.PatternType == IntersectionPattern.Type.TwoWayByTwoWay)
                {
                    var fromIdx = intersection.RelativePosition(from);
                    var toIdx = intersection.RelativePosition(to);
                    mask = _twoWayByTwoWayMasks[fromIdx][toIdx];
                }
                else
                {
                    mask = _baseMask;

                    // Never block the path itself.
                    mask &= ~(1ul << (intersection.GetIndexForIntersectionPath(from, to) * 4));
                }

                _maskCache.Add(key, mask);
                return mask;
            }

            /// Whether or not the path at offset is blocked.
            public bool IsBlocked(StreetIntersection intersection, StreetSegment from, StreetSegment to)
            {
                const ulong mask = 0b1111;
                if (intersection.IntersectingStreets.Count > 4)
                {
                    return OccupationStatus != 0;
                }

                var offset = intersection.GetIndexForIntersectionPath(from, to);
                return (OccupationStatus & (mask << (offset * 4))) != 0;
            }

            /// Try to block the intersection with the given mask. Return true on success.
            public bool TryBlock(StreetIntersection intersection, StreetSegment from, StreetSegment to)
            {
                // Technically this is more restrictive than it needs to be because every path can store up to
                // 16 queued cars, but right now I can't think of an efficient way to find out whether any of the
                // additions would overflow.
                if (_occupyingCars == 16)
                {
                    return false;
                }

                var mask = GetMask(intersection, from, to);
                Block(mask);

#if DEBUG
                intersection.UpdateOccupation(OccupationStatus);
#endif

                return true;
            }

            /// Try to block the intersection with the given mask. Return true on success.
            private void Block(ulong mask)
            {
                OccupationStatus += mask;
                ++_occupyingCars;
            }

            /// Unblock the intersection with the given mask.
            public void Unblock(StreetIntersection intersection, StreetSegment from, StreetSegment to)
            {
                Unblock(GetMask(intersection, from, to));

#if DEBUG
                intersection.UpdateOccupation(OccupationStatus);
#endif
            }

            /// Unblock the intersection with the given mask.
            private void Unblock(ulong mask)
            {
                OccupationStatus -= mask;
                --_occupyingCars;
            }
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

            IntersectionStatus.Initialize();
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
            var status = IntersectionOccupation[intersection];

            // The intersection is blocked.
            if (status.IsBlocked(intersection, _car.Segment, _car.NextSegment))
            {
                return true;
            }

            // The intersection is free again, mark it as blocked for other cars and go.
            if (!status.TryBlock(intersection, _car.Segment, _car.NextSegment))
            {
                return true;
            }

            _blockingIntersection = true;
            return false;
        }

        /// Notify other cars that we exited an intersection.
        public void UnblockIntersection()
        {
            var status = IntersectionOccupation[_car.NextIntersection];

            // Unblock the intersection.
            status.Unblock(_car.NextIntersection, _car.Segment, _car.NextSegment);
            _blockingIntersection = false;
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