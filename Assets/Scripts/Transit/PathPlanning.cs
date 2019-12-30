//using UnityEngine;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Transidious.PublicTransit
//{
//    public class PathStep
//    {
//        public DateTime time;
//    }

//    public class WalkStep : PathStep
//    {

//    }

//    public class TravelStep : PathStep
//    {
//        /// The line to travel with.
//        public Line line;

//        /// The routes to follow.
//        public Route[] routes;

//        public TravelStep(DateTime time, Line line, Route[] routes)
//        {
//            this.time = time;
//            this.line = line;
//            this.routes = routes;
//        }
//    }

//    public class PathPlanningOptions
//    {
//        /// The starting stop.
//        public Stop start;

//        /// The goal stop.
//        public Stop goal;

//        /// The time of the trip.
//        public DateTime time;

//        /// Factor that walking time is multiplied with for scoring.
//        public float walkingTimeFactor = 3.0f;

//        /// Factor that traveling time is multiplied with for scoring.
//        public float travelTimeFactor = 1.0f;

//        /// Factor that waiting time is multiplied with for scoring.
//        public float waitingTimeFactor = 2.0f;

//        /// Number of minutes that a change is penalized with.
//        public float changingPenalty = 10.0f;
//    };

//    public class PathPlanningResult
//    {
//        /// The total cost of the path.
//        public readonly float cost = 0.0f;

//        /// The total duration of the path in minutes.
//        public readonly float duration = 0.0f;

//        /// The time you have to leave when taking this path.
//        public readonly DateTime leaveBy;

//        /// The time you will arrive when taking this path.
//        public readonly DateTime arriveAt;

//        /// The steps to take on this path.
//        public readonly PathStep[] steps;

//        public PathPlanningResult(float cost, float duration, DateTime leaveBy,
//                                  DateTime arriveAt, PathStep[] steps)
//        {
//            this.cost = cost;
//            this.duration = duration;
//            this.leaveBy = leaveBy;
//            this.arriveAt = arriveAt;
//            this.steps = steps;
//        }

//        public override string ToString()
//        {
//            string s = "[" + leaveBy.ToShortTimeString() + "] leave\n";

//            int i = 0;
//            foreach (PathStep step in steps)
//            {
//                if (i++ != 0) s += "\n";
//                s += "[" + step.time.ToShortTimeString() + "] ";

//                if (step is WalkStep)
//                {

//                }
//                else if (step is TravelStep)
//                {
//                    var travelStep = step as TravelStep;
//                    s += "travel line " + travelStep.line.name + " until "
//                       + travelStep.routes[travelStep.routes.Length - 1].endStop.name;
//                }
//            }

//            if (i++ != 0) s += "\n";
//            s += "[" + arriveAt.ToShortTimeString() + "] arrival\n";

//            return s;
//        }
//    }

//    public class PathPlanner
//    {
//        // The path planning options.
//        PathPlanningOptions options;

//        // The set of nodes already evaluated
//        HashSet<Stop> closedSet;

//        // The set of currently discovered nodes that are not evaluated yet.
//        // Initially, only the start node is known.
//        HashSet<Stop> openSet;

//        // For each node, which node it can most efficiently be reached from.
//        // If a node can be reached from many nodes, cameFrom will eventually contain the
//        // most efficient previous step.
//        Dictionary<Stop, Stop> cameFrom;
//        Dictionary<Stop, Route> cameFromRoute;

//        // For each node, the duration of getting from the start node to that node.
//        Dictionary<Stop, float> durationMap;

//        // For each node, the cost of getting from the start node to that node.
//        Dictionary<Stop, float> gScore;

//        // For each node, the total cost of getting from the start node to the goal
//        // by passing by that node. That value is partly known, partly heuristic.
//        Dictionary<Stop, float> fScore;

//        public PathPlanner(PathPlanningOptions options)
//        {
//            this.options = options;
//            this.closedSet = new HashSet<Stop>();
//            this.openSet = new HashSet<Stop>();
//            this.cameFrom = new Dictionary<Stop, Stop>();
//            this.cameFromRoute = new Dictionary<Stop, Route>();
//            this.durationMap = new Dictionary<Stop, float>();
//            this.gScore = new Dictionary<Stop, float>();
//            this.fScore = new Dictionary<Stop, float>();
//        }

//        static float HeuristicCostEstimate(Stop start, Stop goal, Route route = null)
//        {
//            float x1 = start.location.x;
//            float y1 = start.location.y;

//            float x2 = goal.location.x;
//            float y2 = goal.location.y;

//            float speed;
//            if (route)
//            {
//                speed = route.line.AverageSpeed;
//            }
//            else
//            {
//                speed = 50.0f;
//            }

//            float dist = (float)System.Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
//            return (dist / speed) / 60f;
//        }

//        static float GetScore(Dictionary<Stop, float> map, Stop stop)
//        {
//            if (map.TryGetValue(stop, out float val))
//            {
//                return val;
//            }

//            return float.PositiveInfinity;
//        }

//        static Stop Lowest(HashSet<Stop> set, Dictionary<Stop, float> map)
//        {
//            float lowest = float.PositiveInfinity;
//            Stop lowestStop = null;

//            foreach (Stop stop in set)
//            {
//                float score = GetScore(map, stop);
//                if (score < lowest)
//                {
//                    lowest = score;
//                    lowestStop = stop;
//                }
//            }

//            return lowestStop;
//        }

//        PathPlanningResult ReconstructPath()
//        {
//            Stop current = options.goal;
//            List<Stop> stops = new List<Stop> { current };

//            while (cameFrom.TryGetValue(current, out Stop prev))
//            {
//                stops.Add(prev);
//                current = prev;
//            }

//            DateTime time = options.time;
//            Line currentLine = null;
//            Stop previous = stops[stops.Count - 1];
//            List<Route> routes = new List<Route>();
//            List<PathStep> steps = new List<PathStep>();

//            for (int i = stops.Count - 2; i >= 0; --i)
//            {
//                Stop stop = stops[i];
//                Route route = null;

//                foreach (Route r in previous.outgoingRoutes)
//                {
//                    if (r.endStop == stop)
//                    {
//                        route = r;
//                        break;
//                    }
//                }

//                if (!currentLine)
//                {
//                    currentLine = route.line;
//                }
//                else if (currentLine != route.line)
//                {
//                    steps.Add(new TravelStep(time, currentLine, routes.ToArray()));
//                    routes.Clear();
//                }

//                time = time.AddMinutes(route.TravelTime);

//                Debug.Assert(route != null, "No route found!");
//                routes.Add(route);

//                previous = stop;
//            }

//            if (routes.Count != 0)
//            {
//                steps.Add(new TravelStep(time, currentLine, routes.ToArray()));
//            }

//            return new PathPlanningResult(
//                GetScore(gScore, options.goal),
//                GetScore(durationMap, options.goal),
//                options.time,
//                time,
//                steps.ToArray()
//            );
//        }

//        public PathPlanningResult GetPath()
//        {
//            Stop start = options.start;
//            Stop goal = options.goal;

//            openSet.Add(start);

//            // The cost of going from start to start is zero.
//            gScore[start] = 0.0f;
//            durationMap[start] = 0.0f;

//            // For the first node, that value is completely heuristic.
//            fScore[start] = HeuristicCostEstimate(start, goal);

//            while (openSet.Count != 0)
//            {
//                Stop current = Lowest(openSet, fScore);
//                if (current == goal)
//                {
//                    return ReconstructPath();
//                }

//                openSet.Remove(current);
//                closedSet.Add(current);

//                foreach (Route route in current.outgoingRoutes)
//                {
//                    Stop neighbor = route.endStop;
//                    if (closedSet.Contains(neighbor))
//                    {
//                        continue;
//                    }

//                    float tentative_gScore = GetScore(gScore, current)
//                       + route.TravelTime * options.travelTimeFactor;

//                    float duration = GetScore(durationMap, current)
//                       + route.TravelTime;

//                    // If we need to change, calculate a penalty.
//                    Route prevRoute = null;
//                    if (cameFromRoute.TryGetValue(current, out prevRoute))
//                    {
//                        if (route.line != prevRoute.line)
//                        {
//                            tentative_gScore += options.changingPenalty;
//                        }
//                    }

//                    // Find the next departure of this line at the station.
//                    if (options.time != null && (prevRoute == null || route.line != prevRoute.line))
//                    {
//                        /*Time arrival = options.time.getValue() + Minutes(score(durationMap, current));
//                        auto nextDepartureOpt = route->nextDeparture(arrival);
//                        if (!nextDepartureOpt)
//                        {
//                            continue;
//                        }

//                        Time & nextDepartureTime = nextDepartureOpt.getValue();
//                        unsigned long long waitingTime = (nextDepartureTime - arrival).value;

//                        tentative_gScore += waitingTime * options.waitingTimeFactor;
//                        duration += waitingTime;*/
//                    }

//                    if (!openSet.Add(neighbor))
//                    {
//                        if (tentative_gScore >= GetScore(gScore, neighbor))
//                        {
//                            continue;
//                        }
//                    }

//                    // This path is the best until now. Record it!
//                    cameFrom[neighbor] = current;
//                    cameFromRoute[neighbor] = route;

//                    gScore[neighbor] = tentative_gScore;
//                    durationMap[neighbor] = duration;
//                    fScore[neighbor] = gScore[neighbor]
//                       + HeuristicCostEstimate(neighbor, goal, route);
//                }
//            }

//            return null;
//        }
//    }
//}