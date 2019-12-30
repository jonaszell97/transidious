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

            float speed;
            if (route != null)
            {
                speed = route.AverageSpeed;
            }
            else
            {
                speed = 50.0f;
            }

            float dist = (float)System.Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            float kmPerMinute = speed / 60f;
            float distInKm = dist / 1000f;

            return kmPerMinute * distInKm;
        }
    }

    public interface IRoute
    {
        IStop Begin { get; }
        IStop End { get; }
        bool OneWay { get; }

        TimeSpan TravelTime { get; }
        float AverageSpeed { get; }
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
            PublicTransit,
        }

        public DateTime time;
        public Type type { get; }

        protected PathStep(Type type)
        {
            this.type = type;
            this.time = time;
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
                    partialDriveStep.PartialEnd);
            }

            if (pathStep.Details.TryUnpack(out Serialization.PathStep.Types.PublicTransitStep transitStep))
            {
                return new PublicTransitStep(
                    map.GetMapObject<Line>((int)transitStep.LineID),
                    transitStep.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToArray());
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

        private static readonly float avgSpeedKMH = 7f;
        private static readonly float avgSpeedMPS = avgSpeedKMH / 3.6f;

        public WalkStep(Vector2 from, Vector2 to) : base(Type.Walk)
        {
            this.from = from;
            this.to = to;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var distMeters = (from - to).magnitude;
            var seconds = distMeters * avgSpeedMPS;

            return TimeSpan.FromSeconds(seconds);
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

        public PartialDriveStep(Vector2 startPos, Vector2 endPos,
                                DriveSegment segment,
                                bool partialStart, bool partialEnd)
            : base(Type.PartialDrive)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.driveSegment = segment;
            this.partialStart = partialStart;
            this.partialEnd = partialEnd;
        }

        public override TimeSpan EstimateDuration(PathPlanningOptions options)
        {
            var seg = driveSegment.segment;

            var totalLength = seg.length;
            if (partialStart)
            {
                totalLength -= seg.GetDistanceFromStartStopLine(startPos);
            }
            if (partialEnd)
            {
                totalLength -= seg.GetDistanceFromEndStopLine(startPos);
            }

            return seg.GetTravelTime(totalLength);
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

        public Vector3 Location
        {
            get
            {
                return pos;
            }
        }

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

        public bool uTurnAllowed
        {
            get
            {
                return false;
            }
        }
    }

    public class PathPlanningOptions
    {
        /// Whether or not a car can be used.
        public bool allowCar = true;

        /// Whether or not to allow walking.
        public bool allowWalk = true;

        /// The starting position.
        public IStop start;

        /// The goal position.
        public IStop goal;

        /// The time of the trip.
        public DateTime time;

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
    }

    public class PathPlanningResult
    {
        /// The total cost of the path.
        public readonly float cost = 0.0f;

        /// The total duration of the path in minutes.
        public readonly float duration = 0.0f;

        /// The time you have to leave when taking this path.
        public readonly DateTime leaveBy;

        /// The time you will arrive when taking this path.
        public DateTime arriveAt;

        /// The steps to take on this path.
        public readonly List<PathStep> steps;

        public PathPlanningResult(float cost, float duration, DateTime leaveBy,
                                  DateTime? arriveAt, List<PathStep> steps)
        {
            this.cost = cost;
            this.duration = duration;
            this.leaveBy = leaveBy;
            this.arriveAt = arriveAt.HasValue ? arriveAt.Value : new DateTime();
            this.steps = steps;
        }

        public PathPlanningResult(float cost, float duration, List<PathStep> steps)
        {
            this.cost = cost;
            this.duration = duration;
            this.leaveBy = DateTime.Now;
            this.arriveAt = this.leaveBy.AddMinutes(duration);
            this.steps = steps;
        }

        public PointOnStreet GetStartPointOnStreet()
        {
            if (steps.Count < 2 || !(steps[1] is PartialDriveStep))
                return null;

            var drive = steps[1] as PartialDriveStep;
            return new PointOnStreet { street = drive.driveSegment.segment, pos = drive.startPos };
        }

        public PointOnStreet GetEndPointOnStreet()
        {
            if (steps.Count < 2 || !(steps[steps.Count - 2] is PartialDriveStep))
                return null;

            var drive = steps[steps.Count - 2] as PartialDriveStep;
            return new PointOnStreet { street = drive.driveSegment.segment, pos = drive.endPos };
        }

        public void RecalculateTimes(PathPlanningOptions options)
        {
            var time = this.leaveBy;
            foreach (var step in steps)
            {
                step.time = time;
                time = time.Add(step.EstimateDuration(options));
            }

            arriveAt = time;
        }

        public bool IsWalk
        {
            get
            {
                return steps.Count == 2 && steps.All(s => s is WalkStep);
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
            string s = "[" + leaveBy.ToShortTimeString() + "] leave\n";

            int i = 0;
            foreach (PathStep step in steps)
            {
                if (i++ != 0) s += "\n";
                s += "[" + step.time.ToShortTimeString() + "] ";

                if (step is WalkStep)
                {
                    s += "walk";
                }
                else if (step is PublicTransitStep)
                {
                    var travelStep = step as PublicTransitStep;
                    s += "travel line " + travelStep.line.name + " until "
                       + travelStep.routes[travelStep.routes.Length - 1].endStop.name;
                }
                else if (step is DriveStep)
                {
                    var driveStep = step as DriveStep;
                    s += "drive on " + driveStep.driveSegment.segment.name;
                }
                else if (step is PartialDriveStep)
                {
                    var driveStep = step as PartialDriveStep;
                    s += "drive partially on " + driveStep.driveSegment.segment.name;
                }
            }

            if (i++ != 0) s += "\n";
            s += "[" + arriveAt.ToShortTimeString() + "] arrival\n";

            return s;
        }

        public Serialization.PathPlanningResult ToProtobuf()
        {
            var result = new Serialization.PathPlanningResult
            {
                Cost = cost,
                Duration = duration,
                LeaveBy = (ulong)leaveBy.Ticks,
                ArriveAt = (ulong)arriveAt.Ticks,
            };

            result.Steps.AddRange(steps.Select(s => s.ToProtobuf()));
            return result;
        }

        public static PathPlanningResult Deserialize(Map map, Serialization.PathPlanningResult p)
        {
            return new PathPlanningResult(p.Cost, p.Duration,
                new DateTime((long)p.LeaveBy), new DateTime((long)p.ArriveAt),
                p.Steps.Select(s => PathStep.Deserialize(map, s)).ToList());
        }

        public void DebugDraw(MultiMesh multiMesh)
        {
            // var positions = new List<Vector3>();
            // var currentColor = Color.clear;

            // foreach (var step in steps)
            // {
            //     Color c = Color.black;
            //     if (step is WalkStep)
            //     {
            //         c = Color.green;
            //     }
            //     else if (step is DriveStep)
            //     {
            //         c = Color.red;
            //     }
            //     else if (step is PartialDriveStep)
            //     {
            //         c = new Color(0.6f, 0f, 0f, 1f);
            //     }

            //     if (currentColor != Color.clear && currentColor != c)
            //     {
            //         var mesh = MeshBuilder.CreateSmoothLine(positions, 0.001f, 10, -14f);
            //         multiMesh.AddMesh(currentColor, mesh, -14f);

            //         positions.Clear();
            //     }

            //     currentColor = c;

            //     if (step is WalkStep)
            //     {
            //         var walk = step as WalkStep;
            //         positions.Add(new Vector3(walk.from.x, walk.from.y, -14f));
            //         positions.Add(new Vector3(walk.to.x, walk.to.y, -14f));
            //     }
            //     else if (step is DriveStep)
            //     {
            //         var drive = step as DriveStep;
            //         var lanes = drive.driveSegment.segment.street.lanes;
            //         var offset = drive.driveSegment.segment.GetStreetWidth(RenderingDistance.Near) / lanes;
            //         var segPositions = drive.driveSegment.segment.positions.ToArray();

            //         if (drive.driveSegment.backward)
            //             segPositions = segPositions.Reverse().ToArray();

            //         for (int i = 1; i < segPositions.Length; i += 2)
            //         {
            //             var p0 = segPositions[i - 1];
            //             var p1 = segPositions[i];

            //             var dir = p1 - p0;
            //             var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            //             var perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            //             p0 = p0 + (perpendicular * offset);
            //             p1 = p1 + (perpendicular * offset);

            //             positions.Add(new Vector3(p0.x, p0.y, -14f));
            //             positions.Add(new Vector3(p1.x, p1.y, -14f));
            //         }
            //     }
            //     else if (step is PartialDriveStep)
            //     {
            //         var drive = step as PartialDriveStep;
            //         var lanes = drive.driveSegment.segment.street.lanes;
            //         var offset = drive.driveSegment.segment.GetStreetWidth(RenderingDistance.Near) / lanes;

            //         for (int i = 1; i < drive.positions.Length; i += 2)
            //         {
            //             var p0 = drive.positions[i - 1];
            //             var p1 = drive.positions[i];

            //             var dir = p1 - p0;
            //             var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            //             var perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            //             p0 = p0 + (perpendicular * offset);
            //             p1 = p1 + (perpendicular * offset);

            //             positions.Add(new Vector3(p0.x, p0.y, -14f));
            //             positions.Add(new Vector3(p1.x, p1.y, -14f));
            //         }
            //     }
            // }

            // if (positions.Count != 0)
            // {
            //     var mesh = MeshBuilder.CreateSmoothLine(positions, 0.001f, 10, -14f);
            //     multiMesh.AddMesh(currentColor, mesh, -14f);
            // }
        }
    }

    public class PathPlanner
    {
        // The path planning options.
        PathPlanningOptions options;

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

        public PathPlanner(PathPlanningOptions options)
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
        }

        static float EstimateWalkingDuration(Vector3 from, Vector3 to)
        {
            from.z = 0f;
            to.z = 0f;

            return (from - to).magnitude * 7f;
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
            var current = options.goal;
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
                    if (!currentLine)
                    {
                        currentLine = transitRoute.line;
                    }

                    routes.Add(new Tuple<IRoute, bool>(route, backward));

                    if (currentLine != transitRoute.line || i == 0)
                    {
                        steps.Add(new PublicTransitStep(currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
                        currentLine = transitRoute.line;
                        
                        routes.Clear();
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

                cost += (float)route.TravelTime.TotalMinutes * options.travelTimeFactor * preferenceMultiplier;

                Debug.Assert(route != null, "No route found!");
                previous = stop;
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

            cost += GetScore(waitingTimeMap, options.goal) * options.waitingTimeFactor;

            var result = new PathPlanningResult(
                cost,
                GetScore(durationMap, options.goal),
                options.time,
                null,
                steps
            );

            result.RecalculateTimes(options);
            return result;
        }

        public PathPlanningResult GetPath()
        {
            IStop start = options.start;
            IStop goal = options.goal;

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

                    float duration = GetScore(durationMap, current)
                       + (float)route.TravelTime.TotalMinutes;

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
                        DateTime arrival = options.time.AddMinutes(GetScore(durationMap, current));
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

            var result = new PathPlanningResult(
                duration * options.walkingTimeFactor, duration,
                new List<PathStep> { walkStep });

            result.RecalculateTimes(options);
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

        public PathPlanningResult FindClosestDrive(Map map, Vector2 from, Vector2 to)
        {
            var nearestPtFrom = map.GetClosestStreet(from);
            var nearestPtTo = map.GetClosestStreet(to);

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

            var options = new PathPlanningOptions
            {
                start = GetNearestIntersection(startSide, nearestPtFrom),
                goal = GetNearestIntersection(endSide, nearestPtTo, true),
            };

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
                result = new PathPlanningResult(0f, 0f, options.time, options.time, new List<PathStep>());
            }
            else
            {
                var planner = new PathPlanner(options);
                result = planner.GetPath();

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

            var time = options.time;
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
                            backward = nearestPtFrom.seg.Begin == options.start
                        }, true, true));
                }
            }
            else
            {
                if (!nearestPtFrom.pos.Equals(options.start.Location))
                {
                    result.steps.Insert(1, new PartialDriveStep(
                        startPos,
                        options.start.Location,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.seg,
                            backward = nearestPtFrom.seg.Begin == options.start
                        }, true, false));
                }

                result.steps.Add(new PartialDriveStep(
                        options.goal.Location,
                        endPos,
                        new DriveSegment
                        {
                            segment = nearestPtTo.seg,
                            backward = nearestPtTo.seg.End == options.goal,
                        }, false, true));
            }

            result.steps.Add(new WalkStep(endPos, to));
            result.RecalculateTimes(options);

            return result;
        }

        public PathPlanningResult FindFastestTransitRoute(Map map, Vector2 from, Vector2 to, int tries = 3)
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

                    options.start = fromStop;
                    options.goal = toStop;

                    var planner = new PathPlanner(options);
                    var path = planner.GetPath();
                    
                    if (path != null)
                    {
                        path.steps.Insert(0, new WalkStep(from, fromStop.location));
                        path.steps.Add(new WalkStep(toStop.location, to));
                        path.RecalculateTimes(options);

                        return path;
                    }
                }
            }

            if (tries != 0)
            {
                options.maxWalkingDistance *= 2;
                return FindFastestTransitRoute(map, from, to, tries - 1);
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