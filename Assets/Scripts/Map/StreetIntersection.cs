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

        private static int _lastAssignedID = 0;
        public static readonly float DefaultGreenTime = 10f;
        public static readonly float DefaultRedTime = 10f;
        public static readonly float DefaultYellowTime = 2f;
        public static readonly float DefaultYellowRedTime = 4f;

        public TrafficLight(int numTrafficLights, int greenPhase)
        {
            this._timeToNextSwitch = greenPhase == 0
                ? DefaultGreenTime
                : greenPhase * (DefaultRedTime + DefaultYellowTime)
                  + (greenPhase - 1) * DefaultYellowRedTime;

            this._redTime = (numTrafficLights - 1) 
                          * (DefaultRedTime
                             + DefaultYellowTime
                             + DefaultYellowRedTime);

            this.Id = ++_lastAssignedID;
            this.GreenPhase = greenPhase;
            this._status = greenPhase == 0 ? Status.Green : Status.Red;
        }

        public TrafficLight(Serialization.TrafficLight tl)
        {
            Id = tl.ID;
            _status = (Status) tl.Status;
            _timeToNextSwitch = tl.TimeToNextSwitch;
            _redTime = tl.RedTime;
            GreenPhase = tl.GreenPhase;
        }

        public readonly int Id;
        private Status _status;
        private float _timeToNextSwitch;
        private readonly float _redTime;
        public int GreenPhase { get; private set; }
        public Status CurrentPhase => _status;

        public TimeSpan TimeUntilNextRedPhase
        {
            get
            {
                switch (_status)
                {
                    case Status.Red:
                    case Status.YellowRed:
                    default:
                        return TimeSpan.Zero;
                    case Status.Yellow:
                        return TimeSpan.FromSeconds(_timeToNextSwitch);
                    case Status.Green:
                        return TimeSpan.FromSeconds(_timeToNextSwitch + DefaultYellowTime);
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
                switch (_status)
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
            switch (_status)
            {
            case Status.Green:
                _timeToNextSwitch = DefaultGreenTime;
                break;
            case Status.Red:
                _timeToNextSwitch = _redTime;
                break;
            case Status.Yellow:
                _timeToNextSwitch = DefaultYellowTime;
                break;
            case Status.YellowRed:
                _timeToNextSwitch = DefaultYellowRedTime;
                break;
            }
        }

        public void Switch()
        {
            _status = (Status)(((int)_status + 1) % 4);
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
            _timeToNextSwitch -= delta;
            if (_timeToNextSwitch <= 0f)
            {
                Switch();
            }
        }

        public bool MustStop => _status == Status.Red || _status == Status.YellowRed;

        public Serialization.TrafficLight Serialize()
        {
            return new Serialization.TrafficLight
            {
                ID = Id,
                Status = (int)_status,
                RedTime = _redTime,
                TimeToNextSwitch = _timeToNextSwitch,
                GreenPhase = GreenPhase,
            };
        }
    }

    public class StreetIntersection : StaticMapObject, IStop
    {
        /// Position of the intersection.
        public Vector3 position => centroid;

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
            this.centroid = pos;
            this.intersectingStreets = new List<StreetSegment>();
            this.streetAngles = new Dictionary<StreetSegment, float>();
        }

        public new Serialization.StreetIntersection ToProtobuf()
        {
            return new Serialization.StreetIntersection
            {
                MapObject = base.ToProtobuf(),
            };
        }

        public static StreetIntersection Deserialize(Serialization.StreetIntersection inter, Map map)
        {
            var obj = map.CreateIntersection(inter.MapObject.Centroid.Deserialize(), (int)inter.MapObject.Id);
            obj.Deserialize(inter.MapObject);

            return obj;
        }

        public void DeleteSegment(StreetSegment seg)
        {
            intersectingStreets.Remove(seg);
        }

        public Vector2 Location => position;

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
                return intersectingStreets.Where(s => !s.IsOneWay || s.endIntersection == this);
            }
        }

        public IEnumerable<StreetSegment> OutgoingStreets
        {
            get
            {
                return intersectingStreets.Where(s => !s.IsOneWay || s.startIntersection == this);
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

        public void CalculateRelativePositions()
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

        /// Return a unique index for every intersection path.
        public int GetIndexForIntersectionPath(StreetSegment from, StreetSegment to)
        {
            var fromPos = RelativePosition(from);
            var toPos = RelativePosition(to);

            return (fromPos * intersectingStreets.Count) + toPos;
        }

#if DEBUG
        public void CreateTrafficLightSprites()
        {
            var map = SaveManager.loadedMap;
            foreach (var seg in intersectingStreets)
            {
                var tl = seg.GetTrafficLight(this);
                if (tl == null) 
                    continue;

                var sprite1 = SpriteManager.CreateSprite(tl.SpriteName);
                sprite1.transform.SetParent(map.transform);
                sprite1.transform.position = GetTrafficLightPosition(seg);
                sprite1.transform.SetLayer(MapLayer.Foreground);
                sprite1.name = tl.Id.ToString();

                if (tl.spriteObj1 == null)
                    tl.spriteObj1 = sprite1;
                else
                    tl.spriteObj2 = sprite1;
            }
        }
        
        public override void ActivateModal()
        {
            var modal = MainUI.instance.intersectionModal;
            modal.SetIntersection(this);
            modal.modal.Enable();
        }
#endif
    }
}