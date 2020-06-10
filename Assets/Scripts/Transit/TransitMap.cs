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
            UpdateAppearances(new HashSet<Stop>(GameController.instance.loadedMap.transitStops));
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
            var routesToReassign = new List<Tuple<Route, Stop, SlotAssignment>>();
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
                    firstAssignment = AssignSlot(route, from, prevRoute, prevAssignment);
                    if (prevRoute != null && firstAssignment != null && !firstAssignment.Item2)
                    {
                        routesToReassign.Add(Tuple.Create(prevRoute, from, firstAssignment.Item1));
                    }
                }

                if (!stops.Contains(to))
                {
                    continue;
                }

                var sndAssignment = AssignSlot(route, to, nextRoute, firstAssignment?.Item1);
                if (sndAssignment != null && !sndAssignment.Item2)
                {
                    routesToReassign.Add(Tuple.Create(route, from, sndAssignment.Item1));
                }
            }

            foreach (var (route, stop, slot) in routesToReassign)
            {
                slot.Route = null;
                _routeSlotAssignmentMap.Remove(Tuple.Create(route, stop));

                AssignSlot(route, stop, null, slot);
            }
        }

        /// Update a route mesh.
        void UpdateRouteMesh(Route route)
        {
            const float offset = 5f;

            var positions = new List<Vector2>();
            if (_routeSlotAssignmentMap.TryGetValue(Tuple.Create(route, route.beginStop), out var fromAssignment))
            {
                var beginPos = GetSlotPosition(route.beginStop, fromAssignment, true);
                positions.Add(beginPos);

                switch (fromAssignment.SlotDirection)
                {
                    case CardinalDirection.North:
                        positions.Add(new Vector2(beginPos.x, beginPos.y + offset));
                        break;
                    case CardinalDirection.South:
                        positions.Add(new Vector2(beginPos.x, beginPos.y - offset));
                        break;
                    case CardinalDirection.East:
                        positions.Add(new Vector2(beginPos.x + offset, beginPos.y));
                        break;
                    case CardinalDirection.West:
                        positions.Add(new Vector2(beginPos.x - offset, beginPos.y));
                        break;
                }
            }
            else
            {
                positions.Add(route.beginStop.location);
            }

            if (_routeSlotAssignmentMap.TryGetValue(Tuple.Create(route, route.endStop), out var toAssignment))
            {
                var endPos = GetSlotPosition(route.endStop, toAssignment, true);
                switch (toAssignment.SlotDirection)
                {
                    case CardinalDirection.North:
                        positions.Add(new Vector2(endPos.x, endPos.y + offset));
                        break;
                    case CardinalDirection.South:
                        positions.Add(new Vector2(endPos.x, endPos.y - offset));
                        break;
                    case CardinalDirection.East:
                        positions.Add(new Vector2(endPos.x + offset, endPos.y));
                        break;
                    case CardinalDirection.West:
                        positions.Add(new Vector2(endPos.x - offset, endPos.y));
                        break;
                }

                positions.Add(endPos);
            }
            else
            {
                positions.Add(route.endStop.location);
            }

            var collider = route.GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            var mesh = MeshBuilder.CreateSmoothLine(positions, route.line.LineWidth * .3f, 20, 0f, collider);
            route.UpdateMesh(mesh, positions, null);
            route.EnableCollision();
            route.positions = positions;
        }

        /// Assign a desired slot to a route.
        Tuple<SlotAssignment, bool>
        AssignSlot(Route route, Stop stop, Route opposite, SlotAssignment oppositeAssignment = null)
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
                result = AssignSlot(route, stop, inCardinal, assignments, desiredSlot, false);
            }
            else
            {
                var outCardinal = GetCardinalDirection(outDirection.Value);
                var backward = false;

                switch (inCardinal)
                {
                    case CardinalDirection.West:
                    case CardinalDirection.East:
                        if (outCardinal == CardinalDirection.South)
                        {
                            backward = true;
                        }

                        break;
                    case CardinalDirection.North:
                    case CardinalDirection.South:
                        if (outCardinal == CardinalDirection.West)
                        {
                            backward = true;
                        }

                        break;
                }

                result = AssignSlot(route, stop, inCardinal, assignments, desiredSlot, backward);
            }

            return Tuple.Create(result, result?.SlotIndex == desiredSlot);
        }

        /// Assign a desired slot.
        SlotAssignment AssignSlot(Route route, Stop stop, CardinalDirection direction,
                                  SlotAssignment[][] assignments,
                                  int desiredSlot, bool backward)
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
                    var oppositeSlot = assignments[(int) direction.Opposite()][i];
                    oppositeSlot.ReservedForLine = route.line;

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
    }
}