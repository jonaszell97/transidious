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

        /// Reference to the map.
        public Map map;
        
        /// The routes beginning at this stop.
        public List<Route> outgoingRoutes;

        /// The routes ending or beginning at this stop.
        public List<Route> routes;

        /// The 'opposite' stop of this one, i.e. the one on the other side of the street that logically belongs to
        /// this one (only applicable to bus and tram lines).
        public Stop oppositeStop;

        /// Info about lines stopping here.
        public Dictionary<Line, LineData> lineData;

        /// Citizens that are waiting at this stop.
        public Dictionary<Line, List<WaitingCitizen>> waitingCitizens;

        /// The stops current appearance type.
        public Appearance appearance;

        public Vector2 location => transform.position;

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
            // return lineData[line].schedule.GetNextDeparture(after);
            return lineData[line].nextDeparture;
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
            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteManager.GetSprite("Sprites/stop_ring");
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.color = Color.white;

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var tf = transform;
            tf.rotation = new Quaternion();
            tf.localScale = Vector3.one;
            tf.SetLayer(MapLayer.TransitStops);

            this.gameObject.AddComponent<CircleCollider2D>();
            this.appearance = Appearance.Circle;
        }

        void CreateSmallRectMesh()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteManager.GetSprite("Sprites/stop_rect");
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            
            var tf = transform;
            // spriteRenderer.size = 

            float? angle = null;
            foreach (var route in routes)
            {
                Vector2 pt = route.endStop == this
                    ? route.positions[route.positions.Count - 2]
                    : route.positions[1];

                var deg = Math.DirectionalAngleDeg(pt, tf.position);
                if (angle.HasValue)
                {
                    if (!angle.Value.Equals(deg))
                    {
                        angle = null;
                        break;
                    }
                }
                else
                {
                    angle = deg;
                }
            }

            tf.localScale = Vector3.one;
            tf.rotation = Quaternion.Euler(0f, 0f, angle ?? 0f);
            appearance = Appearance.SmallRect;
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

        public void UpdateAppearance()
        {
            if (lineData.Count == 1)
            {
                CreateCircleMesh();
            }
            else
            {
                CreateSmallRectMesh();
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
