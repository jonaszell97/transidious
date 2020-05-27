using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TMPro;
using Transidious.PathPlanning;

namespace Transidious
{
    public class TrafficSimulator : MonoBehaviour
    {
        public enum TurnType
        {
            Straight,
            UTurn,
            RightTurn,
            LeftTurn,
            Unclassified,
        }

        public class DrivingCar
        {
            /// The active path this car belongs to.
            public ActivePath path;
            
            /// The car model.
            public Car car;

            /// Whether or not this car was newly spawned.
            public bool newlySpawned = true;

            /// The segment this car is currently driving on.
            public StreetSegment segment;
            
            /// Whether or not the car is driving backwards wrt to the direction of the segment.
            public bool backward;
            
            /// The next car on the same segment or intersection.
            public DrivingCar next;
            
            /// The previous car on the same segment or intersection.
            public DrivingCar prev;
            
            /// The lane the car is driving on.
            public int lane;
            
            /// The distance from the start of the segment or intersection.
            public float distanceFromStart;

            /// The red traffic light this car is waiting on.
            public TrafficLight waitingForTrafficLight;

            /// The next segment on the path (following an intersection).
            public StreetSegment nextSegment;
            
            /// The next intersection on the path.
            public StreetIntersection nextIntersection;
            
            /// The type of the next turn.
            public TurnType? nextTurn;
            
            /// The lane of the next step on the path.
            public int nextLane;

            public Velocity CurrentVelocity => path.CurrentVelocity;

            public float Acceleration => car.Acceleration.MPS2;

            public Vector2 CurrentPosition => path.transform.position;

            public float Length => path.Bounds.size.y;

            public float DistanceToIntersection => DistanceToGoal;

            public float DistanceToGoal => path.PathFollowingHelper.Threshold - path.PathFollowingHelper.Progress;

            public bool Turning => path.currentStep is TurnStep;
        }

        public Tuple<DrivingCar, float> GetNextCar(DrivingCar car)
        {
            if (car.next != null)
            {
                return Tuple.Create(car.next, car.next.distanceFromStart - car.distanceFromStart);
            }

            if (!car.Turning && car.nextIntersection != null)
            {
                var firstCar = GetFirstCarOnIntersection(car.nextIntersection, car.segment, car.nextSegment);
                if (firstCar != null)
                {
                    var distance = car.DistanceToIntersection + firstCar.distanceFromStart;
                    return Tuple.Create(firstCar, distance);
                }
            }

            if (car.nextSegment != null)
            {
                var firstCar = GetDrivingCars(car.nextSegment)[car.nextLane];
                if (firstCar != null)
                {
                    var intersectionPath = StreetPathBuilder.GetIntersectionPath(
                        car.nextIntersection, car.segment, car.nextSegment);

                    var distance = car.DistanceToIntersection
                                   + intersectionPath.Length
                                   + firstCar.distanceFromStart;
                    
                    return Tuple.Create(firstCar, distance);
                }
            }

            return null;
        }

        /// Reference to the simulation manager.
        public SimulationController sim;

        /// Minimum spacing: the minimum desired net distance. A car can't move if 
        /// the distance from the car in the front is not at least s0. (in km)
        public static readonly float s0_car = 5f;

        /// Desired time headway: the minimum possible time to the vehicle in front 
        /// (in seconds)
        public static readonly float T = 1.5f;

        /// Comfortable braking deceleration. (in m/s^2)
        public static readonly float b = 1.67f;

        /// Exponent.
        public static readonly float delta = 4f;

        /// Velocity update interval in seconds.
        public static readonly float VelocityUpdateInterval = 0.5f;

        /// For each street, an array of linked lists of cars currently driving on this segment, 
        /// indexed by lane from left to right.
        public Dictionary<int, DrivingCar[]> drivingCars;

        /// List of all traffic lights.
        public Dictionary<int, TrafficLight> trafficLights;

        /// Build for street paths.
        public StreetPathBuilder StreetPathBuilder;

        /// List of computed paths on a street, from stop line to stop line. One for each lane.
        Dictionary<StreetSegment, Vector3[][]> computedPaths;

        /// List of computed paths for an intersection.
        Dictionary<StreetIntersection, Vector3[][][][]> computedIntersectionPaths;
        
        /// Intersection path lengths.
        Dictionary<StreetIntersection, float[][]> intersectionPathLengths;

#if DEBUG
        public bool manualTrafficLightControl = false;
        public bool displayPathMetrics;
#endif

        public float CurrentTrafficFactor => GetTrafficFactor(sim.GameTime.Hour);

        void Awake()
        {
            this.drivingCars = new Dictionary<int, DrivingCar[]>();
            this.trafficLights = new Dictionary<int, TrafficLight>();
            this.computedPaths = new Dictionary<StreetSegment, Vector3[][]>();
            this.computedIntersectionPaths = new Dictionary<StreetIntersection, Vector3[][][][]>();
            this.intersectionPathLengths = new Dictionary<StreetIntersection, float[][]>();
            StreetPathBuilder = new StreetPathBuilder();
        }

        void Start()
        {
#if DEBUG
            if (manualTrafficLightControl)
            {
                sim.game.input.RegisterEventListener(InputEvent.MouseDown, obj =>
                {
                    if (obj is StreetSegment seg)
                    {
                        seg.startTrafficLight?.Switch();
                        seg.endTrafficLight?.Switch();
                    }
                });
            }
#endif
        }

        private static float GetTrafficFactor(int hour)
        {
            switch (hour)
            {
                case 21:
                case 22:
                case 23:
                case 0:
                case 1:
                case 2:
                case 3:
                    return 1.1f;
                case 4:
                case 5:
                case 6:
                    return 1.3f;
                case 7:
                case 8:
                case 9:
                    return 2.5f;
                case 10:
                case 11:
                    return 1.7f;
                case 12:
                case 13:
                    return 2.2f;
                case 14:
                    return 1.7f;
                case 15:
                case 16:
                case 17:
                    return 2.2f;
                case 18:
                case 19:
                case 20:
                    return 1.5f;
                default:
                    Debug.LogWarning($"invalid hour {hour}");
                    return 0f;
            }
        }

        bool IsRightLane(StreetSegment seg, int lane)
        {
            if (seg.IsOneWay)
                return true;

            return lane >= (seg.street.lanes / 2);
        }

        public DrivingCar[] GetDrivingCars(StreetSegment seg)
        {
            if (!drivingCars.TryGetValue(seg.id, out DrivingCar[] cars))
            {
                cars = new DrivingCar[seg.street.lanes];
                drivingCars.Add(seg.id, cars);
            }

            return cars;
        }

        public DrivingCar GetDrivingCars(StreetSegment seg, int lane)
        {
            return GetDrivingCars(seg)[lane];
        }

        public DrivingCar[] GetCarsOnIntersection(StreetIntersection intersection)
        {
            if (!drivingCars.TryGetValue(intersection.id, out DrivingCar[] cars))
            {
                cars = new DrivingCar[intersection.intersectingStreets.Count * intersection.intersectingStreets.Count];
                drivingCars.Add(intersection.id, cars);
            }

            return cars;
        }
        
        public DrivingCar GetFirstCarOnIntersection(StreetIntersection intersection, StreetSegment from, StreetSegment to)
        {
            var cars = GetCarsOnIntersection(intersection);
            var fromIdx = intersection.RelativePosition(from);
            var toIdx = intersection.RelativePosition(to);
            var idx = fromIdx * intersection.intersectingStreets.Count + toIdx;
            return cars[idx];
        }

        void SetFirstCarOnIntersection(StreetIntersection intersection, StreetSegment from, StreetSegment to,
                                       DrivingCar car)
        {
            var cars = GetCarsOnIntersection(intersection);
            var fromIdx = intersection.RelativePosition(from);
            var toIdx = intersection.RelativePosition(to);
            var idx = fromIdx * intersection.intersectingStreets.Count + toIdx;
            cars[idx] = car;
        }

        public class PathSegmentInfo
        {
            public StreetSegment segment;
            public int lane;
            public int offset;
            public int length;
            public bool partialStart;
            public bool partialEnd;
            public bool backward;
            public Vector2 direction;

            public PathSegmentInfo(Serialization.Route.Types.PathSegmentInfo data)
            {
                segment = GameController.instance.loadedMap.GetMapObject<StreetSegment>(
                    (int)data.SegmentID);
                lane = data.Lane;
                offset = data.Offset;
                length = data.Length;
                partialStart = data.PartialStart;
                partialEnd = data.PartialEnd;
                backward = data.Backward;
                direction = data.Direction.Deserialize();
            }

            public PathSegmentInfo()
            {

            }
        }

        public List<Vector2> GetCompletePath(PathPlanningResult result,
                                             List<PathSegmentInfo> crossedSegments = null)
        {
            var path = new List<Vector2>();
            foreach (var step in result.path.Steps)
            {
                StreetSegment nextSegment;
                GetStepPath(step, out nextSegment, out bool backward, out bool finalStep, out int lane,
                            out List<Vector2> positions, out bool partialStart, out bool partialEnd,
                            out Vector2 direction);

                if (positions != null)
                {
                    if (crossedSegments != null && nextSegment != null)
                    {
                        crossedSegments.Add(new PathSegmentInfo
                        {
                            segment = nextSegment,
                            lane = lane,
                            offset = path.Count,
                            length = positions.Count,
                            partialStart = partialStart,
                            partialEnd = partialEnd,
                            backward = backward,
                            direction = direction,
                        });
                    }

                    path.AddRange(positions);
                }
            }

            return path;
        }

        public void EnterStreetSegment(DrivingCar newCar,
                                       Car car, StreetSegment seg,
                                       float distanceFromStart, int lane,
                                       ActivePath path, PathStep nextStep)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{newCar.GetHashCode()}] Entering segment {seg.name}");

            newCar.distanceFromStart = distanceFromStart;
            newCar.segment = seg;
            newCar.car = car;
            newCar.lane = lane;
            newCar.path = path;
            newCar.nextIntersection = null;
            newCar.nextSegment = null;
            newCar.nextTurn = null;
            newCar.nextLane = 0;

            var cars = GetDrivingCars(seg);
            var firstCar = cars[lane];
            if (firstCar == null)
            {
                cars[lane] = newCar;
            }
            else if (firstCar.distanceFromStart > distanceFromStart)
            {
                newCar.next = firstCar;
                firstCar.prev = newCar;
                
                cars[lane] = newCar;
            }
            else
            {
                while (firstCar.next != null && firstCar.next.distanceFromStart < distanceFromStart)
                {
                    firstCar = firstCar.next;
                }

                if (firstCar.next != null)
                    firstCar.next.prev = newCar;

                newCar.next = firstCar.next;
                firstCar.next = newCar;
                newCar.prev = firstCar;
            }

            if (nextStep is TurnStep turnStep)
            {
                newCar.nextIntersection = turnStep.intersection;
                newCar.nextSegment = turnStep.to.segment;
                newCar.nextTurn = GetTurnType(newCar.segment, newCar.nextSegment, newCar.nextIntersection);
                newCar.nextLane = GetDefaultLane(newCar.nextSegment, turnStep.to.backward);
            }
        }

        public DrivingCar EnterStreetSegment(Car car, StreetSegment seg,
                                             float distanceFromStart, int lane,
                                             ActivePath path, PathStep nextStep)
        {
            var newCar = new DrivingCar();
            newCar.newlySpawned = true;

            EnterStreetSegment(newCar, car, seg, distanceFromStart, lane, path, nextStep);
            return newCar;
        }

        public void ExitStreetSegment(StreetSegment seg, DrivingCar car)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{car.GetHashCode()}] Exiting segment {seg.name}");

            var cars = GetDrivingCars(seg);
            if (car.prev == null)
            {
                cars[car.lane] = car.next;
            }
            else
            {
                car.prev.next = car.next;
            }

            if (car.next != null)
            {
                car.next.prev = car.prev;
            }
            
            car.prev = null;
            car.next = null;
        }

        TurnType GetTurnType(StreetSegment from, StreetSegment to, StreetIntersection intersection)
        {
            var numIntersectingStreets = intersection.intersectingStreets.Count;

            // The car makes a U-Turn at the intersection
            if (numIntersectingStreets == 1 || from == to)
            {
                return TurnType.UTurn;
            }

            // The car drives straight through the intersection.
            if (numIntersectingStreets == 2)
            {
                return TurnType.Straight;
            }

            if (numIntersectingStreets <= 4)
            {
                var fromPos = intersection.RelativePosition(from);
                var toPos = intersection.RelativePosition(to);

                // The car makes a right turn.
                if (toPos == fromPos + 1 || toPos == fromPos - 3)
                {
                    return TurnType.RightTurn;
                }

                // The car makes a left turn.
                if (toPos == fromPos - 1 || toPos == fromPos + 3)
                {
                    return TurnType.LeftTurn;
                }

                // The car drives straight through.
                Debug.Assert(toPos == fromPos + 2 || toPos == fromPos - 2,
                             "turning from " + fromPos.ToString() + " to " + toPos.ToString());

                return TurnType.Straight;
            }

            // TODO
            return TurnType.Unclassified;
        }

        public void EnterIntersection(DrivingCar drivingCar, StreetIntersection intersection)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{drivingCar.GetHashCode()}] Entering intersection {intersection.name}");

            var firstCar = GetFirstCarOnIntersection(intersection, drivingCar.segment, drivingCar.nextSegment);
            drivingCar.next = null;
            drivingCar.prev = null;
            drivingCar.distanceFromStart = 0f;

            if (firstCar != null)
            {
                firstCar.prev = drivingCar;
                drivingCar.next = firstCar;
            }

            SetFirstCarOnIntersection(intersection, drivingCar.segment, drivingCar.nextSegment, drivingCar);
        }

        public void ExitIntersection(DrivingCar car, StreetIntersection intersection)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{car.GetHashCode()}] Exiting intersection {intersection.name}");

            // FIXME: Technically, this should never happen. No idea why it sometimes does though.
            if (car.next != null)
            {
                car.next.prev = car.prev;
            }

            if (car.prev == null)
            {
                SetFirstCarOnIntersection(intersection, car.segment, car.nextSegment, null);
            }
            else
            {
                car.prev.next = null;
            }

            car.prev = null;
            car.next = null;
        }

        public int GetDefaultLane(StreetSegment seg, bool backward)
        {
            if (backward && !seg.IsOneWay)
            {
                return seg.LeftmostLane;
            }

            return seg.RightmostLane;
        }

        public void GetStepPath(PathStep step, out StreetSegment segment,
                                out bool backward, out bool finalStep,
                                out int lane, out List<Vector2> positions,
                                out bool partialStart, out bool partialEnd,
                                out Vector2 direction)
        {
            finalStep = false;
            partialStart = false;
            partialEnd = false;
            direction = new Vector2();

            if (step is DriveStep driveStep)
            {
                segment = driveStep.driveSegment.segment;
                backward = driveStep.driveSegment.backward;
                lane = GetDefaultLane(segment, backward);

                positions = StreetPathBuilder.GetPath(segment, lane).Points.ToList();
            }
            else if (step is PartialDriveStep partialDriveStep)
            {
                segment = partialDriveStep.driveSegment.segment;
                backward = partialDriveStep.driveSegment.backward;
                lane = GetDefaultLane(segment, backward);

                positions = StreetPathBuilder.GetPath(segment, lane).Points.ToList();
            }
            else if (step is TurnStep turnStep)
            {
                segment = null;
                backward = false;
                lane = GetDefaultLane(turnStep.from.segment, turnStep.from.backward);

                positions = new List<Vector2>();
                StreetPathBuilder.GetStepPath(turnStep).AddPoints(positions, 5);
            }
            else
            {
                segment = null;
                backward = false;
                positions = null;
                lane = 0;
            }
        }

        float sStar(float s0, float T, float v, float deltaV, float a)
        {
            return s0 + v * T + ((v * deltaV) / (2 * Mathf.Sqrt(a * b)));
        }

        float BusyRoadTerm(float s0, float T, float a, float v_alpha, float s_alpha, float deltaV)
        {
            float s_star = sStar(s0, T, v_alpha, deltaV, a);
            return Mathf.Pow(s_star / s_alpha, 2);
        }

        bool ConflictingTurns(StreetIntersection intersection,
                              int carPos, int otherCarPos,
                              TurnType thisTurn, TurnType otherTurn)
        {
            var diff = System.Math.Abs(carPos - otherCarPos);

            switch (otherTurn)
            {
            case TurnType.Unclassified:
                return true;
            case TurnType.Straight:
                /*
                Scenario 1:
                       2
                    |  |  |
                    |     |
                    |  |  |
             -------       -------

           3 -- -- -       -- -- - 1
                  C2 ------->
             -------       -------
                    |  | C|
                    |     |
                    |  |  |
                       0
                No turn possible.
                 */
                if (diff == 3)
                {
                    return false;
                }

                /*
                Scenario 2:
                       2
                    |  |  |
                    |     |
                    |C2|  |
             ------- |     -------
                     |
           3 -- -- - |     -- -- - 1
                     |
             ------- |     -------
                    |V | C|
                    |     |
                    |  |  |
                       0
                Right Turn: OK
                Straight: OK
                 */
                if (diff == 2)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.Straight:
                        return true;
                    default:
                        return false;
                    }
                }

                /*
                Scenario 3:
                       2
                    |  |  |
                    |     |
                    |  |  |
             -------       -------
                   <------- C2
           3 -- -- -       -- -- - 1

             -------       -------
                    |  | C|
                    |     |
                    |  |  |
                       0
                Right Turn: OK
                U-Turn: OK
                 */
                if (diff == 1)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.UTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                Debug.Assert(false, "should not be possible! (" + diff + ")");
                return false;
            case TurnType.UTurn:
                /*
                    Scenario 1:
                           2
                        |  |  |
                        |     |
                        |  |  |
                 -------       -------
                       <---
               3 -- -- -  |    -- -- - 1
                      C2 --
                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                    Straight: OK
                     */
                if (diff == 3)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.Straight:
                        return true;
                    default:
                        return false;
                    }
                }

                /*
                    Scenario 2:
                           2
                        |  |  |
                        |     |
                        |C2| A|
                 ------- |   | -------
                         -----
               3 -- -- -       -- -- - 1

                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                     */
                if (diff == 2)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                /*
                    Scenario 3:
                           2
                        |  |  |
                        |     |
                        |  |  |
                 -------       -------
                             ---- C2
               3 -- -- -     | -- -- - 1
                             --->
                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    No Turn allowed
                     */
                if (diff == 1)
                {
                    return false;
                }

                Debug.Assert(false, "should not be possible! (" + diff + ")");
                return false;
            case TurnType.RightTurn:
                /*
                    Scenario 1:
                           2
                        |  |  |
                        |     |
                        |  |  |
                 -------       -------

               3 -- -- -       -- -- - 1
                      C2 -
                 ------- |     -------
                        |V | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                    Straight: OK
                    Left Turn: OK
                     */
                if (diff == 3)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.Straight:
                    case TurnType.LeftTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                /*
                    Scenario 2:
                           2
                        |  |  |
                        |     |
                        |C2|  |
                 ------- |     -------
                       <--
               3 -- -- -       -- -- - 1

                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                    Straight: OK
                    UTurn: OK
                     */
                if (diff == 2)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.Straight:
                    case TurnType.UTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                /*
                    Scenario 3:
                           2
                        |  |  |
                        |     |
                        |  | A|
                 -------     | -------
                             ---- C2
               3 -- -- -       -- -- - 1

                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                    UTurn: OK
                     */
                if (diff == 1)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                    case TurnType.UTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                Debug.Assert(false, "should not be possible! (" + diff + ")");
                return false;
            case TurnType.LeftTurn:
                /*
                    Scenario 1:
                           2
                        |  |  |
                        |     |
                        |  | A|
                 -------     | -------
                             |
               3 -- -- -     | -- -- - 1
                      C2 -----
                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    No turn allowed
                     */
                if (diff == 3)
                {
                    return false;
                }

                /*
                    Scenario 2:
                           2
                        |  |  |
                        |     |
                        |C2|  |
                 ------- |     -------
                         |
               3 -- -- - |     -- -- - 1
                         -------->
                 -------       -------
                        |  | C|
                        |     |
                        |  |  |
                           0
                    No turn allowed
                     */
                if (diff == 2)
                {
                    return false;
                }

                /*
                    Scenario 3:
                           2
                        |  |  |
                        |     |
                        |  |  |
                 -------       -------
                         ------- C2
               3 -- -- - |     -- -- - 1
                         |
                 ------- |     -------
                        |V | C|
                        |     |
                        |  |  |
                           0
                    Right Turn: OK
                     */
                if (diff == 1)
                {
                    switch (thisTurn)
                    {
                    case TurnType.RightTurn:
                        return true;
                    default:
                        return false;
                    }
                }

                Debug.Assert(false, "should not be possible! (" + diff + ")");
                return false;
            }

            Debug.Assert(false, "should not be possible!");
            return false;
        }

        bool HasRightOfWay(DrivingCar car, StreetIntersection intersection, int relativePos)
        {
            // Another street is considered "to the right" if the angle between the two is between 0 and 120°.
            // We have to give the right of way to cars coming in from the right.
            var rightSlot = relativePos + 1;
            if (rightSlot >= intersection.intersectingStreets.Count)
            {
                
            }

            var rightStreet = intersection.GetStreetAtSlot(rightSlot);

            if (rightStreet == null)
            {
                return true;
            }

            var otherCar = GetDrivingCars(rightStreet, car.lane);
            if (otherCar == null)
            {
                return true;
            }

            while (otherCar.next != null)
            {
                otherCar = otherCar.next;
            }

            if (otherCar.nextIntersection == null)
            {
                return true;
            }

            // If the other car is more than 10m away, we try to cross the intersection.
            if (otherCar.DistanceToIntersection >= 10f)
            {
                return true;
            }

            // Otherwise whether or not we can go depends on the turn types.
            return !ConflictingTurns(intersection, relativePos, rightSlot,
                                     car.nextTurn.Value, otherCar.nextTurn.Value);
        }

        bool IsOccupied(DrivingCar dc, StreetIntersection intersection, int relativePos)
        {
            var carsOnIntersection = GetCarsOnIntersection(intersection);
            foreach (var coi in carsOnIntersection)
            {
                if (coi == dc)
                {
                    continue;
                }
                if (coi.segment == dc.segment)
                {
                    continue;
                }

                var fromPos = intersection.RelativePosition(coi.segment);
                if (ConflictingTurns(intersection, relativePos, fromPos, dc.nextTurn.Value,
                                     coi.nextTurn.Value))
                {
                    return true;
                }
            }

            return false;
        }

        bool MustWait(DrivingCar car, StreetIntersection intersection, float distanceToIntersection)
        {
            // If the intersection is more than 35m away, don't slow down yet.
            if (distanceToIntersection >= 35f)
            {
                return false;
            }

            // Otherwise, check if we have to give the right of way to another approaching car.
            // var relativePos = intersection.RelativePosition(car.segment);
            // if (!HasRightOfWay(car, intersection, relativePos))
            // {
            //     return true;
            // }

            // If we're otherwise ready to enter the intersection, check that it's not occupied.
            // if (distanceToIntersection <= 5f)
            // {
            //     return IsOccupied(car, intersection, relativePos);
            // }

            return false;
        }

        bool MustStop(DrivingCar car, float v_alpha, TrafficLight tl)
        {
            if (tl.MustStop)
                return true;

            // If we won't make it through the intersection at the current velocity, start decelerating.
            var timeToRed = tl.TimeUntilNextRedPhase;
            var distanceLeft = car.DistanceToIntersection;

            return (v_alpha / distanceLeft) >= timeToRed.TotalSeconds;
        }
        
        float GetIntersectionTerm(DrivingCar car, float a, float v_alpha)
        {
            StreetIntersection intersection = car.nextIntersection;

            var distanceToIntersection = car.DistanceToIntersection;
            TrafficLight tl = car.backward
                ? car.segment.startTrafficLight
                : car.segment.endTrafficLight;

            // Check if we have to wait for the traffic light.
            if (tl != null && MustStop(car, v_alpha, tl))
            {
                return BusyRoadTerm(car.Length * 0.5f, 0f, a, v_alpha, distanceToIntersection, v_alpha);
            }

            // Check whether or not there are other cars approaching the intersection.
            if (MustWait(car, intersection, distanceToIntersection))
            {
                return BusyRoadTerm(car.Length * 0.5f, 0f, a, v_alpha, distanceToIntersection, v_alpha);
            }

            return 0f;
        }

        float GetNextCarTerm(DrivingCar car, Tuple<DrivingCar, float> nextCarAndDistance, float v_alpha, float a)
        {
            // Net distance to next vehicle.
            float s_alpha = nextCarAndDistance.Item2;
            Debug.Assert(s_alpha >= 0f, $"negative distance to next car {s_alpha}");

            // float s_alpha = Mathf.Max(car.Length * 0.5f, 
            //     nextCarAndDistance.Item2 - car.Length * 0.5f - nextCarAndDistance.Item1.Length * .5f);
            
            // Approaching rate.
            float deltaV = v_alpha - nextCarAndDistance.Item1.CurrentVelocity.RealTimeMPS;

            return BusyRoadTerm(s0_car, T, a, v_alpha, s_alpha, deltaV);
        }

        public float GetSafeAcceleration(DrivingCar car, float t, float v_alpha, float a)
        {
            // Desired velocity.
            var v0 = Mathf.Min(car.car.MaxVelocity.RealTimeMPS, car.segment.street.MaxSpeed.RealTimeMPS);

            // Free road term
            float freeRoadTerm = Mathf.Pow(v_alpha / v0, delta);
            float busyRoadTerm;

            // If there's another car on the same segment before any intersection, use that.
            var nextCar = GetNextCar(car);
            if (nextCar != null)
            {
                busyRoadTerm = GetNextCarTerm(car, nextCar, v_alpha, a);
            }
            // Otherwise, if there is a non-trivial intersection, check that.
            else if (car.nextIntersection != null)
            {
                busyRoadTerm = GetIntersectionTerm(car, a, v_alpha);
            }
            // Otherwise, we're approaching the goal unobstructed.
            else
            {
                busyRoadTerm =  BusyRoadTerm(0f, 0f, a, v_alpha, car.DistanceToGoal, v_alpha);
            }

            if (float.IsNaN(busyRoadTerm)&&!t.Equals(-1f))
            {
                GetSafeAcceleration(car, -1f, v_alpha, a);
            }

            // Safe Acceleration
            return a * (1 - freeRoadTerm - busyRoadTerm);
        }

        float RungeKutta_k1(DrivingCar car, float h, float yn)
        {
            float v_alpha = yn;
            float t = 0;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta_k2(DrivingCar car, float h, float yn, float k1)
        {
            float v_alpha = yn + (k1 / 2f);
            float t = h / 2;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta_k3(DrivingCar car, float h, float yn, float k2)
        {
            float v_alpha = yn + k2;
            float t = h;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta(DrivingCar car, float yn, int order)
        {
            var k1 = RungeKutta_k1(car, 1f, yn);
            var k2 = k1;
            var sum = k1;
            var cnt = 1;

            // for (int i = 0; i < order - 2; ++i)
            // {
            //     k2 = RungeKutta_k2(car, 1f, yn, k2);
            //     sum += 2 * k2;
            //     cnt += 2;
            // }
            //
            // var k3 = RungeKutta_k3(car, 1f, yn, k2);
            // sum += k3;
            // cnt += 1;

            var acceleration = sum / cnt;
            var velocity = yn + acceleration;

            // For vehicles approaching an already stopped vehicle or a red traffic light, the ballistic update
            // method as described above will lead to negative speeds whenever the end of a time integration interval
            // is not exactly equal to the true stopping time (of course, there is always a mismatch).
            // Then, the ballistic method has to be generalized to simulate following approximate dynamics: 
            // If the true stopping time is within an update time interval, decelerate at constant deceleration (dv/dt)
            // to a complete stop and remain at standstill until this interval has ended.
            // (https://traffic-simulation.de/info/info_IDM.html)
            if (velocity < 0f)
            {
                // FIXME does a constant deceleration of 0.25 always work?
                return Mathf.Max(0f, yn - .25f);
            }

            return velocity;
        }

        /// Based on Intelligent Driver Model (https://en.wikipedia.org/wiki/Intelligent_driver_model)
        public Velocity GetCarVelocity(DrivingCar car, float timeSinceLastUpdate)
        {
            var yn = car.CurrentVelocity.RealTimeMPS;
            return Velocity.FromRealTimeMPS(RungeKutta(car, yn, 3));
        }

        void UpdateTrafficLights()
        {
#if DEBUG
            if (manualTrafficLightControl)
                return;
#endif

            if (trafficLights == null)
            {
                return;
            }

            foreach (var tl in trafficLights)
            {
                tl.Value.Update(Time.deltaTime * sim.SpeedMultiplier);
            }
        }

        void Update()
        {
            UpdateTrafficLights();

#if DEBUG
            RenderCarNumbers();
#endif
        }

#if DEBUG
        public bool renderCarNumbers = false;
        public bool renderTrafficLights = true;
        public bool renderStreetOrder = true;

        void RenderCarNumbers()
        {
            if (!renderCarNumbers)
                return;

            foreach (var c in sim.citizens)
            {
                var path = c.Value.activePath;
                if (path == null || !path.IsDriving)
                {
                    continue;
                }

                TMP_Text txt;
                if (path.transform.childCount == 0)
                {
                    var child = new GameObject();
                    child.transform.SetParent(path.transform, false);

                    txt = child.AddComponent<TMPro.TextMeshPro>();
                    txt.fontSize = 20f;
                    txt.color = Color.black;
                    txt.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
                }
                else
                {
                    txt = path.transform.GetChild(0).GetComponent<TMP_Text>();
                }

                var dc = path._drivingCar;
                var pos = 0;
                var n = 0;

                var current = dc;
                while (current != null)
                {
                    current = current.prev;
                    ++pos;
                    ++n;
                }

                current = dc.next;
                while (current != null)
                {
                    current = current.next;
                    ++n;
                }

                txt.text = pos + " / " + n;
            }
        }
#endif
    }
}