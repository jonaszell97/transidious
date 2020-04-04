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

        public static readonly float DefaultGreenTime = 10f;
        public static readonly float DefaultRedTime = 10f;
        public static readonly float DefaultYellowTime = 2f;
        public static readonly float DefaultYellowRedTime = 4f;

        public TrafficLight(float initialRedTime, float redTime, bool green = false)
        {
            this.redTime = redTime;
            this.timeToNextSwitch = initialRedTime;
            this.status = green ? Status.Green : Status.Red;
        }

        public Status status;
        float timeToNextSwitch;
        float redTime;

        public TimeSpan TimeUntilNextRedPhase
        {
            get
            {
                switch (status)
                {
                    case Status.Red:
                    case Status.YellowRed:
                    default:
                        return TimeSpan.Zero;
                    case Status.Yellow:
                        return TimeSpan.FromSeconds(timeToNextSwitch);
                    case Status.Green:
                        return TimeSpan.FromSeconds(timeToNextSwitch + DefaultYellowTime);
                }
            }
        }

#if DEBUG
        public GameObject spriteObj1;
        public GameObject spriteObj2;

        public string SpriteName
        {
            get
            {
                switch (status)
                {
                    case Status.Green:
                        default:
                        return "Sprites/tl_green";
                    case Status.Red:
                        return "Sprites/tl_red";
                    case Status.Yellow:
                        return "Sprites/tl_yellow";
                    case Status.YellowRed:
                        return "Sprites/tl_yellow_red";
                }
            }
        }
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
                spriteObj1.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite(SpriteName);
            }
            if (spriteObj2 != null)
            {
                spriteObj2.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite(SpriteName);
            }
#endif
        }

        public void Update(float delta)
        {
            timeToNextSwitch -= delta;
            if (timeToNextSwitch <= 0f)
            {
                Switch();
            }
        }

        public bool MustStop => status == Status.Red || status == Status.YellowRed;
    }

    public class StreetIntersection : StaticMapObject, IStop
    {
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
            base.Initialize(MapObjectKind.StreetIntersection, id);

            this.id = id;
            this.name = "";
            this.position = pos;
            this.intersectingStreets = new List<StreetSegment>();
            this.streetAngles = new Dictionary<StreetSegment, float>();
        }

        public new Serialization.StreetIntersection ToProtobuf()
        {
            return new Serialization.StreetIntersection
            {
                MapObject = base.ToProtobuf(),
                Position = ((Vector2)position).ToProtobuf(),
            };
        }

        public static StreetIntersection Deserialize(Serialization.StreetIntersection inter, Map map)
        {
            var obj = map.CreateIntersection(inter.Position.Deserialize(), (int)inter.MapObject.Id);
            obj.Deserialize(inter.MapObject);

            return obj;
        }

        public void DeleteSegment(StreetSegment seg)
        {
            intersectingStreets.Remove(seg);
        }

        public Vector3 Location => position;

        public IEnumerable<IRoute> Routes
        {
            get
            {
                return intersectingStreets.Select(s => s as IRoute);
            }
        }

        public bool IsGoalReached(IStop goal)
        {
            switch (goal)
            {
                case StreetIntersection intersection:
                    return intersection == this;
                case PointOnStreet pos:
                    return intersectingStreets.Contains(pos.street);
            }

            return false;
        }

        public bool uTurnAllowed => intersectingStreets.Count == 1;

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
            if (seg.positions.Count == 1)
            {
                Debug.LogWarning("segment has only one position!");
                return seg.positions[0];
            }
            if (seg.positions.Count == 0)
            {
                Debug.LogWarning("segment has no positions!");
                return Vector3.zero;
            }

            if (this == seg.startIntersection)
            {
                return seg.positions[1];
            }

            Debug.Assert(this == seg.endIntersection, "street does not intersect here");
            return seg.positions[seg.positions.Count - 2];
        }

        public Vector3 GetTrafficLightPosition(StreetSegment seg)
        {
            return GetOffsetPosition(seg, 10f);
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
            intersectingStreets.Add(seg);
        }

        void CalculateRelativePositions()
        {
            if (relativePositions == null)
            {
                relativePositions = new Dictionary<StreetSegment, int>();
            }
            else
            {
                relativePositions.Clear();
                streetAngles.Clear();
            }

            var baseSegment = intersectingStreets[0];
            relativePositions.Add(baseSegment, 0);

            var angles = new Tuple<StreetSegment, float>[intersectingStreets.Count - 1];
            var baseDirection = baseSegment.RelativeDirection(this);

            streetAngles.Add(baseSegment, Math.DirectionalAngleRad(baseDirection, baseDirection) * Mathf.Rad2Deg);

            for (var i = 1; i < intersectingStreets.Count; ++i)
            {
                var otherSegment = intersectingStreets[i];
                var otherDirection = otherSegment.RelativeDirection(this);

                var angle = Math.DirectionalAngleRad(baseDirection, otherDirection);
                streetAngles.Add(otherSegment, angle * Mathf.Rad2Deg);

                angles[i - 1] = Tuple.Create(otherSegment, angle);
            }

            Array.Sort(angles, (v1, v2) => v1.Item2.CompareTo(v2.Item2));

            for (var i = 1; i < intersectingStreets.Count; ++i)
            {
                var (seg, _) = angles[i - 1];
                intersectingStreets[i] = seg;
                relativePositions.Add(seg, i);
            }
            
            /*var numSlots = intersectingStreets.Count;
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

            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < 3; ++j)
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

            emptySlot = 2;*/
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
            if (GameController.instance.sim.trafficSim.renderStreetOrder)
            {
                foreach (var seg in intersectingStreets)
                {
                    var pos = GetOffsetPosition(seg, 5);
                    pos.z = Map.Layer(MapLayer.Foreground);

                    var txt = map.CreateText(pos, relativePositions[seg].ToString(), Color.black);
                    txt.textMesh.fontSize = 5;
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
                if (trafficSim.renderTrafficLights)
                {
                    var sprite1 = SpriteManager.CreateSprite(tl.SpriteName);

                    sprite1.transform.SetParent(map.transform);
                    sprite1.transform.position = GetTrafficLightPosition(seg);
                    sprite1.transform.SetLayer(MapLayer.Foreground);

                    tl.spriteObj1 = sprite1;

                    if (oppositeSeg != null)
                    {
                        var sprite2 = SpriteManager.CreateSprite(tl.SpriteName);

                        sprite2.transform.SetParent(map.transform);
                        sprite2.transform.position = GetTrafficLightPosition(oppositeSeg);
                        sprite2.transform.SetLayer(MapLayer.Foreground);

                        tl.spriteObj2 = sprite2;
                    }
                }
#endif
            }
        }
    }
}