using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;
using UnityEngine;

namespace Transidious
{
    public class Router
    {
        /// Spacing of default hub points.
        private static readonly float _hubSpacing = 500f;

        /// The loaded map.
        private Map _map;

        /// List of current hubs.
        private HashSet<IStop> _hubs;

        /// Cached paths from each hub to every other hub.
        private Dictionary<IStop, Dictionary<IStop, PathPlanningResult>> _pathCache;

        /// The closest hub to each quadrant.
        private IStop[][] _closestHubs;

        /// X and Y size of the closest hubs array.
        private int _hubsWidth, _hubsHeight;

        /// The path planner.
        private PathPlanner _pathPlanner;

        /// Whether or not the cache is valid.
        private bool _cacheValid;

        /// Initialize the router.
        public void Initialize(Map map)
        {
            _map = map;
            _hubs = new HashSet<IStop>();
            _pathCache = new Dictionary<IStop, Dictionary<IStop, PathPlanningResult>>();
            _pathPlanner = new PathPlanner(new PathPlanningOptions());

            /*CreateDefaultHubs();
            FillCache();*/
        }

        /// Whether or not a stop is a hub.
        public bool IsHub(IStop stop)
        {
            return _cacheValid && _hubs.Contains(stop);
        }

        /// Return a route connecting two hubs.
        public PathPlanningResult GetRoute(PathPlanningOptions options, Vector2 from, Vector2 to, DateTime leaveBy)
        {
            var hubs = GetClosestHubs(from, to);
            var cachedPath = _pathCache[hubs.Item1][hubs.Item2];

            if (cachedPath == null)
            {
                _pathPlanner.Reset();
                return _pathPlanner.FindClosestPath(_map, from, to, leaveBy);
            }

            PathStep[] pathToHubOne, pathFromHubTwo;
            if ((from - hubs.Item1.Location).magnitude <= options.maxWalkingDistance)
            {
                pathToHubOne = new PathStep[] { new WalkStep(from, hubs.Item1.Location) };
            }
            else
            {
                _pathPlanner.Reset();
                pathToHubOne = _pathPlanner.FindClosestPath(_map, from, hubs.Item1.Location, leaveBy).path.Steps;
            }

            if ((to - hubs.Item2.Location).magnitude <= options.maxWalkingDistance)
            {
                pathFromHubTwo = new PathStep[] { new WalkStep(hubs.Item2.Location, to) };
            }
            else
            {
                _pathPlanner.Reset();
                pathFromHubTwo = _pathPlanner.FindClosestPath(_map, hubs.Item2.Location, to, leaveBy).path.Steps;
            }

            var steps = new List<PathStep>();
            steps.AddRange(pathToHubOne);
            steps.AddRange(cachedPath.path.Steps);
            steps.AddRange(pathFromHubTwo);

            return new PathPlanningResult(options, leaveBy, new PlannedPath(steps));
        }

        /// Get the closest hub to a point.
        public Tuple<IStop, IStop> GetClosestHubs(Vector2 from, Vector2 to)
        {
            return Tuple.Create(FindClosestHubInDirection(from, to), FindClosestHubInDirection(to, from));
        }

        /// Find the closest hub to a point in a direction.
        public IStop FindClosestHubInDirection(Vector2 pt, Vector2 endPoint)
        {
            var x = Mathf.Clamp((int) ((pt.x - _map.minX) / _hubSpacing), 0, _hubsWidth - 1);
            var y = Mathf.Clamp((int) ((pt.y - _map.minY) / _hubSpacing), 0, _hubsHeight - 1);

            var dir = endPoint - pt;
            var closestX = x; //Mathf.Clamp(x + dir.x.CompareTo(0), 0, _hubsWidth);
            var closestY = y; //Mathf.Clamp(y + dir.y.CompareTo(0), 0, _hubsHeight);

            return _closestHubs[closestX][closestY];
        }

        /// Get path from one hub to another.
        public PathPlanningResult GetPath(IStop from, IStop to)
        {
            if (!_pathCache.TryGetValue(from, out var cache))
            {
                return null;
            }

            return cache.TryGetValue(to, out var path) ? path : null;
        }

#if UNITY_EDITOR
        /// Create default hubs.
        void CreateDefaultHubs()
        {
            _hubsWidth = (int)Mathf.Ceil(_map.width / _hubSpacing);
            _hubsHeight = (int)Mathf.Ceil(_map.height / _hubSpacing);
            _closestHubs = new IStop[_hubsWidth][];

            var x = 0;
            for (var xx = _map.minX; xx < _map.maxX; xx += _hubSpacing, ++x)
            {
                _closestHubs[x] = new IStop[_hubsHeight];

                var y = 0;
                for (var yy = _map.minY; yy < _map.maxY; yy += _hubSpacing, ++y)
                {
                    var pos = new Vector2(xx, yy);

                    if (!_map.FindClosest<StreetIntersection>(out var intersection, pos, inter =>
                    {
                        var distance = (pos - (Vector2)inter.Position).magnitude;
                        if (distance >= _hubSpacing * .5f)
                        {
                            return false;
                        }
                        
                        return inter.IntersectingStreets.Any(s => s.street.type == Street.Type.Highway
                                                                  || s.street.type == Street.Type.Primary
                                                                  || s.street.type == Street.Type.Secondary
                                                                  || s.street.type == Street.Type.Tertiary);
                    }))
                    {
                        continue;
                    }

                    Utility.DrawCircle(pos, 5f, 5f, Color.red);
                    Utility.DrawArrow(pos, intersection.Location, 2f, Color.red);
                    Utility.DrawCircle(intersection.Location, 5f, 5f, Color.yellow);

                    _closestHubs[x][y] = intersection;
                    _hubs.Add(intersection);
                }
            }

            for (x = 0; x < _hubsWidth; ++x)
            {
                for (var y = 0; y < _hubsHeight; ++y)
                {
                    if (_closestHubs[x][y] != null)
                        continue;

                    var center = new Vector2(x * _hubSpacing + _hubSpacing * .5f, y * _hubSpacing + _hubSpacing * .5f);
                    var minDist = float.PositiveInfinity;
                    var found = false;
                    var radius = 1;

                    while (true)
                    {
                        for (var xk = x - radius; xk <= x + radius; ++xk)
                        {
                            if (xk < 0 || xk >= _hubsWidth)
                            {
                                continue;
                            }

                            for (var yk = y - radius; yk <= y + radius; ++yk)
                            {
                                if (yk < 0 || yk >= _hubsHeight)
                                {
                                    continue;
                                }

                                var otherHub = _closestHubs[xk][yk];
                                if (otherHub == null)
                                {
                                    continue;
                                }

                                found = true;

                                var dist = (otherHub.Location - center).sqrMagnitude;
                                if (dist < minDist)
                                {
                                    _closestHubs[x][y] = otherHub;
                                    minDist = dist;
                                }
                            }
                        }

                        if (found)
                        {
                            break;
                        }

                        ++radius;
                        Debug.Assert(radius < _hubsHeight || radius < _hubsWidth, "no hub found!");
                    }
                }
            }
        }
#endif

        /// Refill the cache.
        void FillCache()
        {
            foreach (var hub in _hubs)
            {
                if (!_pathCache.TryGetValue(hub, out var destinations))
                {
                    destinations = new Dictionary<IStop, PathPlanningResult>();
                    _pathCache.Add(hub, destinations);
                }

                foreach (var destination in _hubs)
                {
                    if (hub == destination || destinations.ContainsKey(destination))
                    {
                        continue;
                    }

                    var path = _pathPlanner.FindClosestDrive(hub, destination);
                    if (path == null)
                    {
                        continue;
                    }

                    destinations.Add(destination, path);
                }
            }

            _cacheValid = true;
        }
    }
}