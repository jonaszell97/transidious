﻿using UnityEngine;
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

        float TravelTime { get; }
        float AverageSpeed { get; }
        int AssociatedID { get; }

        DateTime NextDeparture(DateTime after);
    }

    public class PathStep
    {
        public DateTime time;
    }

    public class WalkStep : PathStep
    {
        /// The starting point of the walk.
        public Vector3 from;

        /// The ending point of the walk.
        public Vector3 to;

        public WalkStep(Vector3 from, Vector3 to)
        {
            this.from = from;
            this.to = to;
        }
    }

    public class PublicTransitStep : PathStep
    {
        /// The line to travel with.
        public Line line;

        /// The routes to follow.
        public Route[] routes;

        public PublicTransitStep(DateTime time, Line line, Route[] routes)
        {
            this.time = time;
            this.line = line;
            this.routes = routes;
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

        public DriveStep(DriveSegment driveSegment)
        {
            this.driveSegment = driveSegment;
        }
    }

    public class PartialDriveStep : PathStep
    {
        /// The starting point of the drive.
        public Vector3 startPos;

        /// The end point of the drive.
        public Vector3 endPos;

        /// The street segments to follow.
        public DriveSegment driveSegment;

        /// This step is partial at the start.
        public bool partialStart;

        /// This step is partial at the end.
        public bool partialEnd;

        public PartialDriveStep(Vector3 startPos, Vector3 endPos, DriveSegment segment,
                                bool partialStart, bool partialEnd)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.driveSegment = segment;
            this.partialStart = partialStart;
            this.partialEnd = partialEnd;
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
        /// Whether or not to allow walking.
        public bool allowWalk = true;

        /// The starting position.
        public IStop start;

        /// The goal position.
        public IStop goal;

        /// The time of the trip.
        public DateTime time;

        /// Factor that walking time is multiplied with for scoring.
        public float walkingTimeFactor = 3.0f;

        /// Factor that traveling time is multiplied with for scoring.
        public float travelTimeFactor = 1.0f;

        /// Factor that waiting time is multiplied with for scoring.
        public float waitingTimeFactor = 2.0f;

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
        public readonly DateTime arriveAt;

        /// The steps to take on this path.
        public readonly List<PathStep> steps;

        public PathPlanningResult(float cost, float duration, DateTime leaveBy,
                                  DateTime arriveAt, List<PathStep> steps)
        {
            this.cost = cost;
            this.duration = duration;
            this.leaveBy = leaveBy;
            this.arriveAt = arriveAt;
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

        public bool IsWalk
        {
            get
            {
                return steps.Count == 2 && steps.All(s => s is WalkStep);
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
                    s += "drive";
                }
            }

            if (i++ != 0) s += "\n";
            s += "[" + arriveAt.ToShortTimeString() + "] arrival\n";

            return s;
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
            //         var offset = drive.driveSegment.segment.GetStreetWidth(InputController.RenderingDistance.Near) / lanes;
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
            //         var offset = drive.driveSegment.segment.GetStreetWidth(InputController.RenderingDistance.Near) / lanes;

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
            IStop current = options.goal;
            List<IStop> stops = new List<IStop> { current };

            while (cameFrom.TryGetValue(current, out IStop prev))
            {
                stops.Add(prev);
                current = prev;
            }

            DateTime time = options.time;
            Line currentLine = null;
            IStop previous = stops[stops.Count - 1];
            List<Tuple<IRoute, bool>> routes = new List<Tuple<IRoute, bool>>();
            List<PathStep> steps = new List<PathStep>();

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

                if (stop is Stop)
                {
                    var transitRoute = route as Route;
                    if (!currentLine)
                    {
                        currentLine = transitRoute.line;
                    }
                    else if (currentLine != transitRoute.line)
                    {
                        steps.Add(new PublicTransitStep(time, currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
                        routes.Clear();
                    }
                }

                time = time.AddMinutes(route.TravelTime);

                Debug.Assert(route != null, "No route found!");
                routes.Add(new Tuple<IRoute, bool>(route, backward));

                previous = stop;
            }

            if (routes.Count != 0)
            {
                if (routes.First().Item1 is Route)
                {
                    steps.Add(new PublicTransitStep(time, currentLine, routes.Select(r => r.Item1 as Route).ToArray()));
                }
                else
                {
                    foreach (var r in routes)
                    {
                        steps.Add(new DriveStep(new DriveSegment
                        {
                            segment = r.Item1 as StreetSegment,
                            backward = r.Item2,
                        }));
                    }
                }
            }

            return new PathPlanningResult(
                GetScore(gScore, options.goal),
                GetScore(durationMap, options.goal),
                options.time,
                time,
                steps
            );
        }

        public PathPlanningResult GetPath()
        {
            IStop start = options.start;
            IStop goal = options.goal;

            openSet.Add(start);

            // The cost of going from start to start is zero.
            gScore[start] = 0.0f;
            durationMap[start] = 0.0f;

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
                       + route.TravelTime * options.travelTimeFactor;

                    float duration = GetScore(durationMap, current)
                       + route.TravelTime;

                    // If we need to change, calculate a penalty.
                    if (cameFromRoute.TryGetValue(current, out IRoute prevRoute))
                    {
                        if (route.AssociatedID != prevRoute.AssociatedID)
                        {
                            tentative_gScore += options.changingPenalty;
                        }
                    }

                    // Find the next departure of this line at the station.
                    if (options.time != null && (prevRoute == null || route.AssociatedID != prevRoute.AssociatedID))
                    {
                        DateTime arrival = options.time.AddMinutes(GetScore(durationMap, current));
                        var nextDep = route.NextDeparture(arrival);
                        var waitingTime = (nextDep - arrival).Minutes;

                        /*auto nextDepartureOpt = route->nextDeparture(arrival);
                        if (!nextDepartureOpt)
                        {
                            continue;
                        }

                        Time & nextDepartureTime = nextDepartureOpt.getValue();
                        unsigned long long waitingTime = (nextDepartureTime - arrival).value;*/

                        tentative_gScore += waitingTime * options.waitingTimeFactor;
                        duration += waitingTime;
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
                    fScore[neighbor] = gScore[neighbor]
                       + neighbor.EstimatedDistance(goal, route);
                }
            }

            return null;
        }

        float OuterProduct(Vector3 a, Vector3 b, Vector3 p)
        {
            return (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);
        }

        static Math.PointPosition GetPointPosition(Vector3 p, Map.PointOnStreet pos)
        {
            if (pos.seg.street.isOneWay)
                return Math.PointPosition.Right;

            var a = pos.seg.positions[pos.prevIdx];
            var b = pos.seg.positions[pos.prevIdx + 1];

            return Math.GetPointPosition(a, b, p);
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

        static float GetDistanceFromStart(StreetSegment seg, Vector3 pos, bool backward)
        {
            if (backward)
            {
                return Mathf.Max(0f, seg.GetDistanceFromEndStopLine(pos));
            }
            else
            {
                return Mathf.Max(0f, seg.GetDistanceFromStartStopLine(pos));
            }
        }

        static float GetDistanceFromEnd(StreetSegment seg, Vector3 pos, bool backward)
        {
            if (backward)
            {
                return Mathf.Max(0f, seg.GetDistanceFromStartStopLine(pos));
            }
            else
            {
                return Mathf.Max(0f, seg.GetDistanceFromEndStopLine(pos));
            }
        }

        static Vector3[] GetPartialDrivePositions(Vector3 from, Vector3 to,
                                                  StreetSegment seg, bool backward,
                                                  bool addFrom, bool addTo)
        {
            var segPositions = new List<Vector3>(seg.positions);
            var positions = new List<Vector3>();

            float startDistance = GetDistanceFromStart(seg, from, backward);
            float endDistance = GetDistanceFromEnd(seg, to, backward);

            if (backward)
            {
                segPositions.Reverse();
            }

            int j = 0;
            int jLast = seg.positions.Count - 1;

            while (true)
            {
                var dist = GetDistanceFromStart(seg, segPositions[j], backward);
                if (dist >= startDistance)
                    break;

                if (j == seg.positions.Count - 1)
                    break;

                ++j;
            }

            while (true)
            {
                var dist = GetDistanceFromEnd(seg, segPositions[jLast], backward);
                if (dist >= endDistance)
                    break;

                if (jLast == j)
                    break;

                --jLast;
            }

            var range = segPositions.GetRange(j, jLast - j + (addTo && !addFrom ? 1 : 0));
            if (addFrom && (range.Count == 0 || !from.Equals(range.First())))
                positions.Add(from);

            positions.AddRange(range);

            if (addTo && (range.Count == 0 || !positions.Last().Equals(to)))
                positions.Add(to);

            return positions.ToArray();
        }

        PathPlanningResult CreateWalk(Vector3 from, Vector3 to)
        {
            var duration = EstimateWalkingDuration(from, to);
            return new PathPlanningResult(
                duration * options.walkingTimeFactor, duration,
                new List<PathStep> {
                    new WalkStep(from, to)
                });
        }

        public PathPlanningResult FindClosestDrive(Map map, Vector3 from, Vector3 to)
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

            var startSide = GetPointPosition(from, nearestPtFrom);
            var endSide = GetPointPosition(to, nearestPtTo);

            var options = new PathPlanningOptions
            {
                start = GetNearestIntersection(startSide, nearestPtFrom),
                goal = GetNearestIntersection(endSide, nearestPtTo, true),
            };

            var simpleJourney = false;
            if (nearestPtFrom.seg == nearestPtTo.seg)
            {
                var startDistance = (nearestPtFrom.seg.positions.First() - nearestPtFrom.pos).sqrMagnitude;
                var endDistance = (nearestPtFrom.seg.positions.First() - nearestPtTo.pos).sqrMagnitude;
                var backward = startDistance > endDistance;

                simpleJourney = startSide == endSide
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

            result.steps.Insert(0, new WalkStep(from, nearestPtFrom.pos));

            if (simpleJourney)
            {
                if (!nearestPtFrom.pos.Equals(nearestPtTo.pos))
                {
                    result.steps.Insert(1, new PartialDriveStep(
                        nearestPtFrom.pos,
                        nearestPtTo.pos,
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
                        nearestPtFrom.pos,
                        options.start.Location,
                        new DriveSegment
                        {
                            segment = nearestPtFrom.seg,
                            backward = nearestPtFrom.seg.Begin == options.start
                        }, true, false));
                }

                result.steps.Add(new PartialDriveStep(
                        options.goal.Location,
                        nearestPtTo.pos,
                        new DriveSegment
                        {
                            segment = nearestPtTo.seg,
                            backward = nearestPtTo.seg.End == options.goal,
                        }, false, true));
            }

            result.steps.Add(new WalkStep(nearestPtTo.pos, to));
            return result;
        }
    }
}