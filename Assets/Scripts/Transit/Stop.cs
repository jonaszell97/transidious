using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Transidious;
using Transidious.PathPlanning;

namespace Transidious
{
    public class Stop : DynamicMapObject, IStop
    {
        [System.Serializable]
        public class LineIntersectionInfo
        {
            /// The inbound angle of the intersection.
            public float inboundAngle = float.NaN;

            /// The outbound angle of the intersection.
            public float outboundAngle = float.NaN;
        }

        /// Data type used to store info about the lines that serve this stop.
        public class LineData
        {
            /// The incoming route from the direction of the line's depot stop.
            internal Route incomingRouteFromDepot = null;

            /// The incoming route from the direction of the line's last stop.
            internal Route incomingRouteToDepot = null;

            /// The outgoing route in the direction of the line's last stop.
            internal Route outgoingRouteFromDepot = null;

            /// The outgoing route in the direction of the line's depot stop.
            internal Route outgoingRouteToDepot = null;

            /// Intersection info about the line.
            internal LineIntersectionInfo intersectionInfo = new LineIntersectionInfo();

            /// The schedule for this line at this stop.
            internal ISchedule schedule;
        }

        [System.Serializable]
        public struct SerializableLineData
        {
            public int lineID;
            public int incomingRouteFromDepotID;
            public int incomingRouteToDepotID;
            public int outgoingRouteFromDepotID;
            public int outgoingRouteToDepotID;
            public LineIntersectionInfo intersectionInfo;
        }

        [System.Serializable]
        public struct SerializedStop
        {
            public SerializableMapObject mapObject;
            public SerializableVector2 position;
            public List<int> outgoingRouteIDs;
            public List<int> routeIDs;
            public List<SerializableLineData> lineDataSerial;
        }

        /// Describes the appearance of a stop depending on how many lines intersect
        /// at this stop.
        public enum Appearance
        {
            /// The stop is not visible.
            None,

            /// A circular stop used for when two lines intersect diagonally at the stop.
            Circle,

            /// A small rectangle for stops that are only served by a single line.
            SmallRect,

            /// A large round rectangle for stops with multiple lines coming in parallel
            /// from multiple directions.
            LargeRect,
        }

        public class Slot
        {
            /// The direction of the slot.
            public CardinalDirection direction;

            /// The slot number.
            public int number;

            /// The priority of this slot assignment.
            public int priority = 0;

            /// Reference to the slot assignment.
            internal PendingSlotAssignment assignment = null;
        }

        internal class PendingSlotAssignment
        {
            public Route route;
            public Route outRoute;

            public bool inbound;
            public int preferredSlot = -1;
            public bool downward = false;

            public int parallelPositionInbound = 0;
            public int parallelPositionOutbound = 0;

            /// Whether or not the outbound route is opposite the inbound route.
            public bool oppositeDirections;

            public CardinalDirection direction;
            public bool horizontal;

            /// The assigned slot of the route leaving the stop in the other direction.
            public Slot OutSlot
            {
                get
                {
                    if (inbound)
                        return outRoute?.beginSlot;

                    return outRoute?.endSlot;
                }
            }

            public Stop BeginStop
            {
                get
                {
                    if (inbound)
                    {
                        return route.endStop;
                    }

                    return route.beginStop;
                }
            }

            public Stop EndStop
            {
                get
                {
                    if (inbound)
                    {
                        return route.beginStop;
                    }

                    return route.endStop;
                }
            }

            public Stop PreviousStop
            {
                get
                {
                    if (inbound)
                    {
                        return outRoute?.endStop;
                    }
                    else
                    {
                        return outRoute?.beginStop;
                    }
                }
            }

            public Slot Slot
            {
                get
                {
                    if (inbound)
                    {
                        return route.endSlot;
                    }

                    return route.beginSlot;
                }
                set
                {
                    if (inbound)
                    {
                        route.endSlot = value;
                    }
                    else
                    {
                        route.beginSlot = value;
                    }
                }
            }

            /// The assigned slot of the route leaving the stop in the other direction.
            public Slot OtherSlot
            {
                get
                {
                    if (inbound)
                    {
                        return route.beginSlot;
                    }

                    return route.endSlot;
                }
            }


            public float value
            {
                get
                {
                    if (horizontal)
                    {
                        return y;
                    }

                    return x;
                }
            }

            public float x
            {
                get
                {
                    if (inbound)
                    {
                        return route.path.End.x;
                    }

                    return route.path.Start.x;
                }
            }

            public float y
            {
                get
                {
                    if (inbound)
                    {
                        return route.path.End.y;
                    }

                    return route.path.Start.y;
                }
            }
        }

        class ParallelRouteInfo
        {
            internal List<Route> parallelRoutes;
            internal int linesAbove = 0;
            internal int linesBelow = 0;
            internal CardinalDirection dir;
            internal bool downward = false;
        }

        public Map map;
        public Appearance appearance;

        public List<Route> outgoingRoutes;
        public List<Route> routes;
        public Dictionary<Line, LineData> lineData;

        int width = 1;
        int height = 1;
        public bool wasModified = false;

        List<PendingSlotAssignment> slotsToAssign;
        Dictionary<Stop, ParallelRouteInfo> parallelRoutes;
        Slot[,] slots;

        public float spacePerSlotVertical;
        public float spacePerSlotHorizontal;

        public GameObject spritePrefab;
        
        SpriteRenderer spriteRenderer;

        Sprite circleSprite;
        Sprite smallRectSprite;
        Sprite largeRectSprite;
        Vector3 direction;

        public Vector2 location
        {
            get { return transform.position; }
        }

        public void Initialize(Map map, string name, Vector3 position, int id)
        {
            base.Initialize(MapObjectKind.Line, id, position);

            this.map = map;
            this.name = name;
            this.appearance = Appearance.None;
            this.outgoingRoutes = new List<Route>();
            this.routes = new List<Route>();
            this.lineData = new Dictionary<Line, LineData>();
            this.slotsToAssign = new List<PendingSlotAssignment>();
            this.parallelRoutes = new Dictionary<Stop, ParallelRouteInfo>();

            this.spriteRenderer = this.GetComponent<SpriteRenderer>();

            this.circleSprite = Resources.Load("Sprites/stop_ring", typeof(Sprite)) as Sprite;
            this.smallRectSprite = Resources.Load("Sprites/stop_small_rect", typeof(Sprite)) as Sprite;
            this.largeRectSprite = Resources.Load("Sprites/stop_large_rect", typeof(Sprite)) as Sprite;

            this.transform.position = new Vector3(position.x, position.y,
                                                  Map.Layer(MapLayer.TransitStops));
        }

        public Vector3 Location
        {
            get
            {
                return location;
            }
        }

        public IEnumerable<IRoute> Routes
        {
            get
            {
                return outgoingRoutes.Select(s => s as IRoute);
            }
        }

        public bool IsGoalReached(IStop goal)
        {
            return goal is Stop && this == goal as Stop;
        }

        public bool uTurnAllowed
        {
            get
            {
                return false;
            }
        }

        public DateTime NextDeparture(Line line, DateTime after)
        {
            Debug.Assert(lineData.ContainsKey(line), "line does not stop here!");
            return lineData[line].schedule.GetNextDeparture(after);
            //var offset = line.scheduleOffsets[this];
            //return line.schedule.GetNextDeparture(after.AddMinutes(-offset)).AddMinutes(offset);
        }

        /// \return The incoming route from the direction of the line's depot stop.
        public Route GetIncomingRouteFromDepot(Line line)
        {
            if (lineData.TryGetValue(line, out LineData data))
            {
                return data.incomingRouteFromDepot;
            }

            return null;
        }

        /// \return The incoming route from the direction of the line's last stop.
        public Route GetIncomingRouteToDepot(Line line)
        {
            if (lineData.TryGetValue(line, out LineData data))
            {
                return data.incomingRouteToDepot;
            }

            return null;
        }

        /// \return The outgoing route in the direction of the line's last stop.
        public Route GetOutgoingRouteFromDepot(Line line)
        {
            if (lineData.TryGetValue(line, out LineData data))
            {
                return data.outgoingRouteFromDepot;
            }

            return null;
        }

        /// \return The outgoing route in the direction of the line's depot stop.
        public Route GetOutgoingRouteToDepot(Line line)
        {
            if (lineData.TryGetValue(line, out LineData data))
            {
                return data.outgoingRouteToDepot;
            }

            return null;
        }

        public void AddLine(Line line)
        {
            if (!lineData.ContainsKey(line))
            {
                lineData.Add(line, new LineData());
            }
        }

        LineData GetLineData(Line line)
        {
            if (!lineData.ContainsKey(line))
            {
                lineData.Add(line, new LineData());
            }

            return lineData[line];
        }

        public void SetSchedule(Line line, ISchedule schedule)
        {
            Debug.Assert(lineData.ContainsKey(line), "line does not stop here!");
            lineData[line].schedule = schedule;
        }

        /// Add an incoming a route to this stop.
        public void AddIncomingRoute(Route route)
        {
            Debug.Assert(route.endStop == this);

            LineData data = GetLineData(route.line);
            if (route.isBackRoute)
            {
                data.incomingRouteToDepot = route;
            }
            else
            {
                data.incomingRouteFromDepot = route;

                if (route.path != null)
                    data.intersectionInfo.inboundAngle = route.path.EndAngle;
            }

            routes.Add(route);
        }

        /// Add an outgoing route to this stop.
        public void AddOutgoingRoute(Route route)
        {
            Debug.Assert(route.beginStop == this);
            outgoingRoutes.Add(route);

            LineData data = GetLineData(route.line);
            if (route.isBackRoute)
            {
                data.outgoingRouteToDepot = route;
            }
            else
            {
                data.outgoingRouteFromDepot = route;

                if (route.path != null)
                    data.intersectionInfo.outboundAngle = route.path.BeginAngle;
            }

            routes.Add(route);
        }

        public Vector3 GetSlotLocation(Route route, Slot slot)
        {
            if (height == 1 && width == 1)
            {
                return transform.position;
            }
            if (true)
            {
                return transform.position;
            }

            Vector3 loc = transform.position;
            float halfHeight = spriteRenderer.size.y * 0.5f;
            float halfWidth = spriteRenderer.size.x * 0.5f;
            bool vertical = false;

            // Move to the correct side.
            switch (slot.direction)
            {
            case CardinalDirection.North:
                if (width == 1)
                {
                    return new Vector3(transform.position.x,
                                       transform.position.y + halfHeight);
                }

                loc.y += halfHeight;
                loc.x -= halfWidth;
                break;
            case CardinalDirection.South:
                if (width == 1)
                {
                    return new Vector3(transform.position.x,
                                       transform.position.y - halfHeight);
                }

                loc.y -= halfHeight;
                loc.x -= halfWidth;
                break;
            case CardinalDirection.East:
                if (height == 1)
                {
                    return new Vector3(transform.position.x + halfWidth,
                                       transform.position.y);
                }

                loc.x += halfWidth;
                loc.y -= halfHeight;
                vertical = true;
                break;
            case CardinalDirection.West:
                if (height == 1)
                {
                    return new Vector3(transform.position.x - halfWidth,
                                       transform.position.y);
                }

                loc.x -= halfWidth;
                loc.y -= halfHeight;
                vertical = true;
                break;
            }

            // Move up or down depending on the slot.
            float halfStopWidth = map.input.stopWidth / 2f;
            UpdateSlotSizes();

            if (vertical)
            {
                loc.y += halfStopWidth + (spacePerSlotVertical - map.input.lineWidth * 2f) * 0.5f
                        + (slot.number * (spacePerSlotVertical - map.input.lineWidth * 2f))
                        + (slot.number * (map.input.lineWidth * 2f));
            }
            else
            {
                loc.x += halfStopWidth + (spacePerSlotHorizontal - map.input.lineWidth * 2f) * 0.5f
                        + (slot.number * (spacePerSlotHorizontal - map.input.lineWidth * 2f))
                        + (slot.number * (map.input.lineWidth * 2f));
            }

            return loc;
        }

        void CreateCircleMesh()
        {
            height = 1;
            width = 1;

            spriteRenderer.sprite = circleSprite;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.color = Color.white;

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Destroy(collider);
            }

            this.transform.rotation = new Quaternion();
            this.transform.localScale = new Vector3(5f, 5f, 1f);
            this.transform.position = new Vector3(transform.position.x,
                                                  transform.position.y,
                                                  Map.Layer(MapLayer.TransitStops));

            this.gameObject.AddComponent<CircleCollider2D>();
            this.appearance = Appearance.Circle;
        }

        Vector3 VectorFromAngle(float theta)
        {
            return new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f);
        }

        void CreateSmallRectMesh(Color color, Vector3 direction)
        {
            this.direction = direction;

            spriteRenderer.sprite = smallRectSprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(map.input.lineWidth * 5f, map.input.lineWidth * 2f);
            spriteRenderer.color = color;

            var quat = Quaternion.FromToRotation(new Vector3(1f, 0f, 0f), direction);

            this.transform.position = transform.position + (direction.normalized * (map.input.lineWidth * 1.5f));
            this.transform.rotation = quat;
            this.transform.position = new Vector3(transform.position.x,
                                                          transform.position.y,
                                                          Map.Layer(MapLayer.TransitStops));

            appearance = Appearance.SmallRect;
        }

        float GetSize(int stops)
        {
            switch (stops)
            {
            case 0: case 1: return .5f;
            default: return map.input.stopWidth + stops * (map.input.stopWidth * 0.5f);
            }
        }

        void CreateLargeRectMesh()
        {
            spriteRenderer.sprite = largeRectSprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(GetSize(width), GetSize(height));
            spriteRenderer.color = Color.white;

            this.transform.rotation = new Quaternion();
            this.transform.localScale = Vector3.one;
            this.transform.position = new Vector3(transform.position.x,
                                                          transform.position.y,
                                                          Map.Layer(MapLayer.TransitStops));

            if (width == 1 && height == 1)
            {
                this.appearance = Appearance.Circle;
            }
            else
            {
                this.appearance = Appearance.LargeRect;
            }
        }

        bool SameDirection(float angle1, float angle2)
        {
            while (angle1 >= 180f)
            {
                angle1 -= 180f;
            }
            while (angle2 >= 180f)
            {
                angle2 -= 180f;
            }

            return angle1.Equals(angle2);
        }

        Vector2 GetPerpendicularVector(Vector2 v)
        {
            return new Vector2(v.y, 0 - v.x);
        }

        PendingSlotAssignment GetSlotAssignment(Route route, Route outRoute,
                                                bool inbound,
                                                ref int top, ref int bottom,
                                                ref int left, ref int right)
        {
            float angle;

            Stop nextStop;
            Stop prevStop;

            if (inbound)
            {
                angle = route.originalPath.segments.Last().Angle;
                nextStop = route.beginStop;
                prevStop = outRoute?.endStop;
            }
            else
            {
                angle = route.originalPath.segments.First().Angle;
                nextStop = route.endStop;
                prevStop = outRoute?.beginStop;
            }

            var dir = Math.ClassifyDirection(angle);
            bool opposite = false;

            if (outRoute)
            {
                float outAngle;
                if (inbound)
                {
                    outAngle = outRoute.originalPath.segments.First().Angle;
                }
                else
                {
                    outAngle = outRoute.originalPath.segments.Last().Angle;
                }

                var outDir = Math.ClassifyDirection(outAngle);
                opposite = outDir == dir || outDir == Math.Reverse(dir);
            }

            /*float thisCoord;
            float nextCoord;
            float prevCoord;

            if (dir.IsHorizontal())
            {
                thisCoord = transform.position.y;
                nextCoord = nextStop.transform.position.y;
                prevCoord = (prevStop != null) ? prevStop.transform.position.y : thisCoord;
            }
            else
            {
                thisCoord = transform.position.x;
                nextCoord = nextStop.transform.position.x;
                prevCoord = (prevStop != null) ? prevStop.transform.position.x : thisCoord;
            }

            int cmp1 = thisCoord.CompareTo(nextCoord);
            int cmp2 = thisCoord.CompareTo(prevCoord);

            if (cmp1 != 0 && cmp2 != 0 && cmp1 != cmp2)
            {
                if (inbound)
                {
                    // dir = dir.RotatedLeft();
                }
                else
                {
                   // dir = dir.RotatedRight();
                }
            }*/

            switch (dir)
            {
            case CardinalDirection.North:
                ++top;
                return new PendingSlotAssignment
                {
                    direction = inbound ? Math.Reverse(dir) : dir,
                    route = route,
                    outRoute = outRoute,
                    inbound = inbound,
                    horizontal = false,
                    oppositeDirections = opposite
                };
            case CardinalDirection.South:
                ++bottom;
                return new PendingSlotAssignment
                {
                    direction = inbound ? Math.Reverse(dir) : dir,
                    route = route,
                    outRoute = outRoute,
                    inbound = inbound,
                    horizontal = false,
                    oppositeDirections = opposite
                };
            case CardinalDirection.East:
                ++right;
                return new PendingSlotAssignment
                {
                    direction = inbound ? Math.Reverse(dir) : dir,
                    route = route,
                    outRoute = outRoute,
                    inbound = inbound,
                    horizontal = true,
                    oppositeDirections = opposite
                };
            case CardinalDirection.West:
                ++left;
                return new PendingSlotAssignment
                {
                    direction = inbound ? Math.Reverse(dir) : dir,
                    route = route,
                    outRoute = outRoute,
                    inbound = inbound,
                    horizontal = true,
                    oppositeDirections = opposite
                };
            default:
                throw new System.ArgumentException(string.Format("Illegal enum value {0}", dir));
            }
        }

        void UpdateSlotSizes()
        {
            float halfStopWidth = map.input.stopWidth / 2f;
            if (height > 1)
            {
                float spaceForStopsY = spriteRenderer.size.y - map.input.stopWidth;
                this.spacePerSlotVertical = spaceForStopsY / height;
            }
            else
            {
                this.spacePerSlotVertical = map.input.stopWidth;
            }

            if (width > 1)
            {
                float spaceForStopsX = spriteRenderer.size.x - map.input.stopWidth;
                this.spacePerSlotHorizontal = spaceForStopsX / width;
            }
            else
            {
                this.spacePerSlotHorizontal = map.input.stopWidth;
            }
        }

        void FindParallelRoutes()
        {
            foreach (var route in routes)
            {
                if (route.isBackRoute)
                {
                    continue;
                }

                Stop nextStop;
                CardinalDirection dir;

                if (route.beginStop == this)
                {
                    nextStop = route.endStop;

                    float angle = route.originalPath.segments.First().Angle;
                    dir = Math.ClassifyDirection(angle);
                }
                else
                {
                    nextStop = route.beginStop;

                    float angle = route.originalPath.segments.Last().Angle;
                    dir = Math.Reverse(Math.ClassifyDirection(angle));
                }

                float thisCoord;
                float nextCoord;
                if (dir.IsHorizontal())
                {
                    thisCoord = transform.position.y;
                    nextCoord = nextStop.transform.position.y;
                }
                else
                {
                    thisCoord = transform.position.x;
                    nextCoord = nextStop.transform.position.x;
                }

                int cmp1 = thisCoord.CompareTo(nextCoord);
                bool downward;

                if (dir.IsHorizontal())
                {
                    downward = cmp1 > 0;
                }
                else
                {
                    downward = cmp1 < 0;
                }

                if (parallelRoutes.TryGetValue(nextStop, out ParallelRouteInfo info))
                {
                    info.parallelRoutes.Add(route);
                }
                else
                {
                    parallelRoutes.Add(nextStop, new ParallelRouteInfo
                    {
                        parallelRoutes = new List<Route> { route },
                        dir = dir,
                        downward = downward
                    });
                }
            }

            foreach (var infoPair in parallelRoutes)
            {
                var stop = infoPair.Key;
                var info = infoPair.Value;

                foreach (var route in routes)
                {
                    if (route.isBackRoute || route.beginStop == stop || route.endStop == stop)
                    {
                        continue;
                    }

                    Stop nextStopOnLine;
                    CardinalDirection dir;

                    if (this == route.endStop)
                    {
                        nextStopOnLine = route.beginStop;
                        dir = Math.Reverse(Math.ClassifyDirection(route.originalPath.EndAngle));
                    }
                    else
                    {
                        nextStopOnLine = route.endStop;
                        dir = Math.ClassifyDirection(route.originalPath.BeginAngle);
                    }

                    if (dir != info.dir)
                    {
                        continue;
                    }

                    float thisCoord;
                    float nextCoord;
                    if (dir.IsHorizontal())
                    {
                        thisCoord = transform.position.y;
                        nextCoord = nextStopOnLine.transform.position.y;
                    }
                    else
                    {
                        thisCoord = transform.position.x;
                        nextCoord = nextStopOnLine.transform.position.x;
                    }

                    int cmp1 = thisCoord.CompareTo(nextCoord);
                    if (cmp1 > 0)
                    {
                        ++info.linesBelow;
                    }
                    else if (cmp1 < 0)
                    {
                        ++info.linesAbove;
                    }
                }
            }
        }

        void UpdateSlotsToAssign()
        {
            int top = 0;
            int bottom = 0;
            int right = 0;
            int left = 0;

            slotsToAssign.Clear();
            parallelRoutes.Clear();

            FindParallelRoutes();

            foreach (var routeSet in parallelRoutes)
            {
                var i = 0;
                var count = routeSet.Value.parallelRoutes.Count;
                var single = count == 1;

                foreach (var route in routeSet.Value.parallelRoutes)
                {
                    var data = lineData[route.line];

                    PendingSlotAssignment assignment;
                    if (this == route.beginStop)
                    {
                        assignment = GetSlotAssignment(route, data.incomingRouteFromDepot,
                                                       false, ref top, ref bottom,
                                                       ref left, ref right);
                    }
                    else
                    {
                        assignment = GetSlotAssignment(route, data.outgoingRouteFromDepot,
                                                       true, ref bottom, ref top,
                                                       ref right, ref left);
                    }

                    if (name == "S Storkower Straße")
                    {
                        // Debug.Break();
                    }

                    if (single)
                    {
                        assignment.preferredSlot = 1;
                    }
                    else
                    {
                        assignment.preferredSlot = -i - routeSet.Value.linesAbove;
                    }

                    bool downward;
                    if (assignment.horizontal)
                    {
                        downward = routeSet.Value.downward;
                    }
                    else
                    {
                        downward = !routeSet.Value.downward;
                    }

                    if (downward)
                    {
                        if (assignment.inbound)
                        {
                            assignment.parallelPositionInbound = i;
                            assignment.parallelPositionOutbound = count - i - 1;
                        }
                        else
                        {
                            assignment.parallelPositionOutbound = i;
                            assignment.parallelPositionInbound = count - i - 1;
                        }

                        assignment.downward = true;
                    }
                    else
                    {
                        if (assignment.inbound)
                        {
                            assignment.parallelPositionInbound = count - i - 1;
                            assignment.parallelPositionOutbound = i;
                        }
                        else
                        {
                            assignment.parallelPositionOutbound = count - i - 1;
                            assignment.parallelPositionInbound = i;
                        }

                        assignment.downward = false;
                    }

                    slotsToAssign.Add(assignment);
                    ++i;
                }
            }

            this.height = System.Math.Max(1, System.Math.Max(left, right));
            this.width = System.Math.Max(1, System.Math.Max(top, bottom));

            this.slots = new Slot[4, System.Math.Max(width, height)];
            UpdateSlotSizes();

            foreach (var assignment in slotsToAssign)
            {
                if (assignment.preferredSlot == 1)
                {
                    assignment.preferredSlot = -1;
                }
                else
                {
                    assignment.preferredSlot += (assignment.horizontal ? height : width) - 1;
                }

                if (assignment.preferredSlot < -1)
                {
                    //Debug.Break();
                }
            }
        }

        enum AssignmentReason
        {
            Position, PreferredSlot, OutgoingSlot, PreviousSlot,
        }

        bool TryAssignSlot(HashSet<Stop> worklist,
                           HashSet<Route> routesToUpdate,
                           PendingSlotAssignment a,
                           int preferredSlot, int priority,
                           AssignmentReason reason,
                           bool recurse = true)
        {
            Slot slot = slots[(int)a.direction, preferredSlot];
            Slot outSlot = a.OutSlot;

            // Assign the slot.
            if (slot == null)
            {
                a.Slot = new Slot
                {
                    direction = a.direction,
                    number = preferredSlot,
                    priority = priority,
                    assignment = a
                };

                slots[(int)a.direction, preferredSlot] = a.Slot;
                /*Debug.Log("[" + name + "] assigned slot " + a.direction.ToString()
                          + preferredSlot + " to " + a.route.name
                          + " based on " + reason.ToString()
                          + " (priority: " + priority + ")");*/

                routesToUpdate.Add(a.route);
                worklist.Add(a.EndStop);

                if (recurse && outSlot != null && outSlot.assignment != null)
                {
                    int preferredOutSlot = GetPreferredSlot(outSlot.assignment, a.Slot);
                    if (preferredOutSlot != -1 && IsUsable(outSlot.assignment, preferredOutSlot, priority))
                    {
                        RemoveSlotAssignment(outSlot.assignment);
                        AssignNextAvailableSlot(worklist, routesToUpdate,
                                                outSlot.assignment, preferredOutSlot, priority,
                                                AssignmentTryOrder.Decreasing,
                                                AssignmentReason.OutgoingSlot, false);
                    }
                }

                return true;
            }

            // Don't override a an assignment with the same or higher priority.
            if (slot.priority >= priority)
            {
                return false;
            }

            /*Debug.Log("[" + name + "] reassigned slot " + a.direction.ToString()
                          + preferredSlot + " to " + a.route.name
                          + " based on " + reason.ToString()
                          + " (previous priority: " + slot.priority
                          + ", new priority: " + priority + ")");*/

            // Override a lower priority assignment.
            slotsToAssign.Add(slot.assignment);
            worklist.Add(slot.assignment.EndStop);

            slot.priority = priority;
            slot.assignment.Slot = null;
            slot.assignment = a;

            a.Slot = slot;
            routesToUpdate.Add(a.route);

            if (recurse && outSlot != null && outSlot.assignment != null)
            {
                int preferredOutSlot = GetPreferredSlot(outSlot.assignment, a.Slot);
                if (preferredOutSlot != -1 && IsUsable(outSlot.assignment, preferredOutSlot, priority))
                {
                    RemoveSlotAssignment(outSlot.assignment);
                    AssignNextAvailableSlot(worklist, routesToUpdate,
                                            outSlot.assignment, preferredOutSlot, priority,
                                            AssignmentTryOrder.Decreasing,
                                            AssignmentReason.OutgoingSlot, false);
                }
            }

            return true;
        }

        enum AssignmentTryOrder
        {
            Increasing, Decreasing
        }

        void AssignNextAvailableSlot(HashSet<Stop> worklist,
                                     HashSet<Route> routesToUpdate,
                                     PendingSlotAssignment a,
                                     int preferredSlot, int priority,
                                     AssignmentTryOrder order,
                                     AssignmentReason reason,
                                     bool recurse = true)
        {
            bool switchedOrder = false;
            int currentSlot = preferredSlot;
            int maxSlot = (a.horizontal ? height : width) - 1;

            while (!IsUsable(a, currentSlot, priority))
            {
                if (order == AssignmentTryOrder.Increasing)
                {
                    ++currentSlot;
                    if (currentSlot > maxSlot)
                    {
                        if (switchedOrder)
                        {
                            Debug.LogError("No slot available!");
                            return;
                        }

                        currentSlot = preferredSlot;
                        order = AssignmentTryOrder.Decreasing;
                        switchedOrder = true;
                    }
                }
                else
                {
                    --currentSlot;
                    if (currentSlot < 0)
                    {
                        if (switchedOrder)
                        {
                            Debug.LogError("No slot available!");
                            return;
                        }

                        currentSlot = preferredSlot;
                        order = AssignmentTryOrder.Increasing;
                        switchedOrder = true;
                    }
                }
            }

            bool success = TryAssignSlot(worklist, routesToUpdate, a, currentSlot,
                                         priority, reason, recurse);

            Debug.Assert(success, "no empty slot available!");
        }

        bool IsUsable(PendingSlotAssignment a, int preferredSlot, int priority)
        {
            Slot s = slots[(int)a.direction, preferredSlot];
            return s == null || s.priority < priority;
        }

        void RemoveSlotAssignment(PendingSlotAssignment assignment)
        {
            int size = System.Math.Max(width, height);
            for (int i = 0; i < size; ++i)
            {
                Slot s = slots[(int)assignment.direction, i];
                if (s != null && s.assignment == assignment)
                {
                    slots[(int)assignment.direction, i] = null;
                    break;
                }
            }

            // slotsToAssign.Add(assignment);
        }

        int GetPreferredSlot(PendingSlotAssignment a, Slot outgoingSlot,
                             Stop nextStop)
        {
            return System.Math.Min(outgoingSlot.number, (a.horizontal ? height : width) - 1);
        }

        int GetPreferredSlot(PendingSlotAssignment a, Slot outgoingSlot)
        {
            if (a.direction == outgoingSlot.assignment.direction)
            {
                return -1;
            }

            var outDir = outgoingSlot.assignment.direction;
            switch (a.direction)
            {
            case CardinalDirection.South:
            case CardinalDirection.North:
                switch (outDir)
                {
                case CardinalDirection.North:
                case CardinalDirection.South:
                    return System.Math.Min(width - 1, outgoingSlot.number);
                case CardinalDirection.East:
                case CardinalDirection.West:
                    return Mathf.Clamp(width - outgoingSlot.number, 0, width - 1);
                }

                break;
            case CardinalDirection.East:
            case CardinalDirection.West:
                switch (outDir)
                {
                case CardinalDirection.East:
                case CardinalDirection.West:
                    return System.Math.Min(height - 1, outgoingSlot.number);
                case CardinalDirection.North:
                case CardinalDirection.South:
                    return Mathf.Clamp(height - outgoingSlot.number, 0, height - 1);
                }

                break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", a.direction));
        }

        void UpdateSlotAssignments(HashSet<Stop> worklist,
                                   HashSet<Route> routesToUpdate)
        {
            var originalCount = slotsToAssign.Count;
            for (int i = 0; i < slotsToAssign.Count; ++i)
            {
                var isRetry = i >= originalCount;
                PendingSlotAssignment a = slotsToAssign[i];

                Stop nextStop = a.EndStop;
                Stop prevStop = a.PreviousStop;

                float thisCoord;
                float nextCoord;
                float prevCoord;
                int nextSize;

                if (a.horizontal)
                {
                    thisCoord = transform.position.y;
                    nextCoord = nextStop.transform.position.y;
                    prevCoord = (prevStop != null) ? prevStop.transform.position.y : thisCoord;
                    nextSize = nextStop.height;
                }
                else
                {
                    thisCoord = transform.position.x;
                    nextCoord = nextStop.transform.position.x;
                    prevCoord = (prevStop != null) ? prevStop.transform.position.x : thisCoord;
                    nextSize = nextStop.width;
                }

                int cmp1 = thisCoord.CompareTo(nextCoord);
                int cmp2 = thisCoord.CompareTo(prevCoord);

                // If the route leaves in the opposite direction and has
                // an assigned slot, that takes precedence.
                /*if (a.outRoute != null)
                {
                    Slot otherSlot = a.OutSlot;
                    if (otherSlot != null)
                    {
                        int priority = System.Math.Min(2, otherSlot.priority);
                        int preferredSlot = GetPreferredSlot(a, otherSlot);

                        var order = (cmp1 < 0 || cmp2 < 0) ?
                            AssignmentTryOrder.Increasing
                            : AssignmentTryOrder.Decreasing;

                        if (preferredSlot != -1)
                        {
                            if (!isRetry && !IsUsable(a, preferredSlot, priority))
                            {
                                slotsToAssign.Add(a);
                                continue;
                            }

                            AssignNextAvailableSlot(worklist, routesToUpdate,
                                                    a, preferredSlot, priority, order,
                                                    AssignmentReason.OutgoingSlot);
                            continue;
                        }
                    }
                }*/

                // Check if there is a preferred slot for this route.
                if (a.preferredSlot != -1)
                {
                    if (!isRetry && !IsUsable(a, a.preferredSlot, 3))
                    {
                        slotsToAssign.Add(a);
                        continue;
                    }

                    AssignNextAvailableSlot(worklist, routesToUpdate,
                                            a, a.preferredSlot, 3,
                                            AssignmentTryOrder.Decreasing,
                                            AssignmentReason.PreferredSlot);

                    a.preferredSlot = -1;
                    continue;
                }

                // Find the lowest available slot.
                if (cmp1 < 0)
                {
                    int priority = (cmp2 > 0) ? 0 : 1;
                    int preferredSlot = 0;

                    AssignNextAvailableSlot(worklist, routesToUpdate, a,
                                            preferredSlot, priority,
                                            AssignmentTryOrder.Increasing,
                                            AssignmentReason.Position);
                }
                else
                {
                    int priority = (cmp2 > 0) ? 0 : 1;
                    int preferredSlot = (a.horizontal ? height : width) - 1;

                    AssignNextAvailableSlot(worklist, routesToUpdate, a,
                                            preferredSlot, priority,
                                            AssignmentTryOrder.Decreasing,
                                            AssignmentReason.Position);
                }
            }

            slotsToAssign.Clear();
        }

        public void UpdateMesh(bool force = false, bool fullUpdate = true)
        {
            if (!force && !wasModified)
            {
                return;
            }

            wasModified = false;

            CreateCircleMesh();

            /*
            // Simplest case, only a single line stops at this stop.
            if (lineData.Count == 0)
            {
                CreateCircleMesh();
            }
            else if (lineData.Count == 1)
            {
                var keyValuePair = lineData.First();
                var data = keyValuePair.Value;
                var line = keyValuePair.Key;

                if (data == null)
                {
                    CreateCircleMesh();
                    return;
                }

                var inboundRoute = data.incomingRouteFromDepot;
                var outboundRoute = data.outgoingRouteFromDepot;

                if (inboundRoute == null && outboundRoute == null)
                {
                    return;
                }
                else if (inboundRoute == null)
                {
                    CreateSmallRectMesh(line.color, GetPerpendicularVector(outboundRoute.path.segments.First().Direction));
                    return;
                }
                else if (outboundRoute == null)
                {
                    CreateSmallRectMesh(line.color, GetPerpendicularVector(inboundRoute.path.segments.Last().Direction));
                    return;
                }

                var inboundVector = inboundRoute.path.segments.Last().Direction;
                var outboundVector = outboundRoute.path.segments.First().Direction;

                var angle = Math.Angle(inboundVector, outboundVector);
                var mid = inboundVector.normalized + outboundVector.normalized;

                if (angle >= 0)
                {
                    CreateSmallRectMesh(line.color, GetPerpendicularVector(mid));
                }
                else
                {
                    CreateSmallRectMesh(line.color, GetPerpendicularVector(mid * -1));
                }

                return;
            }
            else
            {
                UpdateSlotsToAssign();
                CreateLargeRectMesh();

                if (!fullUpdate)
                {
                    return;
                }

                var routesToUpdate = new HashSet<Route>();
                var worklist = new HashSet<Stop> { this };

                int i = 0;
                while (worklist.Count != 0)
                {
                    var next = worklist.First();
                    worklist.Remove(next);

                    if (next != this)
                    {
                        next.UpdateMesh(false, false);
                    }

                    next.UpdateSlotAssignments(worklist, routesToUpdate);

                    if (i++ > 1500)
                    {
                        throw new System.Exception("infinite loop");
                    }
                }

                foreach (var route in routesToUpdate)
                {
                    route.UpdatePath();
                }
            }
            */
        }

        public void UpdateScale()
        {
            UpdateSlotSizes();

            if (appearance == Appearance.SmallRect)
            {
                spriteRenderer.size = new Vector2(map.input.lineWidth * 5f, map.input.lineWidth * 2f);
                this.transform.position = transform.position + (direction.normalized * (map.input.lineWidth * 1.5f));
                this.transform.position = new Vector3(this.transform.position.x,
                                                      this.transform.position.y,
                                                      Map.Layer(MapLayer.TransitStops));
            }
        }

        public new Serialization.Stop ToProtobuf()
        {
            var result = new Serialization.Stop
            {
                MapObject = base.ToProtobuf(),
                Position = location.ToProtobuf(),
            };

            result.OutgoingRouteIDs.AddRange(outgoingRoutes.Select(r => (uint)r.Id));
            result.RouteIDs.AddRange(routes.Select(r => (uint)r.Id));

            return result;
        }

        public void Deserialize(Serialization.Stop stop, Map map)
        {
            base.Deserialize(stop.MapObject);
            transform.position = stop.Position.Deserialize();

            outgoingRoutes = stop.OutgoingRouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();
            routes = stop.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();
        }

        public new SerializedStop Serialize()
        {
            return new SerializedStop
            {
                mapObject = base.Serialize(),
                position = new SerializableVector2(transform.position),

                routeIDs = routes.Select(r => r.id).ToList(),
                outgoingRouteIDs = outgoingRoutes.Select(r => r.id).ToList(),
                lineDataSerial = lineData.Select(d => new SerializableLineData
                {
                    lineID = d.Key.id,
                    outgoingRouteFromDepotID = d.Value.outgoingRouteFromDepot?.id ?? 0,
                    outgoingRouteToDepotID = d.Value.outgoingRouteToDepot?.id ?? 0,
                    incomingRouteToDepotID = d.Value.incomingRouteToDepot?.id ?? 0,
                    incomingRouteFromDepotID = d.Value.incomingRouteFromDepot?.id ?? 0,
                    intersectionInfo = d.Value.intersectionInfo
                }).ToList()
            };
        }

        public void Deserialize(SerializedStop stop, Map map)
        {
            base.Deserialize(stop.mapObject);

            foreach (var data in stop.lineDataSerial)
            {
                lineData.Add(map.GetMapObject<Line>(data.lineID), new LineData
                {
                    incomingRouteFromDepot = map.GetMapObject<Route>(data.incomingRouteFromDepotID),
                    incomingRouteToDepot = map.GetMapObject<Route>(data.incomingRouteToDepotID),
                    outgoingRouteFromDepot = map.GetMapObject<Route>(data.outgoingRouteFromDepotID),
                    outgoingRouteToDepot = map.GetMapObject<Route>(data.outgoingRouteToDepotID),
                    intersectionInfo = data.intersectionInfo
                });
            }

            routes = stop.routeIDs.Select(id => map.GetMapObject<Route>(id)).ToList();
            outgoingRoutes = stop.outgoingRouteIDs.Select(id => map.GetMapObject<Route>(id)).ToList();

            wasModified = true;
        }

        void Awake()
        {
            this.spritePrefab = Resources.Load("Prefabs/SpritePrefab") as GameObject;
        }

        public void ActivateModal()
        {
            var modal = GameController.instance.transitEditor.stopInfoModal;
            modal.SetStop(this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.EnableAt(pos);
        }

        public override void OnMouseDown()
        {
            if (!Game.MouseDownActive(MapObjectKind.Stop))
            {
                return;
            }

            base.OnMouseDown();

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            ActivateModal();
        }
    }
}
