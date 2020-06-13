using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious.PathPlanning
{
    public interface IStop
    {
        MapObjectKind Kind { get; }
        
        IEnumerable<IRoute> Routes { get; }
        Vector2 Location { get; }
        bool uTurnAllowed { get; }

        bool IsGoalReached(IStop goal);
    }

    public static class IStopExtensions
    {
        public static float EstimatedDistance(this IStop start, IStop goal)
        {
            var speed = Velocity.FromRealTimeKPH(30);
            return (float)(Distance.Between(start.Location, goal.Location) / speed).TotalSeconds;
        }
    }

    public interface IRoute
    {
        MapObjectKind Kind { get; }

        IStop Begin { get; }
        IStop End { get; }
        bool OneWay { get; }

        TimeSpan TravelTime { get; }
        Velocity AverageSpeed { get; }
        int AssociatedID { get; }

        DateTime NextDeparture(DateTime after);
    }

    public abstract class PathStep
    {
        public enum Type
        {
            Walk,
            Drive,
            PartialDrive,
            Turn,
            PublicTransit,
            Wait,
        }

        public Type type { get; }

        protected PathStep(Type type)
        {
            this.type = type;
        }

        public abstract TimeSpan EstimateDuration(PathPlanningOptions options);

        public abstract float GetCost(PathPlanningOptions options);

        protected abstract Google.Protobuf.IMessage ToProtobufInternal();

        public Serialization.PathStep Serialize()
        {
            var result = new Serialization.PathStep
            {
                Kind = (Serialization.PathStep.Types.PathStepKind)type,
            };

            result.Details = Google.Protobuf.WellKnownTypes.Any.Pack(ToProtobufInternal());
            return result;
        }

        public static PathStep Deserialize(Map map, Serialization.PathStep pathStep)
        {
            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.WaitStep waitStep))
            {
                return new WaitStep(TimeSpan.FromMilliseconds(waitStep.WaitingTime));
            }

            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.WalkStep walkStep))
            {
                return new WalkStep(
                    walkStep.From.Deserialize(), walkStep.To.Deserialize());
            }

            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.DriveStep driveStep))
            {
                return new DriveStep(
                    new DriveSegment {
                    segment = map.GetMapObject<StreetSegment>((int)driveStep.SegmentID),
                    backward = driveStep.Backward,
                });
            }

            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.PartialDriveStep partialDriveStep))
            {
                return new PartialDriveStep(
                    partialDriveStep.StartPos.Deserialize(),
                    partialDriveStep.EndPos.Deserialize(),
                    new DriveSegment
                    {
                        segment = map.GetMapObject<StreetSegment>((int)partialDriveStep.SegmentID),
                        backward = partialDriveStep.Backward,
                    }, partialDriveStep.PartialStart,
                    partialDriveStep.PartialEnd,
                    map.GetMapObject((int)(partialDriveStep.ParkingLotID)));
            }

            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.PublicTransitStep transitStep))
            {
                return new PublicTransitStep(
                    map.GetMapObject<Line>((int)transitStep.LineID),
                    transitStep.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToArray());
            }
            
            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.TurnStep turnStep))
            {
                return new TurnStep(
                    new DriveSegment {
                        segment = map.GetMapObject<StreetSegment>((int)turnStep.FromSegmentID),
                        backward = turnStep.FromBackward,
                    },
                    new DriveSegment {
                        segment = map.GetMapObject<StreetSegment>((int)turnStep.ToSegmentID),
                        backward = turnStep.ToBackward,
                    },
                    map.GetMapObject<StreetIntersection>((int)turnStep.IntersectionID));
            }

            Debug.LogError("invalid path step");
            return null;
        }
    }

    public class WalkStep : PathStep
    {
        /// The starting point of the walk.
        public Vector2 from;

        /// The ending point of the walk.
        public Vector2 to;

        public WalkStep(Vector2 from, Vector2 to) : base(Type.Walk)
        {
            this.from = from;
            this.to = to;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var distMeters = Distance.Between(from, to);
            return distMeters / (options.citizen?.WalkingSpeed ?? Velocity.FromKPH(5));
        }

        public override float GetCost(PathPlanningOptions options)
        {
            return (from - to).magnitude * options.walkingTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            return new Serialization.PathStep.Types.WalkStep
            {
                From = from.ToProtobuf(),
                To = to.ToProtobuf(),
            };
        }
    }

    public class WaitStep : PathStep
    {
        /// The waiting time.
        public TimeSpan waitingTime;

        public WaitStep(TimeSpan waitingTime) : base(Type.Wait)
        {
            this.waitingTime = waitingTime;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            return waitingTime;
        }
        
        public override float GetCost(PathPlanningOptions options)
        {
            return (float)waitingTime.TotalMinutes * options.waitingTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            return new Serialization.PathStep.Types.WaitStep
            {
                WaitingTime = (float)waitingTime.TotalMilliseconds,
            };
        }
    }

    public class PublicTransitStep : PathStep
    {
        /// The line to travel with.
        public Line line;

        /// The routes to follow.
        public Route[] routes;

        public PublicTransitStep(Line line, Route[] routes) : base(Type.PublicTransit)
        {
            this.line = line;
            this.routes = routes;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var ts = new TimeSpan();
            foreach (var route in routes)
            {
                ts += route.TravelTime;
            }

            return ts;
        }

        public override float GetCost(PathPlanningOptions options)
        {
            return (float)EstimateDuration(options).TotalMinutes * options.travelTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            var result = new Serialization.PathStep.Types.PublicTransitStep
            {
                LineID = (uint)line.Id,
            };

            result.RouteIDs.AddRange(routes.Select(r => (uint)r.Id));
            return result;
        }
    }

    public struct DriveSegment
    {
        public StreetSegment segment;
        public bool backward;
    }

    public class DriveStep : PathStep
    {
        /// The street segments to follow.
        public DriveSegment driveSegment;

        public DriveStep(DriveSegment driveSegment) : base(Type.Drive)
        {
            this.driveSegment = driveSegment;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            return driveSegment.segment.TravelTime;
        }

        public override float GetCost(PathPlanningOptions options)
        {
            return (float)EstimateDuration(options).TotalMinutes * options.travelTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            return new Serialization.PathStep.Types.DriveStep
            {
                SegmentID = (uint)driveSegment.segment.id,
                Backward = driveSegment.backward,
            };
        }
    }

    public class PartialDriveStep : PathStep
    {
        /// The starting point of the drive.
        public Vector2 startPos;

        /// The end point of the drive.
        public Vector2 endPos;

        /// The street segments to follow.
        public DriveSegment driveSegment;

        /// This step is partial at the start.
        public bool partialStart;

        /// This step is partial at the end.
        public bool partialEnd;

        // The parking lot that this drive ends at.
        public IMapObject parkingLot;

        public PartialDriveStep(Vector2 startPos, Vector2 endPos,
                                DriveSegment segment,
                                bool partialStart, bool partialEnd,
                                IMapObject parkingLot = null)
            : base(Type.PartialDrive)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.driveSegment = segment;
            this.partialStart = partialStart;
            this.partialEnd = partialEnd;
            this.parkingLot = parkingLot;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var seg = driveSegment.segment;

            var totalLength = seg.length;
            if (partialStart)
            {
                if (driveSegment.backward)
                {
                    totalLength -= seg.GetDistanceFromEnd(startPos);

                }
                else
                {
                    totalLength -= seg.GetDistanceFromStart(startPos);
                }
            }
            if (partialEnd)
            {
                if (driveSegment.backward)
                {
                    totalLength -= seg.GetDistanceFromStart(endPos);

                }
                else
                {
                    totalLength -= seg.GetDistanceFromEnd(endPos);
                }
            }

            if (totalLength < 0f)
            {
                totalLength = seg.length;
                if (partialStart)
                {
                    if (driveSegment.backward)
                    {
                        totalLength -= seg.GetDistanceFromEnd(startPos);

                    }
                    else
                    {
                        totalLength -= seg.GetDistanceFromStart(startPos);
                    }
                }
                if (partialEnd)
                {
                    if (driveSegment.backward)
                    {
                        totalLength -= seg.GetDistanceFromStart(endPos);

                    }
                    else
                    {
                        totalLength -= seg.GetDistanceFromEnd(endPos);
                    }
                }

                Debug.LogWarning("total length < 0");
                totalLength = seg.length;
            }

            return seg.GetTravelTime(Distance.FromMeters(totalLength));
        }

        public float DistanceFromStart
        {
            get
            {
                if (!partialStart)
                {
                    return 0f;
                }

                var sim = GameController.instance.sim.trafficSim;
                var path = sim.StreetPathBuilder.GetPath(driveSegment.segment,
                    sim.GetDefaultLane(driveSegment.segment, driveSegment.backward));

                return path.GetDistanceFromStart(startPos);
            }
        }
        
        public override float GetCost(PathPlanningOptions options)
        {
            return (float)EstimateDuration(options).TotalMinutes * options.travelTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            return new Serialization.PathStep.Types.PartialDriveStep
            {
                StartPos = startPos.ToProtobuf(),
                EndPos = endPos.ToProtobuf(),
                SegmentID = (uint)driveSegment.segment.id,
                Backward = driveSegment.backward,
                PartialStart = partialStart,
                PartialEnd = partialEnd,
                ParkingLotID = (uint) (parkingLot?.Id ?? 0),
            };
        }
    }
    
    public class TurnStep : PathStep
    {
        /// The street segment from which the turn originates.
        public DriveSegment from;
        
        /// The street segment to which the turn goes.
        public DriveSegment to;
        
        /// The intersection of the turn.
        public StreetIntersection intersection;

        public TurnStep(DriveSegment from, DriveSegment to, StreetIntersection intersection) : base(Type.Turn)
        {
            this.from = from;
            this.to = to;
            this.intersection = intersection;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var trafficSim = GameController.instance.sim.trafficSim;
            var length = trafficSim.StreetPathBuilder.GetIntersectionPath(intersection, from.segment, to.segment).Length;

            return Distance.FromMeters(length) / from.segment.AverageSpeed;
        }
        
        public override float GetCost(PathPlanningOptions options)
        {
            return (float)EstimateDuration(options).TotalMinutes * options.travelTimeFactor;
        }

        protected override Google.Protobuf.IMessage ToProtobufInternal()
        {
            return new Serialization.PathStep.Types.TurnStep
            {
                FromSegmentID = (uint)from.segment.id,
                FromBackward = from.backward,
                ToSegmentID = (uint)to.segment.id,
                ToBackward = to.backward,
                IntersectionID = (uint)intersection.id,
            };
        }
    }

    public class PointOnStreet : IStop
    {
        public MapObjectKind Kind => MapObjectKind.StreetSegment;
        public StreetSegment street;
        public Vector2 pos;
        public int prevIdx;

        public IEnumerable<IRoute> Routes
        {
            get
            {
                var routes = new List<IRoute>();
                routes.AddRange(street.End.Routes);

                if (!street.IsOneWay)
                    routes.AddRange(street.Begin.Routes);

                return routes;
            }
        }

        public Vector2 Location => pos;

        public bool IsGoalReached(IStop goal)
        {
            if (goal is StreetIntersection)
            {
                var intersection = goal as StreetIntersection;
                foreach (var s in intersection.IntersectingStreets)
                {
                    if (s == street)
                        return true;
                }
            }
            if (goal is PointOnStreet)
            {
                return (goal as PointOnStreet).street == street;
            }

            return false;
        }

        public bool uTurnAllowed => false;
    }

    public class PathPlanningOptions
    {
        /// The citizen these options belong to.
        public Citizen citizen;

        /// Whether or not a car can be used.
        public bool allowCar = true;

        /// Whether or not to allow walking.
        public bool allowWalk = true;

        /// Whether or not to allow rivers.
        public bool useRivers = false;

        /// The maximum acceptable walking distance between stations.
        public float maxWalkingDistance = 1000f;

        /// Factor that walking time is multiplied with for scoring.
        public float walkingTimeFactor = 3.0f;

        /// Factor that traveling time is multiplied with for scoring.
        public float travelTimeFactor = 1.0f;

        /// Factor that waiting time is multiplied with for scoring.
        public float waitingTimeFactor = 2.0f;

        /// Factor that driving time is multiplied with for scoring.
        public float carTimeFactor = 2.0f;

        /// Number of minutes that a change is penalized with.
        public float changingPenalty = 10.0f;

        public Serialization.PathPlanningOptions ToProtobuf()
        {
            return new Serialization.PathPlanningOptions
            {
                AllowCar = allowCar,
                AllowWalk = allowWalk,
                MaxWalkingDistance = maxWalkingDistance,
                WalkingTimeFactor = walkingTimeFactor,
                TravelTimeFactor = travelTimeFactor,
                WaitingTimeFactor = waitingTimeFactor,
                CarTimeFactor = carTimeFactor,
                ChangingPenalty = changingPenalty,
            };
        }

        public static PathPlanningOptions Deserialize(Serialization.PathPlanningOptions o)
        {
            return new PathPlanningOptions
            {
                allowCar = o.AllowCar,
                allowWalk = o.AllowWalk,
                maxWalkingDistance = o.MaxWalkingDistance,
                walkingTimeFactor = o.WalkingTimeFactor,
                travelTimeFactor = o.TravelTimeFactor,
                waitingTimeFactor = o.WaitingTimeFactor,
                carTimeFactor = o.CarTimeFactor,
                changingPenalty = o.ChangingPenalty,
            };
        }
    }

    public class PlannedPath
    {
        /// The steps to take on this path.
        public readonly PathStep[] Steps;

        /// Whether or not this path is a pure walk.
        public bool IsWalk => Steps.All(s => s is WalkStep);

        /// Whether or not this path uses any public transit.
        public bool UsesPublicTransit => Steps.Any(step => step.type == PathStep.Type.PublicTransit);

        /// Whether or not this path is a valid tram path.
        public bool ValidForTram
        {
            get
            {
                foreach (var step in Steps)
                {
                    if (step is DriveStep && !(step as DriveStep).driveSegment.segment.hasTramTracks)
                    {
                        return false;
                    }
                    if (step is PartialDriveStep && !(step as PartialDriveStep).driveSegment.segment.hasTramTracks)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public PlannedPath(PathStep[] steps)
        {
            this.Steps = steps;
        }

        public PlannedPath(List<PathStep> steps)
        {
            DriveStep previousDrive = null;
            PartialDriveStep previousPartialDrive = null;

            for (var i = 0; i < steps.Count; ++i)
            {
                var step = steps[i];
                switch (step.type)
                {
                    case PathStep.Type.Drive:
                    case PathStep.Type.PartialDrive:
                    {
                        DriveSegment? prevSeg = previousDrive?.driveSegment ?? previousPartialDrive?.driveSegment;
                        if (prevSeg == null)
                        {
                            break;
                        }

                        DriveSegment nextSeg;
                        StreetIntersection intersection;
                        
                        if (step is DriveStep driveStep)
                        {
                            intersection = driveStep.driveSegment.backward
                                ? driveStep.driveSegment.segment.endIntersection
                                : driveStep.driveSegment.segment.startIntersection;

                            nextSeg = driveStep.driveSegment;
                        }
                        else
                        {
                            var partialDriveStep = step as PartialDriveStep;
                            intersection = partialDriveStep.driveSegment.backward
                                ? partialDriveStep.driveSegment.segment.endIntersection
                                : partialDriveStep.driveSegment.segment.startIntersection;

                            nextSeg = partialDriveStep.driveSegment;
                        }

                        var turnStep = new TurnStep(prevSeg.Value, nextSeg, intersection);
                        steps.Insert(i++, turnStep);
                         
                        break;
                    }
                    default:
                        break;
                }

                previousDrive = step as DriveStep;
                previousPartialDrive = step as PartialDriveStep;
            }

            this.Steps = steps.ToArray();
        }

        public PlannedPath(Serialization.PlannedPath path, Map map)
        {
            this.Steps = path.Steps.Select(p => PathStep.Deserialize(map, p)).ToArray();
        }

        public Serialization.PlannedPath Serialize()
        {
            var result = new Serialization.PlannedPath();
            result.Steps.AddRange(Steps.Select(s => s.Serialize()));

            return result;
        }
        
        public void DebugDraw()
        {
            var positions = new List<Vector2>();
            foreach (var step in Steps)
            {
                var c = Color.black;
                var width = 1f;

                if (step is WalkStep)
                {
                    var walk = step as WalkStep;
                    positions.Add(new Vector2(walk.from.x, walk.from.y));
                    positions.Add(new Vector2(walk.to.x, walk.to.y));
                    c = Color.green;
                }
                else if (step is PublicTransitStep transitStep)
                {
                    c = transitStep.line.color;
                    width *= 1.5f;

                    foreach (var route in transitStep.routes)
                    {
                        positions.AddRange(route.positions.Select(v => (Vector2)v));
                        route.line.SetTransparency(.5f);
                    }
                }
                else if (step is DriveStep || step is PartialDriveStep)
                {
                    c = Color.red;
                    width *= 2f;

                    var trafficSim = GameController.instance.sim.trafficSim;
                    trafficSim.GetStepPath(step, out StreetSegment segment,
                        out bool backward, out bool finalStep,
                        out int lane, out positions,
                        out bool partialStart, out bool partialEnd,
                        out Vector2 direction);
                }

                if (positions.Count != 0)
                {
                    Utility.DrawLine(positions.ToArray(), width, c, Map.Layer(MapLayer.Foreground));
                    positions.Clear();
                }
            }
        }
    }

    public class PathPlanningResult
    {
        /// The path planning options used to obtain the path.
        public readonly PathPlanningOptions options;

        /// The total cost of the path.
        public readonly float cost;

        /// The time you have to leave when taking this path.
        public readonly DateTime leaveBy;

        /// The time you will arrive when taking this path.
        public DateTime arriveAt => arrivalTimes.Last();

        /// The actual path.
        public readonly PlannedPath path;

        /// The departure times for each step.
        public readonly DateTime[] arrivalTimes;

        /// The total duration of the path in minutes.
        public TimeSpan duration => arriveAt - leaveBy;

        public PathPlanningResult(PathPlanningOptions options,  DateTime leaveBy, PlannedPath path)
        {
            this.options = options;
            this.leaveBy = leaveBy;
            this.path = path;
            this.arrivalTimes = new DateTime[path.Steps.Length];

            var time = leaveBy;
            var i = 0;

            foreach (var step in path.Steps)
            {
                cost += step.GetCost(options);
                arrivalTimes[i++] = time.Add(step.EstimateDuration(options));
            }
        }

        public PathPlanningResult(Serialization.PathPlanningResult p, Map map)
        {
            this.options = PathPlanningOptions.Deserialize(p.Options);
            this.leaveBy = new DateTime((long) p.LeaveBy);
            this.cost = p.Cost;
            this.path = new PlannedPath(p.Path, map);
            this.arrivalTimes = p.StepTimes.Select(ticks => new DateTime(ticks)).ToArray();
        }

        public override string ToString()
        {
            string s = $"Cost: {cost}, duration: {duration.TotalMinutes:n2}min\n";
            s += "[" + leaveBy.ToLongTimeString() + "] leave\n";

            var i = 0;
            var time = leaveBy;

            foreach (PathStep step in path.Steps)
            {
                if (i++ != 0) s += "\n";

                var endTime = time.Add(step.EstimateDuration(options));
                s += $"[{time.ToLongTimeString()} - {endTime.ToLongTimeString()}] ";

                if (step is WalkStep)
                {
                    s += "walk";
                }
                else if (step is WaitStep waitStep)
                {
                    s += $"wait for line {(path.Steps[i] as PublicTransitStep).line.name}";
                }
                else if (step is PublicTransitStep travelStep)
                {
                    s += $"travel line {travelStep.line.name} until {travelStep.routes[travelStep.routes.Length - 1].endStop.name}";
                }
                else if (step is DriveStep driveStep)
                {
                    s += $"drive on {driveStep.driveSegment.segment.name}";
                }
                else if (step is PartialDriveStep partialDriveStep)
                {
                    s += $"drive partially on {partialDriveStep.driveSegment.segment.name}";
                }
                else if (step is TurnStep turnStep)
                {
                    s += $"turn from {turnStep.from.segment.name} to {turnStep.to.segment.name}";
                }

                time = endTime;
            }

            if (i++ != 0) s += "\n";
            s += "[" + time.ToLongTimeString() + "] arrival\n";

            return s;
        }

        public Serialization.PathPlanningResult ToProtobuf()
        {
            var result = new Serialization.PathPlanningResult
            {
                Cost = cost,
                LeaveBy = (ulong)leaveBy.Ticks,
                ArriveAt = (ulong)arriveAt.Ticks,
                Options = options.ToProtobuf(),
                Path = path.Serialize(),
            };

            return result;
        }
    }

    public class PathPlanner
    {
        // The path planning options.
        PathPlanningOptions options;

        /// The starting position.
        IStop start;

        /// The goal position.
        IStop goal;

        /// The router instance.
        private Router _router;

        // The set of nodes already evaluated
        HashSet<IStop> closedSet;

        // The set of currently discovered nodes that are not evaluated yet.
        // Initially, only the start node is known.
        HashSet<IStop> openSet;

        // For each node, which node it can most efficiently be reached from.
        // If a node can be reached from many nodes, cameFrom will eventually contain the
        // most efficient previous step.
        Dictionary<IStop, IStop> cameFrom;
        Dictionary<IStop, IRoute> cameFromRoute;
        Dictionary<IStop, PlannedPath> cameFromHub;

        // For each node, the duration of getting from the start node to that node.
        Dictionary<IStop, float> durationMap;

        // For each node, the waiting time at that node.
        Dictionary<IStop, float> waitingTimeMap;

        // For each node, the cost of getting from the start node to that node.
        Dictionary<IStop, float> gScore;

        // For each node, the total cost of getting from the start node to the goal
        // by passing by that node. That value is partly known, partly heuristic.
        Dictionary<IStop, float> fScore;

        public PathPlanner(PathPlanningOptions options = null)
        {
            this._router = GameController.instance.router;
            this.options = options;
            this.closedSet = new HashSet<IStop>();
            this.openSet = new HashSet<IStop>();
            this.cameFrom = new Dictionary<IStop, IStop>();
            this.cameFromRoute = new Dictionary<IStop, IRoute>();
            this.cameFromHub = new Dictionary<IStop, PlannedPath>();
            this.durationMap = new Dictionary<IStop, float>();
            this.waitingTimeMap = new Dictionary<IStop, float>();
            this.gScore = new Dictionary<IStop, float>();
            this.fScore = new Dictionary<IStop, float>();
        }

        public void Reset()
        {
            closedSet.Clear();
            openSet.Clear();
            cameFrom.Clear();
            cameFromRoute.Clear();
            cameFromHub.Clear();
            durationMap.Clear();
            waitingTimeMap.Clear();
            gScore.Clear();
            fScore.Clear();
        }

        static float GetScore(Dictionary<IStop, float> map, IStop stop)
        {
            if (map.TryGetValue(stop, out float val))
            {
                return val;
            }

            return float.PositiveInfinity;
        }

        static IStop Lowest(HashSet<IStop> set, Dictionary<IStop, float> map)
        {
            float lowest = float.PositiveInfinity;
            IStop lowestIStop = null;

            foreach (IStop stop in set)
            {
                float score = GetScore(map, stop);
                if (score < lowest)
                {
                    lowest = score;
                    lowestIStop = stop;
                }
            }

            return lowestIStop;
        }

        List<PathStep> ReconstructPath()
        {
            var routes = new List<Tuple<IStop, IRoute>>();
            var current = goal;

            while (true)
            {
                if (!cameFrom.ContainsKey(current))
                {
                    break;
                }

                var route = cameFromRoute[current];
                current = cameFrom[current];

                routes.Add(Tuple.Create(current, route));
            }

            var transitRoutes = new List<Route>();
            var steps = new List<PathStep>();

            for (var i = routes.Count - 1; i >= 0; --i)
            {
                var (stop, route) = routes[i];
                if (route == null)
                {
                    var hubPath = cameFromHub[stop];
                    hubPath.DebugDraw();
                    steps.AddRange(hubPath.Steps);

                    continue;
                }

                var backward = route.End == stop;
                switch (stop)
                {
                    case Stop _:
                    {
                        var transitRoute = route as Route;
                        if (transitRoutes.Count > 0 && transitRoutes[0].line != transitRoute.line)
                        {
                            steps.Add(new PublicTransitStep(
                                transitRoutes[0].line, 
                                transitRoutes.ToArray()));

                            transitRoutes.Clear();
                        }

                        transitRoutes.Add(transitRoute);
                        break;
                    }
                    case StreetIntersection _:
                        steps.Add(new DriveStep(new DriveSegment
                        {
                            segment = route as StreetSegment,
                            backward = backward,
                        }));
                        break;
                }
            }

            if (transitRoutes.Count > 0)
            {
                steps.Add(new PublicTransitStep(
                    transitRoutes[0].line, 
                    transitRoutes.ToArray()));
            }

            return steps;
        }

        // Based on heuristic A* (https://en.wikipedia.org/wiki/A*_search_algorithm)
        List<PathStep> GetPath()
        {
            Reset();
            openSet.Add(start);

            // The cost of going from start to start is zero.
            gScore[start] = 0.0f;
            durationMap[start] = 0.0f;
            waitingTimeMap[start] = 0.0f;

            // For the first node, that value is completely heuristic.
            fScore[start] = start.EstimatedDistance(goal);

            while (openSet.Count != 0)
            {
                IStop current = Lowest(openSet, fScore);
                if (current.IsGoalReached(goal))
                {
                    return ReconstructPath();
                }

                openSet.Remove(current);
                closedSet.Add(current);

                if (_router?.IsHub(current) ?? false)
                {
                    Debug.Log("using hub!");
                    var neighbor = _router.FindClosestHubInDirection(current.Location, goal.Location);
                    var path = _router.GetPath(current, neighbor);
                
                    if (path != null)
                    {
                        var duration = path.duration;
                        var tentative_gScore = GetScore(gScore, current) + (float)duration.TotalMinutes * options.travelTimeFactor;

                        cameFrom[neighbor] = current;
                        cameFromRoute[neighbor] = null;
                        cameFromHub[current] = path.path;

                        gScore[neighbor] = tentative_gScore;
                        durationMap[neighbor] = GetScore(durationMap, current) + (float)duration.TotalMinutes;
                        waitingTimeMap[neighbor] = 0;
                
                        fScore[neighbor] = tentative_gScore + neighbor.EstimatedDistance(goal);
                
                        if (!closedSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                    
                    continue;
                }

                foreach (IRoute route in current.Routes)
                {
                    IStop neighbor;
                    if (current == route.Begin)
                    {
                        neighbor = route.End;
                    }
                    else if (!route.OneWay)
                    {
                        Debug.Assert(current == route.End);
                        neighbor = route.Begin;
                    }
                    else
                    {
                        continue;
                    }

                    if (current == neighbor && !current.uTurnAllowed)
                    {
                        continue;
                    }

                    var tentative_gScore = GetScore(gScore, current)
                                           + (float)route.TravelTime.TotalMinutes * options.travelTimeFactor;

                    var durationUntilNow = GetScore(durationMap, current);
                    var duration = durationUntilNow + (float)route.TravelTime.TotalMinutes;

                    var waitingTime = 0f;
                    if (current.Kind == MapObjectKind.Stop)
                    {
                        waitingTime = GetScore(waitingTimeMap, current);

                        // If we need to change, calculate a penalty.
                        if (cameFromRoute.TryGetValue(current, out IRoute prevRoute))
                        {
                            if (route.AssociatedID != prevRoute.AssociatedID)
                            {
                                tentative_gScore += options.changingPenalty;
                            }
                        }

                        // Find the maximum waiting time.
                        if ((prevRoute == null || route.AssociatedID != prevRoute.AssociatedID))
                        {
                            var wait = (route as Route).line.schedule.dayInterval;

                            tentative_gScore += wait * options.waitingTimeFactor;
                            duration += wait;
                            waitingTime += wait;
                        }
                    }

                    if (tentative_gScore >= GetScore(gScore, neighbor))
                    {
                        continue;
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor] = current;
                    cameFromRoute[neighbor] = route;

                    gScore[neighbor] = tentative_gScore;
                    durationMap[neighbor] = duration;
                    waitingTimeMap[neighbor] = waitingTime;

                    fScore[neighbor] = tentative_gScore + neighbor.EstimatedDistance(goal);

                    if (!closedSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            return null;
        }

        static IStop GetNearestIntersection(Math.PointPosition pointPos, PointOnStreet pos, bool isGoal = false)
        {
            switch (pointPos)
            {
            case Math.PointPosition.Left:
                return isGoal ? pos.street.End : pos.street.Begin;
            default:
                return isGoal ? pos.street.Begin : pos.street.End;
            }
        }

        PathPlanningResult CreateWalk(DateTime time, Vector3 from, Vector3 to)
        {
            var walkStep = new WalkStep(from, to);
            return new PathPlanningResult(options, time, new PlannedPath(new PathStep[] { walkStep }));
        }

        public Tuple<Vector2, Math.PointPosition> GetPositionOnLane(PointOnStreet pointOnStreet, Vector2 loc)
        {
            var street = pointOnStreet.street;
            var closestPtAndPosFrom = street.GetClosestPointAndPosition(loc);
            var positions = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                street, (closestPtAndPosFrom.Item2 == Math.PointPosition.Right || street.OneWay)
                    ? street.RightmostLane 
                    : street.LeftmostLane);

            return StreetSegment.GetClosestPointAndPosition(loc, positions.Points);
        }

        IMapObject FindNearestParkingLot(Vector2 pos)
        {
            var found = GameController.instance.loadedMap.FindClosest(out NaturalFeature lot, pos, f =>
            {
                if (f.type != NaturalFeature.Type.Parking)
                    return false;

                return f.HasCapacity(OccupancyKind.ParkingCitizen);
            });

            Debug.Assert(found, "no parking lot found");
            return lot;
        }

        public PathPlanningResult FindClosestDrive(IStop from, IStop to)
        {
            this.start = from;
            this.goal = to;

            var steps = GetPath();
            return steps == null
                ? null
                : new PathPlanningResult(options, GameController.instance.sim.GameTime, new PlannedPath(steps));
        }

        public PathPlanningResult FindClosestDrive(Map map, Vector2 from, Vector2 to,
                                                   bool endAtParkingLot = false, DateTime? leaveBy = null)
        {
            var time = leaveBy ?? GameController.instance.sim.GameTime;
            var nearestPtFrom = map.GetClosestStreet(from, true, !options.useRivers);

            IMapObject nearestParking = null;
            if (endAtParkingLot)
            {
                nearestParking = FindNearestParkingLot(to);
            }

            var nearestPtTo = map.GetClosestStreet(endAtParkingLot ? nearestParking.VisualCenter : to, true, !options.useRivers);
            if (nearestPtFrom == null || nearestPtTo == null)
            {
                return !options.allowWalk ? null : CreateWalk(time, from, to);
            }

            var startSide = Math.GetPointPosition(from, nearestPtFrom);
            var endSide = Math.GetPointPosition(to, nearestPtTo);

            this.start = GetNearestIntersection(startSide, nearestPtFrom);
            this.goal = GetNearestIntersection(endSide, nearestPtTo, true);

            var startPosOnLane = GetPositionOnLane(nearestPtFrom, from);
            var endPosOnLane = GetPositionOnLane(nearestPtTo, to);

            var simpleJourney = false;
            if (nearestPtFrom.street == nearestPtTo.street)
            {
                var startDistance = (nearestPtFrom.street.drivablePositions.First() - nearestPtFrom.pos).sqrMagnitude;
                var endDistance = (nearestPtFrom.street.drivablePositions.First() - nearestPtTo.pos).sqrMagnitude;
                var backward = startDistance > endDistance;

                simpleJourney = !startDistance.Equals(endDistance) && startSide == endSide
                    && !(startSide == Math.PointPosition.Right && backward)
                    && !(startSide == Math.PointPosition.Left && !backward);
            }

            List<PathStep> steps;
            if (simpleJourney)
            {
                steps = new List<PathStep>();
            }
            else
            {
                steps = GetPath();

                if (steps == null)
                {
                    return !options.allowWalk ? null : CreateWalk(time, from, to);
                }
            }

            var startPos = startPosOnLane.Item1;
            var endPos = endPosOnLane.Item1;

            steps.Insert(0, new WalkStep(from, startPos));

            if (simpleJourney)
            {
                if (!startPos.Equals(endPos))
                {
                    steps.Insert(1, new PartialDriveStep(
                        startPos,
                        endPos,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.street,
                            backward = nearestPtFrom.street.Begin == start
                        }, true, true, nearestParking));
                }
            }
            else
            {
                if (!nearestPtFrom.pos.Equals(start.Location))
                {
                    steps.Insert(1, new PartialDriveStep(
                        startPos,
                        start.Location,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.street,
                            backward = nearestPtFrom.street.Begin == start
                        }, true, false));
                }

                steps.Add(new PartialDriveStep(
                        goal.Location,
                        endPos,
                        new DriveSegment
                        {
                            segment = nearestPtTo.street,
                            backward = nearestPtTo.street.End == goal,
                        }, false, true, nearestParking));
            }

            if (nearestParking is NaturalFeature f)
            {
                endPos = f.VisualCenter;
            }

            steps.Add(new WalkStep(endPos, to));
            return new PathPlanningResult(options, time, new PlannedPath(steps));
        }

        public PathPlanningResult FindFastestTransitRoute(Map map, Vector2 from, Vector2 to, DateTime? leaveBy = null)
        {
            var time = leaveBy ?? GameController.instance.sim.GameTime;

            var nearbyStopsFrom = map.GetStopsInRadius(from, options.maxWalkingDistance);
            var nearbyStopsTo = map.GetStopsInRadius(to, options.maxWalkingDistance);

            var minCost = float.PositiveInfinity;
            PathPlanningResult minPath = null;

            foreach (var fromStop in nearbyStopsFrom)
            {
                foreach (var toStop in nearbyStopsTo)
                {
                    if (fromStop == toStop)
                    {
                        continue;
                    }

                    start = fromStop;
                    goal = toStop;

                    var steps = GetPath();
                    if (steps != null)
                    {
                        steps.Insert(0, new WalkStep(from, fromStop.location));
                        steps.Add(new WalkStep(toStop.location, to));

                        var result = new PathPlanningResult(options, time, new PlannedPath(steps));
                        if (result.cost <= minCost)
                        {
                            minCost = result.cost;
                            minPath = result;
                        }
                    }
                }
            }

            return minPath;
        }

        public PathPlanningResult FindClosestPath(Map map, Vector2 from, Vector2 to, DateTime? leaveBy = null)
        {
            var time = leaveBy ?? GameController.instance.sim.GameTime;
            
            var distance = (from - to).magnitude;
            if (distance <= options.maxWalkingDistance)
            {
                return CreateWalk(time, from, to);
            }

            var transitResult = FindFastestTransitRoute(map, from, to, time);
            if (!options.allowCar)
            {
                if (transitResult == null)
                {
                    return options.allowWalk ? CreateWalk(time, from, to) : null;
                }

                return transitResult;
            }

            var carResult = FindClosestDrive(map, from, to, true, time);
            if (carResult == null)
            {
                return transitResult;
            }

            return (transitResult == null || carResult.cost < transitResult.cost) ? carResult : transitResult;
        }
    }
}