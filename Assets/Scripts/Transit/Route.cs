using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class Route : DynamicMapObject, IRoute
    {
        public Line line;

        public List<Vector3> positions;
        public float length;

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
            base.Initialize(MapObjectKind.Line, id, new Vector2());

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
                return (length / (AverageSpeed / 3.6f)) / 60f;
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
            return beginStop.NextDeparture(line, after);
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

            for (var i = 1; i < positions.Count; ++i)
            {
                length += (positions[i] - positions[i - 1]).magnitude;
            }

            var collider = this.GetComponent<PolygonCollider2D>();
            //mesh = MeshBuilder.CreateSmoothLine(positions, line.LineWidth, 20, 0, collider);
            mesh = MeshBuilder.CreateBakedLineMesh(positions, line.LineWidth, collider);

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
            meshFilter.mesh = mesh;
            meshRenderer.sharedMaterial = line.material;
            transform.position = new Vector3(transform.position.x,
                                             transform.position.y,
                                             Map.Layer(MapLayer.TransitLines));
        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public new Serialization.Route ToProtobuf()
        {
            var result = new Serialization.Route
            {
                MapObject = base.ToProtobuf(),

                LineID = (uint)line.Id,
                BeginStopID = (uint)beginStop.Id,
                EndStopID = (uint)endStop.Id,

                TotalTravelTime = totalTravelTime,
            };

            foreach (var entry in pathSegmentInfoMap)
            {
                var info = new Serialization.Route.Types.PathSegmentInfoMapEntry
                {
                    Key = (uint)entry.Key,
                    Value = new Serialization.Route.Types.PathSegmentInfo
                    {
                        SegmentID = (uint)entry.Value.segment.Id,
                        Lane = entry.Value.lane,
                        Offset = entry.Value.offset,
                        Length = entry.Value.length,
                        PartialStart = entry.Value.partialStart,
                        PartialEnd = entry.Value.partialEnd,
                        Direction = entry.Value.direction.ToProtobuf(),
                    },
                };

                result.PathSegmentInfoMap.Add(info);
            }

            foreach (var entry in streetSegmentOffsetMap)
            {
                var info = new Serialization.Route.Types.StreetSegmentOffsetMapEntry
                {
                    Key = new Serialization.Route.Types.StreetSegmentKey
                    {
                        Segment = entry.Key.Item1.Id,
                        Lane = entry.Key.Item2,
                    },
                };

                info.Value.AddRange(entry.Value.Select(e => new Serialization.Route.Types.PathSegmentInfo
                {
                    SegmentID = (uint)e.segment.Id,
                    Lane = e.lane,
                    Offset = e.offset,
                    Length = e.length,
                    PartialStart = e.partialStart,
                    PartialEnd = e.partialEnd,
                    Direction = e.direction.ToProtobuf(),
                }));

                result.StreetSegmentOffsetMap.Add(info);
            }

            result.Positions.AddRange(positions.Select(s => ((Vector2)s).ToProtobuf()));
            return result;
        }

        public void Deserialize(Serialization.Route route, Map map)
        {
            base.Deserialize(route.MapObject);

            Initialize(map.GetMapObject<Line>((int)route.LineID),
                       map.GetMapObject<Stop>((int)route.BeginStopID),
                       map.GetMapObject<Stop>((int)route.EndStopID),
                       route.Positions?.Select(
                            v => new Vector3(v.X, v.Y, Map.Layer(MapLayer.TransitLines))).ToList()
                                ?? null,
                       false);

            for (var i = 0; i < route.PathSegmentInfoMap.Count; ++i)
            {
                var key = route.PathSegmentInfoMap[i].Key;
                var value = route.PathSegmentInfoMap[i].Value;

                pathSegmentInfoMap.Add((int)key, new TrafficSimulator.PathSegmentInfo(value));
            }

            for (var i = 0; i < route.StreetSegmentOffsetMap.Count; ++i)
            {
                var key = route.StreetSegmentOffsetMap[i].Key;
                var value = route.StreetSegmentOffsetMap[i].Value;

                streetSegmentOffsetMap.Add(
                    Tuple.Create(map.GetMapObject<StreetSegment>(key.Segment), key.Lane),
                    value.Select(info => new TrafficSimulator.PathSegmentInfo(info)).ToList());
            }
        }

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();

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

        public override void OnMouseExit()
        {
            base.OnMouseExit();

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

        public override void OnMouseDown()
        {
            if (!Game.MouseDownActive(MapObjectKind.Line))
            {
                return;
            }

            base.OnMouseDown();

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

            modal.SetLine(line, this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.EnableAt(pos);
        }
    }
}
