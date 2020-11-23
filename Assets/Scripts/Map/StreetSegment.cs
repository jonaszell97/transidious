using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class StreetSegment : StaticMapObject, IRoute
    {
        [System.Flags] public enum Flags
        {
            None   = 0x0,
            OneWay = 0x1,
            Bridge = 0x2,
            All    = ~None,
        }

        public class Lane : IRoute
        {
            /// The segment this lane belongs to.
            public StreetSegment segment;

            /// The lane number.
            public int laneNumber;

            /// The direction of this lane relative to the street segments (arbitrary) direction.
            public bool forward;

            /// C'tor.
            public Lane(StreetSegment segment, int laneNumber, bool forward)
            {
                this.segment = segment;
                this.laneNumber = laneNumber;
                this.forward = forward;
            }

            /// The start intersection of this lane.
            public StreetIntersection startIntersection => forward 
                ? segment.startIntersection
                : segment.endIntersection;
            
            /// The end intersection of this lane.
            public StreetIntersection endIntersection => forward 
                ? segment.endIntersection
                : segment.startIntersection;

            /// The start traffic light of this lane.
            public TrafficLight startTrafficLight => forward 
                ? segment.startTrafficLight
                : segment.endTrafficLight;

            /// The end traffic light of this lane.
            public TrafficLight endTrafficLight => forward 
                ? segment.endTrafficLight
                : segment.startTrafficLight;

            /// The length of this lane.
            public Distance length => Distance.FromMeters(segment.length);

            /// The positions of this segment.
            public List<Vector2> positions
            {
                get
                {
                    if (forward)
                        return segment.positions;

                    var cpy = new List<Vector2>(segment.positions);
                    cpy.Reverse();

                    return cpy;
                }
            }

            /// The drivable positions of this segment.
            public List<Vector2> drivablePositions
            {
                get
                {
                    if (forward)
                        return segment.drivablePositions;

                    var cpy = new List<Vector2>(segment.drivablePositions);
                    cpy.Reverse();

                    return cpy;
                }
            }
            
            public MapObjectKind Kind => segment.kind;

            public IStop Begin => startIntersection;

            public IStop End => endIntersection;
            
            public bool OneWay => true;

            public TimeSpan TravelTime => GetTravelTime(length);

            public TimeSpan GetTravelTime(Distance length)
            {
                var seconds = length / segment.street.AverageSpeed;
                return seconds.Multiply(GameController.instance.sim.trafficSim.CurrentTrafficFactor);
            }

            public Velocity AverageSpeed => segment.street.AverageSpeed;

            public int AssociatedID => 0;

            public DateTime NextDeparture(DateTime after)
            {
                return after;
            }
        }

        /// The street this segment is part of.
        public Street street;

        /// The position of this segment in the street's segment list.
        public int position;

        /// <summary>
        /// The path of this street segment.
        /// </summary>
        public List<Vector2> positions;
        public List<Vector2> drivablePositions;

        /// List of distances from the start to this position.
        public List<float> cumulativeDistances;

        /// The intersection at the beginning the street.
        public StreetIntersection startIntersection;

        /// The intersection at the end the street.
        public StreetIntersection endIntersection;

        /// The length of this street segment.
        public float length;

        /// Whether or not there are tram tracks on this street segment.
        public bool hasTramTracks;

        /// The bus / tram routes that drive on this street segment.
        HashSet<Route>[] transitRoutes;

        /// The text label for this segments street name.
        public Text streetName;

        /// The direction arrow on this street segment.
        public GameObject directionArrow;

        /// The traffic light at the start intersection.
        public TrafficLight startTrafficLight;

        /// The traffic light at the end intersection.
        public TrafficLight endTrafficLight;

        public GameObject tramTrackMeshObj;

        public static readonly float laneWidth = 3f;

        /// Distance of the stop line from the middle of the intersection.
        public float BeginStopLineDistance = 10f;

        /// Distance of the stop line from the middle of the intersection.
        public float EndStopLineDistance = 10f;

        /// The flags.
        public Flags flags = Flags.None;

        /// The number of cars that can park on the side of the road.
        public int Capacity;

        /// Whether or not this street is onw-way only.
        public bool IsOneWay
        {
            get => flags.HasFlag(Flags.OneWay);
            set
            {
                if (value)
                {
                    flags |= Flags.OneWay;
                }
                else
                {
                    flags &= ~Flags.OneWay;
                }
            }
        }

        /// Whether or not this is a bridge.
        public bool IsBridge
        {
            get => flags.HasFlag(Flags.Bridge);
            set
            {
                if (value)
                {
                    flags |= Flags.Bridge;
                }
                else
                {
                    flags &= ~Flags.Bridge;
                }
            }
        }

        public bool IsRiver => street.type == Street.Type.River;
        
        public Distance distance => Distance.FromMeters(length);
        
        public void Initialize(Street street, int position, List<Vector2> positions,
                               StreetIntersection startIntersection,
                               StreetIntersection endIntersection,
                               bool isOneWay = false,
                               bool hasTramTracks = false, int id = -1)
        {
            base.Initialize(MapObjectKind.StreetSegment, id);

            this.street = street;
            this.position = position;
            this.startIntersection = startIntersection;
            this.endIntersection = endIntersection;
            this.hasTramTracks = hasTramTracks;
            this.cumulativeDistances = new List<float>();
            this.positions = positions;
            this.IsOneWay = isOneWay;

            startIntersection.AddIntersectingStreet(this);

            if (startIntersection != endIntersection)
            {
                endIntersection.AddIntersectingStreet(this);
            }
        }

        public void CalculateLength()
        {
            length = 0f;

            for (int i = 1; i < positions.Count; ++i)
            {
                var p0 = positions[i - 1];
                var p1 = positions[i];

                cumulativeDistances.Add(length);
                length += (p1 - p0).magnitude;
            }

            cumulativeDistances.Add(length);

            if (/*(startIntersection?.NumIntersectingStreets ?? 0) < 2 ||*/ length <= 10f)
            {
                BeginStopLineDistance = 0f;
            }
            else if (length <= 20f)
            {
                BeginStopLineDistance = 3f;
            }
            else if (length <= 30f)
            {
                BeginStopLineDistance = 5f;
            }
            else
            {
                BeginStopLineDistance = 10f;
            }

            if (/*(endIntersection?.NumIntersectingStreets ?? 0) < 2 ||*/ length <= 10f)
            {
                EndStopLineDistance = 0f;
            }
            else if (length <= 20f)
            {
                EndStopLineDistance = 3f;
            }
            else if (length <= 30f)
            {
                EndStopLineDistance = 5f;
            }
            else
            {
                EndStopLineDistance = 10f;
            }

            UpdateDrivablePositions();
            Capacity = (int)Mathf.Floor(length / 15f) * 2;
        }

        public override int GetCapacity(OccupancyKind kind)
        {
            if (kind == OccupancyKind.ParkingCitizen)
            {
                return Capacity;
            }

            return 0;
        }

        void UpdateDrivablePositions()
        {
            const float sqrThresholdDist = 2.5f * 2.5f;

            // Skip positions that are in front of the start stop line.
            int i = 1;
            while (i < positions.Count && cumulativeDistances[i] <= BeginStopLineDistance)
            {
                ++i;
            }

            // Skip positions that are behind the end stop line.
            int iLast = positions.Count - 1;
            while (iLast > 0 && (length - cumulativeDistances[iLast] <= EndStopLineDistance))
            {
                --iLast;
            }

            drivablePositions = new List<Vector2>();

            // Include stop line positions.
            drivablePositions.Add(GetStartStopLinePosition());

            float dist;
            for (int j = i; j <= iLast; ++j)
            {
                if (j == i)
                {
                    dist = (drivablePositions[0] - positions[j]).sqrMagnitude;
                    if (dist <= sqrThresholdDist)
                    {
                        continue;
                    }
                }

                drivablePositions.Add(positions[j]);
            }

            var endStopLinePos = GetEndStopLinePosition();

            dist = (drivablePositions.Last() - endStopLinePos).sqrMagnitude;
            if (dist > sqrThresholdDist || drivablePositions.Count < 2)
            {
                drivablePositions.Add(endStopLinePos);
            }
        }

        public int LanePositionFromMiddle(int lane, bool ignoreOneWay = false)
        {
            if (IsOneWay && !ignoreOneWay)
                return lane + 1;

            var lanes = street.lanes;
            var halfLanes = lanes / 2;
            var isLeftLane = lane < halfLanes;

            if (isLeftLane)
            {
                return halfLanes - lane;
            }
            else
            {
                return lane - halfLanes + 1;
            }
        }

        public int RightmostLane => street.lanes - 1;

        public int LeftmostLane => 0;

        public int MirrorLane(int lane)
        {
            var lanes = street.lanes;
            var halfLanes = lanes / 2;
            var isLeftLane = lane < halfLanes;

            if (isLeftLane)
            {
                return halfLanes + (halfLanes - lane) - 1;
            }
            else
            {
                return halfLanes - 1 - (lane - halfLanes);
            }
        }

        public HashSet<Route> GetTransitRoutes(int lane)
        {
            if (transitRoutes == null)
            {
                transitRoutes = new HashSet<Route>[this.street.lanes];
            }
            if (transitRoutes[lane] == null)
            {
                transitRoutes[lane] = new HashSet<Route>();
            }

            return transitRoutes[lane];
        }

        public bool StartUTurnAllowed
        {
            get
            {
                return true;
            }
        }

        public bool EndUTurnAllowed
        {
            get
            {
                return true;
            }
        }

        public Tuple<Vector2, Math.PointPosition> GetClosestPointAndPosition(Vector2 pos)
        {
            var result = GetClosestPointAndPosition(pos, drivablePositions);
            return Tuple.Create((Vector2) result.Item1, result.Item2);
        }

        public static Tuple<Vector2, Math.PointPosition>
        GetClosestPointAndPosition(Vector2 pos, IReadOnlyList<Vector2> positions)
        {
            var minDist = float.PositiveInfinity;
            var minPt = Vector2.zero;
            var minIdx = 0;

            for (int i = 1; i < positions.Count; ++i)
            {
                var p0 = positions[i - 1];
                var p1 = positions[i];

                var closestPt = Math.NearestPointOnLine(p0, p1, pos);
                var sqrDist = (closestPt - pos).sqrMagnitude;

                if (sqrDist < minDist)
                {
                    minDist = sqrDist;
                    minPt = closestPt;
                    minIdx = i;
                }
            }

            var pointPos = Math.GetPointPosition(positions[minIdx - 1], positions[minIdx], pos);
            return new Tuple<Vector2, Math.PointPosition>(minPt, pointPos);
        }

        public int GetClosestPoint(Vector2 pos)
        {
            return GetClosestPoint(pos, drivablePositions);
        }

        public static int GetClosestPoint(Vector2 pos, IReadOnlyList<Vector2> positions)
        {
            var minDist = float.PositiveInfinity;
            var minIdx = -1;

            for (int i = 1; i < positions.Count; ++i)
            {
                var p0 = positions[i - 1];
                var p1 = positions[i];

                var closestPt = Math.NearestPointOnLine(p0, p1, pos);

                var sqrDist = (closestPt - pos).sqrMagnitude;
                if (sqrDist < minDist)
                {
                    minDist = sqrDist;
                    minIdx = i - 1;
                }
            }

            return minIdx;
        }

        public float GetDistanceFromStart(Vector2 pos)
        {
            var closestIdx = GetClosestPoint(pos, positions);
            Vector2 closestPt = positions[closestIdx];

            return cumulativeDistances[closestIdx] + (closestPt - pos).magnitude;
        }

        public float GetDistanceFromEnd(Vector2 pos)
        {
            return length - GetDistanceFromStart(pos);
        }

        public float GetDistanceFromStartStopLine(Vector2 pos)
        {
            var closestIdx = GetClosestPoint(pos, positions);
            Vector2 closestPt = positions[closestIdx];

            var cumulativeDist = cumulativeDistances[closestIdx] - BeginStopLineDistance;
            return cumulativeDist + (closestPt - pos).magnitude;
        }

        public float GetDistanceFromStartStopLine(Vector2 pos, IReadOnlyList<Vector2> offsetPositions)
        {
            var closestIdx = GetClosestPoint(pos, offsetPositions);
            Vector2 closestPt = offsetPositions[closestIdx];

            float dist = (closestPt - pos).magnitude;
            for (var i = 1; i <= closestIdx; ++i)
            {
                dist += (offsetPositions[i] - offsetPositions[i - 1]).magnitude;
            }

            return dist;
        }

        public float GetDistanceFromEndStopLine(Vector2 pos)
        {
            return length - GetDistanceFromStartStopLine(pos) - BeginStopLineDistance - EndStopLineDistance;
        }

        public Vector2 GetOffsetPointFromStart(float distance)
        {
            if (distance > length)
            {
                return positions.Last();
            }
            if (distance.Equals(0f))
            {
                return positions.First();
            }

            int i = 0;
            for (; i < positions.Count - 1; ++i)
            {
                if (cumulativeDistances[i] >= distance)
                {
                    break;
                }
            }

            Debug.Assert(i > 0, "should not be possible");

            var p0 = positions[i - 1];
            var p1 = positions[i];
            var dir = (p1 - p0).normalized;

            return p0 + ((distance - cumulativeDistances[i - 1]) * dir);
        }

        public Vector2 GetOffsetPointFromEnd(float distance)
        {
            if (distance > length)
            {
                return positions.First();
            }
            if (distance.Equals(0f))
            {
                return positions.Last();
            }

            int i = positions.Count - 1;
            for (; i > 0; --i)
            {
                if ((length - cumulativeDistances[i]) >= distance)
                {
                    break;
                }
            }

            Debug.Assert(i < positions.Count - 1, "should not be possible");

            var p0 = positions[i + 1];
            var p1 = positions[i];
            var dir = (p1 - p0).normalized;

            return p0 + ((distance - (length - cumulativeDistances[i + 1])) * dir);
        }

        public Vector2 GetStartStopLinePosition()
        {
            return GetOffsetPointFromStart(BeginStopLineDistance);
        }

        public Vector2 GetEndStopLinePosition()
        {
            return GetOffsetPointFromEnd(EndStopLineDistance);
        }

        public Vector2 RelativeDirection(StreetIntersection intersection)
        {
            if (intersection == startIntersection)
            {
                return positions[1] - positions[0];
            }

            Debug.Assert(intersection == endIntersection);
            return positions[positions.Count - 2] - positions[positions.Count - 1];
        }

        public TrafficLight GetTrafficLight(StreetIntersection intersection)
        {
            return intersection == startIntersection ? startTrafficLight : endTrafficLight;
        }

        public void SetTrafficLight(StreetIntersection intersection, TrafficLight tl)
        {
            if (intersection == startIntersection)
            {
                startTrafficLight = tl;
            }
            else
            {
                endTrafficLight = tl;
            }
        }

        public StreetIntersection GetOppositeIntersection(StreetIntersection intersection)
        {
            return intersection == startIntersection ? endIntersection : startIntersection;
        }

        public Vector2 RandomPoint
        {
            get
            {
                var offset = RNG.Next(0f, length);
                for (var i = 0; i < positions.Count; ++i)
                {
                    var dist = cumulativeDistances[i];
                    if (dist.Equals(offset))
                    {
                        return positions[i];
                    }
                    if (offset < dist)
                    {
                        continue;
                    }

                    var p0 = positions[i];
                    var p1 = positions[i + 1];

                    return p0 + ((p1 - p0).normalized * (dist - offset));
                }

                return positions.First();
            }
        }

        public IStop Begin => startIntersection;

        public IStop End => endIntersection;

        public bool OneWay => IsOneWay;

        public TimeSpan TravelTime => GetTravelTime(Distance.FromMeters(length));

        public TimeSpan GetTravelTime(Distance length)
        {
            var seconds = length / street.AverageSpeed;
            return seconds.Multiply(Game.sim.trafficSim.CurrentTrafficFactor);
        }

        public Velocity AverageSpeed => street.AverageSpeed;

        public int AssociatedID => 0;

        public DateTime NextDeparture(DateTime after)
        {
            return after;
        }

        public float GetStreetWidth(RenderingDistance distance)
        {
            return GetStreetWidth(street.type, street.lanes, distance);
        }

        public static float GetStreetWidth(Street.Type type, int lanes, RenderingDistance distance)
        {
            switch (type)
            {
            case Street.Type.Highway:
            case Street.Type.Primary:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth + 3f;
                case RenderingDistance.Far:
                    return lanes * laneWidth;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth + 2f;
                case RenderingDistance.Far:
                    return lanes * laneWidth;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Link:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth + 1f;
                case RenderingDistance.Far:
                    return lanes * laneWidth;
                }

                break;
            case Street.Type.Residential:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth;
                case RenderingDistance.Far:
                    return lanes * laneWidth - 1f;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth * 0.3f;
                case RenderingDistance.Far:
                    return 0f;
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return laneWidth * 2.2f;
                case RenderingDistance.Far:
                    return laneWidth * 2f;
                }

                break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public float GetBorderWidth(RenderingDistance distance)
        {
            return GetBorderWidth(street.type, distance);
        }

        public static float GetBorderWidth(Street.Type type, RenderingDistance distance)
        {
            if (type == Street.Type.River)
                return 0f;

            return 1f;
        }

        public Color GetStreetColor(MapDisplayMode mode = MapDisplayMode.Day)
        {
            return GetStreetColor(street.type, mode);
        }

        public static Color GetStreetColor(Street.Type type, MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (type)
            {
                case Street.Type.Path:
                    return Colors.GetColor($"street.path{mode}");
                case Street.Type.River:
                    return Colors.GetColor($"street.river{mode}");
                default:
                    return Colors.GetColor($"street.default{mode}");
            }
        }

        public Color GetBorderColor(MapDisplayMode mode = MapDisplayMode.Day)
        {
            return GetBorderColor(street.type, mode);
        }

        public static Color GetBorderColor(Street.Type type, MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (type)
            {
                case Street.Type.Path:
                    return Colors.GetColor($"street.pathBorder{mode}");
                case Street.Type.River:
                    return Colors.GetColor($"street.riverBorder{mode}");
                default:
                    return Colors.GetColor($"street.defaultBorder{mode}");
            }
        }
        
        public int LanesPerDirection
        {
            get
            {
                if (IsOneWay)
                {
                    return street.lanes;
                }

                return street.lanes / 2;
            }
        }

        static LineRenderer _tmpRenderer;

        public Tuple<Mesh, Mesh> CreateMeshes()
        {
            if (_tmpRenderer == null)
            {
                var tmpObj = new GameObject();
                _tmpRenderer = tmpObj.AddComponent<LineRenderer>();
                _tmpRenderer.enabled = false;
            }

            var z = IsBridge ? .1f : (street.type == Street.Type.River ? -.1f : 0f);

            _tmpRenderer.positionCount = positions.Count;
            _tmpRenderer.SetPositions(positions.Select(v => new Vector3(v.x, v.y, z)).ToArray());

            _tmpRenderer.numCornerVertices = Settings.Current.qualitySettings.StreetCornerVerts;
            _tmpRenderer.numCapVertices = Settings.Current.qualitySettings.StreetCapVerts;

            // if (startIntersection.RelativePosition(this) == 0
            // || endIntersection.RelativePosition(this) == 0)
            // {
            //     tmpRenderer.numCapVertices = Settings.Current.qualitySettings.StreetCapVerts;
            // }
            // else
            // {
            //     tmpRenderer.numCapVertices = 0;
            // }

            var streetWidth = 2f * GetStreetWidth(RenderingDistance.Near);
            _tmpRenderer.startWidth = streetWidth;
            _tmpRenderer.endWidth = _tmpRenderer.startWidth;

            var streetMesh = new Mesh();
            _tmpRenderer.BakeMesh(streetMesh);

            Mesh outlineMesh = null;
            if (street.type != Street.Type.River)
            {
                var borderWidth = streetWidth + GetBorderWidth(RenderingDistance.Near);
                _tmpRenderer.startWidth = borderWidth;
                _tmpRenderer.endWidth = _tmpRenderer.startWidth;

                outlineMesh = new Mesh();
                _tmpRenderer.BakeMesh(outlineMesh);
            }

            return Tuple.Create(streetMesh, outlineMesh);
        }

        public void UpdateMesh()
        {
#if DEBUG
            if (GameController.instance.ImportingMap)
            {
                return;
            }
#endif

            var meshes = CreateMeshes();
            var streetMesh = meshes.Item1;

            var streetWidth = 2f * GetStreetWidth(RenderingDistance.Near);
            var borderWidth = streetWidth + 2f * GetBorderWidth(RenderingDistance.Near);

            var colliderPath = MeshBuilder.CreateLineCollider(positions, borderWidth * .5f);
            var collisionRect = MeshBuilder.GetCollisionRect(streetMesh);

            foreach (var tile in street.map.GetTilesForObject(this))
            {
                tile.AddCollider(this, colliderPath, collisionRect, false);
            }
        }

        public void UpdateColor(MapDisplayMode mode)
        {
            UpdateColor(GetStreetColor(mode));
            UpdateBorderColor(GetBorderColor(mode));
        }

        Tuple<Mesh, Mesh> GetTramTrackMesh(Vector2[] path, bool isRightLane)
        {
            var trackDistance = 1.2f;
            var trackWidth = 0.15f;
            var offset = (isRightLane ? -1f : +1f);

            var meshRight = MeshBuilder.CreateSmoothLine(
                path, trackWidth, 20,
                Map.Layer(MapLayer.StreetMarkings),
                null, false, false,
                trackDistance * .5f + trackWidth * .5f + offset);

            var meshLeft = MeshBuilder.CreateSmoothLine(path, trackWidth, 20,
                Map.Layer(MapLayer.StreetMarkings),
                null, false, false,
                trackDistance * -.5f - trackWidth * .5f + offset);

            return new Tuple<Mesh, Mesh>(meshRight, meshLeft);
        }

        void GetIntersectionMeshes(List<Mesh> trackMeshes)
        {
            if (startIntersection != null)
            {
                GetIntersectionMeshes(startIntersection, trackMeshes);
            }
            if (endIntersection != null)
            {
                GetIntersectionMeshes(endIntersection, trackMeshes);
            }
        }

        void GetIntersectionMeshes(StreetIntersection intersection,
                                   List<Mesh> trackMeshes)
        {
            /*
            var trafficSim = street.map.Game.sim.trafficSim;
            foreach (var s in intersection.intersectingStreets)
            {
                if (s == this || !s.hasTramTracks)
                {
                    continue;
                }
                if (!(IsOneWay && intersection == startIntersection)
                && !(s.IsOneWay && intersection == s.endIntersection))
                {
                    var rightLane = this.RightmostLane;
                    var rightPath = trafficSim.GetPath(intersection, this, s, rightLane);
                    var rightMeshes = GetTramTrackMesh(rightPath, true);

                    trackMeshes.Add(rightMeshes.Item1);
                    trackMeshes.Add(rightMeshes.Item2);
                }

                if (!(IsOneWay && intersection == endIntersection)
                && !(s.IsOneWay && intersection == s.startIntersection))
                {
                    // Create tracks for left lane.
                    var leftLane = this.LeftmostLane;
                    var leftPath = trafficSim.GetPath(intersection, s, this, leftLane);
                    var leftMeshes = GetTramTrackMesh(leftPath, true);

                    s.UpdateTramTracks(leftMeshes.Item1, leftMeshes.Item2);
                }
            }
            */
        }

        void CreateTramTrackMesh()
        {
            /*
            var trafficSim = street.map.Game.sim.trafficSim;

            // Create tracks for right lane.
            var rightLane = this.RightmostLane;
            var rightPath = trafficSim.GetPath(this, rightLane);
            var rightMeshes = GetTramTrackMesh(rightPath, true);

            var trackMeshes = new List<Mesh> { rightMeshes.Item1, rightMeshes.Item2 };
            if (!IsOneWay)
            {
                // Create tracks for left lane.
                var leftLane = this.LeftmostLane;
                var leftPath = trafficSim.GetPath(this, leftLane);
                var leftMeshes = GetTramTrackMesh(leftPath, false);

                trackMeshes.Add(leftMeshes.Item1);
                trackMeshes.Add(leftMeshes.Item2);
            }

            // Create meshes for intersections.
            GetIntersectionMeshes(trackMeshes);

            var tramTrackMesh = MeshBuilder.CombineMeshes(trackMeshes);

            if (tramTrackMeshObj == null)
            {
                tramTrackMeshObj = GameObject.Instantiate(GameController.instance.loadedMap.meshPrefab);
            }

            var filter = tramTrackMeshObj.GetComponent<MeshFilter>();
            filter.sharedMesh = tramTrackMesh;

            var renderer = tramTrackMeshObj.GetComponent<MeshRenderer>();
            renderer.material = GameController.instance.GetUnlitMaterial(GetBorderColor());
            */
        }

        void UpdateTramTracks(Mesh trackRight, Mesh trackLeft)
        {
            if (tramTrackMeshObj == null)
            {
                tramTrackMeshObj = GameObject.Instantiate(GameController.instance.loadedMap.meshPrefab);
                tramTrackMeshObj.GetComponent<MeshFilter>().sharedMesh = MeshBuilder.CombineMeshes(trackRight, trackLeft);
            }
            else
            {
                var previousMesh = tramTrackMeshObj.GetComponent<MeshFilter>().sharedMesh;
                tramTrackMeshObj.GetComponent<MeshFilter>().sharedMesh = MeshBuilder.CombineMeshes(previousMesh, trackRight, trackLeft);
            }

        }

        public void AddTramTracks()
        {
            if (this.hasTramTracks)
            {
                return;
            }

            this.hasTramTracks = true;
            this.CreateTramTrackMesh();
        }

        public float GetFontSize(float orthographicSize)
        {
            float min;
            float max;
            float factor;

            switch (street.type)
            {
            case Street.Type.Highway:
            case Street.Type.Primary:
                min = 7f;
                max = 12f;
                factor = 6f / (100f);

                break;
            default:
                min = 5f;
                max = 10f;
                factor = 5f / (100f);

                break;
            }

            return Mathf.Clamp(factor * orthographicSize, min, max);
        }

        public void UpdateTextScale(RenderingDistance dist)
        {
            if (streetName == null)
            {
                return;
            }

            var setTextActive = false;
            switch (dist)
            {
            case RenderingDistance.Far:
                switch (street.type)
                {
                case Street.Type.Primary:
                    setTextActive = true;
                    break;
                }

                break;
            default:
                setTextActive = true;
                break;
            }

            if (setTextActive)
            {
                streetName.gameObject.SetActive(true);

                var newFontSize = GetFontSize(Camera.main.orthographicSize);
                var scale = newFontSize / streetName.textMesh.fontSize;
                streetName.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                streetName.gameObject.SetActive(false);
            }
        }

        public void UpdateScale(RenderingDistance dist)
        {
            UpdateTextScale(dist);
        }

        public void UpdateColor(Color c)
        {
            //var streetRenderer = streetMeshObj.GetComponent<MeshRenderer>();
            //streetRenderer.material = GameController.instance.GetUnlitMaterial(c);
        }

        public void ResetColor()
        {
            UpdateColor(GetStreetColor());
        }

        public void UpdateBorderColor(Color c)
        {
            //var outlineRenderer = outlineMeshObj.GetComponent<MeshRenderer>();
            //outlineRenderer.material = GameController.instance.GetUnlitMaterial(c);
        }

        public new Serialization.StreetSegment ToProtobuf()
        {
            var result = new Serialization.StreetSegment
            {
                MapObject = base.ToProtobuf(),
                StartIntersectionID = (uint)(startIntersection?.id ?? 0),
                EndIntersectionID = (uint)(endIntersection?.id ?? 0),
                StartTrafficLightID = startTrafficLight?.Id ?? 0,
                EndTrafficLightID = endTrafficLight?.Id ?? 0,
                HasTramTracks = hasTramTracks,
                Flags = (int)flags,
            };

            result.Positions.AddRange(positions.Select(s => ((Vector2)s).ToProtobuf()));
            return result;
        }

        public void DeleteSegment()
        {
            street.DeleteSegment(this);
            startIntersection?.DeleteSegment(this);
            endIntersection?.DeleteSegment(this);

            foreach (var tile in street.map.tiles)
            {
                tile.mapObjects.Remove(this);
            }

            GameObject.Destroy(this.streetName?.gameObject);
        }

        public void HighlightBorder(bool bulldozing)
        {
            var game = street.map.Game;
            if (bulldozing)
            {
                UpdateBorderColor(game.bulldozeHighlightColor);
            }
            else
            {
                UpdateBorderColor(game.highlightColor);
            }
        }

#if DEBUG
        private static LineRenderer _highlightLine;

        public void Highlight(Color c)
        {
            if (_highlightLine == null)
            {
                var obj = new GameObject();
                _highlightLine = obj.AddComponent<LineRenderer>();
                _highlightLine.startWidth = 3f;
                _highlightLine.endWidth = 3f;
            }

            _highlightLine.material = Game.GetUnlitMaterial(c);
            _highlightLine.positionCount = positions.Count;
            _highlightLine.SetPositions(positions.Select(v=>v.WithZ(Map.Layer(MapLayer.Streets, 1))).ToArray());
            _highlightLine.gameObject.SetActive(true);
        }

        public void Unhighlight()
        {
            _highlightLine?.gameObject.SetActive(false);
        }

        public override void ActivateModal()
        {
            var modal = MainUI.instance.streetModal;
            modal.SetStreet(this);
            modal.modal.Enable();

            Highlight(Color.red);
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            if (!Game.MouseDownActive(MapObjectKind.StreetSegment))
            {
                return;
            }

            if (InputController.PointerOverUIObject)
            {
                return;
            }

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var radius = GetStreetWidth(RenderingDistance.Near);

            if (Distance.Between(pos, positions.First()).Meters <= radius)
            {
                if (MainUI.instance.intersectionModal.intersection == startIntersection)
                {
                    MainUI.instance.intersectionModal.modal.Disable();
                    return;
                }

                startIntersection.ActivateModal();
                return;
            }

            if (Distance.Between(pos, positions.Last()).Meters <= radius)
            {
                if (MainUI.instance.intersectionModal.intersection == endIntersection)
                {
                    MainUI.instance.intersectionModal.modal.Disable();
                    return;
                }

                endIntersection.ActivateModal();
                return;
            }

            var modal = MainUI.instance.streetModal;
            if (modal.modal.Active && modal.segment == this)
            {
                modal.modal.Disable();
                return;
            }

            ActivateModal();
        }

        public override bool ShouldCheckMouseOver()
        {
            return true;
        }
#endif
    }
}
