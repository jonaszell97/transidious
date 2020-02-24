using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
            public Car car;
            public StreetSegment segment;
            public StreetSegment nextSegment;
            public StreetIntersection nextIntersection;
            public TurnType? nextTurn;
            public bool backward;
            public DrivingCar next;
            public Vector2 exactPosition;
            public int lane;
            public float distanceFromStart;

            public PathPlanningResult path;
            public int stepIdx;

            public TrafficLight waitingForTrafficLight;

#if DEBUG
            public Text numberTxt;
#endif

            public float CurrentVelocity
            {
                get
                {
                    return car.pathFollow?.velocity ?? 0f;
                }
            }

            public Vector2 CurrentDirection
            {
                get
                {
                    return car.pathFollow?.direction ?? Vector2.zero;
                }
            }

            public float Acceleration
            {
                get
                {
                    return car.acceleration;
                }
            }

            public float DistanceToIntersection
            {
                get
                {
                    Debug.Assert(nextIntersection != null, "there is no next intersection");
                    float stopLineDistance;
                    if (nextIntersection == segment.endIntersection)
                    {
                        stopLineDistance = segment.EndStopLineDistance;
                    }
                    else
                    {
                        stopLineDistance = segment.BeginStopLineDistance;
                    }

                    return segment.length - distanceFromStart - 2 * stopLineDistance;
                }
            }

            public float DistanceToGoal
            {
                get
                {
                    return car.pathFollow.threshold - car.pathFollow.progress;
                    // Debug.Assert(nextIntersection == null, "goal is not on the current street");

                    // var step = path.steps[stepIdx] as PartialDriveStep;
                    // var goalDistance = segment.GetDistanceFromStart(step.endPos);

                    // return goalDistance - distanceFromStart;
                }
            }
        }

        public class CarOnIntersection
        {
            public Car car;
            public TurnType turnType;
            public StreetSegment comesFrom;
            public StreetSegment goesTo;
        }

        public class ActivePath
        {
            public PathPlanningResult path;
            public int stepIdx;
            public float currentVelocity;
            public float progress;
            public bool isTurn;

            public Serialization.ActivePath ToProtobuf()
            {
                return new Serialization.ActivePath
                {
                    Path = path.ToProtobuf(),
                    StepNo = (uint)stepIdx,
                    CurrentVelocity = currentVelocity,
                    StepProgress = progress,
                    IsTurn = isTurn,
                };
            }

            public static ActivePath Deserialize(Map map, Serialization.ActivePath ap)
            {
                return new ActivePath
                {
                    path = PathPlanningResult.Deserialize(map, ap.Path),
                    stepIdx = (int)ap.StepNo,
                    currentVelocity = ap.CurrentVelocity,
                    progress = ap.StepProgress,
                    isTurn = ap.IsTurn,
                };
            }
        }

        /// Reference to the simulation manager.
        public SimulationController sim;

        /// Minimum spacing: the minimum desired net distance. A car can't move if 
        /// the distance from the car in the front is not at least s0. (in km)
        public float s0_car = 2f * Map.Meters;
        public float s0_intersection = 0f;
        public float s0_trafficLight = 0f;

        /// Desired time headway: the minimum possible time to the vehicle in front 
        /// (in seconds)
        public float T = 1.5f;

        /// Comfortable braking deceleration. (in m/s^2)
        public float b = 1.67f * Map.Meters;

        /// Exponent.
        public float delta = 4f;

        /// Velocity update interval in seconds.
        public static readonly float VelocityUpdateInterval = 0.4f;

        /// For each street, an array of linked lists of cars currently driving on this segment, 
        /// indexed by lane from left to right.
        public Dictionary<StreetSegment, DrivingCar[]> drivingCars;

        /// Map from intersections to the cars currently crossing the intersection.
        Dictionary<StreetIntersection, List<CarOnIntersection>> intersectionMap;

        /// List of all traffic lights.
        public List<TrafficLight> trafficLights;

        /// List of computed paths on a street, from stop line to stop line. One for each lane.
        Dictionary<StreetSegment, Vector3[][]> computedPaths;

        /// List of computed paths for an intersection.
        Dictionary<StreetIntersection, Vector3[][][][]> computedIntersectionPaths;

#if DEBUG
        public bool manualTrafficLightControl = false;
#endif

        public float CurrentTrafficFactor
        {
            get
            {
                return GetTrafficFactor(sim.GameTime.Hour);
            }
        }

        void Awake()
        {
            this.drivingCars = new Dictionary<StreetSegment, DrivingCar[]>();
            this.trafficLights = new List<TrafficLight>();
            this.intersectionMap = new Dictionary<StreetIntersection, List<CarOnIntersection>>();
            this.computedPaths = new Dictionary<StreetSegment, Vector3[][]>();
            this.computedIntersectionPaths = new Dictionary<StreetIntersection, Vector3[][][][]>();
        }

        void Start()
        {
#if DEBUG
            //if (manualTrafficLightControl)
            //{
            //    sim.game.input.RegisterEventListener(InputEvent.MouseDown, (DynamicMapObject obj) =>
            //    {
            //        var seg = obj as StreetSegment;
            //        if (seg == null)
            //        {
            //            return;
            //        }

            //        seg.startTrafficLight?.Switch();
            //        seg.endTrafficLight?.Switch();
            //    });
            //}
#endif
        }

        public float GetTrafficFactor(int hour)
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
            if (seg.street.isOneWay)
                return true;

            return lane >= (seg.street.lanes / 2);
        }

        public float GetDistanceFromStart(StreetSegment seg, Vector3 pos, int lane)
        {
            var dist = Mathf.Min(seg.GetDistanceFromStart(pos), seg.length);
            if (IsRightLane(seg, lane))
            {
                return dist;
            }
            else
            {
                return seg.length - dist;
            }
        }

        DrivingCar[] GetDrivingCars(StreetSegment seg)
        {
            if (!drivingCars.TryGetValue(seg, out DrivingCar[] cars))
            {
                cars = new DrivingCar[seg.street.lanes];
                drivingCars.Add(seg, cars);
            }

            return cars;
        }

        DrivingCar GetDrivingCars(StreetSegment seg, int lane)
        {
            return GetDrivingCars(seg)[lane];
        }

        List<CarOnIntersection> GetCarsOnIntersection(StreetIntersection intersection)
        {
            if (!intersectionMap.TryGetValue(intersection, out List<CarOnIntersection> cars))
            {
                cars = new List<CarOnIntersection>();
                intersectionMap.Add(intersection, cars);
            }

            return cars;
        }

        public class PathSegmentInfo
        {
            [System.Serializable]
            public struct Serializable
            {
                public int segmentID;
                public int lane;
                public int offset;
                public int length;
                public bool partialStart;
                public bool partialEnd;
                public bool backward;
                public SerializableVector2 direction;
            }

            public StreetSegment segment;
            public int lane;
            public int offset;
            public int length;
            public bool partialStart;
            public bool partialEnd;
            public bool backward;
            public Vector2 direction;

            public Serializable Serialize()
            {
                Debug.Assert(length > 1);

                return new Serializable
                {
                    segmentID = segment.id,
                    lane = lane,
                    offset = offset,
                    length = length,
                    partialStart = partialStart,
                    partialEnd = partialEnd,
                    backward = backward,
                    direction = new SerializableVector2(direction),
                };
            }

            public PathSegmentInfo(Serializable data)
            {
                segment = GameController.instance.loadedMap.GetMapObject<StreetSegment>(
                    data.segmentID);
                lane = data.lane;
                offset = data.offset;
                length = data.length;
                partialStart = data.partialStart;
                partialEnd = data.partialEnd;
                backward = data.backward;
                direction = data.direction.ToVector();
            }

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

        public List<Vector3> GetCompletePath(PathPlanningResult result,
                                             List<PathSegmentInfo> crossedSegments = null)
        {
            var path = new List<Vector3>();

            List<Vector3> positions;
            StreetSegment segment = null;
            int prevLane = 0;
            bool backward;
            int lane;
            bool finalStep;

            var addFirstIntersectionPos = false;
            foreach (var step in result.steps)
            {
                StreetSegment nextSegment;
                GetStepPath(step, out nextSegment, out backward, out finalStep, out lane,
                            out positions, out bool partialStart, out bool partialEnd,
                            out Vector2 direction);

                if (segment != null && nextSegment != null)
                {
                    StreetIntersection nextIntersection;
                    if (backward)
                    {
                        nextIntersection = nextSegment.endIntersection;
                    }
                    else
                    {
                        nextIntersection = nextSegment.startIntersection;
                    }

#if false
                    var _ids = nextIntersection.intersectingStreets.Select(s => s.id);
                    if (!(_ids.Contains(segment.Id) && _ids.Contains(nextSegment.Id)))
                    {
                        gameObject.transform.position = nextIntersection.position;
                        gameObject.DrawCircle(5f, 5f, Color.red);

                        gameObject.transform.position = segment.positions[segment.positions.Count / 2];
                        gameObject.DrawCircle(5f, 5f, Color.blue);

                        gameObject.transform.position = nextSegment.positions[segment.positions.Count / 2];
                        gameObject.DrawCircle(5f, 5f, Color.green);

                        Debug.LogWarning(nextIntersection.intersectingStreets.Count);
                    }
#endif

                    var intersectionPath = GetPath(nextIntersection, segment,
                                                   nextSegment, prevLane);

                    if (intersectionPath == null)
                    {
                        Debug.LogWarning("missing intersection path");
                    }
                    else
                    {
                        var begin = 1;
                        var end = intersectionPath.Length - 1;

                        if (addFirstIntersectionPos)
                        {
                            begin = 0;
                        }

                        for (int i = begin; i < end; ++i)
                        {
                            path.Add(intersectionPath[i]);
                        }
                    }
                }

                addFirstIntersectionPos = false;

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
                else if (partialStart)
                {
                    addFirstIntersectionPos = true;
                }
                else if (partialEnd)
                {
                    StreetIntersection nextIntersection;
                    if (backward)
                    {
                        nextIntersection = nextSegment.endIntersection;
                    }
                    else
                    {
                        nextIntersection = nextSegment.startIntersection;
                    }

                    var intersectionPath = GetPath(nextIntersection, segment,
                                                   nextSegment, prevLane);

                    path.Add(intersectionPath.Last());
                }

                segment = nextSegment;
                prevLane = lane;
            }

            return path;
        }

        public Vector3[] GetPath(StreetSegment seg, int lane)
        {
            if (!computedPaths.TryGetValue(seg, out Vector3[][] paths))
            {
                paths = ComputePaths(seg);
                computedPaths.Add(seg, paths);
            }

            return paths[lane];
        }

        static Tuple<Vector3, Vector3> GetOffsetPoints(StreetSegment seg, int lane, Vector3 p0, Vector3 p1)
        {
            var lanes = seg.street.lanes;
            var halfLanes = lanes / 2;
            var offset = seg.GetStreetWidth(RenderingDistance.Near) / lanes;
            var isLeftLane = lane < halfLanes;

            int laneOffset = seg.LanePositionFromMiddle(lane, true);

            var currentOffset = offset * laneOffset;
            if (isLeftLane)
            {
                currentOffset = -currentOffset;
            }

            return GetOffsetPoints(p0, p1, currentOffset, out Vector3 _);
        }

        static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
                                                       float currentOffset, out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            p0 = p0 + (perpendicular * currentOffset);
            p1 = p1 + (perpendicular * currentOffset);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }

        static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
                                                       float currentOffset, Vector3 prevPerpendicular,
                                                       out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            var mid = (perpendicular + prevPerpendicular).normalized;
            perpendicular = mid;

            p0 = p0 + (mid * currentOffset);
            p1 = p1 + (mid * currentOffset);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }

        static List<Vector3> ComputePath(StreetSegment seg, int lane, float offset)
        {
            var segPositions = seg.drivablePositions;

            var lanes = seg.street.lanes;
            var halfLanes = lanes / 2;
            var isLeftLane = lane < halfLanes;
            var laneOffset = seg.LanePositionFromMiddle(lane, true);

            var currentOffset = offset * laneOffset;
            if (isLeftLane)
            {
                currentOffset = -currentOffset;
            }

            var positions = new List<Vector3>();
            var perpendicular = Vector3.zero;

            for (int j = 1; j < segPositions.Count; ++j)
            {
                Vector3 p0 = segPositions[j - 1];
                Vector3 p1 = segPositions[j];

                if (j == 1)
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, out perpendicular);
                    positions.Add(offsetPoints.Item1);
                    positions.Add(offsetPoints.Item2);
                }
                else
                {
                    var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, perpendicular,
                                                       out perpendicular);

                    positions.Add(offsetPoints.Item2);
                }
            }

            return positions;
        }

        Vector3[][] ComputePaths(StreetSegment seg)
        {
            var paths = new Vector3[seg.street.lanes][];
            var lanes = seg.street.lanes;
            var halfLanes = lanes / 2;
            var offset = seg.GetStreetWidth(RenderingDistance.Near) / lanes;
            var segPositions = seg.drivablePositions;

            var positions = new List<Vector3>();
            for (int lane = 0; lane < lanes; ++lane)
            {
                var isLeftLane = lane < halfLanes;
                var laneOffset = seg.LanePositionFromMiddle(lane, true);

                var currentOffset = offset * laneOffset;
                if (isLeftLane)
                {
                    currentOffset = -currentOffset;
                }

                var perpendicular = Vector3.zero;
                for (int j = 1; j < segPositions.Count; ++j)
                {
                    Vector3 p0 = segPositions[j - 1];
                    Vector3 p1 = segPositions[j];

                    if (j == 1)
                    {
                        var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, out perpendicular);
                        positions.Add(offsetPoints.Item1);
                        positions.Add(offsetPoints.Item2);
                    }
                    else
                    {
                        var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, perpendicular,
                                                           out perpendicular);

                        positions.Add(offsetPoints.Item2);
                    }
                }

                paths[lane] = positions.ToArray();
                positions.Clear();
            }

            return paths;
        }

        public Vector3[] GetPath(StreetIntersection intersection,
                                 StreetSegment from, StreetSegment to,
                                 int lane)
        {
            if (!computedIntersectionPaths.TryGetValue(intersection,
                                                       out Vector3[][][][] paths))
            {
                paths = ComputeIntersectionPaths(intersection);
                computedIntersectionPaths.Add(intersection, paths);
            }

            return paths[intersection.RelativePosition(from)]
                [from.LanePositionFromMiddle(lane) - 1]
                [intersection.RelativePosition(to)];
        }

        public Vector3[][][][] ComputeIntersectionPaths(StreetIntersection intersection)
        {
            var paths = new Vector3[intersection.NumIntersectingStreets][][][];
            var currentPath = new List<Vector3>();

            foreach (var from in intersection.IncomingStreets)
            {
                var fromLanes = from.street.lanes;
                var fromLanesPerDirection = from.street.LanesPerDirection;

                ref var fromPaths = ref paths[intersection.RelativePosition(from)];
                fromPaths = new Vector3[fromLanesPerDirection][][];

                var endsHere = from.endIntersection == intersection;

                int fromLane = fromLanesPerDirection;
                if (from.street.isOneWay)
                {
                    --fromLane;
                    Debug.Assert(endsHere, "invalid one-way street!");
                }
                else if (!endsHere)
                {
                    --fromLane;
                }

                while (true)
                {
                    ref var toPaths = ref fromPaths[from.LanePositionFromMiddle(fromLane) - 1];
                    toPaths = new Vector3[intersection.NumIntersectingStreets][];

                    foreach (var to in intersection.OutgoingStreets)
                    {
                        var uturn = false;
                        if (from == to)
                        {
                            if (endsHere && !from.StartUTurnAllowed)
                            {
                                continue;
                            }
                            else if (!endsHere && !from.EndUTurnAllowed)
                            {
                                continue;
                            }

                            uturn = true;
                        }

                        var toLanes = to.street.lanes;
                        var toLanesHalf = to.street.LanesPerDirection;

                        int toLane;
                        var distanceFromMiddle = from.LanePositionFromMiddle(fromLane);

                        if (uturn)
                        {
                            toLane = endsHere ? fromLane : from.MirrorLane(fromLane);
                        }
                        else if (from.street.LanesPerDirection == to.street.LanesPerDirection)
                        {
                            // Use equivalent lane
                            if (to.street.isOneWay)
                            {
                                toLane = distanceFromMiddle - 1;
                            }
                            else
                            {
                                toLane = toLanesHalf + distanceFromMiddle - 1;
                            }
                        }
                        else
                        {
                            // Use rightmost lane
                            toLane = to.RightmostLane;
                        }

                        Vector2 p0_A, p1_A, p0_B, p1_B;
                        var incomingPath = GetPath(from, fromLane);

                        if (endsHere)
                        {
                            p0_A = incomingPath[incomingPath.Length - 2];
                            p1_A = incomingPath[incomingPath.Length - 1];
                        }
                        else
                        {
                            p0_A = incomingPath[1];
                            p1_A = incomingPath[0];
                        }

                        if (to.startIntersection == intersection)
                        {
                            var outgoingPath = GetPath(to, toLane);

                            p0_B = outgoingPath[0];
                            p1_B = outgoingPath[1];
                        }
                        else
                        {
                            var outgoingPath = GetPath(to, to.MirrorLane(toLane));

                            p0_B = outgoingPath[outgoingPath.Length - 1];
                            p1_B = outgoingPath[outgoingPath.Length - 2];
                        }

                        // If the streets are almost parallel, an intersection point 
                        // might not make sense.
                        if (uturn)
                        {
                            var controlPt1 = p1_A + (p1_A - p0_A).normalized * 5f * Map.Meters;
                            var controlPt2 = p0_B + (p0_B - p1_B).normalized * 5f * Map.Meters;

                            MeshBuilder.AddCubicBezierCurve(currentPath,
                                p1_A, p0_B, controlPt1, controlPt2, 4);
                        }
                        else if (!Math.EquivalentAngles(p0_A, p1_A, p0_B, p1_B, 10f))
                        {
                            var intPt = Math.GetIntersectionPoint(
                                p0_A, p1_A, p0_B, p1_B, out bool found);

                            Debug.Assert(found, "streets do not intersect!");

                            MeshBuilder.AddQuadraticBezierCurve(
                                currentPath, p1_A, p0_B, intPt, 4);
                        }
                        else
                        {
                            currentPath.Add(p1_A);

                            var dir = p0_B - p1_A;
                            currentPath.Add(p1_A + (dir * .25f));
                            currentPath.Add(p1_A + (dir * .75f));

                            currentPath.Add(p0_B);
                        }

                        toPaths[intersection.RelativePosition(to)] = currentPath.ToArray();
                        currentPath.Clear();
                    }

                    if (!endsHere || from.street.isOneWay)
                    {
                        --fromLane;
                        if (fromLane < 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        ++fromLane;
                        if (fromLane == fromLanes)
                        {
                            break;
                        }
                    }
                }
            }

            return paths;
        }

        public DrivingCar EnterStreetSegment(Car car, StreetSegment seg,
                                             Vector3 pos, int lane)
        {
            var distanceFromStart = GetDistanceFromStart(seg, pos, lane);
            var newCar = new DrivingCar
            {
                car = car,
                segment = seg,
                exactPosition = pos,
                distanceFromStart = distanceFromStart,
                lane = lane
            };

            var cars = GetDrivingCars(seg);

            var firstCar = cars[lane];
            if (firstCar == null)
            {
                cars[lane] = newCar;
            }
            else if (firstCar.distanceFromStart > distanceFromStart)
            {
                newCar.next = firstCar;
                cars[lane] = newCar;
            }
            else
            {
                var prevCar = firstCar;
                firstCar = firstCar.next;

                while (firstCar != null && firstCar.distanceFromStart < distanceFromStart)
                {
                    prevCar = firstCar;
                    firstCar = firstCar.next;
                }

                prevCar.next = newCar;
                newCar.next = firstCar;
            }

            return newCar;
        }

        public void ExitStreetSegment(StreetSegment seg, DrivingCar car)
        {
#if DEBUG
            Destroy(car.numberTxt?.gameObject);
            car.numberTxt = null;
#endif

            var cars = GetDrivingCars(seg);
            var firstCar = cars[car.lane];
            if (firstCar == car)
            {
                cars[car.lane] = car.next;
                return;
            }

            while (firstCar.next != car)
            {
                Debug.Assert(firstCar.next != null, "car is not driving on this street!");
                firstCar = firstCar.next;
            }

            firstCar.next = car.next;
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

        CarOnIntersection EnterIntersection(DrivingCar drivingCar, StreetIntersection intersection,
                                            StreetSegment nextSegment)
        {
            var coi = new CarOnIntersection
            {
                car = drivingCar.car,
                turnType = drivingCar.nextTurn.Value,
                comesFrom = drivingCar.segment,
                goesTo = nextSegment
            };

            GetCarsOnIntersection(intersection).Add(coi);
            return coi;
        }

        void ExitIntersection(CarOnIntersection car, StreetIntersection intersection)
        {
            GetCarsOnIntersection(intersection).Remove(car);
        }

        int GetDefaultLane(StreetSegment seg, bool backward)
        {
            if (backward && !seg.street.isOneWay)
            {
                return seg.LeftmostLane;
            }

            return seg.RightmostLane;
        }

        void SetNextStep(DrivingCar car, PathPlanningResult result, int nextStep)
        {
            car.path = result;
            car.stepIdx = nextStep - 1;

            if (nextStep >= result.steps.Count)
            {
                return;
            }

            var step = result.steps[nextStep];
            if (step is DriveStep)
            {
                var drive = step as DriveStep;
                if (drive.driveSegment.backward)
                {
                    car.nextIntersection = drive.driveSegment.segment.endIntersection;
                }
                else
                {
                    car.nextIntersection = drive.driveSegment.segment.startIntersection;
                }

                car.nextSegment = drive.driveSegment.segment;
                car.nextTurn = GetTurnType(car.segment, car.nextSegment, car.nextIntersection);
            }
            else if (step is PartialDriveStep)
            {
                var drive = step as PartialDriveStep;
                if (drive.driveSegment.backward)
                {
                    car.nextIntersection = drive.driveSegment.segment.endIntersection;
                }
                else
                {
                    car.nextIntersection = drive.driveSegment.segment.startIntersection;
                }

                car.nextSegment = drive.driveSegment.segment;
                car.nextTurn = GetTurnType(car.segment, car.nextSegment, car.nextIntersection);
            }
        }

        public void GetStepPath(PathStep step, out StreetSegment segment,
                                out bool backward, out bool finalStep,
                                out int lane, out List<Vector3> positions,
                                out bool partialStart, out bool partialEnd,
                                out Vector2 direction)
        {
            finalStep = false;
            partialStart = false;
            partialEnd = false;
            direction = new Vector2();

            if (step is DriveStep)
            {
                var drive = step as DriveStep;

                segment = drive.driveSegment.segment;
                backward = drive.driveSegment.backward;
                lane = GetDefaultLane(segment, backward);

                positions = GetPath(segment, lane).ToList();
                if (backward)
                {
                    positions.Reverse();
                }
            }
            else if (step is PartialDriveStep)
            {
                var drive = step as PartialDriveStep;
                finalStep = drive.partialEnd;

                partialStart = drive.partialStart;
                partialEnd = drive.partialEnd;

                segment = drive.driveSegment.segment;
                backward = drive.driveSegment.backward;
                lane = GetDefaultLane(segment, backward);

                var path = GetPath(segment, lane);
                if (backward)
                {
                    path = path.Reverse().ToArray();
                }

                var minIdx = 0;
                Vector3 startPos;

                if (partialStart)
                {
                    var startDistance = ((Vector3)drive.startPos - path[0]).sqrMagnitude;
                    for (int i = 0; i < path.Length; ++i)
                    {
                        var pt = path[i];
                        var dist = (pt - path[0]).sqrMagnitude;
                        var cmp = dist.CompareTo(startDistance);

                        if (cmp > 0)
                        {
                            minIdx = i;
                            break;
                        }
                        else if (cmp == 0)
                        {
                            minIdx = i + 1;
                            break;
                        }
                    }

                    startPos = drive.startPos;
                }
                else
                {
                    startPos = path.First();
                }

                var maxIdx = path.Length;
                Vector3 endPos;

                if (partialEnd)
                {
                    var endDistance = ((Vector3)drive.endPos - path[0]).sqrMagnitude;
                    for (int i = 0; i < path.Length; ++i)
                    {
                        var pt = path[i];
                        var dist = (pt - path[0]).sqrMagnitude;
                        var cmp = dist.CompareTo(endDistance);

                        if (cmp >= 0)
                        {
                            maxIdx = i;
                            break;
                        }
                    }

                    endPos = drive.endPos;
                }
                else
                {
                    endPos = path.Last();
                }

                // This can happen when the the start and end position are the first position on
                // the street, which is technically still a valid path since an intersection will be
                // crossed.
                if (startPos.Equals(endPos))
                {
                    positions = null;

                    if (startPos.Equals(path.First()))
                    {
                        partialStart = false;
                        partialEnd = true;
                        direction = (path[1] - startPos).normalized;
                    }
                    else
                    {
                        Debug.Assert(startPos.Equals(path.Last()));
                        partialStart = true;
                        partialEnd = false;
                        direction = (endPos - path[path.Length - 2]).normalized;
                    }

                    return;
                }

                positions = new List<Vector3>();

                if (partialStart)
                {
                    positions.Add(startPos);
                }

                for (int i = minIdx; i < maxIdx; ++i)
                {
                    positions.Add(path[i]);
                }

                if (partialEnd)
                {
                    positions.Add(endPos);
                }
            }
            else
            {
                segment = null;
                backward = false;
                positions = null;
                lane = 0;
            }
        }

        void InitWalk(Citizien citizien, List<Vector3> positions,
                      PathPlanningResult result, int stepNo,
                      float progress = 0f)
        {
            var walkingCitizienObj = Instantiate(sim.walkingCitizienPrefab);
            var wc = walkingCitizienObj.GetComponent<WalkingCitizien>();
            wc.Initialize(sim, citizien, Utility.RandomColor);
            wc.transform.position = new Vector3(positions[0].x, positions[0].y, wc.transform.position.z);

            var finalStep = stepNo == result.steps.Count - 1;
            wc.FollowPath(positions, finalStep, (PathFollowingObject obj) =>
            {
                Destroy(walkingCitizienObj);

                if (!finalStep)
                {
                    InitStep(citizien, result, stepNo + 1);
                }
                else
                {
                    citizien.activePath = null;
                }
            });

            if (progress > 0f)
            {
                wc.pathFollow.SimulateProgress(progress);
            }
        }

        void InitStep(Citizien citizien, PathPlanningResult result,
                      int stepNo, float startingVelocity = 0f,
                      float progress = 0f, bool initTurn = false)
        {
            List<Vector3> positions;
            StreetSegment segment;
            bool backward;
            int lane;
            bool finalStep = false;

            citizien.activePath.stepIdx = stepNo;
            citizien.activePath.progress = 0f;
            citizien.activePath.isTurn = false;
            citizien.activePath.currentVelocity = startingVelocity;

            var step = result.steps[stepNo];
            switch (step.type)
            {
                case PathStep.Type.Walk:
                    {
                        var walk = step as WalkStep;
                        InitWalk(citizien, new List<Vector3> { walk.from, walk.to },
                                 result, stepNo, progress);

                        if (stepNo + 1 == result.steps.Count)
                        {
                            citizien.car?.gameObject.SetActive(false);
                        }

                        return;
                    }
                case PathStep.Type.Drive:
                case PathStep.Type.PartialDrive:
                    break;
                case PathStep.Type.PublicTransit:
                default:
                    Debug.LogError("unhandled path step type: " + step.type);
                    return;
            }

            GetStepPath(step, out segment, out backward, out finalStep,
                        out lane, out positions,
                        out bool partialStart, out bool partialEnd,
                        out Vector2 direction);

            if (positions == null || positions.Count == 0)
            {
                InitStep(citizien, result, stepNo + 1);
                return;
            }

            var car = citizien.car;
            if (car == null)
            {
                InitWalk(citizien, positions, result, stepNo);
                return;
            }

            var pos = positions.First();
            car.transform.position = new Vector3(pos.x, pos.y, car.transform.position.z);
            car.gameObject.SetActive(true);

            var drivingCar = EnterStreetSegment(car, segment, positions.First(), lane);
            drivingCar.backward = backward;

            car.drivingCar = drivingCar;
            SetNextStep(drivingCar, result, stepNo + 1);

            PathFollowingObject.CompletionCallback callback = (PathFollowingObject obj) =>
            {
                ExitStreetSegment(segment, drivingCar);

                if (stepNo + 1 != result.steps.Count)
                {
                    InitTurn(drivingCar, result, stepNo + 1, drivingCar.CurrentVelocity, progress);
                }
                else
                {
                    citizien.activePath = null;
                    car.gameObject.SetActive(false);
                }
            };

            if (initTurn)
            {
                var lastPos = positions.Last();
                car.transform.SetPositionInLayer(lastPos);

                callback.Invoke(null);
                return;
            }

            car.FollowPath(positions, startingVelocity, finalStep, callback);

            if (progress > 0f)
            {
                car.pathFollow.SimulateProgress(progress);
            }
        }

        Vector3 GetPerpendicularVector(DrivingCar drivingCar, PathPlanningResult result, int nextStep,
                                       out Vector3 p0, out Vector3 p1, out float offset)
        {
            var step = result.steps[nextStep];
            StreetIntersection intersection;

            if (step is DriveStep)
            {
                var drive = step as DriveStep;
                offset = drive.driveSegment.segment.GetStreetWidth(RenderingDistance.Near) / drive.driveSegment.segment.street.lanes;

                if (drive.driveSegment.backward)
                {
                    p0 = drive.driveSegment.segment.GetEndStopLinePosition();
                    intersection = drive.driveSegment.segment.endIntersection;

                }
                else
                {
                    p0 = drive.driveSegment.segment.GetStartStopLinePosition();
                    intersection = drive.driveSegment.segment.startIntersection;
                }
            }
            else if (step is PartialDriveStep)
            {
                var drive = step as PartialDriveStep;
                offset = drive.driveSegment.segment.GetStreetWidth(RenderingDistance.Near) / drive.driveSegment.segment.street.lanes;

                if (drive.driveSegment.backward)
                {
                    p0 = drive.driveSegment.segment.GetEndStopLinePosition();
                    intersection = drive.driveSegment.segment.endIntersection;
                }
                else
                {
                    p0 = drive.driveSegment.segment.GetStartStopLinePosition();
                    intersection = drive.driveSegment.segment.startIntersection;
                }
            }
            else
            {
                Debug.Assert(false, "should never happen!");
                p0 = Vector3.zero;
                offset = 0f;
                intersection = null;
            }

            p1 = intersection.position;

            var dir = p1 - p0;
            var perpendicular2d = Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;

            return new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);
        }

        void AddIntersectionPath(DrivingCar drivingCar, List<Vector3> path,
                                 PathPlanningResult result, int nextStep)
        {
            var intersectionPath = GetPath(drivingCar.nextIntersection, drivingCar.segment,
                                           drivingCar.nextSegment, drivingCar.lane);

            path.AddRange(intersectionPath);
        }

        void InitTurn(DrivingCar drivingCar, PathPlanningResult result,
                      int nextStep, float currentVelocity, float progress = 0f)
        {
            var step = result.steps[nextStep];
            if (!(step is DriveStep || step is PartialDriveStep))
                return;

            drivingCar.car.driver.activePath.isTurn = true;
            drivingCar.car.driver.activePath.progress = 0f;

            // First, generate a smooth path crossing the intersection.
            var intersectionPath = new List<Vector3>();
            AddIntersectionPath(drivingCar, intersectionPath, result, nextStep);

            var coi = EnterIntersection(drivingCar, drivingCar.nextIntersection, drivingCar.nextSegment);

            // After that, continue with the next segment.
            drivingCar.car.FollowPath(intersectionPath, currentVelocity, false,
                                      (PathFollowingObject obj) =>
            {
                ExitIntersection(coi, drivingCar.nextIntersection);
                InitStep(drivingCar.car.driver, result, nextStep, currentVelocity);
            });

            if (progress > 0)
            {
                drivingCar.car.pathFollow.SimulateProgress(progress);
            }
        }

        public void SpawnCar(PathPlanningResult result, Citizien driver)
        {
            var startPos = result.GetStartPointOnStreet();
            if (startPos == null)
            {
                return;
            }

            sim.CreateCar(driver, Vector3.zero);
            FollowPath(driver, result);
        }

        public void FollowPath(Citizien citizien, PathPlanningResult result)
        {
            citizien.activePath = new ActivePath
            {
                path = result,
                currentVelocity = 0f,
                stepIdx = 0,
                isTurn = false,
                progress = 0f,
            };

            InitStep(citizien, result, 0);
        }

        public void FollowPath(Citizien citizien, ActivePath path)
        {
            InitStep(citizien, path.path, path.stepIdx, path.currentVelocity,
                     path.progress, path.isTurn);
        }

        float sStar(float s0, float T, float v, float deltaV, float a)
        {
            return s0 + Mathf.Max(0f, v * T + ((v * deltaV) / (2 * Mathf.Sqrt(a * b))));
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
            // We have to give the right of way to cars coming in from the right.
            var rightSlot = relativePos + 1;
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
            if (otherCar.DistanceToIntersection >= 10f * Map.Meters)
            {
                return true;
            }

            // Otherwise whether or not we can go depends on the turn types.
            return !ConflictingTurns(intersection, relativePos, rightSlot,
                                     car.nextTurn.Value, otherCar.nextTurn.Value);
        }

        bool IsOccupied(DrivingCar car, StreetIntersection intersection, int relativePos)
        {
            var carsOnIntersection = GetCarsOnIntersection(intersection);
            foreach (var coi in carsOnIntersection)
            {
                if (coi.car == car.car)
                {
                    continue;
                }
                if (coi.comesFrom == car.segment)
                {
                    continue;
                }

                var fromPos = intersection.RelativePosition(coi.comesFrom);
                if (ConflictingTurns(intersection, relativePos, fromPos, car.nextTurn.Value,
                                     coi.turnType))
                {
                    return true;
                }
            }

            return false;
        }

        bool MustWait(DrivingCar car, StreetIntersection intersection, float distanceToIntersection)
        {
            // If the intersection is more than 35m away, don't slow down yet.
            if (distanceToIntersection >= 35f * Map.Meters)
            {
                return false;
            }

            var relativePos = intersection.RelativePosition(car.segment);

            // If the intersection is between 3 and 10m away, slow down.
            // if (distanceToIntersection >= 0.003f)
            // {
            //     // If there is no street on the right, we always have the right of way.
            //     return relativePos + 1 != intersection.emptySlot;
            // }

            // Otherwise, check if we have to give the right of way to another car.
            return !HasRightOfWay(car, intersection, relativePos)
                || IsOccupied(car, intersection, relativePos);
        }

        float GetIntersectionTerm(DrivingCar car, float a, float v_alpha)
        {
            StreetIntersection intersection = car.nextIntersection;
            if (intersection == null)
            {
                float goalDistance = car.DistanceToGoal;
                // Debug.Log("distance estimate:" + goalDistance);

                return BusyRoadTerm(0f, 1.5f, a, v_alpha, goalDistance, v_alpha);
            }

            TrafficLight tl;
            if (car.backward)
            {
                tl = car.segment.startTrafficLight;
            }
            else
            {
                tl = car.segment.endTrafficLight;
            }

            var distanceToIntersection = car.DistanceToIntersection;
            if (tl != null && tl.MustStop)
            {
                return BusyRoadTerm(car.car.length * 0.5f, 1.5f, a, v_alpha, distanceToIntersection, v_alpha);
            }

            if (!MustWait(car, intersection, distanceToIntersection))
            {
                return 0f;
            }

            return BusyRoadTerm(car.car.length * 0.5f, 1.5f, a, v_alpha, distanceToIntersection, v_alpha);
        }

        public float IntersectionTerm(DrivingCar car, float v, float s, float T)
        {
            return -Mathf.Pow(Mathf.Pow(v, 2) / (2 * s), 2) * (1 / b);
        }

        public float GetSafeAcceleration(DrivingCar car, float t, float v_alpha, float a)
        {
            // Desired velocity.
            var v0 = Mathf.Min(car.car.maxVelocity, car.segment.street.MaxSpeedMetersPerSecond);

            // Free road term
            float freeRoadTerm = Mathf.Pow(v_alpha / v0, delta);

            float busyRoadTerm;
            if (car.next == null)
            {
                busyRoadTerm = GetIntersectionTerm(car, a, v_alpha);
            }
            else
            {
                // Approaching rate.
                float deltaV = car.CurrentVelocity - car.next.CurrentVelocity;

                // Net distance to next vehicle.
                float s_alpha = (car.next.exactPosition - car.exactPosition).magnitude
                    - car.next.car.length - (car.car.length * 0.5f);

                busyRoadTerm = BusyRoadTerm(s0_car, T, a, v_alpha, s_alpha, deltaV);
            }

            // Safe Acceleration
            return a * (1 - freeRoadTerm - busyRoadTerm);
        }

        float RungeKutta_k1(DrivingCar car, float h, float yn)
        {
            float v_alpha = car.CurrentVelocity;
            float t = 0;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta_k2(DrivingCar car, float h, float yn, float k1)
        {
            float v_alpha = car.CurrentVelocity + (k1 / 2f);
            float t = h / 2;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta_k3(DrivingCar car, float h, float yn, float k2)
        {
            float v_alpha = car.CurrentVelocity + k2;
            float t = h;

            return h * GetSafeAcceleration(car, t, v_alpha, car.Acceleration);
        }

        float RungeKutta(DrivingCar car, float h, float yn, int order)
        {
            var k1 = RungeKutta_k1(car, h, yn);
            var k2 = k1;
            var sum = k1;
            var cnt = 1;

            for (int i = 0; i < order - 2; ++i)
            {
                k2 = RungeKutta_k2(car, h, yn, k2);
                sum += 2 * k2;
                cnt += 2;
            }

            var k3 = RungeKutta_k3(car, h, yn, k2);
            sum += k3;
            cnt += 1;

            return Mathf.Max(0f, yn + sum / cnt);
        }

        public float GetCarVelocity(DrivingCar car)
        {
            var tn = car.car.timeSinceLastUpdate;
            var yn = car.CurrentVelocity;

            return RungeKutta(car, tn, yn, 3);
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
                tl.Update(sim.SpeedMultiplier);
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

        void RenderCarNumbers()
        {
            if (!renderCarNumbers)
                return;

            foreach (var pair in drivingCars)
            {
                foreach (var carIt in pair.Value)
                {
                    var car = carIt;

                    int i = 0;
                    while (car != null)
                    {
                        Vector3 pos = car.exactPosition;
                        pos.z = Map.Layer(MapLayer.Foreground);

                        var txt = ((int)(car.distanceFromStart / Map.Meters)).ToString() + "m/" + car.lane.ToString() + "/" + i.ToString();
                        if (car.numberTxt == null)
                        {
                            car.numberTxt = sim.game.loadedMap.CreateText(pos, txt, Color.black, 11);
                            car.numberTxt.textMesh.fontSize = .01f;
                            car.numberTxt.textMesh.alignment = TMPro.TextAlignmentOptions.Center;
                        }
                        else
                        {
                            car.numberTxt.transform.position = pos;
                            car.numberTxt.textMesh.text = txt;
                        }

                        car.numberTxt.textMesh.autoSizeTextContainer = true;
                        car.numberTxt.textMesh.ForceMeshUpdate();

                        car = car.next;
                        ++i;
                    }
                }
            }
        }
#endif
    }
}