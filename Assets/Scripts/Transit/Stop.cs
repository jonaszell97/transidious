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

            /// The next departure of the line at this stop.
            internal DateTime nextDeparture;

            /// The schedule for this line at this stop.
            internal ISchedule schedule;
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

        // A citizen waiting at this stop.
        public struct WaitingCitizen
        {
            /// The path that citizen is following.
            public ActivePath path;

            /// The line the citizen is waiting for.
            public Line waitingForLine;

            ///  The transit step that leads to this waiting citizen.
            public PublicTransitStep transitStep;

            /// The final stop on the route.
            public Stop finalStop;
        }

        public Map map;
        public Appearance appearance;

        public List<Route> outgoingRoutes;
        public List<Route> routes;
        public Dictionary<Line, LineData> lineData;

        public Dictionary<Line, List<WaitingCitizen>> waitingCitizens;

        int width = 1;
        int height = 1;
        public bool wasModified = false;

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
            this.waitingCitizens = new Dictionary<Line, List<WaitingCitizen>>();

            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            this.circleSprite = Resources.Load("Sprites/stop_ring", typeof(Sprite)) as Sprite;
            this.smallRectSprite = Resources.Load("Sprites/stop_small_rect", typeof(Sprite)) as Sprite;
            this.largeRectSprite = Resources.Load("Sprites/stop_large_rect", typeof(Sprite)) as Sprite;

            this.transform.position = new Vector3(position.x, position.y,
                                                  Map.Layer(MapLayer.TransitStops));
        }

        public Vector2 Location => location;

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

        public void AddWaitingCitizen(ActivePath path, PublicTransitStep step)
        {
            var line = step.line;
            if (!waitingCitizens.ContainsKey(line))
            {
                waitingCitizens.Add(line, new List<WaitingCitizen>());
            }

            waitingCitizens[line].Add(new WaitingCitizen
            {
                path = path,
                waitingForLine = line,
                transitStep = step,
                finalStop = step.routes.Last().endStop,
            });
        }

        public List<WaitingCitizen> GetWaitingCitizens(Line line)
        {
            if (!waitingCitizens.ContainsKey(line))
            {
                waitingCitizens.Add(line, new List<WaitingCitizen>());
            }

            return waitingCitizens[line];
        }

        public int TotalWaitingCitizens
        {
            get
            {
                return waitingCitizens.Sum(list => list.Value.Count);
            }
        }

        public ISchedule GetSchedule(Line line)
        {
            Debug.Assert(lineData.ContainsKey(line), "line does not stop here!");
            return lineData[line].schedule;
        }

        public DateTime NextDeparture(Line line, DateTime after)
        {
            Debug.Assert(lineData.ContainsKey(line), "line does not stop here!");
            return lineData[line].schedule.GetNextDeparture(after);
        }

        public void SetNextDeparture(Line line, DateTime dep)
        {
            Debug.Assert(lineData.ContainsKey(line), "line does not stop here!");
            lineData[line].nextDeparture = dep;
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
            }

            routes.Add(route);
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
            // spriteRenderer.size = new Vector2(line.lineWidth * 5f, map.input.lineWidth * 2f);
            spriteRenderer.color = color;

            var quat = Quaternion.FromToRotation(new Vector3(1f, 0f, 0f), direction);

            // this.transform.position = transform.position + (direction.normalized * (map.input.lineWidth * 1.5f));
            this.transform.rotation = quat;
            this.transform.position = new Vector3(transform.position.x,
                                                          transform.position.y,
                                                          Map.Layer(MapLayer.TransitStops));

            appearance = Appearance.SmallRect;
        }

        void CreateLargeRectMesh()
        {
            spriteRenderer.sprite = largeRectSprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            // spriteRenderer.size = new Vector2(GetSize(width), GetSize(height));
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

        public new Serialization.Stop ToProtobuf()
        {
            var result = new Serialization.Stop
            {
                MapObject = base.ToProtobuf(),
                Position = location.ToProtobuf(),
            };

            result.OutgoingRouteIDs.AddRange(outgoingRoutes.Select(r => (uint)r.Id));
            result.RouteIDs.AddRange(routes.Select(r => (uint)r.Id));
            result.Schedules.AddRange(lineData.Select(l => new Serialization.Stop.Types.StopSchedule
            {
                LineID = (uint) l.Key.Id,
                Schedule = l.Value.schedule?.Serialize(),
            }));

            return result;
        }

        public void Deserialize(Serialization.Stop stop, Map map)
        {
            base.Deserialize(stop.MapObject);
            outgoingRoutes = stop.OutgoingRouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();
            routes = stop.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();

            foreach (var sched in stop.Schedules)
            {
                var line = map.GetMapObject<Line>((int)sched.LineID);
                AddLine(line);

                if (sched.Schedule != null)
                {
                    SetSchedule(line, ContinuousSchedule.Deserialize(sched.Schedule));
                }
            }
        }

        void Awake()
        {
            this.spritePrefab = Resources.Load("Prefabs/SpritePrefab") as GameObject;
        }

        public void ActivateModal()
        {
            var modal = MainUI.instance.stopModal;
            modal.SetStop(this);
            modal.modal.Enable();
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            if (!Game.MouseDownActive(MapObjectKind.Stop))
            {
                return;
            }

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }
            
            if (MainUI.instance.stopModal.stop == this)
            {
                MainUI.instance.stopModal.modal.Disable();
                return;
            }

            ActivateModal();
        }
    }
}
