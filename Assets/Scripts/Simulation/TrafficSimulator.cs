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
            public ActivePath Path;
            
            /// The car model.
            public Car Car;

            /// The segment this car is currently driving on.
            public StreetSegment Segment;
            
            /// Whether or not the car is driving backwards wrt to the direction of the segment.
            public bool Backward;

#if DEBUG
            /// The next car on the same segment or intersection.
            private DrivingCar _next;
            public DrivingCar Next
            {
                get => _next;
                set
                {
                    if (value?.Path.citizen == Path.citizen)
                    {
                        int _ = 0;
                    }
                    Debug.Assert(value?.Path.citizen != Path.citizen, "next car is self!");
                    _next = value;
                }
            }

            /// The previous car on the same segment or intersection.
            private DrivingCar _prev;
            public DrivingCar Prev
            {
                get => _prev;
                set
                {
                    if (value?.Path.citizen == Path.citizen)
                    {
                        int _ = 0;
                    }
                    Debug.Assert(value?.Path.citizen != Path.citizen, "previous car is self!");
                    _prev = value;
                }
            }
#else
            /// The next car on the same segment or intersection.
            public DrivingCar Next;

            /// The previous car on the same segment or intersection.
            public DrivingCar Prev;
#endif
            
            /// The lane the car is driving on.
            public int Lane;
            
            /// The distance from the start of the segment or intersection.
            public float DistanceFromStart;

            /// The next segment on the path (following an intersection).
            public StreetSegment NextSegment;
            
            /// The next intersection on the path.
            public StreetIntersection NextIntersection;
            
            /// The type of the next turn.
            public TurnType? NextTurn;
            
            /// The lane of the next step on the path.
            public int NextLane;

            public Velocity CurrentVelocity => Path.CurrentVelocity;

            public float Acceleration => Car.Acceleration.MPS2;

            public Vector2 CurrentPosition => Path.transform.position;

            public float Length => Path.Bounds.size.y;

            public float DistanceToIntersection => DistanceToGoal;

            public float DistanceToGoal => Path.PathFollowingHelper.Threshold - Path.PathFollowingHelper.Progress;

            public bool Turning => Path.currentStep is TurnStep;
        }

        public Tuple<DrivingCar, float> GetNextCar(DrivingCar car)
        {
            if (car.Next != null)
            {
                return Tuple.Create(car.Next, car.Next.DistanceFromStart - car.DistanceFromStart);
            }

            if (!car.Turning && car.NextIntersection != null)
            {
                var firstCar = GetFirstCarOnIntersection(car.NextIntersection, car.Segment, car.NextSegment);
                if (firstCar != null)
                {
                    var distance = car.DistanceToIntersection + firstCar.DistanceFromStart;
                    return Tuple.Create(firstCar, distance);
                }
            }

            if (car.NextSegment != null)
            {
                var firstCar = GetDrivingCars(car.NextSegment)[car.NextLane];
                if (firstCar != null)
                {
                    var intersectionPath = StreetPathBuilder.GetIntersectionPath(
                        car.NextIntersection, car.Segment, car.NextSegment);

                    var distance = car.DistanceToIntersection
                                   + intersectionPath.Length
                                   + firstCar.DistanceFromStart;
                    
                    return Tuple.Create(firstCar, distance);
                }
            }

            return null;
        }

        /// Reference to the simulation manager.
        public SimulationController sim;

        /// For each street, an array of linked lists of cars currently driving on this segment, 
        /// indexed by lane from left to right.
        public Dictionary<int, DrivingCar[]> drivingCars;

        /// List of all traffic lights.
        public Dictionary<int, TrafficLight> trafficLights;

        /// Build for street paths.
        public StreetPathBuilder StreetPathBuilder;

#if DEBUG
        public bool manualTrafficLightControl = false;
        public bool displayPathMetrics;
#endif

        public float CurrentTrafficFactor => GetTrafficFactor(sim.GameTime.Hour);

        void Awake()
        {
            this.drivingCars = new Dictionary<int, DrivingCar[]>();
            this.trafficLights = new Dictionary<int, TrafficLight>();
            StreetPathBuilder = new StreetPathBuilder();
        }

        void Start()
        {
            // IDM needs to be initialized after the map is loaded.
            GameController.instance.onLoad.AddListener(IDM.Initialize);

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
                var n = intersection.Pattern?.PatternType == IntersectionPattern.Type.TwoWayByTwoWay
                    ? 4 : intersection.IntersectingStreets.Count;

                cars = new DrivingCar[n * n];
                drivingCars.Add(intersection.id, cars);
            }

            return cars;
        }
        
        public DrivingCar GetFirstCarOnIntersection(StreetIntersection intersection, StreetSegment from, StreetSegment to)
        {
            var cars = GetCarsOnIntersection(intersection);
            var fromIdx = intersection.RelativePosition(from);
            var toIdx = intersection.RelativePosition(to);
            var idx = fromIdx * intersection.IntersectingStreets.Count + toIdx;
            return cars[idx];
        }

        void SetFirstCarOnIntersection(StreetIntersection intersection, StreetSegment from, StreetSegment to,
                                       DrivingCar car)
        {
            var cars = GetCarsOnIntersection(intersection);
            var fromIdx = intersection.RelativePosition(from);
            var toIdx = intersection.RelativePosition(to);
            var idx = fromIdx * intersection.IntersectingStreets.Count + toIdx;
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

            newCar.DistanceFromStart = distanceFromStart;
            newCar.Segment = seg;
            newCar.Car = car;
            newCar.Lane = lane;
            newCar.Path = path;
            newCar.NextIntersection = null;
            newCar.NextSegment = null;
            newCar.NextTurn = null;
            newCar.NextLane = 0;

            var cars = GetDrivingCars(seg);
            var firstCar = cars[lane];
            if (firstCar == null)
            {
                cars[lane] = newCar;
            }
            else if (firstCar.DistanceFromStart > distanceFromStart)
            {
                newCar.Next = firstCar;
                firstCar.Prev = newCar;
                
                cars[lane] = newCar;
            }
            else
            {
                while (firstCar.Next != null && firstCar.Next.DistanceFromStart < distanceFromStart)
                {
                    firstCar = firstCar.Next;
                }

                if (firstCar.Next != null)
                    firstCar.Next.Prev = newCar;

                newCar.Next = firstCar.Next;
                firstCar.Next = newCar;
                newCar.Prev = firstCar;
            }

            if (nextStep is TurnStep turnStep)
            {
                newCar.NextIntersection = turnStep.intersection;
                newCar.NextSegment = turnStep.to.segment;
                newCar.NextTurn = GetTurnType(newCar.Segment, newCar.NextSegment, newCar.NextIntersection);
                newCar.NextLane = GetDefaultLane(newCar.NextSegment, turnStep.to.backward);
            }
        }

        public DrivingCar EnterStreetSegment(Car car, StreetSegment seg,
                                             float distanceFromStart, int lane,
                                             ActivePath path, PathStep nextStep)
        {
            var newCar = new DrivingCar();
            EnterStreetSegment(newCar, car, seg, distanceFromStart, lane, path, nextStep);
            return newCar;
        }

        public void ExitStreetSegment(StreetSegment seg, DrivingCar car)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{car.GetHashCode()}] Exiting segment {seg.name}");

            var cars = GetDrivingCars(seg);
            if (car.Prev == null)
            {
                cars[car.Lane] = car.Next;
            }
            else
            {
                car.Prev.Next = car.Next;
            }

            if (car.Next != null)
            {
                car.Next.Prev = car.Prev;
            }
            
            car.Prev = null;
            car.Next = null;
        }

        TurnType GetTurnType(StreetSegment from, StreetSegment to, StreetIntersection intersection)
        {
            var numIntersectingStreets = intersection.IntersectingStreets.Count;

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

            return TurnType.Unclassified;
        }

        public void EnterIntersection(DrivingCar drivingCar, StreetIntersection intersection)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{drivingCar.GetHashCode()}] Entering intersection {intersection.name}");

            var firstCar = GetFirstCarOnIntersection(intersection, drivingCar.Segment, drivingCar.NextSegment);
            drivingCar.Next = null;
            drivingCar.Prev = null;
            drivingCar.DistanceFromStart = 0f;

            if (firstCar != null)
            {
                firstCar.Prev = drivingCar;
                drivingCar.Next = firstCar;
            }

            SetFirstCarOnIntersection(intersection, drivingCar.Segment, drivingCar.NextSegment, drivingCar);
        }

        public void ExitIntersection(DrivingCar car, StreetIntersection intersection)
        {
            Logger.Log(Logger.LogType.TrafficSim, $"[{car.GetHashCode()}] Exiting intersection {intersection.name}");

            // This can happen when a path is aborted midway through.
            if (car.Next != null)
            {
                car.Next.Prev = car.Prev;
            }

            if (car.Prev == null)
            {
                SetFirstCarOnIntersection(intersection, car.Segment, car.NextSegment, null);
            }
            else
            {
                car.Prev.Next = null;
            }

            car.Prev = null;
            car.Next = null;
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

                positions = StreetPathBuilder.GetStepPath(step).Points.ToList();
            }
            else if (step is PartialDriveStep partialDriveStep)
            {
                segment = partialDriveStep.driveSegment.segment;
                backward = partialDriveStep.driveSegment.backward;
                lane = GetDefaultLane(segment, backward);

                positions = StreetPathBuilder.GetStepPath(step).Points.ToList();
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
                    current = current.Prev;
                    ++pos;
                    ++n;
                }

                current = dc.Next;
                while (current != null)
                {
                    current = current.Next;
                    ++n;
                }

                txt.text = pos + " / " + n;
            }
        }
#endif
    }
}