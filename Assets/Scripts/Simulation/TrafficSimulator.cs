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
            public ActivePath path;
            public Car car;
            public StreetSegment segment;
            public bool backward;
            public DrivingCar next;
            public DrivingCar prev;
            public int lane;
            public float distanceFromStart;
            public TrafficLight waitingForTrafficLight;
            
            public StreetSegment nextSegment;
            public StreetIntersection nextIntersection;
            public TurnType? nextTurn;
            public int nextLane;

            public Velocity CurrentVelocity => path.CurrentVelocity;

            public Vector2 CurrentDirection => path.CurrentDirection;

            public float Acceleration => car.Acceleration;

            public Vector2 CurrentPosition => path.transform.position;

            public float Length => path.Bounds.size.y;

            public float DistanceToNextCar => next != null
                ? (next.CurrentPosition - CurrentPosition).magnitude - next.Length * .5f - Length * .5f 
                : 0f;

            public float DistanceToIntersection => DistanceToGoal;

            public float DistanceToGoal => path.PathFollowingHelper.threshold - path.PathFollowingHelper.progress;

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
                    var distance = car.DistanceToIntersection
                                   + GetIntersectionLength(car.nextIntersection, car.segment, car.nextSegment)
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

        public float CurrentTrafficFactor
        {
            get
            {
                return GetTrafficFactor(sim.GameTime.Hour);
            }
        }

        void Awake()
        {
            this.drivingCars = new Dictionary<int, DrivingCar[]>();
            this.trafficLights = new Dictionary<int, TrafficLight>();
            this.computedPaths = new Dictionary<StreetSegment, Vector3[][]>();
            this.computedIntersectionPaths = new Dictionary<StreetIntersection, Vector3[][][][]>();
            this.intersectionPathLengths = new Dictionary<StreetIntersection, float[][]>();
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
            if (seg.IsOneWay)
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
            if (!drivingCars.TryGetValue(seg.id, out DrivingCar[] cars))
            {
                cars = new DrivingCar[seg.street.lanes];
                drivingCars.Add(seg.id, cars);
            }

            return cars;
        }

        DrivingCar GetDrivingCars(StreetSegment seg, int lane)
        {
            return GetDrivingCars(seg)[lane];
        }

        DrivingCar[] GetCarsOnIntersection(StreetIntersection intersection)
        {
            if (!drivingCars.TryGetValue(intersection.id, out DrivingCar[] cars))
            {
                cars = new DrivingCar[intersection.intersectingStreets.Count * intersection.intersectingStreets.Count];
                drivingCars.Add(intersection.id, cars);
            }

            return cars;
        }
        
        DrivingCar GetFirstCarOnIntersection(StreetIntersection intersection, StreetSegment from, StreetSegment to)
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

            foreach (var step in result.path.Steps)
            {
                StreetSegment nextSegment;
                GetStepPath(step, out nextSegment, out bool backward, out bool finalStep, out int lane,
                            out List<Vector3> positions, out bool partialStart, out bool partialEnd,
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
            if (!computedIntersectionPaths.TryGetValue(intersection, out Vector3[][][][] paths))
            {
                paths = ComputeIntersectionPaths(intersection);
            }

            return paths[intersection.RelativePosition(from)]
                [from.LanePositionFromMiddle(lane) - 1]
                [intersection.RelativePosition(to)];
        }

        public float GetIntersectionLength(StreetIntersection intersection, StreetSegment from, StreetSegment to)
        {
            if (!computedIntersectionPaths.TryGetValue(intersection, out Vector3[][][][] paths))
            {
                ComputeIntersectionPaths(intersection);
            }

            return intersectionPathLengths[intersection][intersection.RelativePosition(from)]
                [intersection.RelativePosition(to)];
        }

        public Vector3[][][][] ComputeIntersectionPaths(StreetIntersection intersection)
        {
            var paths = new Vector3[intersection.NumIntersectingStreets][][][];
            var lengths = new float[intersection.NumIntersectingStreets][];
            
            var currentPath = new List<Vector3>();

            foreach (var from in intersection.IncomingStreets)
            {
                var fromLanes = from.street.lanes;
                var fromLanesPerDirection = from.LanesPerDirection;

                var idx = intersection.RelativePosition(from);
                ref var fromPaths = ref paths[idx];
                fromPaths = new Vector3[fromLanesPerDirection][][];

                ref var fromLengths = ref lengths[idx];
                fromLengths = new float[intersection.NumIntersectingStreets];

                var endsHere = from.endIntersection == intersection;

                int fromLane = fromLanesPerDirection;
                if (from.IsOneWay)
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
                    idx = from.LanePositionFromMiddle(fromLane) - 1;
                    ref var toPaths = ref fromPaths[idx];
                    toPaths = new Vector3[intersection.NumIntersectingStreets][];

                    var i = 0;
                    foreach (var to in intersection.OutgoingStreets)
                    {
                        var uturn = false;
                        if (from == to)
                        {
                            if (endsHere && !from.StartUTurnAllowed)
                            {
                                continue;
                            }
                            if (!endsHere && !from.EndUTurnAllowed)
                            {
                                continue;
                            }

                            uturn = true;
                        }
                        
                        var toLanes = to.street.lanes;
                        var toLanesHalf = to.LanesPerDirection;

                        int toLane;
                        var distanceFromMiddle = from.LanePositionFromMiddle(fromLane);

                        if (uturn)
                        {
                            toLane = endsHere ? fromLane : from.MirrorLane(fromLane);
                        }
                        else if (from.LanesPerDirection == to.LanesPerDirection)
                        {
                            // Use equivalent lane
                            if (to.IsOneWay)
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
                            var controlPt1 = p1_A + (p1_A - p0_A).normalized * 5f;
                            var controlPt2 = p0_B + (p0_B - p1_B).normalized * 5f;

                            MeshBuilder.AddCubicBezierCurve(currentPath,
                                p1_A, p0_B, controlPt1, controlPt2, 8, false);
                        }
                        else if (!Math.EquivalentAngles(p0_A, p1_A, p0_B, p1_B, 10f))
                        {
                            var intPt = Math.GetIntersectionPoint(
                                p0_A, p1_A, p0_B, p1_B, out bool found);

                            Debug.Assert(found, "streets do not intersect!");

                            MeshBuilder.AddQuadraticBezierCurve(
                                currentPath, p1_A, p0_B, intPt, 8, false);
                        }
                        else
                        {
                            // currentPath.Add(p1_A);

                            var dir = p0_B - p1_A;
                            currentPath.Add(p1_A + (dir * .25f));
                            currentPath.Add(p1_A + (dir * .75f));

                            // currentPath.Add(p0_B);
                        }

                        idx = intersection.RelativePosition(to);
                        toPaths[idx] = currentPath.ToArray();
                        fromLengths[idx] = Math.PathLength(currentPath);

                        currentPath.Clear();
                    }

                    if (!endsHere || from.IsOneWay)
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

            computedIntersectionPaths.Add(intersection, paths);
            intersectionPathLengths.Add(intersection, lengths);

            return paths;
        }

        public DrivingCar EnterStreetSegment(Car car, StreetSegment seg,
                                             Vector3 pos, int lane,
                                             ActivePath path)
        {
            var distanceFromStart = GetDistanceFromStart(seg, pos, lane);
            var newCar = new DrivingCar
            {
                path = path,
                car = car,
                segment = seg,
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

            if (path.nextStep is TurnStep turnStep)
            {
                newCar.nextIntersection = turnStep.intersection;
                newCar.nextSegment = turnStep.to.segment;
                newCar.nextTurn = GetTurnType(newCar.segment, newCar.nextSegment, newCar.nextIntersection);
                newCar.nextLane = GetDefaultLane(newCar.nextSegment, turnStep.to.backward);
            }

            return newCar;
        }

        public void ExitStreetSegment(StreetSegment seg, DrivingCar car)
        {
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
            var firstCar = GetFirstCarOnIntersection(intersection, drivingCar.segment, drivingCar.nextSegment);
            drivingCar.next = null;
            drivingCar.prev = null;

            if (firstCar != null)
            {
                firstCar.prev = drivingCar;
                drivingCar.next = firstCar;
            }

            SetFirstCarOnIntersection(intersection, drivingCar.segment, drivingCar.nextSegment, drivingCar);
        }

        public void ExitIntersection(DrivingCar car, StreetIntersection intersection)
        {
            if (car.next != null)
            {
                GameController.instance.EnterPause();
                GameController.instance.input.SetCameraPosition(car.CurrentPosition);
                GameController.instance.input.SetZoomLevel(InputController.minZoom);
                car.path.citizen.ActivateModal();
                return;
            }
            
            Debug.Assert(car.next == null, "only last car can exit intersection!");

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
                
                positions = new List<Vector3>();

                // This can happen when the the start and end position are the first position on
                // the street, which is technically still a valid path since an intersection will be
                // crossed.
                if (startPos.Equals(endPos))
                {
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

                    // FIXME is there a better fix for this? Seems a tad hacky
                    positions.Add(startPos);
                    positions.Add(startPos + (Vector3)(direction * .001f));

                    return;
                }

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
            else if (step is TurnStep turnStep)
            {
                segment = null;
                backward = false;
                lane = GetDefaultLane(turnStep.from.segment, turnStep.from.backward);
                positions = GetIntersectionPath(turnStep, lane);
            }
            else
            {
                segment = null;
                backward = false;
                positions = null;
                lane = 0;
            }
        }

        public List<Vector3> GetIntersectionPath(TurnStep step, int lane)
        {
            return GetPath(step.intersection, step.from.segment, step.to.segment, lane).ToList();
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
            float s_alpha = Mathf.Max(car.Length * 0.5f, 
                nextCarAndDistance.Item2 - car.Length * 0.5f - nextCarAndDistance.Item1.Length * .5f);

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

            for (int i = 0; i < order - 2; ++i)
            {
                k2 = RungeKutta_k2(car, 1f, yn, k2);
                sum += 2 * k2;
                cnt += 2;
            }

            var k3 = RungeKutta_k3(car, 1f, yn, k2);
            sum += k3;
            cnt += 1;

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

                if (path._drivingState.drivingCar.Turning)
                {
                    txt.text = "";
                    continue;
                }

                var dc = path._drivingState.drivingCar;
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

                if (pos == 1)
                {
                    Debug.Assert(dc == GetDrivingCars(dc.segment)[dc.lane], "not first");
                }

                txt.text = pos + " / " + n;
            }
        }
#endif
    }
}