using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

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
                UpdateStop(stop);
                affectedRoutes.AddRange(stop.routes.Where(r => !r.isBackRoute));
            }

            var affectedLines = new HashSet<Line>();
            foreach (var route in affectedRoutes)
            {
                affectedLines.Add(route.line);
            }

            // Reassign affected routes to the best-fitting slot and update their paths.
            foreach (var affectedLine in affectedLines)
            {
                UpdateLine(affectedLine, stops);
            }

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
        void UpdateStop(Stop stop)
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
                var direction = nextStop.location - stop.location;

                ++neededSlots[(int) GetCardinalDirection(direction)];
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
        void UpdateLine(Line line, HashSet<Stop> stops)
        {
            var routesToReassign = new List<Tuple<Route, Stop, Route, SlotAssignment>>();
            for (var i = 0; i < line.routes.Count; ++i)
            {
                var route = line.routes[i];
                if (route.isBackRoute)
                {
                    continue;
                }

                var prevRoute = i > 0 ? line.routes[i - 1] : null;
                var nextRoute = i + 1 < line.routes.Count ? line.routes[i + 1] : null;

                var from = route.beginStop;
                var to = route.endStop;

                SlotAssignment prevAssignment = null;
                if (prevRoute != null)
                {
                    _routeSlotAssignmentMap.TryGetValue(Tuple.Create(prevRoute, from), out prevAssignment);
                }

                Tuple<SlotAssignment, bool> firstAssignment = null;
                if (stops.Contains(from))
                {
                    firstAssignment = AssignSlot(route, from, prevRoute, prevAssignment, true);
                    if (prevRoute != null && firstAssignment != null && !firstAssignment.Item2)
                    {
                        routesToReassign.Add(Tuple.Create(prevRoute, from, route, firstAssignment.Item1));
                    }
                }

                if (!stops.Contains(to))
                {
                    continue;
                }

                var sndAssignment = AssignSlot(route, to, nextRoute, firstAssignment?.Item1, false);
                if (sndAssignment != null && sndAssignment.Item1.SlotIndex != firstAssignment?.Item1.SlotIndex)
                {
                    routesToReassign.Add(Tuple.Create(route, from, nextRoute, sndAssignment.Item1));
                }
            }

            foreach (var (route, stop, oppositeRoute, slot) in routesToReassign)
            {
                var key = Tuple.Create(route, stop);
                if (_routeSlotAssignmentMap.TryGetValue(key, out var existingSlot))
                {
                    existingSlot.Route = null;
                    _routeSlotAssignmentMap.Remove(key);
                }

                AssignSlot(route, stop, oppositeRoute, slot, false);
            }
        }

        /// Assign a desired slot to a route.
        Tuple<SlotAssignment, bool>
        AssignSlot(Route route, Stop stop, Route opposite, SlotAssignment oppositeAssignment,
                   bool reserveOpposite)
        {
            if (!_slotAssignmentMap.TryGetValue(stop, out var assignments))
            {
                return null;
            }

            Vector2 inDirection;
            Vector2? outDirection = null;

            if (stop == route.beginStop)
            {
                inDirection = route.positions[1] - route.positions[0];
                if (opposite != null)
                {
                    outDirection = opposite.positions.SecondToLast() - opposite.positions.Last();
                }
            }
            else
            {
                inDirection = route.positions.SecondToLast() - route.positions.Last();
                if (opposite != null)
                {
                    outDirection = opposite.positions[1] - opposite.positions[0];
                }
            }

            var inCardinal = GetCardinalDirection(inDirection);
            var desiredSlot = 0;

            if (oppositeAssignment?.SlotDirection.IsParallelTo(inCardinal) ?? false)
            {
                desiredSlot = oppositeAssignment.SlotIndex;
            }

            SlotAssignment result;
            if (outDirection == null)
            {
                result = AssignSlot(route, stop, inCardinal, assignments, desiredSlot, false,
                                    reserveOpposite ? oppositeAssignment : null);
            }
            else
            {
                var beginStopPos = route.beginStop.location;
                var endStopPos = route.endStop.location;

                // Find the destination quadrant.
                StopQuadrant dstQuadrant;
                if (endStopPos.x >= beginStopPos.x)
                {
                    dstQuadrant = endStopPos.y >= beginStopPos.y ? StopQuadrant.TopRight : StopQuadrant.BottomRight;
                }
                else
                {
                    dstQuadrant = endStopPos.y >= beginStopPos.y ? StopQuadrant.TopLeft : StopQuadrant.BottomLeft;
                }

                // Normalize the angle to be relative to the quadrant.
                var angle = Math.AngleFromHorizontalAxis(endStopPos - beginStopPos);
                switch (dstQuadrant)
                {
                    case StopQuadrant.TopRight:
                        break;
                    case StopQuadrant.TopLeft:
                        angle -= 90f;
                        break;
                    case StopQuadrant.BottomLeft:
                        angle -= 180f;
                        break;
                    case StopQuadrant.BottomRight:
                        angle -= 270f;
                        break;
                }

                CardinalDirection outCardinal;
                if (angle < 15f)
                {
                    outCardinal = GetCardinalDirection(outDirection.Value);
                }
                else
                {
                    switch (dstQuadrant)
                    {
                        case StopQuadrant.TopRight:
                        case StopQuadrant.BottomRight:
                            outCardinal = CardinalDirection.West;
                            break;
                        case StopQuadrant.TopLeft:
                        case StopQuadrant.BottomLeft:
                            outCardinal = CardinalDirection.East;
                            break;
                        default:
                            throw new ArgumentException("invalid quadrant");
                    }
                }
                
                var backward = false;
                switch (inCardinal)
                {
                    case CardinalDirection.West:
                        if (outCardinal == CardinalDirection.South)
                        {
                            backward = true;
                            desiredSlot = assignments[(int) inCardinal].Length - 1;
                        }

                        break;
                    case CardinalDirection.North:
                        if (outCardinal == CardinalDirection.East)
                        {
                            backward = true;
                            desiredSlot = assignments[(int) inCardinal].Length - 1;
                        }

                        break;
                    case CardinalDirection.East:
                        if (outCardinal == CardinalDirection.South)
                        {
                            backward = true;
                            desiredSlot = assignments[(int) inCardinal].Length - 1;
                        }

                        break;
                    case CardinalDirection.South:
                        if (outCardinal == CardinalDirection.East || outCardinal == CardinalDirection.West)
                        {
                            backward = true;
                            desiredSlot = assignments[(int) inCardinal].Length - 1;
                        }

                        break;
                }

                result = AssignSlot(route, stop, inCardinal, assignments, desiredSlot, backward,
                                    reserveOpposite ? oppositeAssignment : null);
            }

            return Tuple.Create(result, result?.SlotIndex == desiredSlot);
        }

        /// Assign a desired slot.
        SlotAssignment AssignSlot(Route route, Stop stop, CardinalDirection direction,
                                  SlotAssignment[][] assignments,
                                  int desiredSlot, bool backward,
                                  SlotAssignment oppositeAssignment)
        {
            var availableSlots = assignments[(int) direction];
            desiredSlot = Mathf.Clamp(desiredSlot, 0, availableSlots.Length - 1);

            var inc = backward ? -1 : 1;
            for (int i = desiredSlot, n = 0; n != availableSlots.Length; ++n)
            {
                var slot = availableSlots[i];
                if (slot.Route == null && (slot.ReservedForLine == null || slot.ReservedForLine == route.line))
                {
                    slot.Route = route;
                    slot.ReservedForLine = null;

                    _routeSlotAssignmentMap[Tuple.Create(route, stop)] = slot;

                    // Reserve the opposite slot for this line.
                    if (oppositeAssignment == null)
                    {
                        var oppositeSlot = assignments[(int) direction.Opposite()][i];
                        oppositeSlot.ReservedForLine = route.line;
                    }

                    return slot;
                }

                i += inc;
                if (i == -1)
                {
                    i = availableSlots.Length - 1;
                }
                else if (i == availableSlots.Length)
                {
                    i = 0;
                }
            }

            Debug.LogError("no empty slot!");
            return null;
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
            TopLeft, TopRight, BottomLeft, BottomRight,
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
            if (beginStopPos.x.Equals(endStopPos.x) || beginStopPos.y.Equals(endStopPos.y))
            {
                positions.Add(beginStopPos);
                positions.Add(endStopPos);

                FinalizeRouteMesh(route, positions);
                return;
            }

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
                positions.Add(beginStopPos);
                positions.Add(endStopPos);

                FinalizeRouteMesh(route, positions);
                return;
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

            positions.Add(endPos);
            FinalizeRouteMesh(route, positions);
        }

        /// Update a route mesh.
        void UpdateRouteMesh_(Route route)
        {
            const float offset = 5f;
            var positions = new List<Vector2>();

            var beginStop = route.beginStop;
            var endStop = route.endStop;

            var beginStopPos = beginStop.location;
            var endStopPos = endStop.location;

            // Case M: Same X / Y
            if (beginStopPos.x.Equals(endStopPos.x) || beginStopPos.y.Equals(endStopPos.y))
            {
                positions.Add(beginStopPos);
                positions.Add(endStopPos);

                FinalizeRouteMesh(route, positions);
                return;
            }

            // Find the destination quadrant.
            StopQuadrant dstQuadrant;
            if (endStopPos.x >= beginStopPos.x)
            {
                dstQuadrant = endStopPos.y >= beginStopPos.y ? StopQuadrant.TopRight : StopQuadrant.BottomRight;
            }
            else
            {
                dstQuadrant = endStopPos.y >= beginStopPos.y ? StopQuadrant.TopLeft : StopQuadrant.BottomLeft;
            }

            // Normalize the angle to be relative to the quadrant.
            var angle = Math.AngleFromHorizontalAxis(endStopPos - beginStopPos);
            switch (dstQuadrant)
            {
                case StopQuadrant.TopRight:
                    break;
                case StopQuadrant.TopLeft:
                    angle -= 90f;
                    break;
                case StopQuadrant.BottomLeft:
                    angle -= 180f;
                    break;
                case StopQuadrant.BottomRight:
                    angle -= 270f;
                    break;
            }

            // Find the destination location.
            StopLocation dstLocation;

            const float thresholdAngle = 45f;
            const float thresholdDistance = 50f;
            if (angle <= thresholdAngle)
            {
                var distanceFromXAxis = Mathf.Abs(endStopPos.y - beginStopPos.y);
                dstLocation = distanceFromXAxis <= thresholdDistance ? StopLocation.A : StopLocation.B;
            }
            else
            {
                var distanceFromYAxis = Mathf.Abs(endStopPos.x - beginStopPos.x);
                dstLocation = distanceFromYAxis <= thresholdDistance ? StopLocation.D : StopLocation.C;
            }

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

            float startOffset;
            switch (dstLocation)
            {
                default:
                case StopLocation.A:
                case StopLocation.D:
                    startOffset = Mathf.Abs(endStopPos.y - beginStopPos.y);
                    break;
                case StopLocation.B:
                case StopLocation.C:
                    startOffset = (endStopPos - beginStopPos).magnitude * (1f / 3f);
                    break;
            }

            switch (beginCardinal)
            {
                case CardinalDirection.North:
                    positions.Add(new Vector2(beginPos.x, beginPos.y + startOffset));
                    break;
                case CardinalDirection.South:
                    positions.Add(new Vector2(beginPos.x, beginPos.y - startOffset));
                    break;
                case CardinalDirection.East:
                    positions.Add(new Vector2(beginPos.x + startOffset, beginPos.y));
                    break;
                case CardinalDirection.West:
                    positions.Add(new Vector2(beginPos.x - startOffset, beginPos.y));
                    break;
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

            float endOffset;
            switch (dstLocation)
            {
                default:
                case StopLocation.A:
                case StopLocation.D:
                    endOffset = 0f;
                    break;
                case StopLocation.B:
                case StopLocation.C:
                    endOffset = (endStopPos - beginStopPos).magnitude * (1f / 3f);
                    break;
            }

            switch (endCardinal)
            {
                case CardinalDirection.North:
                    positions.Add(new Vector2(endPos.x, endPos.y + endOffset));
                    break;
                case CardinalDirection.South:
                    positions.Add(new Vector2(endPos.x, endPos.y - endOffset));
                    break;
                case CardinalDirection.East:
                    positions.Add(new Vector2(endPos.x + endOffset, endPos.y));
                    break;
                case CardinalDirection.West:
                    positions.Add(new Vector2(endPos.x - endOffset, endPos.y));
                    break;
            }

            positions.Add(endPos);
            FinalizeRouteMesh(route, positions);
        }
    }
}