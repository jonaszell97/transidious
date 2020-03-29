using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious.PathPlanning
{
    public interface IStop
    {
        IEnumerable<IRoute> Routes { get; }
        Vector3 Location { get; }
        bool uTurnAllowed { get; }

        bool IsGoalReached(IStop goal);
    }

    public static class IStopExtensions
    {
        public static float EstimatedDistance(this IStop start, IStop goal, IRoute route = null)
        {
            float x1 = start.Location.x;
            float y1 = start.Location.y;

            float x2 = goal.Location.x;
            float y2 = goal.Location.y;

            Velocity speed;
            if (route != null)
            {
                speed = route.AverageSpeed;
            }
            else
            {
                speed = Velocity.FromRealTimeKPH(50);
            }

            return (float)(Distance.Between(start.Location, goal.Location) / speed).TotalSeconds;
        }
    }

    public interface IRoute
    {
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

        public DateTime time;
        public Type type { get; }

        protected PathStep(Type type)
        {
            this.type = type;
            this.time = default;
        }

        public abstract TimeSpan EstimateDuration(PathPlanningOptions options);

        protected abstract Google.Protobuf.IMessage ToProtobufInternal();

        public Serialization.PathStep ToProtobuf()
        {
            var result = new Serialization.PathStep
            {
                Kind = (Serialization.PathStep.Types.PathStepKind)type,
                Timestamp = (ulong)time.Ticks,
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

                Debug.LogError("total length < 0");
                totalLength = seg.length;
            }

            return seg.GetTravelTime(Distance.FromMeters(totalLength));
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
            // FIXME
            return TimeSpan.Zero;
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
        public StreetSegment street;
        public Vector3 pos;

        public IEnumerable<IRoute> Routes
        {
            get
            {
                var routes = new List<IRoute>();
                routes.AddRange(street.End.Routes);

                if (!street.street.isOneWay)
                    routes.AddRange(street.Begin.Routes);

                return routes;
            }
        }

        public Vector3 Location => pos;

        public bool IsGoalReached(IStop goal)
        {
            if (goal is StreetIntersection)
            {
                var intersection = goal as StreetIntersection;
                foreach (var s in intersection.intersectingStreets)
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

    public class PathPlanningResult
    {
        /// The path planning options used to obtain the path.
        public readonly PathPlanningOptions options;

        /// The total cost of the path.
        public readonly float cost = 0.0f;

        /// The time you have to leave when taking this path.
        public readonly DateTime leaveBy;

        /// The time you will arrive when taking this path.
        public DateTime arriveAt;

        /// The steps to take on this path.
        public readonly List<PathStep> steps;

        /// The total duration of the path in minutes.
        public float duration => (float)(arriveAt - leaveBy).TotalMinutes;
        
        public PathPlanningResult(PathPlanningOptions options,
                                  float cost, DateTime leaveBy,
                                  DateTime? arriveAt, List<PathStep> steps)
        {
            this.options = options;
            this.cost = cost;
            this.leaveBy = leaveBy;
            this.arriveAt = arriveAt.HasValue ? arriveAt.Value : new DateTime();
            this.steps = steps;
        }

        public PathPlanningResult(PathPlanningOptions options, float cost, List<PathStep> steps)
        {
            this.options = options;
            this.cost = cost;
            this.leaveBy = DateTime.Now;
            this.steps = steps;
        }

        public void RecalculateTimes()
        {
            var time = leaveBy;
            DriveStep previousDrive = null;
            PartialDriveStep previousPartialDrive = null;

            for (var i = 0; i < steps.Count; ++i)
            {
                var step = steps[i];
                switch (step.type)
                {
                    case PathStep.Type.PublicTransit:
                    {
                        var travelStep = step as PublicTransitStep;
                        var departure = travelStep.routes[0].NextDeparture(time);
                        if (departure > time)
                        {
                            if (i == 0 || !(steps[i - 1] is WaitStep))
                            {
                                var waitStep = new WaitStep(departure - time);
                                waitStep.time = time;

                                steps.Insert(i, waitStep);
                                i += 1;
                            }

                            time = departure;
                        }

                        break;
                    }
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
                        steps.Insert(i, turnStep);
                        i += 1;
                         
                        break;
                    }
                    default:
                        break;
                }

                step.time = time;
                time = time.Add(step.EstimateDuration(options));

                previousDrive = step as DriveStep;
                previousPartialDrive = step as PartialDriveStep;
            }

            arriveAt = time;
        }

        public bool IsWalk
        {
            get
            {
                return steps.All(s => s is WalkStep);
            }
        }

        public bool UsesPublicTransit
        {
            get
            {
                return steps.Any(step => step.type == PathStep.Type.PublicTransit);
            }
        }

        public bool ValidForTram
        {
            get
            {
                foreach (var step in steps)
                {
                    if (step is DriveStep
                    && !(step as DriveStep).driveSegment.segment.hasTramTracks)
                    {
                        return false;
                    }
                    else if (step is PartialDriveStep
                    && !(step as PartialDriveStep).driveSegment.segment.hasTramTracks)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override string ToString()
        {
            string s = "Cost: " + cost + ", duration: " + duration + "min\n";
            s += "[" + leaveBy.ToLongTimeString() + "] leave\n";

            var i = 0;
            var time = leaveBy;

            foreach (PathStep step in steps)
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
                    s += $"wait for line {(steps[i] as PublicTransitStep).line.name}";
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
            };

            result.Steps.AddRange(steps.Select(s => s.ToProtobuf()));
            return result;
        }

        public static PathPlanningResult Deserialize(Map map, Serialization.PathPlanningResult p)
        {
            return new PathPlanningResult(PathPlanningOptions.Deserialize(p.Options),
                p.Cost,
                new DateTime((long)p.LeaveBy), new DateTime((long)p.ArriveAt),
                p.Steps.Select(s => PathStep.Deserialize(map, s)).ToList());
        }

        public void DebugDraw()
        {
            var positions = new List<Vector3>();
            foreach (var step in steps)
            {
                var c = Color.black;
                var width = 1f;

                if (step is WalkStep)
                {
                    var walk = step as WalkStep;
                    positions.Add(new Vector3(walk.from.x, walk.from.y, -14f));
                    positions.Add(new Vector3(walk.to.x, walk.to.y, -14f));
                    c = Color.green;
                }
                else if (step is PublicTransitStep transitStep)
                {
                    c = transitStep.line.color;
                    width *= 1.5f;

                    foreach (var route in transitStep.routes)
                    {
                        positions.AddRange(route.positions);
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
                    Utility.DrawLine(positions.ToArray(), width, c);
                    positions.Clear();
                }
            }
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

        /// The time of the trip.
        DateTime time;

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

        // For each node, the duration of getting from the start node to that node.
        Dictionary<IStop, float> durationMap;

        // For each node, the waiting time at that node.
        Dictionary<IStop, float> waitingTimeMap;

        // For each node, the cost of getting from the start node to that node.
        Dictionary<IStop, float> gScore;

        // For each node, the total cost of getting from the start node to the goal
        // by passing by that node. That value is partly known, partly heuristic.
        Dictionary<IStop, float> fScore;

        public PathPlanner(PathPlanningOptions options, DateTime? time = null)
        {
            this.options = options;
            this.closedSet = new HashSet<IStop>();
            this.openSet = new HashSet<IStop>();
            this.cameFrom = new Dictionary<IStop, IStop>();
            this.cameFromRoute = new Dictionary<IStop, IRoute>();
            this.durationMap = new Dictionary<IStop, float>();
            this.waitingTimeMap = new Dictionary<IStop, float>();
            this.gScore = new Dictionary<IStop, float>();
            this.fScore = new Dictionary<IStop, float>();
            this.time = time ?? GameController.instance.sim.GameTime;
        }

        void Reset()
        {
            closedSet.Clear();
            openSet.Clear();
            cameFrom.Clear();
            cameFromRoute.Clear();
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

        PathPlanningResult ReconstructPath()
        {
            var current = this.goal;
            var stops = new List<IStop> { current };

            while (cameFrom.TryGetValue(current, out IStop prev))
            {
                stops.Add(prev);
                current = prev;
            }

            Line currentLine = null;
            var previous = stops[stops.Count - 1];
            var routes = new List<Tuple<IRoute, bool>>();
            var steps = new List<PathStep>();
            var cost = 0f;
            var firstTransitStop = true;

            for (int i = stops.Count - 2; i >= 0; --i)
            {
                IStop stop = stops[i];
                IRoute route = null;
                bool backward = false;

                foreach (IRoute r in previous.Routes)
                {
                    if (r.End == stop || r.Begin == stop)
                    {
                        route = r;
                        backward = r.Begin == stop;
                        break;
                    }
                }

                var preferenceMultiplier = 1f;
                if (stop is Stop)
                {
                    if (!firstTransitStop)
                    {
                        cost += options.changingPenalty;
                    }

                    firstTransitStop = false;

                    var transitRoute = route as Route;
                    if (currentLine == null || currentLine == transitRoute.line)
                    {
                        currentLine = transitRoute.line;
                        routes.Add(new Tuple<IRoute, bool>(route, backward));
                    }
                    else
                    {
                        steps.Add(new PublicTransitStep(currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
                        currentLine = transitRoute.line;

                        routes.Clear();
                        routes.Add(new Tuple<IRoute, bool>(route, backward));
                    }
                }
                else if (stop is StreetIntersection)
                {
                    steps.Add(new DriveStep(new DriveSegment
                    {
                        segment = route as StreetSegment,
                        backward = backward,
                    }));

                    preferenceMultiplier = options.carTimeFactor;
                }

                cost += (float)route.TravelTime.TotalSeconds * options.travelTimeFactor * preferenceMultiplier;

                Debug.Assert(route != null, "No route found!");
                previous = stop;
            }

            if (routes.Count > 0)
            {
                steps.Add(new PublicTransitStep(currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
            }

            //if (routes.Count != 0)
            //{
            //    var route = routes.First().Item1 as Route;
            //    if (route != null)
            //    {
            //        steps.Add(new PublicTransitStep(time, currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
            //        cost += route.TravelTime * options.travelTimeFactor;
            //    }
            //    else
            //    {
            //        foreach (var r in routes)
            //        {
            //            steps.Add(new DriveStep(new DriveSegment
            //            {
            //                segment = r.Item1 as StreetSegment,
            //                backward = r.Item2,
            //            }));

            //            cost += r.Item1.TravelTime * options.travelTimeFactor * options.carTimeFactor;
            //        }
            //    }
            //}

            cost += GetScore(waitingTimeMap, this.goal) * options.waitingTimeFactor;

            var result = new PathPlanningResult(
                options,
                cost,
                this.time,
                null,
                steps
            );

            result.RecalculateTimes();
            return result;
        }

        public PathPlanningResult GetPath()
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
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    float tentative_gScore = GetScore(gScore, current)
                       + (float)route.TravelTime.TotalMinutes * options.travelTimeFactor;

                    float durationUntilNow = GetScore(durationMap, current);
                    float duration = durationUntilNow + (float)route.TravelTime.TotalMinutes;

                    float waitingTime = GetScore(waitingTimeMap, current);

                    // If we need to change, calculate a penalty.
                    if (cameFromRoute.TryGetValue(current, out IRoute prevRoute))
                    {
                        if (route.AssociatedID != prevRoute.AssociatedID)
                        {
                            tentative_gScore += options.changingPenalty;
                        }
                    }

                    // Find the next departure of this line at the station.
                    if ((prevRoute == null || route.AssociatedID != prevRoute.AssociatedID))
                    {
                        var arrival = this.time.AddMinutes(durationUntilNow);
                        var nextDep = route.NextDeparture(arrival);
                        var wait = (nextDep - arrival).Minutes;

                        tentative_gScore += wait * options.waitingTimeFactor;
                        duration += wait;
                        waitingTime += wait;
                    }

                    if (!openSet.Add(neighbor))
                    {
                        if (tentative_gScore >= GetScore(gScore, neighbor))
                        {
                            continue;
                        }
                    }

                    // This path is the best until now. Record it!
                    cameFrom[neighbor] = current;
                    cameFromRoute[neighbor] = route;

                    gScore[neighbor] = tentative_gScore;
                    durationMap[neighbor] = duration;
                    waitingTimeMap[neighbor] = waitingTime;

                    fScore[neighbor] = gScore[neighbor]
                       + neighbor.EstimatedDistance(goal, route);
                }
            }

            return null;
        }

        static IStop GetNearestIntersection(Math.PointPosition pointPos, Map.PointOnStreet pos, bool isGoal = false)
        {
            switch (pointPos)
            {
            case Math.PointPosition.Left:
                return isGoal ? pos.seg.End : pos.seg.Begin;
            default:
                return isGoal ? pos.seg.Begin : pos.seg.End;
            }
        }

        PathPlanningResult CreateWalk(Vector3 from, Vector3 to)
        {
            var walkStep = new WalkStep(from, to);
            var duration = (float)walkStep.EstimateDuration(options).TotalSeconds;

            var result = new PathPlanningResult(options,
                duration * options.walkingTimeFactor,
                new List<PathStep> { walkStep });

            result.RecalculateTimes();
            return result;
        }

        public Tuple<Vector3, Math.PointPosition> GetPositionOnLane(Map.PointOnStreet pointOnStreet, Vector2 loc)
        {
            var street = pointOnStreet.seg;
            var closestPtAndPosFrom = street.GetClosestPointAndPosition(loc);
            var positions = GameController.instance.sim.trafficSim.GetPath(
                street, closestPtAndPosFrom.Item2 == Math.PointPosition.Right
                    ? street.RightmostLane 
                    : street.LeftmostLane);

            return StreetSegment.GetClosestPointAndPosition(loc, positions);
        }

        IMapObject FindNearestParkingLot(Vector2 pos)
        {
            var found = GameController.instance.loadedMap.FindClosest(out NaturalFeature lot, pos, f =>
            {
                if (f.type != NaturalFeature.Type.Parking)
                    return false;

                if (f.visitors == f.capacity)
                    return false;

                return true;
            });

            Debug.Assert(found, "no parking lot found");
            return lot;
        }
        
        public PathPlanningResult FindClosestDrive(Map map, Vector2 from, Vector2 to, bool endAtParkingLot = false)
        {
            var nearestPtFrom = map.GetClosestStreet(from);

            IMapObject nearestParking = null;
            if (endAtParkingLot)
            {
                nearestParking = FindNearestParkingLot(to);
            }

            var nearestPtTo = map.GetClosestStreet(endAtParkingLot ? nearestParking.Centroid : to);
            if (nearestPtFrom == null || nearestPtTo == null)
            {
                if (!this.options.allowWalk)
                {
                    return null;
                }

                return CreateWalk(from, to);
            }

            var startSide = Math.GetPointPosition(from, nearestPtFrom);
            var endSide = Math.GetPointPosition(to, nearestPtTo);

            this.start = GetNearestIntersection(startSide, nearestPtFrom);
            this.goal = GetNearestIntersection(endSide, nearestPtTo, true);

            var startPosOnLane = GetPositionOnLane(nearestPtFrom, from);
            var endPosOnLane = GetPositionOnLane(nearestPtTo, to);

            var simpleJourney = false;
            if (nearestPtFrom.seg == nearestPtTo.seg)
            {
                var startDistance = (nearestPtFrom.seg.drivablePositions.First() - nearestPtFrom.pos).sqrMagnitude;
                var endDistance = (nearestPtFrom.seg.drivablePositions.First() - nearestPtTo.pos).sqrMagnitude;
                var backward = startDistance > endDistance;

                simpleJourney = !startDistance.Equals(endDistance) && startSide == endSide
                    && !(startSide == Math.PointPosition.Right && backward)
                    && !(startSide == Math.PointPosition.Left && !backward);
            }

            PathPlanningResult result;
            if (simpleJourney)
            {
                result = new PathPlanningResult(options, 0f, this.time, null, new List<PathStep>());
            }
            else
            {
                result = GetPath();

                if (result == null)
                {
                    if (!this.options.allowWalk)
                    {
                        return null;
                    }

                    return CreateWalk(from, to);
                }
            }

            var startPos = startPosOnLane.Item1;
            var endPos = endPosOnLane.Item1;

            var time = this.time;
            result.steps.Insert(0, new WalkStep(from, startPos));

            time = time.Add(result.steps.First().EstimateDuration(options));

            if (simpleJourney)
            {
                if (!startPos.Equals(endPos))
                {
                    result.steps.Insert(1, new PartialDriveStep(
                        startPos,
                        endPos,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.seg,
                            backward = nearestPtFrom.seg.Begin == start
                        }, true, true, nearestParking));
                }
            }
            else
            {
                if (!nearestPtFrom.pos.Equals(start.Location))
                {
                    result.steps.Insert(1, new PartialDriveStep(
                        startPos,
                        start.Location,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.seg,
                            backward = nearestPtFrom.seg.Begin == start
                        }, true, false));
                }

                result.steps.Add(new PartialDriveStep(
                        goal.Location,
                        endPos,
                        new DriveSegment
                        {
                            segment = nearestPtTo.seg,
                            backward = nearestPtTo.seg.End == goal,
                        }, false, true, nearestParking));
            }

            if (nearestParking is NaturalFeature f)
            {
                endPos = f.centroid;
            }

            result.steps.Add(new WalkStep(endPos, to));
            result.RecalculateTimes();

            return result;
        }

        public PathPlanningResult FindFastestTransitRoute(Map map, Vector2 from, Vector2 to)
        {
            var nearbyStopsFrom = map.GetMapObjectsInRadius<Stop>(from, options.maxWalkingDistance);
            var nearbyStopsTo = map.GetMapObjectsInRadius<Stop>(to, options.maxWalkingDistance);

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

                    var path = GetPath();
                    if (path != null)
                    {
                        path.steps.Insert(0, new WalkStep(from, fromStop.location));
                        path.steps.Add(new WalkStep(toStop.location, to));
                        path.RecalculateTimes();

                        return path;
                    }
                }
            }

            return CreateWalk(from, to);
        }

        public PathPlanningResult FindClosestPath(Map map, Vector2 from, Vector2 to)
        {
            var distance = (from - to).magnitude;
            if (distance <= options.maxWalkingDistance)
            {
                return CreateWalk(from, to);
            }

            var transitResult = FindFastestTransitRoute(map, from, to);
            if (!options.allowCar)
            {
                return transitResult;
            }

            var carResult = FindClosestDrive(map, from, to);
            if (carResult == null)
            {
                return transitResult;
            }

            Debug.LogWarning($"car: {carResult.cost} <-> transit: {transitResult.cost}");

            if (carResult.cost < transitResult.cost)
            {
                return carResult;
            }

            return transitResult;
        }
    }
}