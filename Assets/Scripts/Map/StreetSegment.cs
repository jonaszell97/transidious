using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class StreetSegment : StaticMapObject, IRoute
    {
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
            public float length => segment.length;

            /// The positions of this lane.
            public Vector3[] path
            {
                get
                {
                    var traffiSim = GameController.instance.sim.trafficSim;
                    return traffiSim.GetPath(segment, laneNumber);
                }
            }

            /// The positions of this segment.
            public List<Vector3> positions
            {
                get
                {
                    if (forward)
                        return segment.positions;

                    var cpy = new List<Vector3>(segment.positions);
                    cpy.Reverse();

                    return cpy;
                }
            }

            /// The drivable positions of this segment.
            public List<Vector3> drivablePositions
            {
                get
                {
                    if (forward)
                        return segment.drivablePositions;

                    var cpy = new List<Vector3>(segment.drivablePositions);
                    cpy.Reverse();

                    return cpy;
                }
            }

            public IStop Begin => startIntersection;

            public IStop End => endIntersection;
            
            public bool OneWay => true;

            public TimeSpan TravelTime => GetTravelTime(length);

            public TimeSpan GetTravelTime(float length)
            {
                var seconds = (length / (segment.street.AverageSpeedKPH * Math.Kph2Mps));
                return TimeSpan.FromSeconds(seconds * GameController.instance.sim.trafficSim.CurrentTrafficFactor);
            }

            public float AverageSpeed => segment.street.AverageSpeedKPH;

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
        public List<Vector3> positions;
        public List<Vector3> drivablePositions;

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

        public static readonly float laneWidth = 3f * Map.Meters;

        /// Distance of the stop line from the middle of the intersection.
        public float BeginStopLineDistance = 10f * Map.Meters;

        /// Distance of the stop line from the middle of the intersection.
        public float EndStopLineDistance = 10f * Map.Meters;

        public void Initialize(Street street, int position, List<Vector3> positions,
                               StreetIntersection startIntersection,
                               StreetIntersection endIntersection,
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

            if (/*(startIntersection?.NumIntersectingStreets ?? 0) < 2 ||*/ length <= 10f * Map.Meters)
            {
                BeginStopLineDistance = 0f;
            }
            else if (length <= 20f * Map.Meters)
            {
                BeginStopLineDistance = 3f * Map.Meters;
            }
            else if (length <= 30f * Map.Meters)
            {
                BeginStopLineDistance = 5f * Map.Meters;
            }
            else
            {
                BeginStopLineDistance = 10f * Map.Meters;
            }

            if (/*(endIntersection?.NumIntersectingStreets ?? 0) < 2 ||*/ length <= 10f * Map.Meters)
            {
                EndStopLineDistance = 0f;
            }
            else if (length <= 20f * Map.Meters)
            {
                EndStopLineDistance = 3f * Map.Meters;
            }
            else if (length <= 30f * Map.Meters)
            {
                EndStopLineDistance = 5f * Map.Meters;
            }
            else
            {
                EndStopLineDistance = 10f * Map.Meters;
            }

            UpdateDrivablePositions();
        }

        void UpdateDrivablePositions()
        {
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

            drivablePositions = new List<Vector3>();

            // Include stop line positions.
            drivablePositions.Add(GetStartStopLinePosition());

            for (int j = i; j <= iLast; ++j)
            {
                drivablePositions.Add(positions[j]);
            }

            drivablePositions.Add(GetEndStopLinePosition());
        }

        public int LanePositionFromMiddle(int lane, bool ignoreOneWay = false)
        {
            if (street.isOneWay && !ignoreOneWay)
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

        public int RightmostLane
        {
            get
            {
                return street.lanes - 1;
            }
        }

        public int LeftmostLane
        {
            get
            {
                return 0;
            }
        }

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

        public Tuple<Vector3, Math.PointPosition> GetClosestPointAndPosition(Vector3 pos)
        {
            return GetClosestPointAndPosition(pos, drivablePositions);
        }

        public static Tuple<Vector3, Math.PointPosition>
        GetClosestPointAndPosition(Vector3 pos, IReadOnlyList<Vector3> positions)
        {
            var minDist = float.PositiveInfinity;
            var minPt = Vector3.zero;
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
            return new Tuple<Vector3, Math.PointPosition>(minPt, pointPos);
        }

        public int GetClosestPoint(Vector3 pos)
        {
            return GetClosestPoint(pos, drivablePositions);
        }

        public static int GetClosestPoint(Vector3 pos, IReadOnlyList<Vector3> positions)
        {
            pos.z = 0f;

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

        public float GetDistanceFromStartStopLine(Vector2 pos, IReadOnlyList<Vector3> offsetPositions)
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

        public Vector3 GetOffsetPointFromStart(float distance)
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

        public Vector3 GetOffsetPointFromEnd(float distance)
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

        public Vector3 GetStartStopLinePosition()
        {
            return GetOffsetPointFromStart(BeginStopLineDistance);
        }

        public Vector3 GetEndStopLinePosition()
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

        public Vector2 RandomPoint
        {
            get
            {
                var offset = UnityEngine.Random.Range(0f, length);
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

        public IStop Begin
        {
            get
            {
                return startIntersection;
            }
        }

        public IStop End
        {
            get
            {
                return endIntersection;
            }
        }

        public bool OneWay
        {
            get
            {
                return street.isOneWay;
            }
        }

        public TimeSpan TravelTime
        {
            get
            {
                return GetTravelTime(this.length);
            }
        }

        public TimeSpan GetTravelTime(float length)
        {
            var seconds = (length / (street.AverageSpeedKPH * Math.Kph2Mps));
            return TimeSpan.FromSeconds(seconds * Game.sim.trafficSim.CurrentTrafficFactor);
        }

        public float AverageSpeed
        {
            get
            {
                return street.AverageSpeedKPH;
            }
        }

        public int AssociatedID
        {
            get
            {
                return 0;
            }
        }

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
                    return lanes * laneWidth + 3f * Map.Meters;
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return lanes * laneWidth;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth + 2f * Map.Meters;
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                    return lanes * laneWidth;
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Link:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth + 1f * Map.Meters;
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                    return lanes * laneWidth;
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Residential:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth;
                case RenderingDistance.Far:
                    return lanes * laneWidth - 1f * Map.Meters;
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return lanes * laneWidth * 0.3f;
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
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
                case RenderingDistance.VeryFar:
                    return laneWidth * 1.8f;
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            default:
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
#if DEBUG
            if (GameController.instance.ImportingMap)
            {
                return 3f * Map.Meters;
            }
#endif

            switch (type)
            {
            case Street.Type.Highway:
            case Street.Type.Primary:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return 1f * Map.Meters;
                case RenderingDistance.Far:
                    return 3f * Map.Meters;
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return 1f * Map.Meters;
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                    return 3f * Map.Meters;
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
            case Street.Type.Link:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return 1f * Map.Meters;
                case RenderingDistance.Far:
                    return 3f * Map.Meters;
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                case RenderingDistance.Near:
                    return 2f * Map.Meters;
                case RenderingDistance.Far:
                    return 3f * Map.Meters;
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return 0f;
                }

                break;
            default:
                break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public Color GetStreetColor(RenderingDistance distance,
                                    MapDisplayMode mode = MapDisplayMode.Day)
        {
            return GetStreetColor(street.type, distance, mode);
        }

        public static Color GetStreetColor(Street.Type type, RenderingDistance distance,
                                           MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (type)
            {
            case Street.Type.Highway:
            case Street.Type.Primary:
            case Street.Type.Secondary:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                    switch (mode)
                    {
                    case MapDisplayMode.Day:
                    default:
                        return Color.white;
                    case MapDisplayMode.Night:
                        return new Color(.35f, .35f, .35f);
                    }
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    switch (mode)
                    {
                    case MapDisplayMode.Day:
                    default:
                        return new Color(0.7f, 0.7f, 0.7f);
                    case MapDisplayMode.Night:
                        return new Color(.35f, .35f, .35f);
                    }
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
            case Street.Type.Link:
                switch (mode)
                {
                case MapDisplayMode.Day:
                default:
                    return Color.white;
                case MapDisplayMode.Night:
                    return new Color(.35f, .35f, .35f);
                }
            case Street.Type.Path:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return new Color(232f / 255f, 220f / 255f, 192f / 255f);
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return new Color(160f / 255f, 218f / 255f, 242f / 255f);
                }

                break;
            default:
                break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public Color GetBorderColor(RenderingDistance distance,
                                    MapDisplayMode mode = MapDisplayMode.Day)
        {
            return GetBorderColor(street.type, distance, mode);
        }

        public static Color GetBorderColor(Street.Type type, RenderingDistance distance,
                                           MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (type)
            {
            case Street.Type.Highway:
            case Street.Type.Primary:
            case Street.Type.Secondary:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    switch (mode)
                    {
                    case MapDisplayMode.Day:
                    default:
                        return Color.gray;
                    case MapDisplayMode.Night:
                        return new Color(.7f, .7f, .7f);
                    }
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
            case Street.Type.Link:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    switch (mode)
                    {
                    case MapDisplayMode.Day:
                    default:
                        return Color.gray;
                    case MapDisplayMode.Night:
                        return new Color(.7f, .7f, .7f);
                    }
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return new Color(0f, 0f, 0f, 0f);
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                case RenderingDistance.Near:
                case RenderingDistance.Far:
                case RenderingDistance.VeryFar:
                case RenderingDistance.Farthest:
                    return new Color(116f / 255f, 187f / 255f, 218f / 255f);
                }

                break;
            default:
                break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public static RenderingDistance GetMaxVisibleRenderingDistance(Street.Type type)
        {
            switch (type)
            {
                case Street.Type.Highway:
                case Street.Type.Primary:
                case Street.Type.River:
                    return RenderingDistance.Farthest;
                case Street.Type.Secondary:
                case Street.Type.Tertiary:
                    return RenderingDistance.VeryFar;
                case Street.Type.Residential:
                    return RenderingDistance.Far;
                default:
                    return RenderingDistance.Near;

            }

            throw new ArgumentException($"Illegal enum value {type}");
        }

        public RenderingDistance GetMaxVisibleRenderingDistance()
        {
            return GetMaxVisibleRenderingDistance(street.type);
        }

        public static int totalVerts = 0;
        public static LineRenderer tmpRenderer;

        public Tuple<Mesh, Mesh> CreateMeshes()
        {
            float lineLayer;
            float lineOutlineLayer;

            if (street.type == Street.Type.River)
            {
                lineLayer = Map.Layer(MapLayer.Rivers);
                lineOutlineLayer = Map.Layer(MapLayer.RiverOutlines);
            }
            else
            {
                lineLayer = Map.Layer(MapLayer.Streets);
                lineOutlineLayer = Map.Layer(MapLayer.StreetOutlines);
            }

            if (tmpRenderer == null)
            {
                var tmpObj = new GameObject();
                tmpRenderer = tmpObj.AddComponent<LineRenderer>();
                tmpRenderer.enabled = false;
            }

            tmpRenderer.positionCount = positions.Count;
            tmpRenderer.SetPositions(
                positions.Select(v => new Vector3(v.x, v.y, lineLayer)).ToArray());

            tmpRenderer.numCornerVertices = Settings.Current.qualitySettings.StreetCornerVerts;

            if (startIntersection.RelativePosition(this) == 0
            || endIntersection.RelativePosition(this) == 0)
            {
                tmpRenderer.numCapVertices = Settings.Current.qualitySettings.StreetCapVerts;
            }
            else
            {
                tmpRenderer.numCapVertices = 0;
            }

            var streetWidth = 2f * GetStreetWidth(RenderingDistance.Near);
            tmpRenderer.startWidth = streetWidth;
            tmpRenderer.endWidth = tmpRenderer.startWidth;

            var streetMesh = new Mesh();
            tmpRenderer.BakeMesh(streetMesh);

            tmpRenderer.SetPositions(
                positions.Select(v => new Vector3(v.x, v.y, lineOutlineLayer)).ToArray());

            var borderWidth = streetWidth + GetBorderWidth(RenderingDistance.Near);
            tmpRenderer.startWidth = borderWidth;
            tmpRenderer.endWidth = tmpRenderer.startWidth;

            var outlineMesh = new Mesh();
            tmpRenderer.BakeMesh(outlineMesh);

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
            var outlineMesh = meshes.Item2;

            var streetWidth = 2f * GetStreetWidth(RenderingDistance.Near);
            var borderWidth = streetWidth + 2f * GetBorderWidth(RenderingDistance.Near);

            var colliderPath = MeshBuilder.CreateLineCollider(positions, borderWidth * .5f);
            var collisionRect = MeshBuilder.GetCollisionRect(outlineMesh);
            // var renderingDist = GetMaxVisibleRenderingDistance();

            foreach (var tile in street.map.GetTilesForObject(this))
            {
                //tile.AddMesh("Streets", streetMesh,
                //             GetStreetColor(RenderingDistance.Near),
                //             lineLayer, renderingDist);

                //tile.AddMesh("Streets", outlineMesh,
                //             GetBorderColor(RenderingDistance.Near),
                //             lineOutlineLayer, renderingDist);

                tile.AddCollider(this, colliderPath, collisionRect, false);
            }
        }

#if false
        public void UpdateMeshOld()
        {
#if DEBUG
            if (GameController.instance.ImportingMap)
            {
                return;
            }
#endif

            float lineLayer;
            float lineOutlineLayer;

            if (street.type == Street.Type.River)
            {
                lineLayer = Map.Layer(MapLayer.Rivers);
                lineOutlineLayer = Map.Layer(MapLayer.RiverOutlines);
            }
            else
            {
                lineLayer = Map.Layer(MapLayer.Streets);
                lineOutlineLayer = Map.Layer(MapLayer.StreetOutlines);
            }

            streetMeshObj = new GameObject();
            outlineMeshObj = new GameObject();

            var streetTransform = streetMeshObj.transform;
            streetTransform.SetParent(this.transform);

            var outlineTransform = outlineMeshObj.transform;
            outlineTransform.SetParent(this.transform);

            var streetLine = streetMeshObj.AddComponent<LineRenderer>();
            streetLine.positionCount = positions.Count;
            streetLine.SetPositions(
                positions.Select(v => new Vector3(v.x, v.y, lineLayer)).ToArray());

            var streetWidth = 2f * GetStreetWidth(RenderingDistance.Near);
            streetLine.startWidth = streetWidth;
            streetLine.endWidth = streetLine.startWidth;

            var outlineLine = outlineMeshObj.AddComponent<LineRenderer>();
            outlineLine.positionCount = positions.Count;
            outlineLine.SetPositions(
                positions.Select(v => new Vector3(v.x, v.y, lineOutlineLayer)).ToArray());

            var borderWidth = streetWidth + 2f * GetBorderWidth(RenderingDistance.Near);
            outlineLine.startWidth = borderWidth;
            outlineLine.endWidth = outlineLine.startWidth;

            streetLine.numCornerVertices = 5;
            outlineLine.numCornerVertices = 5;

            if (startIntersection.RelativePosition(this) == 0
            || endIntersection.RelativePosition(this) == 0)
            {
                streetLine.numCapVertices = 10;
                outlineLine.numCapVertices = 10;
            }

#if DEBUG
            if (GameController.instance.ImportingMap)
            {
                streetLine.sharedMaterial = GameController.instance.GetUnlitMaterial(
                    Color.blue);
                outlineLine.sharedMaterial = GameController.instance.GetUnlitMaterial(
                    Color.red);

                return;
            }
#endif

            var collider = GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            MeshBuilder.CreateLineCollider(positions, borderWidth * .5f, collider);
            UpdateColor(GameController.instance.displayMode);

            /*
            var startCap = false; // startIntersection.intersectingStreets.Count == 1;
            var endCap = false; // endIntersection.intersectingStreets.Count == 1;

            foreach (var distVal in Enum.GetValues(typeof(RenderingDistance)))
            {
                var dist = (RenderingDistance)distVal;
                if (!meshes.TryGetValue(dist, out StreetSegmentMeshInfo meshInfo))
                {
                    meshInfo = new StreetSegmentMeshInfo();
                    meshes.Add(dist, meshInfo);
                }

                var width = GetStreetWidth(dist);
                if (!width.Equals(0f))
                {
                    meshInfo.streetMesh = MeshBuilder.CreateSmoothLine(
                        positions, width, 5, 0f, null, startCap, endCap);

#if DEBUG
                    if (dist == RenderingDistance.Near)
                        totalVerts += meshInfo.streetMesh.triangles.Length;
#endif
                }

                var borderWidth = GetBorderWidth(dist);
                if (!borderWidth.Equals(0f))
                {
                    PolygonCollider2D collider = null;
                    if (dist == RenderingDistance.Near)
                    {
                        collider = GetComponent<PolygonCollider2D>();
                    }

                    meshInfo.outlineMesh = MeshBuilder.CreateSmoothLine(
                        positions, width + borderWidth, 5,
                        0f, collider, startCap, endCap);

#if DEBUG
                    if (dist == RenderingDistance.Near)
                        totalVerts += meshInfo.outlineMesh.triangles.Length;
#endif
                }

                // foreach (var tile in street.map.GetTilesForObject(this))
                // {
                //     if (meshInfo.streetMesh != null)
                //     {
                //         if (dist == RenderingDistance.Near)
                //             totalVerts += meshInfo.streetMesh.triangles.Length;

                //         tile.mesh.AddMesh(GetStreetColor(dist),
                //             meshInfo.streetMesh, dist, lineLayer);
                //     }
                //     if (meshInfo.outlineMesh != null)
                //     {
                //         if (dist == RenderingDistance.Near)
                //             totalVerts += meshInfo.outlineMesh.triangles.Length;

                //         tile.mesh.AddMesh(GetBorderColor(dist),
                //             meshInfo.outlineMesh, dist, lineOutlineLayer);
                //     }
                // }
            }

            UpdateScale(street.map.input?.renderingDistance ?? RenderingDistance.Near);
            */
        }

#endif

        public void UpdateColor(MapDisplayMode mode)
        {
            UpdateColor(GetStreetColor(RenderingDistance.Near, mode));
            UpdateBorderColor(GetBorderColor(RenderingDistance.Near, mode));
        }

        Tuple<Mesh, Mesh> GetTramTrackMesh(Vector3[] path, bool isRightLane)
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
            var trafficSim = street.map.Game.sim.trafficSim;
            foreach (var s in intersection.intersectingStreets)
            {
                if (s == this || !s.hasTramTracks)
                {
                    continue;
                }
                if (!(street.isOneWay && intersection == startIntersection)
                && !(s.street.isOneWay && intersection == s.endIntersection))
                {
                    var rightLane = this.RightmostLane;
                    var rightPath = trafficSim.GetPath(intersection, this, s, rightLane);
                    var rightMeshes = GetTramTrackMesh(rightPath, true);

                    trackMeshes.Add(rightMeshes.Item1);
                    trackMeshes.Add(rightMeshes.Item2);
                }

                if (!(street.isOneWay && intersection == endIntersection)
                && !(s.street.isOneWay && intersection == s.startIntersection))
                {
                    // Create tracks for left lane.
                    var leftLane = this.LeftmostLane;
                    var leftPath = trafficSim.GetPath(intersection, s, this, leftLane);
                    var leftMeshes = GetTramTrackMesh(leftPath, true);

                    s.UpdateTramTracks(leftMeshes.Item1, leftMeshes.Item2);
                }
            }
        }

        void CreateTramTrackMesh()
        {
            var trafficSim = street.map.Game.sim.trafficSim;

            // Create tracks for right lane.
            var rightLane = this.RightmostLane;
            var rightPath = trafficSim.GetPath(this, rightLane);
            var rightMeshes = GetTramTrackMesh(rightPath, true);

            var trackMeshes = new List<Mesh> { rightMeshes.Item1, rightMeshes.Item2 };
            if (!street.isOneWay)
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
            renderer.material = GameController.instance.GetUnlitMaterial(GetBorderColor(RenderingDistance.Near));
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

#if DEBUG
        Mesh[] _laneMeshes = null;
        List<Mesh> _startIntersectionMeshes = null;
        List<Mesh> _endIntersectionMeshes = null;
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            foreach (var pos in positions)
            {
                Gizmos.DrawSphere(pos, 3f * Map.Meters);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetStartStopLinePosition(), 3f * Map.Meters);

            Gizmos.color = Color.black;
            Gizmos.DrawSphere(GetEndStopLinePosition(), 3f * Map.Meters);

            // #if DRAW_STREETS
            //             var trafficSim = street.map.Game.sim.trafficSim;
            //             if (_laneMeshes == null)
            //             {
            //                 _laneMeshes = new Mesh[street.lanes];
            //                 for (int lane = 0; lane < street.lanes; ++lane)
            //                 {
            //                     var path = trafficSim.GetPath(this, lane);
            //                     _laneMeshes[lane] = MeshBuilder.CreateSmoothLine(new List<Vector3>(path), 1f * Map.Meters);
            //                 }
            //             }
            //             if (!street.isOneWay && startIntersection != null && _startIntersectionMeshes == null)
            //             {
            //                 _startIntersectionMeshes = new List<Mesh>();

            //                 for (int lane = street.lanes / 2; lane < street.lanes; ++lane)
            //                 {
            //                     foreach (var outgoing in startIntersection.OutgoingStreets)
            //                     {
            //                         var path = trafficSim.GetPath(startIntersection, this, outgoing, lane);
            //                         if (path == null)
            //                         {
            //                             continue;
            //                         }

            //                         _startIntersectionMeshes.Add(MeshBuilder.CreateSmoothLine(new List<Vector3>(path), .75f * Map.Meters));
            //                     }
            //                 }
            //             }
            //             if (endIntersection != null && _endIntersectionMeshes == null)
            //             {
            //                 _endIntersectionMeshes = new List<Mesh>();

            //                 for (int lane = 0; lane < street.lanes / 2; ++lane)
            //                 {
            //                     foreach (var outgoing in endIntersection.IncomingStreets)
            //                     {
            //                         var path = trafficSim.GetPath(endIntersection, outgoing, this, lane);
            //                         if (path == null)
            //                         {
            //                             continue;
            //                         }

            //                         _endIntersectionMeshes.Add(MeshBuilder.CreateSmoothLine(new List<Vector3>(path), .75f * Map.Meters));
            //                     }
            //                 }
            //             }

            //             for (int lane = 0; lane < street.lanes; ++lane)
            //             {
            //                 if (lane == 0)
            //                 {
            //                     Gizmos.color = Color.blue;
            //                 }
            //                 else
            //                 {
            //                     Gizmos.color = new Color(252f / 255f, 169f / 255f, 4f / 255f);
            //                 }

            //                 var mesh = _laneMeshes[lane];
            //                 Gizmos.DrawMesh(mesh);
            //             }

            //             if (_startIntersectionMeshes != null)
            //             {
            //                 foreach (var mesh in _startIntersectionMeshes)
            //                 {
            //                     Gizmos.color = Color.green;
            //                     Gizmos.DrawMesh(mesh);
            //                 }
            //             }

            //             if (_endIntersectionMeshes != null)
            //             {
            //                 foreach (var mesh in _endIntersectionMeshes)
            //                 {
            //                     Gizmos.color = Color.yellow;
            //                     Gizmos.DrawMesh(mesh);
            //                 }
            //             }
            // #endif
        }

        void UpdateDirectionalArrows(RenderingDistance dist)
        {
            if (directionArrow == null)
            {
                return;
            }

            if (dist == RenderingDistance.Near)
            {
                directionArrow.SetActive(true);
            }
            else
            {
                directionArrow.SetActive(false);
            }
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
                min = 7f * Map.Meters;
                max = 12f * Map.Meters;
                factor = 6f / (100f * Map.Meters);

                break;
            default:
                min = 5f * Map.Meters;
                max = 10f * Map.Meters;
                factor = 5f / (100f * Map.Meters);

                break;
            }

            return Mathf.Clamp(factor * orthographicSize, min, max);
        }

        public void UpdateTextScale(RenderingDistance dist)
        {
            UpdateDirectionalArrows(dist);

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
                default:
                    break;
                }

                break;
            case RenderingDistance.VeryFar:
            case RenderingDistance.Farthest:
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
                // streetName.textMesh.fontSize = GetFontSize(Camera.main.orthographicSize);
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

        public void ResetColor(RenderingDistance dist)
        {
            UpdateColor(GetStreetColor(dist));
        }

        public void UpdateBorderColor(Color c)
        {
            //var outlineRenderer = outlineMeshObj.GetComponent<MeshRenderer>();
            //outlineRenderer.material = GameController.instance.GetUnlitMaterial(c);
        }

        public void ResetBorderColor(RenderingDistance dist)
        {
            UpdateBorderColor(GetBorderColor(dist));
        }

        public new Serialization.StreetSegment ToProtobuf()
        {
            var result = new Serialization.StreetSegment
            {
                MapObject = base.ToProtobuf(),
                StartIntersectionID = (uint)(startIntersection?.id ?? 0),
                EndIntersectionID = (uint)(endIntersection?.id ?? 0),
                HasTramTracks = hasTramTracks,
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

        public override bool ShouldCheckMouseOver()
        {
            return true;
        }
    }
}
