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

        public List<Vector2> positions;
        public float length;

        List<Vector2> overlapAwarePositions;
        List<float> overlapAwareWidths;

        /// For each street segment this route is on, the index into the position vector where that
        /// segments positions start.
        Dictionary<Tuple<StreetSegment, int>, List<TrafficSimulator.PathSegmentInfo>> streetSegmentOffsetMap;
        Dictionary<int, TrafficSimulator.PathSegmentInfo> pathSegmentInfoMap;

        public Stop beginStop;
        public Stop endStop;
        
        public TimeSpan totalTravelTime;
        public bool isBackRoute = false;

        public Mesh mesh;
        MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        private static ColorGradient _lineGradient;
        
        public void Initialize(Line line, Stop beginStop, Stop endStop, List<Vector2> positions,
                               bool isBackRoute = false, int id = -1)
        {
            base.Initialize(MapObjectKind.Line, id, new Vector2());

            this.line = line;
            this.positions = positions;
            this.beginStop = beginStop;
            this.endStop = endStop;
            this.transform.position = new Vector3(0, 0, 0);
            this.transform.SetParent(line.transform, false);
            this.isBackRoute = isBackRoute;
            this.name = "(" + line.name + ") " + beginStop.name + " -> " + endStop.name;

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

        public TimeSpan TravelTime => Distance.FromMeters(length) / AverageSpeed;

        public IStop Begin => beginStop;

        public IStop End => endStop;

        public bool OneWay => true;

        public Velocity AverageSpeed => line.AverageSpeed;

        public int AssociatedID => line.id;

        public List<Vector2> CurrentPositions => overlapAwarePositions ?? positions;

        public List<float> CurrentWidths => overlapAwareWidths;
        
        public Distance distance => Distance.FromMeters(length);

        public void UpdateMesh(Mesh mesh, List<Vector2> newPositions, List<float> newWidths)
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

            // var collider = this.GetComponent<PolygonCollider2D>();
            // mesh = MeshBuilder.CreateSmoothLine(positions, line.LineWidth, 20, 0, collider);
            // mesh = MeshBuilder.CreateBakedLineMesh(positions, line.LineWidth, collider);

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

                TotalTravelTime = (float)totalTravelTime.TotalMilliseconds,
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

            if (positions != null)
                result.Positions.AddRange(positions.Select(s => ((Vector2)s).ToProtobuf()));

            return result;
        }

        public void Deserialize(Serialization.Route route, Map map)
        {
            base.Deserialize(route.MapObject);

            Initialize(map.GetMapObject<Line>((int)route.LineID),
                       map.GetMapObject<Stop>((int)route.BeginStopID),
                       map.GetMapObject<Stop>((int)route.EndStopID),
                       route.Positions?.Select(v => v.Deserialize()).ToList(),
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

            if (selectedLine == line)
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

            if (_lineGradient == null)
            {
                var gr = Instantiate(GameController.instance.colorGradientPrefab);
                _lineGradient = gr.GetComponent<ColorGradient>();
            }

            _lineGradient.Initialize(line.color, high, c => line.material.color = c, 1.25f);
            _lineGradient.gameObject.SetActive(true);
        }

        public override void OnMouseExit()
        {
            base.OnMouseExit();

            if (selectedLine == line)
            {
                return;
            }

            DeactivateGradient();

            if (line.material == null)
            {
                Debug.LogError($"no material on line {line.name}");
            }
            else
            {
                line.material.color = line.color;
            }
        }

        private static Line selectedLine => MainUI.instance.lineModal.selectedLine;

        public static void DeactivateGradient()
        {
            if (_lineGradient != null)
            {
                _lineGradient.gameObject.SetActive(false);
            }
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            if (!Game.MouseDownActive(MapObjectKind.Line))
            {
                return;
            }

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = MainUI.instance.lineModal;
            if (modal.selectedLine == line)
            {
                modal.modal.Disable();
                return;
            }

            if (modal.selectedLine != null && modal.selectedLine != this.line)
            {
                DeactivateGradient();
                modal.selectedLine.material.color = selectedLine.color;
            }

            modal.SetLine(line, this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.Enable();
        }
    }
}
