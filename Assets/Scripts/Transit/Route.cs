using UnityEngine;
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
        List<Vector3> overlapAwarePositions;
        List<float> overlapAwareWidths;
        public Path path;
        public Path originalPath;

        /// For each street segment this route is on, the index into the position vector where that
        /// segments positions start.
        Dictionary<Tuple<StreetSegment, int>, List<TrafficSimulator.PathSegmentInfo>> streetSegmentOffsetMap;

        public Stop beginStop;
        public Stop.Slot beginSlot;

        public Stop endStop;
        public Stop.Slot endSlot;

        public float totalTravelTime;
        public bool isBackRoute = false;

        public Mesh mesh;
        MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

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
            GetComponent<MeshFilter>().mesh = mesh;
            overlapAwarePositions = newPositions;
            overlapAwareWidths = newWidths;
        }

        public DateTime NextDeparture(DateTime after)
        {
            return after;
        }

        public void AddStreetSegmentOffset(TrafficSimulator.PathSegmentInfo info)
        {
            if (streetSegmentOffsetMap == null)
            {
                streetSegmentOffsetMap = new Dictionary<Tuple<StreetSegment, int>, List<TrafficSimulator.PathSegmentInfo>>();
            }

            var key = new Tuple<StreetSegment, int>(info.segment, info.lane);
            if (!streetSegmentOffsetMap.ContainsKey(key))
            {
                streetSegmentOffsetMap.Add(key, new List<TrafficSimulator.PathSegmentInfo>());
            }

            streetSegmentOffsetMap[key].Add(info);
        }

        public List<TrafficSimulator.PathSegmentInfo> GetStreetSegmentOffsets(StreetSegment seg, int lane)
        {
            var key = new Tuple<StreetSegment, int>(seg, lane);
            Debug.Assert(streetSegmentOffsetMap != null && streetSegmentOffsetMap.ContainsKey(key));
            return streetSegmentOffsetMap[key];
        }

        public TrafficSimulator.PathSegmentInfo GetSegmentForPosition(int pos)
        {
            foreach (var entry in streetSegmentOffsetMap)
            {
                foreach (var data in entry.Value)
                {
                    if (data.offset <= pos && data.offset + data.length >= pos)
                    {
                        return data;
                    }
                }
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
            if (isBackRoute && !line.map.input.renderBackRoutes)
            {
                return;
            }

            meshFilter.mesh = mesh;
            meshRenderer.sharedMaterial = line.material;
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
            meshRenderer = GetComponent<MeshRenderer>();
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

        private static Line selectedLine;

        IEnumerator UpdateColorPickerNextFrame(ColorPicker colorPicker)
        {
            yield return null;

            colorPicker.UpdateBoundingBoxes(true);
            colorPicker.SetColor(line.color);

            yield break;
        }

        IEnumerator UpdateModalPositionNextFrame(UIModal modal, Vector3 pos)
        {
            yield return null;

            modal.PositionAt(pos);
            
            yield break;
        }

        protected override void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }
            if (selectedLine != null && selectedLine != this.line)
            {
                selectedLine.gradient = null;
                selectedLine.material.color = selectedLine.color;
            }

            selectedLine = this.line;

            var modal = GameController.instance.transitEditor.lineInfoModal;
            modal.Enable();
            StartCoroutine(UpdateModalPositionNextFrame(modal, Camera.main.ScreenToWorldPoint(Input.mousePosition)));

            var colorPicker = modal.GetComponentInChildren<ColorPicker>();
            var logo = modal.titleInput.transform.parent.gameObject.GetComponent<UnityEngine.UI.Image>();

            if (!modal.initialized)
            {
                modal.initialized = true;

                var maxCharacters = 32;
                modal.titleInput.interactable = true;
                modal.titleInput.onValidateInput = (string text, int charIndex, char addedChar) => {
                    if (text.Length + 1 >= maxCharacters)
                    {
                        return '\0';
                    }

                    return addedChar;
                };
                modal.titleInput.onSubmit.AddListener((string newName) => {
                    if (newName.Length == 0 || newName.Length > maxCharacters)
                    {
                        modal.titleInput.text = selectedLine.name;
                        return;
                    }

                    selectedLine.name = newName;
                });

                modal.onClose.AddListener(() => {
                    selectedLine.gradient = null;
                    selectedLine.material.color = selectedLine.color;
                    selectedLine = null;
                });

                colorPicker.onChange.AddListener((Color c) => {
                    selectedLine.gradient = null;
                    selectedLine.SetColor(c);
                    logo.color = c;
                });
            }

            logo.color = line.color;
            switch (line.type)
            {
                case TransitType.Bus:
                case TransitType.LightRail:
                case TransitType.IntercityRail:
                    logo.sprite = GameController.instance.roundedRectSprite;
                    logo.type = UnityEngine.UI.Image.Type.Sliced;
                    break;
                default:
                    logo.sprite = GameController.instance.squareSprite;
                    logo.type = UnityEngine.UI.Image.Type.Simple;
                    break;
            }

            logo.GetComponentInChildren<TMPro.TMP_InputField>().text = line.name;

            // We need to this next frame, otherwise the position of the color picker won't be up-to-date. 
            StartCoroutine(UpdateColorPickerNextFrame(colorPicker));
        }
    }
}
