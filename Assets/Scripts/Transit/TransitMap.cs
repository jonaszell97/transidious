using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace Transidious
{
    public class TransitMap
    {
        /// An assignment of a line to a slot of a stop.
        class SlotAssignment
        {
            /// The cardinal direction of the assigned slot.
            public readonly CardinalDirection SlotDirection;

            /// The index of the assigned slot.
            public readonly int SlotIndex;
            
            /// The assigned route.
            public Route Route;

            /// The line this slot is reserved for.
            public Line ReservedForLine;

            /// C'tor.
            public SlotAssignment(CardinalDirection slotDirection, int slotIndex)
            {
                SlotDirection = slotDirection;
                SlotIndex = slotIndex;
                Route = null;
                ReservedForLine = null;
            }
        }

        /// Map from stops to their available slots.
        private readonly Dictionary<Stop, SlotAssignment[][]> _slotAssignmentMap;

        /// Map from routes to their assigned slots.
        private readonly Dictionary<Tuple<Route, Stop>, SlotAssignment> _routeSlotAssignmentMap;

        /// C'tor.
        public TransitMap()
        {
            _slotAssignmentMap = new Dictionary<Stop, SlotAssignment[][]>();
            _routeSlotAssignmentMap = new Dictionary<Tuple<Route, Stop>, SlotAssignment>();
        }

        /// Initialize the transit map.
        public void Initialize()
        {
            UpdateAppearances(new HashSet<Stop>(GameController.instance.loadedMap.transitStops
                .Where(s => s.Type == Stop.StopType.Underground)));
        }

        /// Update stop and line appearance for all stops on a line.
        public void UpdateAppearances(Line line)
        {
            UpdateAppearances(new HashSet<Stop>(line.stops));
        }

        /// Update stop and line appearance for all stops on a line.
        void UpdateAppearances(HashSet<Stop> stops)
        {
            // Calculate how many slots we need for each cardinal direction for every stop.
            var affectedRoutes = new List<Route>();
            foreach (var stop in stops)
            {
                affectedRoutes.AddRange(stop.routes.Where(r => !r.isBackRoute));
            }

            var affectedLines = new HashSet<Line>();
            foreach (var route in affectedRoutes)
            {
                affectedLines.Add(route.line);
            }

            if (affectedLines.Count == 0)
            {
                return;
            }

            var cardinalDirections = new Dictionary<Tuple<Route, Stop>, CardinalDirection>();

            // Update the needed number of slots for all affected stops.
            foreach (var affectedLine in affectedLines)
            {
                UpdateStopsOnLine(affectedLine, cardinalDirections);
            }

            // Update affected stops.
            var affectedStops = new HashSet<Stop>(cardinalDirections.Select(k => k.Key.Item2));
            foreach (var stop in affectedStops)
            {
                UpdateStop(stop, cardinalDirections);
            }

            // Find contiguous segments of lines.
            var contiguousStops = new List<List<Tuple<Route, Stop>>>();
            foreach (var affectedLine in affectedLines)
            {
                FindContiguousStops(affectedLine, cardinalDirections, contiguousStops);
            }

            // Assign slots to routes.
            AssignSlots(contiguousStops, cardinalDirections);

            // Update route meshes according to their assigned slots.
            foreach (var affectedLine in affectedLines)
            {
                foreach (var route in affectedLine.routes)
                {
                    if (route.isBackRoute)
                    {
                        continue;
                    }

                    UpdateRouteMesh(route);
                }
            }
        }

        /// Get the cardinal direction of a Vector2.
        CardinalDirection GetCardinalDirection(Vector2 vec)
        {
            var angle = Math.AngleFromHorizontalAxis(vec);
            if (angle > 315f || angle < 45f)
            {
                return CardinalDirection.East;
            }
            
            if (angle < 135f)
            {
                return CardinalDirection.North;
            }
            
            if (angle < 225f)
            {
                return CardinalDirection.West;
            }

            return CardinalDirection.South;
        }

        /// Update the appearance of a single stop.
        void UpdateStop(Stop stop, Dictionary<Tuple<Route, Stop>, CardinalDirection> cardinalDirections)
        {
            if (stop.lineData.Count <= 1)
            {
                stop.CreateCircleMesh();
                _slotAssignmentMap.Remove(stop);
                return;
            }

            var neededSlots = new[] {0, 0, 0, 0};
            foreach (var route in stop.routes)
            {
                if (route.isBackRoute)
                {
                    continue;
                }

                var nextStop = stop == route.endStop ? route.beginStop : route.endStop;
                ++neededSlots[(int) cardinalDirections[Tuple.Create(route, nextStop)]];
            }

            var height = Mathf.Max(1, neededSlots[(int) CardinalDirection.East], neededSlots[(int) CardinalDirection.West]);
            var width = Mathf.Max(1, neededSlots[(int) CardinalDirection.North], neededSlots[(int) CardinalDirection.South]);

            if (height == 1 && width == 1)
            {
                stop.CreateCircleMesh();
                _slotAssignmentMap.Remove(stop);
            }
            else
            {
                var slotAssignments = new SlotAssignment[4][];
                for (var x = 0; x < 4; ++x)
                {
                    var dir = (CardinalDirection) x;
                    int size;
                    if (dir == CardinalDirection.North || dir == CardinalDirection.South)
                    {
                        size = width;
                    }
                    else
                    {
                        size = height;
                    }

                    slotAssignments[x] = new SlotAssignment[size];

                    for (var y = 0; y < size; ++y)
                    {
                        slotAssignments[x][y] = new SlotAssignment(dir, y);
                    }
                }

                _slotAssignmentMap[stop] = slotAssignments;
                stop.CreateSmallRectMesh(width, height, Quaternion.identity);
            }
        }

        /// Update the appearance and slot assignment of all routes on a line.
        void UpdateStopsOnLine(Line line, Dictionary<Tuple<Route, Stop>, CardinalDirection> cardinalDirections)
        {
            // Calculate the desired direction each route should start and end in.
            for (var i = 0; i < line.routes.Count; ++i)
            {
                var route = line.routes[i];
                if (route.isBackRoute)
                {
                    break;
                }

                var start = route.beginStop;
                var end = route.endStop;

                CardinalDirection startDir;
                if (i == 0)
                {
                    // For the first stop always use the default direction.
                    startDir = GetCardinalDirection(route.positions[1] - route.positions[0]);
                }
                else
                {
                    // Prefer to exit via the opposite cardinal direction of the previous route, unless the angle
                    // would be too sharp.
                    var prevRoute = line.routes[i - 1];
                    var inDir = prevRoute.positions.Last() - prevRoute.positions.SecondToLast();
                    var outDir = route.positions[1] - route.positions[0];

                    var angle = Math.DirectionalAngleDeg(inDir, outDir);
                    if ((angle >= 80f && angle <= 100f) || (angle >= 260f && angle <= 280f))
                    {
                        // Angle is too sharp.
                        startDir = GetCardinalDirection(outDir);
                    }
                    else
                    {
                        startDir = cardinalDirections[Tuple.Create(prevRoute, start)].Opposite();
                    }
                }

                cardinalDirections.Add(Tuple.Create(route, start), startDir);

                // Get the 'quadrant' the end stop is in relative to the beginning.
                var nextStopQuadrant = GetQuadrant(start, end);
                var endVec = route.positions.SecondToLast() - route.positions.Last();

                // Try to fit the route according the the assignment of the begin stop.
                CardinalDirection endDir;
                switch (nextStopQuadrant)
                {
                    case StopQuadrant.Centered:
                        endDir = GetCardinalDirection(endVec);
                        break;
                    case StopQuadrant.TopRight:
                        switch (startDir)
                        {
                            case CardinalDirection.North:
                                endDir = CardinalDirection.West;
                                break;
                            case CardinalDirection.East:
                                endDir = CardinalDirection.South;
                                break;
                            default:
                                Debug.LogError("should not be possible");
                                endDir = GetCardinalDirection(endVec);
                                break;
                        }

                        break;
                    case StopQuadrant.BottomRight:
                        switch (startDir)
                        {
                            case CardinalDirection.South:
                                endDir = CardinalDirection.West;
                                break;
                            case CardinalDirection.East:
                                endDir = CardinalDirection.North;
                                break;
                            default:
                                Debug.LogError("should not be possible");
                                endDir = GetCardinalDirection(endVec);
                                break;
                        }

                        break;
                    case StopQuadrant.TopLeft:
                        switch (startDir)
                        {
                            case CardinalDirection.North:
                                endDir = CardinalDirection.East;
                                break;
                            case CardinalDirection.West:
                                endDir = CardinalDirection.South;
                                break;
                            default:
                                Debug.LogError("should not be possible");
                                endDir = GetCardinalDirection(endVec);
                                break;
                        }

                        break;
                    case StopQuadrant.BottomLeft:
                        switch (startDir)
                        {
                            case CardinalDirection.South:
                                endDir = CardinalDirection.East;
                                break;
                            case CardinalDirection.West:
                                endDir = CardinalDirection.North;
                                break;
                            default:
                                Debug.LogError("should not be possible");
                                endDir = GetCardinalDirection(endVec);
                                break;
                        }

                        break;
                    default:
                        Debug.LogError($"bad enum value {nextStopQuadrant}");
                        endDir = GetCardinalDirection(endVec);
                        break;
                }

                cardinalDirections.Add(Tuple.Create(route, end), endDir);
            }
        }

        /// Update the appearance and slot assignment of all routes on a line.
        void FindContiguousStops(Line line, Dictionary<Tuple<Route, Stop>, CardinalDirection> cardinalDirections,
                                 List<List<Tuple<Route, Stop>>> contiguousStops)
        {
            // Find contigous parts of the line (i.e. routes that that enter and exit in the opposite cardinal
            // directions) and make sure there's no jumps in the slot assignments.
            for (var i = 0; i < line.routes.Count;)
            {
                var firstRoute = line.routes[i];
                if (firstRoute.isBackRoute)
                {
                    break;
                }

                var horizontal = cardinalDirections[Tuple.Create(firstRoute, firstRoute.beginStop)].IsHorizontal();
                var endHorizontal = cardinalDirections[Tuple.Create(firstRoute, firstRoute.endStop)].IsHorizontal();

                if (horizontal != endHorizontal)
                {
                    ++i;
                    continue;
                }

                var curr = new List<Tuple<Route, Stop>>
                {
                    Tuple.Create(firstRoute, firstRoute.beginStop),
                    Tuple.Create(firstRoute, firstRoute.endStop)
                };

                while (++i < line.routes.Count)
                {
                    var nextRoute = line.routes[i];
                    if (nextRoute.isBackRoute)
                    {
                        break;
                    }

                    var startDir = cardinalDirections[Tuple.Create(nextRoute, nextRoute.beginStop)];
                    if (startDir.IsHorizontal() != horizontal)
                    {
                        break;
                    }

                    curr.Add(Tuple.Create(nextRoute, nextRoute.beginStop));

                    var endDir = cardinalDirections[Tuple.Create(nextRoute, nextRoute.endStop)];
                    if (endDir.IsHorizontal() != horizontal)
                    {
                        break;
                    }

                    curr.Add(Tuple.Create(nextRoute, nextRoute.endStop));
                }

                contiguousStops.Add(curr);
            }
        }

        /// Find the optimal slot assignment for all routes.
        void AssignSlots(List<List<Tuple<Route, Stop>>> allContiguousStops,
                         Dictionary<Tuple<Route, Stop>, CardinalDirection> cardinalDirections)
        {
            int DoNextAssignment(List<Tuple<Route, Stop>> contiguousStops,
                                 Dictionary<SlotAssignment, Tuple<Route, Stop>> solution)
            {
                var score = Int32.MaxValue;
                var currentSlot = 0;
                var bestSlot = -1;

                while (true)
                {
                    var mismatches = 0;
                    var allMatch = true;
                    var foundHigher = false;

                    foreach (var (route, stop) in contiguousStops)
                    {
                        if (!_slotAssignmentMap.ContainsKey(stop))
                        {
                            continue;
                        }

                        var dir = cardinalDirections[Tuple.Create(route, stop)];
                        var slots = _slotAssignmentMap[stop][(int) dir];
                        foundHigher |= slots.Length > currentSlot;

                        var idx = Mathf.Min(currentSlot, slots.Length - 1);
                        if (solution.ContainsKey(slots[idx]))
                        {
                            ++mismatches;
                            allMatch = false;
                        }
                    }

                    if (mismatches < score)
                    {
                        score = mismatches;
                        bestSlot = currentSlot;
                    }

                    if (allMatch || !foundHigher)
                    {
                        break;
                    }

                    ++currentSlot;
                }

                foreach (var (route, stop) in contiguousStops)
                {
                    if (!_slotAssignmentMap.ContainsKey(stop))
                    {
                        continue;
                    }

                    var key = Tuple.Create(route, stop);
                    var dir = cardinalDirections[key];
                    var slots = _slotAssignmentMap[stop][(int) dir];
                    var idx = Mathf.Min(bestSlot, slots.Length - 1);

                    var slot = slots[idx];
                    while (solution.ContainsKey(slot))
                    {
                        idx = (idx + 1) % slots.Length;
                        slot = slots[idx];
                    }

                    solution.Add(slot, key);
                }

                return score;
            }

            Dictionary<SlotAssignment, Tuple<Route, Stop>> bestSolution = null;
            var bestScore = int.MaxValue;

            var currentSolution = new Dictionary<SlotAssignment, Tuple<Route, Stop>>();
            foreach (var permutation in allContiguousStops.GetPermutations())
            {
                var currentScore = 0;
                foreach (var contiguousStops in permutation)
                {
                    currentScore += DoNextAssignment(contiguousStops, currentSolution);
                    if (currentScore >= bestScore)
                    {
                        break;
                    }
                }

                if (currentScore < bestScore)
                {
                    bestSolution = currentSolution;
                    bestScore = currentScore;

                    if (bestScore == 0)
                    {
                        break;
                    }

                    currentSolution = new Dictionary<SlotAssignment, Tuple<Route, Stop>>();
                }
                else
                {
                    currentSolution.Clear();
                }
            }

            Debug.Assert(bestSolution != null, "no solution found");

            foreach (var (slot, routeStopPair) in bestSolution)
            {
                slot.Route = routeStopPair.Item1;
                _routeSlotAssignmentMap[routeStopPair] = slot;
            }
        }

#if DEBUG
        [UsedImplicitly]
        private void DisplaySlotPositions(Stop stop, SlotAssignment[][] slotAssignments)
        {
            for (var x = 0; x < slotAssignments.Length; ++x)
            {
                var assignments = slotAssignments[x];
                for (var y = 0; y < assignments.Length; ++y)
                {
                    var c = slotAssignments[x][y].Route?.line.color ?? Color.black;
                    var obj = Utility.DrawCircle(GetSlotPosition(stop, slotAssignments[x][y], false), .25f, .25f, c);
                    obj.name = $"{(CardinalDirection)x} {y}";
                }
            }
        }
#endif

        /// Get the world position of a slot of a particular stop.
        Vector2 GetSlotPosition(Stop stop, SlotAssignment slot, bool moveToCenter)
        {
            var sr = stop.GetComponent<SpriteRenderer>();
            var bounds = sr.bounds;
            var extents = (Vector2) bounds.extents;

            Vector2 pos;
            switch (slot.SlotDirection)
            {
                case CardinalDirection.North:
                    pos = stop.location + new Vector2(-extents.x, extents.y);
                    break;
                case CardinalDirection.South:
                    pos = stop.location + new Vector2(-extents.x, -extents.y);
                    break;
                case CardinalDirection.East:
                    pos = stop.location + new Vector2(extents.x, extents.y);
                    break;
                case CardinalDirection.West:
                    pos = stop.location + new Vector2(-extents.x, extents.y);
                    break;
                default:
                    Debug.LogError("invalid cardinal direction");
                    return stop.location;
            }

            var size = stop.size;
            if (slot.SlotDirection == CardinalDirection.North || slot.SlotDirection == CardinalDirection.South)
            {
                if (size.x.Equals(1))
                {
                    pos = new Vector2(pos.x + extents.x, pos.y);
                }
                else
                {
                    var padding = (bounds.extents.x / size.x);
                    var spaceAvailable = bounds.size.x - 2 * padding;
                    var spacePerSlot = spaceAvailable / size.x;

                    pos = new Vector2(pos.x + padding + (slot.SlotIndex * spacePerSlot) + spacePerSlot * .5f, pos.y);
                }

                if (moveToCenter)
                {
                    if (slot.SlotDirection == CardinalDirection.North)
                    {
                        pos = new Vector2(pos.x, pos.y - extents.y);
                    }
                    else
                    {
                        pos = new Vector2(pos.x, pos.y + extents.y);
                    }
                }
            }
            else
            {
                if (size.y.Equals(1))
                {
                    pos = new Vector2(pos.x, pos.y - extents.y);
                }
                else
                {
                    var padding = (bounds.extents.y / size.y);
                    var spaceAvailable = bounds.size.y - 2 * padding;
                    var spacePerSlot = spaceAvailable / size.y;

                    pos = new Vector2(pos.x, pos.y - padding - (slot.SlotIndex * spacePerSlot) - spacePerSlot * .5f);
                }

                if (moveToCenter)
                {
                    if (slot.SlotDirection == CardinalDirection.East)
                    {
                        pos = new Vector2(pos.x - extents.x, pos.y);
                    }
                    else
                    {
                        pos = new Vector2(pos.x + extents.x, pos.y);
                    }
                }
            }

            return pos;
        }

#if DEBUG
        void CreateDebugSubwayLines()
        {
            var map = GameController.instance.loadedMap;
            var basePos = map.GetMapObject<NaturalFeature>("Sophie-Charlotte-Platz").VisualCenter;

            var Ruhleben = map.GetOrCreateStop(Stop.StopType.Underground, "U Ruhleben",             basePos + new Vector2(-2400f, 600f));
            var OlympiaStadion = map.GetOrCreateStop(Stop.StopType.Underground, "U Olympiastadion", basePos + new Vector2(-2200f, 400f));
            var NeuWestend = map.GetOrCreateStop(Stop.StopType.Underground, "U Neu-Westend",        basePos + new Vector2(-2000f, -200f));
            var THP = map.GetOrCreateStop(Stop.StopType.Underground, "U Theodor-Heuss-Platz",       basePos + new Vector2(-1600f, 0f));
            var Kaiserdamm = map.GetOrCreateStop(Stop.StopType.Underground, "U Kaiserdamm",         basePos + new Vector2(-800f, 0f));
            var SCP = map.GetOrCreateStop(Stop.StopType.Underground, "U Sophie-Charlotte-Platz",    basePos);

            var u2 = map.CreateLine(TransitType.Subway, "U2", Color.red)
                .AddStop(Ruhleben)
                .AddStop(OlympiaStadion)
                .AddStop(NeuWestend)
                .AddStop(THP)
                .AddStop(Kaiserdamm)
                .AddStop(SCP)
                .Loop();

            UpdateAppearances(u2.line);
            u2.Finish();
        }
#endif

        /*
         * X: Current stop
         * A, B, C, D, M: Destination stops
         * |-------------------------------------------------|
         * |                        |   D    C               |
         * |                        M                     B  |
         * |                        |                     A  |
         * |-----------M------------X------------M-----------|
         * |                        |                        |
         * |                        M                        |
         * |                        |                        |
         * |-------------------------------------------------|
         */
        enum StopLocation
        {
            A, B, C, D,
        }

        enum StopQuadrant
        { 
            Centered, TopLeft, TopRight, BottomLeft, BottomRight,
        }

        StopQuadrant GetQuadrant(Stop start, Stop end)
        {
            var beginStopPos = start.location;
            var endStopPos = end.location;

            if (beginStopPos.x.Equals(endStopPos.x) || beginStopPos.y.Equals(endStopPos.y))
            {
                return StopQuadrant.Centered;
            }

            if (endStopPos.x >= beginStopPos.x)
            {
                return endStopPos.y >= beginStopPos.y ? StopQuadrant.TopRight : StopQuadrant.BottomRight;
            }

            return endStopPos.y >= beginStopPos.y ? StopQuadrant.TopLeft : StopQuadrant.BottomLeft;
        }

        void FinalizeRouteMesh(Route route, List<Vector2> positions)
        {
            var collider = route.GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            var mesh = MeshBuilder.CreateSmoothLine(positions, route.line.LineWidth * .3f, 20, 0f, collider);
            route.UpdateMesh(mesh, positions, null);
            route.EnableCollision();
            route.positions = positions;
        }

        void UpdateRouteMesh(Route route)
        {
            var positions = new List<Vector2>();

            var beginStop = route.beginStop;
            var endStop = route.endStop;

            var beginStopPos = beginStop.location;
            var endStopPos = endStop.location;

            // Case M: Same X / Y
            var directRoute = beginStopPos.x.Equals(endStopPos.x) || beginStopPos.y.Equals(endStopPos.y);

            const float offset = 5f;
            const float thresholdAngle = 15f;

            var distance = (endStopPos - beginStopPos).magnitude;
            var angle = Math.AngleFromHorizontalAxis(endStopPos - beginStopPos) % 90f;
            if (angle > 45f)
            {
                angle = 90f - angle;
            }

            if (angle < thresholdAngle || distance < 2 * offset)
            {
                directRoute = true;
            }

            var oneThirdDistance = distance * (1f / 3f);

            Vector2 beginPos;
            CardinalDirection beginCardinal;

            if (_routeSlotAssignmentMap.TryGetValue(Tuple.Create(route, route.beginStop), out var fromAssignment))
            {
                beginPos = GetSlotPosition(route.beginStop, fromAssignment, true);
                beginCardinal = fromAssignment.SlotDirection;
            }
            else
            {
                beginPos = route.beginStop.location;
                beginCardinal = GetCardinalDirection(route.positions[1] - route.positions[0]);
            }

            positions.Add(beginPos);

            // Avoid 90-degree angles.
            if (!directRoute)
            {
                switch (beginCardinal)
                {
                    case CardinalDirection.North:
                        positions.Add(new Vector2(beginPos.x, beginPos.y + oneThirdDistance));
                        break;
                    case CardinalDirection.South:
                        positions.Add(new Vector2(beginPos.x, beginPos.y - oneThirdDistance));
                        break;
                    case CardinalDirection.East:
                        positions.Add(new Vector2(beginPos.x + oneThirdDistance, beginPos.y));
                        break;
                    case CardinalDirection.West:
                        positions.Add(new Vector2(beginPos.x - oneThirdDistance, beginPos.y));
                        break;
                }
            }

            Vector2 endPos;
            CardinalDirection endCardinal;
            if (_routeSlotAssignmentMap.TryGetValue(Tuple.Create(route, route.endStop), out var toAssignment))
            {
                endPos = GetSlotPosition(route.endStop, toAssignment, true);
                endCardinal = toAssignment.SlotDirection;
            }
            else
            {
                endPos = route.endStop.location;
                endCardinal = GetCardinalDirection(route.positions.SecondToLast() - route.positions.Last());
            }

            if (!directRoute)
            {
                switch (endCardinal)
                {
                    case CardinalDirection.North:
                        positions.Add(new Vector2(endPos.x, endPos.y + oneThirdDistance));
                        break;
                    case CardinalDirection.South:
                        positions.Add(new Vector2(endPos.x, endPos.y - oneThirdDistance));
                        break;
                    case CardinalDirection.East:
                        positions.Add(new Vector2(endPos.x + oneThirdDistance, endPos.y));
                        break;
                    case CardinalDirection.West:
                        positions.Add(new Vector2(endPos.x - oneThirdDistance, endPos.y));
                        break;
                }
            }

            positions.Add(endPos);
            FinalizeRouteMesh(route, positions);
        }
    }
}