using UnityEngine;
using System;
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

    public class StreetIntersection : MapObject, IStop
    {
        [System.Serializable]
        public struct SerializedStreetIntersection
        {
            public SerializableMapObject mapObject;
            public SerializableVector2 position;
        }

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

        public void Initialize(int id, Vector3 pos)
        {
            base.Initialize(Kind.StreetIntersection, id);

            this.id = id;
            this.position = pos;
            this.intersectingStreets = new List<StreetSegment>();
            this.streetAngles = new Dictionary<StreetSegment, float>();

            this.transform.localScale = new Vector3(14, 14, 0);
            this.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.StreetOutlines));
        }

        public new SerializedStreetIntersection Serialize()
        {
            return new SerializedStreetIntersection
            {
                mapObject = base.Serialize(),
                position = new SerializableVector2(position),
            };
        }

        public static StreetIntersection Deserialize(Map map, SerializedStreetIntersection inter)
        {
            var obj = map.CreateIntersection(inter.position.ToVector(),
                                             inter.mapObject.id);

            obj.Deserialize(inter.mapObject);
            return obj;
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
            var angle = Math.PointAngleDeg(p0, position);

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
            bool foundLink = false;

            foreach (var seg in intersectingStreets)
            {
                allOneWay &= seg.street.isOneWay;
                switch (seg.street.type)
                {
                case Street.Type.Primary:
                case Street.Type.Secondary:
                    // case Street.Type.Tertiary:
                    shouldGenerateTrafficLights = true;
                    break;
                case Street.Type.Link:
                    foundLink = true;
                    break;
                default:
                    break;
                }
            }

            if (!shouldGenerateTrafficLights || allOneWay || foundLink)
            {
                return;
            }

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
}