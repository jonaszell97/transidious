using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class TrafficLight
    {
        public enum Status
        {
            Green = 0,
            Yellow = 1,
            Red = 2,
            YellowRed = 3,
        }

        public static float DefaultGreenTime
        {
            get
            {
                return 5f;
            }
        }

        public static float DefaultRedTime
        {
            get
            {
                return 5f;
            }
        }

        public static float DefaultYellowTime
        {
            get
            {
                return 1f;
            }
        }

        public static float DefaultYellowRedTime
        {
            get
            {
                return 2f;
            }
        }

        public TrafficLight(float initialRedTime, float redTime, bool green = false)
        {
            this.redTime = redTime;
            this.timeToNextSwitch = initialRedTime;
            this.status = green ? Status.Green : Status.Red;
        }

        public Status status;
        public float timeToNextSwitch;
        public float redTime;

#if DEBUG
        public GameObject spriteObj1;
        public GameObject spriteObj2;
#endif

        void SetTimeToNextSwitch()
        {
            switch (status)
            {
                case Status.Green:
                    timeToNextSwitch = DefaultGreenTime;
                    break;
                case Status.Red:
                    timeToNextSwitch = redTime;
                    break;
                case Status.Yellow:
                    timeToNextSwitch = DefaultYellowTime;
                    break;
                case Status.YellowRed:
                    timeToNextSwitch = DefaultYellowRedTime;
                    break;
            }
        }

        public void Switch()
        {
            status = (Status)(((int)status + 1) % 4);
            SetTimeToNextSwitch();

#if DEBUG
            if (spriteObj1 != null)
            {
                var map = spriteObj1.GetComponentInParent<Map>();
                spriteObj1.GetComponent<SpriteRenderer>().sprite = map.Game.trafficLightSprites[(int)status];
            }
            if (spriteObj2 != null)
            {
                var map = spriteObj2.GetComponentInParent<Map>();
                spriteObj2.GetComponent<SpriteRenderer>().sprite = map.Game.trafficLightSprites[(int)status];
            }
#endif
        }

        public void Update(int speedMultiplier)
        {
            timeToNextSwitch -= Time.deltaTime * speedMultiplier;
            if (timeToNextSwitch <= 0f)
            {
                Switch();
            }
        }

        public bool MustStop
        {
            get
            {
                return status == Status.Red || status == Status.YellowRed;
            }
        }
    }

    public class StreetIntersection : IStop
    {
        [System.Serializable]
        public struct SerializedStreetIntersection
        {
            public SerializableVector3 position;
        }

        /// ID of the intersection.
        public int id;

        /// Position of the intersection.
        public Vector3 position;

        /// Intersecting streets.
        public List<StreetSegment> intersectingStreets;

        /// Angles of intersecting streets.
        Dictionary<StreetSegment, float> streetAngles;

        /// Relative positions of intersecting streets; clockwise and paired based on
        /// traffic light placement.
        Dictionary<StreetSegment, int> relativePositions;

        /// The empty slot in this intersection, or -1 if there is none.
        public int emptySlot = -1;

        /// Whether or not this intersection has traffic lights.
        public bool hasTrafficLights;

        public int numTrafficLights;

        public int NumIntersectingStreets
        {
            get
            {
                if (relativePositions == null)
                {
                    CalculateRelativePositions();
                }
                if (emptySlot != -1)
                {
                    return intersectingStreets.Count + 1;
                }

                return intersectingStreets.Count;
            }
        }

        public StreetIntersection(int id, Vector3 pos)
        {
            this.id = id;
            this.position = pos;
            this.intersectingStreets = new List<StreetSegment>();
            this.streetAngles = new Dictionary<StreetSegment, float>();
        }

        public SerializedStreetIntersection Serialize()
        {
            return new SerializedStreetIntersection
            {
                position = new SerializableVector3(position)
            };
        }

        public static void Deserialize(Map map, SerializedStreetIntersection inter)
        {
            map.CreateIntersection(inter.position.ToVector());
        }

        public void DeleteSegment(StreetSegment seg)
        {
            intersectingStreets.Remove(seg);
        }

        public Vector3 Location
        {
            get
            {
                return position;
            }
        }

        public IEnumerable<IRoute> Routes
        {
            get
            {
                return intersectingStreets.Select(s => s as IRoute);
            }
        }

        public bool IsGoalReached(IStop goal)
        {
            if (goal is StreetIntersection)
            {
                return goal as StreetIntersection == this;
            }
            if (goal is PointOnStreet)
            {
                var pos = goal as PointOnStreet;
                foreach (var s in intersectingStreets)
                {
                    if (s == pos.street)
                        return true;
                }
            }

            return false;
        }

        public bool uTurnAllowed
        {
            get
            {
                return intersectingStreets.Count == 1;
            }
        }

        public IEnumerable<StreetSegment> IncomingStreets
        {
            get
            {
                return intersectingStreets.Where(s => !s.street.isOneWay || s.endIntersection == this);
            }
        }

        public IEnumerable<StreetSegment> OutgoingStreets
        {
            get
            {
                return intersectingStreets.Where(s => !s.street.isOneWay || s.startIntersection == this);
            }
        }

        public float GetAngle(StreetSegment seg)
        {
            return streetAngles[seg];
        }

        public int RelativePosition(StreetSegment seg)
        {
            if (relativePositions == null || !relativePositions.ContainsKey(seg))
            {
                CalculateRelativePositions();
            }

            return relativePositions[seg];
        }

        public Vector3 GetNextPosition(StreetSegment seg)
        {
            if (this == seg.startIntersection)
            {
                return seg.positions[1];
            }

            Debug.Assert(this == seg.endIntersection, "street does not intersect here");
            return seg.positions[seg.positions.Count - 2];
        }

        public Vector3 GetTrafficLightPosition(StreetSegment seg)
        {
            return GetOffsetPosition(seg, 10f * Map.Meters);
        }

        public Vector3 GetOffsetPosition(StreetSegment seg, float offset)
        {
            var nextPos = GetNextPosition(seg);
            var vec = (nextPos - position).normalized;

            return position + vec * offset;
        }

        void SetTrafficLight(StreetSegment seg, TrafficLight tl)
        {
            if (this == seg.startIntersection)
            {
                seg.startTrafficLight = tl;
            }
            else
            {
                Debug.Assert(this == seg.endIntersection, "street does not intersect here");
                seg.endTrafficLight = tl;
            }
        }

        public void AddIntersectingStreet(StreetSegment seg)
        {
            // Get the angle that this street connects to the intersection with.
            var p0 = GetTrafficLightPosition(seg);
            var angle = Math.PointAngle(p0, position);

            streetAngles.Add(seg, angle);

            int i = 0;
            for (; i < intersectingStreets.Count; ++i)
            {
                var otherAngle = streetAngles[intersectingStreets[i]];
                if (angle < otherAngle)
                {
                    break;
                }
            }

            if (i == intersectingStreets.Count)
            {
                intersectingStreets.Add(seg);
            }
            else
            {
                intersectingStreets.Insert(i, seg);
            }
        }

        // bool TrySlotAssignment(int startFrom, int numSlots)
        // {
        //     var step = numSlots / 2;
        //     for (int i = startFrom; i < startFrom + step; ++i)
        //     {
        //         var slot = i % numSlots;
        //         var seg = intersectingStreets[slot];

        //         var oppositeSlot = (i + step) % numSlots;
        //         if (oppositeSlot == intersectingStreets.Count)
        //         {
        //             continue;
        //         }

        //         var otherSeg = intersectingStreets[oppositeSlot];
        //         var angle = streetAngles[seg];
        //         var oppositeAngle = streetAngles[otherSeg];

        //         if (angle < 0f)
        //         {
        //             angle += 180f;
        //         }
        //         if (oppositeAngle < 0f)
        //         {
        //             oppositeAngle += 180f;
        //         }

        //         var angleDiff = Mathf.Abs(angle - oppositeAngle);
        //         if (angleDiff >= 30f)
        //         {
        //             return false;
        //         }
        //     }

        //     return true;
        // }

        // void ApplySlotAssignment(int startFrom, int numSlots)
        // {
        //     var step = numSlots / 2;
        //     for (int i = startFrom; i < startFrom + step; ++i)
        //     {
        //         var slot = i % numSlots;
        //         var seg = intersectingStreets[slot];
        //         relativePositions.Add(seg, slot);

        //         var oppositeSlot = (i + step) % numSlots;
        //         if (oppositeSlot == intersectingStreets.Count)
        //         {
        //             continue;
        //         }

        //         var otherSeg = intersectingStreets[oppositeSlot];
        //         relativePositions.Add(otherSeg, oppositeSlot + 1);
        //     }
        // }

        // class SlotAssigner
        // {
        //     public List<StreetSegment> segments;
        //     StreetSegment firstAssignment;
        //     StreetSegment secondAssignment;
        //     float minScore = float.PositiveInfinity;
        //     Tuple<StreetSegment, StreetSegment> bestAssignment;

        //     float CurrentScore
        //     {
        //         get
        //         {

        //         }
        //     }

        //     void SaveCurrentAssignment()
        //     {
        //         bestAssignment = new Tuple<StreetSegment, StreetSegment>(firstAssignment, secondAssignment);
        //     }

        //     void Assign(StreetSegment seg)
        //     {
        //         if (firstAssignment == null)
        //         {
        //             firstAssignment = seg;
        //         }
        //         else
        //         {
        //             secondAssignment = seg;
        //         }
        //     }

        //     void Unassign()
        //     {
        //         if (secondAssignment != null)
        //         {
        //             secondAssignment = null;
        //         }
        //         else
        //         {
        //             firstAssignment = null;
        //         }
        //     }

        //     void FindSlotAssignments(int offset, int k)
        //     {
        //         if (k == 0)
        //         {
        //             var score = CurrentScore;
        //             if (score < minScore)
        //             {
        //                 SaveCurrentAssignment();
        //                 return;
        //             }
        //         }

        //         for (int i = offset; i <= segments.Count - k; ++i)
        //         {
        //             Assign(segments[offset]);
        //             FindSlotAssignments(offset + 1, k - 1);
        //             Unassign();
        //         }
        //     }

        //     public Tuple<StreetSegment, StreetSegment> FindBestSlotAssignments()
        //     {
        //         FindSlotAssignments(0, 2);
        //     }
        // }

        void CalculateRelativePositions()
        {
            if (relativePositions == null)
            {
                relativePositions = new Dictionary<StreetSegment, int>();
            }
            else
            {
                relativePositions.Clear();
            }

            var numSlots = intersectingStreets.Count;
            var uneven = numSlots % 2 != 0;
            if (numSlots != 3)
            {
                for (int i = 0; i < numSlots; ++i)
                {
                    relativePositions.Add(intersectingStreets[i], i);
                }

                return;
            }

            var minAngle = float.PositiveInfinity;
            var iMin = 0;
            var jMin = 0;

            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    if (i == j)
                        continue;

                    var seg = intersectingStreets[i];
                    var oppositeSeg = intersectingStreets[j];

                    var angle = Math.NormalizeAngle(streetAngles[seg]);
                    var oppositeAngle = Math.NormalizeAngle(streetAngles[oppositeSeg]);

                    var angleDiff = Mathf.Abs(angle - oppositeAngle);
                    if (angleDiff < minAngle)
                    {
                        minAngle = angleDiff;
                        iMin = i;
                        jMin = j;
                    }
                }
            }

            relativePositions[intersectingStreets[iMin]] = 1;
            relativePositions[intersectingStreets[jMin]] = 3;

            if (iMin != 0 && jMin != 0)
            {
                relativePositions[intersectingStreets[0]] = 0;
            }
            else if (iMin != 1 && jMin != 1)
            {
                relativePositions[intersectingStreets[1]] = 0;
            }
            else if (iMin != 2 && jMin != 2)
            {
                relativePositions[intersectingStreets[2]] = 0;
            }

            emptySlot = 2;

            intersectingStreets.Sort((StreetSegment s1, StreetSegment s2) =>
            {
                return relativePositions[s1].CompareTo(relativePositions[s2]);
            });
        }

        public StreetSegment GetStreetAtSlot(int slot)
        {
            foreach (var seg in intersectingStreets)
            {
                if (relativePositions[seg] == slot)
                    return seg;
            }

            return null;
        }

        public int GetValidSlot(int slot)
        {
            var slotCount = intersectingStreets.Count;
            if (slotCount % 2 != 0)
            {
                ++slotCount;
            }

            return slot % slotCount;
        }

        public void GenerateTrafficLights(Map map)
        {
            if (intersectingStreets.Count <= 2)
                return;

            CalculateRelativePositions();

#if DEBUG
            if (map.renderStreetOrder)
            {
                foreach (var seg in intersectingStreets)
                {
                    var pos = GetOffsetPosition(seg, 5 * Map.Meters);
                    pos.z = Map.Layer(MapLayer.Foreground);

                    var txt = map.CreateText(pos, relativePositions[seg].ToString(), Color.black);
                    txt.textMesh.fontSize = 10 * Map.Meters;
                    txt.transform.position = pos;
                    txt.textMesh.alignment = TMPro.TextAlignmentOptions.Center;
                    txt.textMesh.autoSizeTextContainer = true;
                    txt.textMesh.ForceMeshUpdate();
                }
            }
#endif

            bool shouldGenerateTrafficLights = false;
            bool allOneWay = true;

            foreach (var seg in intersectingStreets)
            {
                allOneWay &= seg.street.isOneWay;
                switch (seg.street.type)
                {
                    case Street.Type.Primary:
                    case Street.Type.Secondary:
                    case Street.Type.Tertiary:
                        shouldGenerateTrafficLights = true;
                        break;
                    default:
                        break;
                }

                if (shouldGenerateTrafficLights)
                    break;
            }

            if (!shouldGenerateTrafficLights || allOneWay)
                return;

            var trafficSim = map.Game.sim.trafficSim;

            int numStreets = intersectingStreets.Count;
            if (numStreets % 2 != 0)
            {
                ++numStreets;
            }

            numTrafficLights = numStreets / 2;
            var redTime = (numTrafficLights - 1) * (TrafficLight.DefaultRedTime
                    + TrafficLight.DefaultYellowTime
                + TrafficLight.DefaultYellowRedTime);

            for (int i = 0; i < numTrafficLights; ++i)
            {
                var initialRedTime = i == 0
                    ? TrafficLight.DefaultGreenTime
                    : i * (TrafficLight.DefaultRedTime + TrafficLight.DefaultYellowTime)
                        + (i - 1) * TrafficLight.DefaultYellowRedTime;

                var tl = new TrafficLight(initialRedTime, redTime, i == 0);
                trafficSim.trafficLights.Add(tl);

                var seg = intersectingStreets[i];
                SetTrafficLight(seg, tl);

                var relativePos = relativePositions[seg];
                StreetSegment oppositeSeg = null;

                foreach (var otherSeg in intersectingStreets)
                {
                    if (seg == otherSeg)
                        continue;

                    var otherPos = relativePositions[otherSeg];
                    if (System.Math.Abs(otherPos - relativePos) == numTrafficLights)
                    {
                        oppositeSeg = otherSeg;
                        break;
                    }
                }

                if (oppositeSeg != null)
                    SetTrafficLight(oppositeSeg, tl);

#if DEBUG
                if (map.renderTrafficLights)
                {
                    var sprite1 = map.Game.CreateSprite(
                        map.Game.trafficLightSprites[(int)tl.status]);

                    sprite1.transform.SetParent(map.transform);
                    sprite1.transform.position = GetTrafficLightPosition(seg);
                    sprite1.transform.position = new Vector3(sprite1.transform.position.x, sprite1.transform.position.y, Map.Layer(MapLayer.Foreground));

                    tl.spriteObj1 = sprite1;

                    if (oppositeSeg != null)
                    {
                        var sprite2 = map.Game.CreateSprite(
                            map.Game.trafficLightSprites[(int)tl.status]);

                        sprite2.transform.SetParent(map.transform);
                        sprite2.transform.position = GetTrafficLightPosition(oppositeSeg);
                        sprite2.transform.position = new Vector3(sprite2.transform.position.x, sprite2.transform.position.y, Map.Layer(MapLayer.Foreground));

                        tl.spriteObj2 = sprite2;
                    }
                }
#endif
            }
        }
    }

    public class StreetSegment : MapObject, IRoute
    {
        [System.Serializable]
        public struct SerializedStreetSegment
        {
            public List<SerializableVector3> positions;
            public int startIntersectionID;
            public int endIntersectionID;
            public bool hasTramTracks;
        }

        /// ID of the street segment.
        public int id;

        /// The street this segment is part of.
        public Street street;

        /// The position of this segment in the street's segment list.
        public int position;

        /// <summary>
        /// The path of this street segment.
        /// </summary>
        public List<Vector3> positions;

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
        HashSet<Route> transitRoutes;

        /// The text label for this segments street name.
        public Transidious.Text streetName;

        /// The direction arrow on this street segment.
        public GameObject directionArrow;

        /// The traffic light at the start intersection.
        public TrafficLight startTrafficLight;

        /// The traffic light at the end intersection.
        public TrafficLight endTrafficLight;

        /// Game object carrying the street mesh.
        GameObject streetMeshObj;

        /// Game object carrying the outline mesh.
        public GameObject outlineMeshObj;

        class StreetSegmentMeshInfo
        {
            internal Mesh streetMesh;
            internal Mesh outlineMesh;
        }

        Dictionary<InputController.RenderingDistance, StreetSegmentMeshInfo> meshes;

        public static readonly float laneWidth = 3f * Map.Meters;

        /// Distance of the stop line from the middle of the intersection.
        public float BeginStopLineDistance = 10f * Map.Meters;

        /// Distance of the stop line from the middle of the intersection.
        public float EndStopLineDistance = 10f * Map.Meters;

        public void Initialize(Street street, int position, List<Vector3> positions,
                               StreetIntersection startIntersection,
                               StreetIntersection endIntersection,
                               bool hasTramTracks = false)
        {
            base.inputController = street.map.input;
            this.street = street;
            this.position = position;
            this.startIntersection = startIntersection;
            this.endIntersection = endIntersection;
            this.hasTramTracks = hasTramTracks;
            this.meshes = new Dictionary<InputController.RenderingDistance, StreetSegmentMeshInfo>();
            this.cumulativeDistances = new List<float>();

            UpdateMesh(positions);

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

            if ((startIntersection?.NumIntersectingStreets ?? 0) < 2 || length <= 10f * Map.Meters)
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

            if ((endIntersection?.NumIntersectingStreets ?? 0) < 2 || length <= 10f * Map.Meters)
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

        public HashSet<Route> TransitRoutes
        {
            get
            {
                if (transitRoutes == null)
                {
                    transitRoutes = new HashSet<Route>();
                }

                return transitRoutes;
            }
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
            return GetClosestPointAndPosition(pos, positions);
        }

        public Tuple<Vector3, Math.PointPosition>
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
            var closestIdx = GetClosestPoint(pos);
            Vector2 closestPt = positions[closestIdx];

            return cumulativeDistances[closestIdx] + (closestPt - pos).magnitude;
        }

        public float GetDistanceFromEnd(Vector2 pos)
        {
            return length - GetDistanceFromStart(pos);
        }

        public float GetDistanceFromStartStopLine(Vector2 pos)
        {
            var closestIdx = GetClosestPoint(pos);
            Vector2 closestPt = positions[closestIdx];

            var cumulativeDist = cumulativeDistances[closestIdx] - BeginStopLineDistance;
            return cumulativeDist + (closestPt - pos).magnitude;
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

        public float TravelTime
        {
            get
            {
                var kmPerMinute = street.maxspeed / 60f;
                var lengthInKm = length / 1000f;

                return kmPerMinute * lengthInKm;
            }
        }

        public float AverageSpeed
        {
            get
            {
                return street.maxspeed;
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

        public float GetStreetWidth(InputController.RenderingDistance distance)
        {
            return GetStreetWidth(street.type, street.lanes, distance);
        }

        public static float GetStreetWidth(Street.Type type, int lanes, InputController.RenderingDistance distance)
        {
            switch (type)
            {
                case Street.Type.Primary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return lanes * laneWidth + 2f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return lanes * laneWidth;
                    }

                    break;
                case Street.Type.Secondary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return lanes * laneWidth + 1f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                            return lanes * laneWidth;
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.Tertiary:
                case Street.Type.Residential:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return lanes * laneWidth;
                        case InputController.RenderingDistance.Far:
                            return lanes * laneWidth - 1f * Map.Meters;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.Path:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                            return lanes * laneWidth * 0.9f;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.River:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return laneWidth * 2.2f;
                        case InputController.RenderingDistance.Far:
                            return laneWidth * 2f;
                        case InputController.RenderingDistance.VeryFar:
                            return laneWidth * 1.8f;
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                default:
                    break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public float GetBorderWidth(InputController.RenderingDistance distance)
        {
            return GetBorderWidth(street.type, distance);
        }

        public static float GetBorderWidth(Street.Type type, InputController.RenderingDistance distance)
        {
            switch (type)
            {
                case Street.Type.Primary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return 1f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                            return 3f * Map.Meters;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.Secondary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return 1f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                            return 3f * Map.Meters;
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.Tertiary:
                case Street.Type.Residential:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return 1f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                            return 3f * Map.Meters;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.Path:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                case Street.Type.River:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                            return 2f * Map.Meters;
                        case InputController.RenderingDistance.Far:
                            return 3f * Map.Meters;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return 0f;
                    }

                    break;
                default:
                    break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public Color GetStreetColor(InputController.RenderingDistance distance)
        {
            return GetStreetColor(street.type, distance);
        }

        public static Color GetStreetColor(Street.Type type, InputController.RenderingDistance distance)
        {
            switch (type)
            {
                case Street.Type.Primary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                            return Color.white;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(0.7f, 0.7f, 0.7f);
                    }

                    break;
                case Street.Type.Secondary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                            return Color.white;
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(0.7f, 0.7f, 0.7f);
                    }

                    break;
                case Street.Type.Tertiary:
                case Street.Type.Residential:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return Color.white;
                    }

                    break;
                case Street.Type.Path:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(92f / 255f, 92f / 255f, 87f / 255f);
                    }

                    break;
                case Street.Type.River:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(160f / 255f, 218f / 255f, 242f / 255f);
                    }

                    break;
                default:
                    break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        public Color GetBorderColor(InputController.RenderingDistance distance)
        {
            return GetBorderColor(street.type, distance);
        }

        public static Color GetBorderColor(Street.Type type, InputController.RenderingDistance distance)
        {
            switch (type)
            {
                case Street.Type.Primary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return Color.gray;
                    }

                    break;
                case Street.Type.Secondary:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return Color.gray;
                    }

                    break;
                case Street.Type.Tertiary:
                case Street.Type.Residential:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return Color.gray;
                    }

                    break;
                case Street.Type.Path:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(0f, 0f, 0f, 0f);
                    }

                    break;
                case Street.Type.River:
                    switch (distance)
                    {
                        case InputController.RenderingDistance.Near:
                        case InputController.RenderingDistance.Far:
                        case InputController.RenderingDistance.VeryFar:
                        case InputController.RenderingDistance.Farthest:
                            return new Color(116f / 255f, 187f / 255f, 218f / 255f);
                    }

                    break;
                default:
                    break;
            }

            throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
        }

        void CreateMeshes()
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

            // if (street.map.input.combineStreetMeshes)
            // {
            //     foreach (var dist in Enum.GetValues(typeof(InputController.RenderingDistance)))
            //     {
            //         street.map.streetMesh.AddStreetSegment(
            //             (InputController.RenderingDistance)dist,
            //             positions,
            //             GetStreetWidth((InputController.RenderingDistance)dist),
            //             GetBorderWidth((InputController.RenderingDistance)dist),
            //             GetStreetColor((InputController.RenderingDistance)dist),
            //             GetBorderColor((InputController.RenderingDistance)dist),
            //             startIntersection.intersectingStreets.Count == 1,
            //             endIntersection.intersectingStreets.Count == 1,
            //             lineLayer, lineOutlineLayer);
            //     }

            //     return;
            // }

            foreach (var distVal in Enum.GetValues(typeof(InputController.RenderingDistance)))
            {
                var dist = (InputController.RenderingDistance)distVal;
                if (!meshes.TryGetValue(dist, out StreetSegmentMeshInfo meshInfo))
                {
                    meshInfo = new StreetSegmentMeshInfo();
                    meshes.Add(dist, meshInfo);
                }

                var width = GetStreetWidth(dist);
                if (!width.Equals(0f))
                {
                    meshInfo.streetMesh = MeshBuilder.CreateSmoothLine(positions, width, 20, lineLayer);
                }

                var borderWidth = GetBorderWidth(dist);
                if (!borderWidth.Equals(0f))
                {
                    PolygonCollider2D collider = null;
                    if (dist == InputController.RenderingDistance.Near)
                    {
                        collider = GetComponent<PolygonCollider2D>();
                    }

                    meshInfo.outlineMesh = MeshBuilder.CreateSmoothLine(
                        positions, width + borderWidth, 20,
                        lineOutlineLayer, false, collider);
                }
            }

            UpdateScale(street.map.input?.renderingDistance ?? InputController.RenderingDistance.Near);
        }

        Tuple<Mesh, Mesh> GetTramTrackMesh(Vector3[] path, bool isRightLane)
        {
            var trackDistance = 1.2f;
            var trackWidth = 0.15f;
            var offset = (isRightLane ? -1f : +1f);

            var meshRight = MeshBuilder.CreateSmoothLine(
                path, trackWidth, 20,
                Map.Layer(MapLayer.StreetMarkings),
                false, null, false, false,
                trackDistance * .5f + trackWidth * .5f + offset);

            var meshLeft = MeshBuilder.CreateSmoothLine(path, trackWidth, 20,
                Map.Layer(MapLayer.StreetMarkings),
                false, null, false, false,
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

        void CreateTramTrackMesh(StreetSegmentMeshInfo meshInfo)
        {
            var trafficSim = street.map.Game.sim.trafficSim;

            // Create tracks for right lane.
            var rightLane = this.RightmostLane;
            var rightPath = trafficSim.GetPath(this, rightLane);
            var rightMeshes = GetTramTrackMesh(rightPath, true);

            var trackMeshes = new List<Mesh> { meshInfo.outlineMesh, rightMeshes.Item1,
                rightMeshes.Item2 };

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

            meshInfo.outlineMesh = MeshBuilder.CombineMeshes(trackMeshes);
            UpdateScale(inputController.renderingDistance);
        }

        void UpdateTramTracks(Mesh trackRight, Mesh trackLeft)
        {
            var meshInfo = this.meshes[InputController.RenderingDistance.Near];
            meshInfo.outlineMesh = MeshBuilder.CombineMeshes(meshInfo.outlineMesh, trackRight,
                                                             trackLeft);

            UpdateScale(inputController.renderingDistance);
        }

        public void AddTramTracks()
        {
            this.hasTramTracks = true;
            this.CreateTramTrackMesh(meshes[InputController.RenderingDistance.Near]);
        }

        public void UpdateMesh(List<Vector3> positions)
        {
            this.positions = positions;
            CreateMeshes();
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

#if DEBUG
            var trafficSim = street.map.Game.sim.trafficSim;
            if (_laneMeshes == null)
            {
                _laneMeshes = new Mesh[street.lanes];
                for (int lane = 0; lane < street.lanes; ++lane)
                {
                    var path = trafficSim.GetPath(this, lane);
                    _laneMeshes[lane] = MeshBuilder.CreateSmoothLine(new List<Vector3>(path), 1f * Map.Meters);
                }
            }
            if (!street.isOneWay && startIntersection != null && _startIntersectionMeshes == null)
            {
                _startIntersectionMeshes = new List<Mesh>();

                for (int lane = street.lanes / 2; lane < street.lanes; ++lane)
                {
                    foreach (var outgoing in startIntersection.OutgoingStreets)
                    {
                        var path = trafficSim.GetPath(startIntersection, this, outgoing, lane);
                        _startIntersectionMeshes.Add(MeshBuilder.CreateSmoothLine(new List<Vector3>(path), .75f * Map.Meters));
                    }
                }
            }
            if (endIntersection != null && _endIntersectionMeshes == null)
            {
                _endIntersectionMeshes = new List<Mesh>();

                for (int lane = street.lanes / 2; lane < street.lanes; ++lane)
                {
                    foreach (var outgoing in endIntersection.OutgoingStreets)
                    {
                        var path = trafficSim.GetPath(endIntersection, this, outgoing, lane);
                        _endIntersectionMeshes.Add(MeshBuilder.CreateSmoothLine(new List<Vector3>(path), .75f * Map.Meters));
                    }
                }
            }

            for (int lane = 0; lane < street.lanes; ++lane)
            {
                if (lane == 0)
                {
                    Gizmos.color = Color.blue;
                }
                else
                {
                    Gizmos.color = new Color(252f / 255f, 169f / 255f, 4f / 255f);
                }

                var mesh = _laneMeshes[lane];
                Gizmos.DrawMesh(mesh);
            }

            if (_startIntersectionMeshes != null)
            {
                foreach (var mesh in _startIntersectionMeshes)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawMesh(mesh);
                }
            }

            if (_endIntersectionMeshes != null)
            {
                foreach (var mesh in _endIntersectionMeshes)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawMesh(mesh);
                }
            }
#endif
        }

        void UpdateDirectionalArrows(InputController.RenderingDistance dist)
        {
            if (directionArrow == null)
            {
                return;
            }

            if (dist == InputController.RenderingDistance.Near)
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
                case Street.Type.Primary:
                    min = 9f * Map.Meters;
                    max = 15f * Map.Meters;
                    factor = 6f / (100f * Map.Meters);

                    break;
                default:
                    min = 7.5f * Map.Meters;
                    max = 15f * Map.Meters;
                    factor = 5f / (100f * Map.Meters);

                    break;
            }

            return Mathf.Clamp(factor * orthographicSize, min, max);
        }

        public void UpdateTextScale(InputController.RenderingDistance dist)
        {
            UpdateDirectionalArrows(dist);

            if (streetName == null)
            {
                return;
            }

            var setTextActive = false;
            switch (dist)
            {
                case InputController.RenderingDistance.Far:
                    switch (street.type)
                    {
                        case Street.Type.Primary:
                            setTextActive = true;
                            break;
                        default:
                            break;
                    }

                    break;
                case InputController.RenderingDistance.VeryFar:
                case InputController.RenderingDistance.Farthest:
                    break;
                default:
                    if (streetName != null)
                    {
                        setTextActive = true;
                    }

                    break;
            }

            if (setTextActive)
            {
                streetName.gameObject.SetActive(true);
                streetName.textMesh.fontSize = GetFontSize(Camera.main.orthographicSize);
            }
            else
            {
                streetName.gameObject.SetActive(false);
            }
        }

        public void UpdateScale(InputController.RenderingDistance dist)
        {
            if (!meshes.TryGetValue(dist, out StreetSegmentMeshInfo meshInfo))
            {
                return;
            }

            if (streetMeshObj == null)
            {
                streetMeshObj = Instantiate(street.map.meshPrefab);
                streetMeshObj.transform.SetParent(this.transform);

                outlineMeshObj = Instantiate(street.map.meshPrefab);
                outlineMeshObj.transform.SetParent(this.transform);
            }

            var streetRenderer = streetMeshObj.GetComponent<MeshRenderer>();
            var streetFilter = streetMeshObj.GetComponent<MeshFilter>();

            streetFilter.mesh = meshInfo.streetMesh;
            streetRenderer.material = GameController.GetUnlitMaterial(GetStreetColor(dist));

            var outlineRenderer = outlineMeshObj.GetComponent<MeshRenderer>();
            var outlineFilter = outlineMeshObj.GetComponent<MeshFilter>();

            outlineFilter.mesh = meshInfo.outlineMesh;
            outlineRenderer.material = GameController.GetUnlitMaterial(GetBorderColor(dist));

            // var meshCollider = GetComponent<MeshCollider>();
            // meshCollider.sharedMesh = null;
            // meshCollider.sharedMesh = streetFilter.sharedMesh;

            // if (streetFilter.sharedMesh != null)
            // {
            //     var polygonCollider = GetComponent<PolygonCollider2D>();
            //     MeshBuilder.MeshCollider(streetFilter.sharedMesh, polygonCollider);
            // }
        }

        public void UpdateColor(Color c)
        {
            var streetRenderer = streetMeshObj.GetComponent<MeshRenderer>();
            streetRenderer.material = GameController.GetUnlitMaterial(c);
        }

        public void ResetColor(InputController.RenderingDistance dist)
        {
            UpdateColor(GetStreetColor(dist));
        }

        public void UpdateBorderColor(Color c)
        {
            var outlineRenderer = outlineMeshObj.GetComponent<MeshRenderer>();
            outlineRenderer.material = GameController.GetUnlitMaterial(c);
        }

        public void ResetBorderColor(InputController.RenderingDistance dist)
        {
            UpdateBorderColor(GetBorderColor(dist));
        }

        public SerializedStreetSegment Serialize()
        {
            if (hasTramTracks)
            {
                Debug.Log("henlo");
            }
            return new SerializedStreetSegment
            {
                positions = positions.Select(p => new SerializableVector3(p)).ToList(),
                startIntersectionID = startIntersection?.id ?? 0,
                endIntersectionID = endIntersection?.id ?? 0,
                hasTramTracks = hasTramTracks,
            };
        }

        public void DeleteSegment()
        {
            street.DeleteSegment(this);
            startIntersection?.DeleteSegment(this);
            endIntersection?.DeleteSegment(this);

            foreach (var tiles in street.map.tiles)
            {
                foreach (var tile in tiles)
                {
                    tile.streetSegments.Remove(this);
                }
            }

            Destroy(this.gameObject);
            Destroy(this.streetName?.gameObject ?? null);
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
    }
}
