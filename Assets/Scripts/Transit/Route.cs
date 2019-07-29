using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Transidious;
using Transidious.PathPlanning;

namespace Transidious
{
    public class Route : MapObject, IRoute
    {
        [System.Serializable]
        public struct SerializedRoute
        {
            public int id;
            public int lineID;
            public SerializableVector3[] positions;
            public Path.SerializedPath path;
            public Path.SerializedPath originalPath;

            public int beginStopID;
            public int endStopID;

            public float totalTravelTime;
            public bool isBackRoute;
        }

        public int id;
        public Line line;

        public List<Vector3> positions;
        public Path path;
        public Path originalPath;

        /// For each street segment this route is on, the index into the position vector where that
        /// segments positions start.
        Dictionary<StreetSegment, List<Tuple<int, int>>> streetSegmentOffsetMap;

        public Stop beginStop;
        public Stop.Slot beginSlot;

        public Stop endStop;
        public Stop.Slot endSlot;

        public float totalTravelTime;
        public bool isBackRoute = false;

        public Mesh mesh;
        MeshFilter meshFilter;
        Renderer m_Renderer;

        public void Initialize(Line line, Stop beginStop, Stop endStop, List<Vector3> positions, bool isBackRoute = false)
        {
            base.inputController = line.map.input;
            this.line = line;
            this.positions = positions;
            this.beginStop = beginStop;
            this.beginSlot = null;
            this.endStop = endStop;
            this.endSlot = null;
            this.transform.position = new Vector3(0, 0, 0);
            this.transform.SetParent(line.transform);
            this.isBackRoute = isBackRoute;
            this.name = "(" + line.name + ") " + beginStop.name + " -> " + endStop.name;
            this.originalPath = path;

            UpdatePath();

            Route previousRoute = beginStop.GetIncomingRouteFromDepot(line);
            if (previousRoute == null)
            {
                this.totalTravelTime = this.TravelTime;
            }
            else
            {
                this.totalTravelTime = previousRoute.totalTravelTime + this.TravelTime;
            }
        }

        public float TravelTime
        {
            get
            {
                return 0f; // path.length / line.AverageSpeed;
            }
        }

        public IStop Begin
        {
            get
            {
                return beginStop;
            }
        }

        public IStop End
        {
            get
            {
                return endStop;
            }
        }

        public bool OneWay
        {
            get
            {
                return true;
            }
        }

        public float AverageSpeed
        {
            get
            {
                return line.AverageSpeed;
            }
        }

        public int AssociatedID
        {
            get
            {
                return line.id;
            }
        }

        public DateTime NextDeparture(DateTime after)
        {
            return after;
        }

        public ColorGradient Gradient
        {
            get
            {
                var gradient = this.gameObject.GetComponent<ColorGradient>();
                if (gradient != null)
                {
                    return gradient;
                }

                gradient = this.gameObject.AddComponent<ColorGradient>();
                gradient.Initialize(line.map.Game);

                return gradient;
            }
        }

        public void SetTransparency(float a)
        {
            Color current = this.m_Renderer.material.color;
            this.m_Renderer.material = GameController.GetUnlitMaterial(Math.ApplyTransparency(current, a));
        }

        public void ResetTransparency()
        {
            this.m_Renderer.material = GameController.GetUnlitMaterial(this.line.color);
        }

        public void AddStreetSegmentOffset(StreetSegment seg, int offset, int length)
        {
            if (streetSegmentOffsetMap == null)
            {
                streetSegmentOffsetMap = new Dictionary<StreetSegment, List<Tuple<int, int>>>();
            }
            if (!streetSegmentOffsetMap.ContainsKey(seg))
            {
                streetSegmentOffsetMap.Add(seg, new List<Tuple<int, int>>());
            }

            streetSegmentOffsetMap[seg].Add(new Tuple<int, int>(offset, length));
        }

        public List<Tuple<int, int>> GetStreetSegmentOffsets(StreetSegment seg)
        {
            Debug.Assert(streetSegmentOffsetMap != null && streetSegmentOffsetMap.ContainsKey(seg));
            return streetSegmentOffsetMap[seg];
        }

        public void UpdatePath()
        {
            if (positions == null)
            {
                return;
            }

            var collider = this.GetComponent<PolygonCollider2D>();
            mesh = MeshBuilder.CreateSmoothLine(positions, line.LineWidth, 20, 0, false, collider);

            UpdateMesh();
        }

        void OnDrawGizmosSelected()
        {
            var collider = this.GetComponent<PolygonCollider2D>();
            if (collider.pathCount == 0)
                return;

            Gizmos.color = Color.red;
            foreach (var pos in GetComponent<PolygonCollider2D>().GetPath(0))
            {
                Gizmos.DrawSphere(pos, 1f);
            }
        }

        public void UpdatePathStylized()
        {
            bool update = false;
            Vector3 beginLoc;
            Vector3 endLoc;

            if (beginSlot == null)
            {
                beginLoc = beginStop.location;
            }
            else
            {
                beginLoc = beginStop.GetSlotLocation(this, beginSlot);
            }

            if (endSlot == null)
            {
                endLoc = endStop.location;
            }
            else
            {
                endLoc = endStop.GetSlotLocation(this, endSlot);
            }

            if (path == null)
            {
                path = new Path(beginLoc, endLoc);
                originalPath = new Path(path);
                update = true;
            }
            else
            {
                path = new Path(originalPath);
                update = true;

                if (path.Start != beginLoc)
                {
                    path.AdjustStart(beginLoc, false, false);
                }
                if (path.End != endLoc)
                {
                    path.AdjustEnd(endLoc, false, false);
                }
            }

            if (isBackRoute && !line.map.input.renderBackRoutes)
            {
                meshFilter.mesh = new Mesh();
                return;
            }

            if (beginStop.appearance == Stop.Appearance.LargeRect && beginSlot != null)
            {
                float factor = 0.1f;

                var dir = Math.ClassifyDirection(originalPath.BeginAngle);
                float spaceBetweenLines;

                if (dir == CardinalDirection.North || dir == CardinalDirection.South)
                {
                    spaceBetweenLines = (beginStop.spacePerSlotHorizontal - line.map.input.lineWidth * 2f);
                }
                else
                {
                    spaceBetweenLines = (beginStop.spacePerSlotVertical - line.map.input.lineWidth * 2f);
                }

                factor += beginSlot.assignment.parallelPositionInbound
                    * Mathf.Sqrt(2 * spaceBetweenLines * spaceBetweenLines);

                path.RemoveStartAngle(Math.DirectionVector(dir) * factor);
                update = true;
            }
            if (endStop.appearance == Stop.Appearance.LargeRect && endSlot != null)
            {
                float factor = 0.1f;

                var dir = Math.ClassifyDirection(originalPath.EndAngle);
                float spaceBetweenLines;

                if (dir == CardinalDirection.North || dir == CardinalDirection.South)
                {
                    spaceBetweenLines = (endStop.spacePerSlotHorizontal - line.map.input.lineWidth * 2f);
                }
                else
                {
                    spaceBetweenLines = (endStop.spacePerSlotVertical - line.map.input.lineWidth * 2f);
                }

                factor += endSlot.assignment.parallelPositionOutbound
                    * Mathf.Sqrt(2 * spaceBetweenLines * spaceBetweenLines);

                path.RemoveEndAngle(Math.DirectionVector(dir) * -factor);
                update = true;
            }

            if (update)
            {
                path.width = line.map.input.lineWidth;
                UpdateMesh();

                // Some stops might need to be updated.
                if (beginStop.appearance == Stop.Appearance.SmallRect)
                {
                    beginStop.UpdateMesh(true);
                }
                if (endStop.appearance == Stop.Appearance.SmallRect)
                {
                    endStop.UpdateMesh(true);
                }
            }
        }

        void UpdateMesh()
        {
            if (isBackRoute && !line.map.input.renderBackRoutes)
            {
                return;
            }

            meshFilter.mesh = mesh;
            m_Renderer.material = GameController.GetUnlitMaterial(line.color);
            transform.position = new Vector3(transform.position.x,
                                             transform.position.y,
                                             Map.Layer(MapLayer.TransitLines));
        }

        public void UpdateScale()
        {

        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            m_Renderer = GetComponent<Renderer>();
        }

        public SerializedRoute Serialize()
        {
            return new SerializedRoute
            {
                id = id,
                lineID = line.id,

                positions = positions?.Select(v => new SerializableVector3(v)).ToArray() ?? null,

                beginStopID = beginStop.id,
                endStopID = endStop.id,

                totalTravelTime = totalTravelTime,
                isBackRoute = isBackRoute
            };
        }

        public void Deserialize(SerializedRoute route, Map map)
        {
            id = route.id;
            Initialize(map.transitLineIDMap[route.lineID], map.transitStopIDMap[route.beginStopID],
                       map.transitStopIDMap[route.endStopID],
                       route.positions?.Select(v => v.ToVector()).ToList() ?? null,
                       route.isBackRoute);

            originalPath = Path.Deserialize(route.originalPath);
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        protected override void OnMouseEnter()
        {
            if (line.map.Game.transitEditor.active)
            {
                return;
            }

            foreach (var r in this.line.routes)
            {
                var gradient = r.Gradient;

                float h, s, v;
                Color.RGBToHSV(r.line.color, out h, out s, out v);

                var diff = v < .65f ? .35f : -.35f;
                Color high = Color.HSVToRGB(h, s, v + diff);
                gradient.Activate(r.line.color, high, 1.25f);
            }
        }

        protected override void OnMouseExit()
        {
            foreach (var r in this.line.routes)
            {
                r.Gradient.Stop();
            }
        }
    }
}
