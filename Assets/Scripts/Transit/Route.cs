using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class Route : MapObject, IRoute
    {
        [System.Serializable]
        public struct SerializedRoute
        {
            public SerializableMapObject mapObject;
            public int lineID;
            public SerializableVector2[] positions;
            public Path.SerializedPath path;
            public Path.SerializedPath originalPath;

            public int beginStopID;
            public int endStopID;

            public SerializableDictionary<Tuple<int, int>, TrafficSimulator.PathSegmentInfo.Serializable[]>
                streetSegmentOffsetMap;

            public SerializableDictionary<int, TrafficSimulator.PathSegmentInfo.Serializable> pathSegmentInfoMap;

            public float totalTravelTime;
            public bool isBackRoute;
        }

        public Line line;

        public List<Vector3> positions;
        List<Vector3> overlapAwarePositions;
        List<float> overlapAwareWidths;
        public Path path;
        public Path originalPath;

        /// For each street segment this route is on, the index into the position vector where that
        /// segments positions start.
        Dictionary<Tuple<StreetSegment, int>, List<TrafficSimulator.PathSegmentInfo>> streetSegmentOffsetMap;
        Dictionary<int, TrafficSimulator.PathSegmentInfo> pathSegmentInfoMap;

        public Stop beginStop;
        public Stop.Slot beginSlot;

        public Stop endStop;
        public Stop.Slot endSlot;

        public float totalTravelTime;
        public bool isBackRoute = false;

        public Mesh mesh;
        MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        public void Initialize(Line line, Stop beginStop, Stop endStop, List<Vector3> positions,
                               bool isBackRoute = false, int id = -1)
        {
            base.Initialize(Kind.Line, id);

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

            this.pathSegmentInfoMap = new Dictionary<int, TrafficSimulator.PathSegmentInfo>();
            this.streetSegmentOffsetMap = new Dictionary<Tuple<StreetSegment, int>,
                                                         List<TrafficSimulator.PathSegmentInfo>>();

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

        void OnDestroy()
        {
            if (streetSegmentOffsetMap == null)
            {
                return;
            }

            foreach (var entry in streetSegmentOffsetMap)
            {
                foreach (var seg in entry.Value)
                {
                    seg.segment.GetTransitRoutes(seg.lane).Remove(this);
                }
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

        public List<Vector3> CurrentPositions
        {
            get
            {
                if (overlapAwarePositions != null)
                {
                    return overlapAwarePositions;
                }

                return positions;
            }
        }

        public List<float> CurrentWidths
        {
            get
            {
                return overlapAwareWidths;
            }
        }

        public void UpdateMesh(Mesh mesh, List<Vector3> newPositions, List<float> newWidths)
        {
            this.mesh = mesh;
            overlapAwarePositions = newPositions;
            overlapAwareWidths = newWidths;
            UpdateMesh();
        }

        public DateTime NextDeparture(DateTime after)
        {
            return after;
        }

        public void AddStreetSegmentOffset(TrafficSimulator.PathSegmentInfo info)
        {
            var key = new Tuple<StreetSegment, int>(info.segment, info.lane);
            if (!streetSegmentOffsetMap.ContainsKey(key))
            {
                streetSegmentOffsetMap.Add(key, new List<TrafficSimulator.PathSegmentInfo>());
            }

            streetSegmentOffsetMap[key].Add(info);

            for (var i = info.offset; i < info.offset + info.length; ++i)
            {
                pathSegmentInfoMap.Add(i, info);
            }
        }

        public Dictionary<Tuple<StreetSegment, int>, List<TrafficSimulator.PathSegmentInfo>>
        GetStreetSegmentOffsetInfo()
        {
            return streetSegmentOffsetMap;
        }

        public List<TrafficSimulator.PathSegmentInfo> GetStreetSegmentOffsets(StreetSegment seg, int lane)
        {
            var key = new Tuple<StreetSegment, int>(seg, lane);
            Debug.Assert(streetSegmentOffsetMap != null && streetSegmentOffsetMap.ContainsKey(key));
            return streetSegmentOffsetMap[key];
        }

        public TrafficSimulator.PathSegmentInfo GetSegmentForPosition(int pos)
        {
            if (pathSegmentInfoMap.TryGetValue(pos, out TrafficSimulator.PathSegmentInfo Value))
            {
                return Value;
            }

            return null;
        }

        public void UpdatePath()
        {
            if (positions == null)
            {
                return;
            }

            var collider = this.GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            mesh = MeshBuilder.CreateSmoothLine(positions, line.LineWidth, 20, 0, collider);
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
            return;
            if (isBackRoute && !line.map.input.renderBackRoutes)
            {
                return;
            }

            if (!name.Contains("M45") && !name.Contains("309"))
            {
                return;
            }

            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = true;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.sharedMaterial = line.material;
            lineRenderer.positionCount = CurrentPositions.Count;
            lineRenderer.SetPositions(CurrentPositions.Select(
                v => new Vector3(v.x, v.y, Map.Layer(MapLayer.TransitLines))).ToArray());

            if (true || CurrentWidths == null)
            {
                lineRenderer.startWidth = line.LineWidth * 2f;
                lineRenderer.endWidth = line.LineWidth * 2f;

                return;
            }

            var curve = new AnimationCurve();
            var time = 0f;
            var timeStep = 1f / (CurrentWidths.Count - 1);

            foreach (var width in CurrentWidths)
            {
                curve.AddKey(time, width);
                time += timeStep;
            }

            lineRenderer.widthCurve = curve;

            // meshFilter.mesh = mesh;
            // meshRenderer.sharedMaterial = line.material;
            // transform.position = new Vector3(transform.position.x,
            //                                  transform.position.y,
            //                                  Map.Layer(MapLayer.TransitLines));
        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public new SerializedRoute Serialize()
        {
            return new SerializedRoute
            {
                mapObject = base.Serialize(),
                lineID = line.id,

                positions = positions?.Select(v => new SerializableVector2(v)).ToArray() ?? null,

                beginStopID = beginStop.id,
                endStopID = endStop.id,

                streetSegmentOffsetMap = new SerializableDictionary<Tuple<int, int>,
                                                        TrafficSimulator.PathSegmentInfo.Serializable[]>
                {
                    keys = this.streetSegmentOffsetMap.Keys.Select(key => Tuple.Create(key.Item1.id, key.Item2)).ToArray(),
                    values = this.streetSegmentOffsetMap.Values.Select(
                        list => list.Select(info => info.Serialize()).ToArray()).ToArray(),
                },
                pathSegmentInfoMap = new SerializableDictionary<int,
                                                        TrafficSimulator.PathSegmentInfo.Serializable>
                {
                    keys = this.pathSegmentInfoMap.Keys.ToArray(),
                    values = this.pathSegmentInfoMap.Values.Select(info => info.Serialize()).ToArray(),
                },

                totalTravelTime = totalTravelTime,
                isBackRoute = isBackRoute
            };
        }

        public void Deserialize(SerializedRoute route, Map map)
        {
            base.Deserialize(route.mapObject);

            Initialize(map.GetMapObject<Line>(route.lineID),
                       map.GetMapObject<Stop>(route.beginStopID),
                       map.GetMapObject<Stop>(route.endStopID),
                       route.positions?.Select(
                            v => new Vector3(v.x, v.y, Map.Layer(MapLayer.TransitLines))).ToList()
                                ?? null,
                       route.isBackRoute);

            for (var i = 0; i < route.pathSegmentInfoMap.keys.Length; ++i)
            {
                var key = route.pathSegmentInfoMap.keys[i];
                var value = route.pathSegmentInfoMap.values[i];

                pathSegmentInfoMap.Add(key, new TrafficSimulator.PathSegmentInfo(value));
            }

            for (var i = 0; i < route.streetSegmentOffsetMap.keys.Length; ++i)
            {
                var key = route.streetSegmentOffsetMap.keys[i];
                var value = route.streetSegmentOffsetMap.values[i];

                streetSegmentOffsetMap.Add(
                    Tuple.Create(map.GetMapObject<StreetSegment>(key.Item1), key.Item2),
                    value.Select(info => new TrafficSimulator.PathSegmentInfo(info)).ToList());
            }

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
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            if (line.map.Game.transitEditor.active || selectedLine == line)
            {
                return;
            }

            float h, s, v;
            Color.RGBToHSV(line.color, out h, out s, out v);

            float increase = .5f;
            Color high;
            if (v < (1f - increase))
            {
                high = Color.HSVToRGB(h, s, v + increase);
            }
            else
            {
                high = Color.HSVToRGB(h, s, v - increase);
            }

            line.gradient = new ColorGradient(line.color, high, 1.25f);
        }

        protected override void OnMouseExit()
        {
            if (selectedLine == line)
            {
                return;
            }

            line.gradient = null;
            line.material.color = line.color;
        }

        private static Line selectedLine
        {
            get
            {
                return GameController.instance.transitEditor.lineInfoModal.selectedLine;
            }
        }

        protected override void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = GameController.instance.transitEditor.lineInfoModal;
            if (modal.selectedLine != null && modal.selectedLine != this.line)
            {
                modal.selectedLine.gradient = null;
                modal.selectedLine.material.color = selectedLine.color;
            }

            modal.modal.Enable();
            modal.SetLine(line, this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            this.RunNextFrame(() =>
            {
                modal.modal.PositionAt(pos);
            });
        }
    }
}
