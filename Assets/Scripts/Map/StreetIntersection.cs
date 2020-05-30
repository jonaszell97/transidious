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
            Debug.Assert(numTrafficLights > 1);

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

    public class IntersectionPattern
    {
        public enum Type
        {
            /// A two-way by two-way intersection.
            TwoWayByTwoWay,

            /// A double-one-way by two-way intersection.
            DoubleOneWayByTwoWay,

            /// A double-one-way by double-one-way intersection.
            DoubleOneWayByDoubleOneWay,
        }

        /// The last assigned id.
        private static int _lastAssignedID;

        /// The ID of this pattern.
        public int ID;

        /// Pattern type.
        public readonly Type PatternType;

        /// The streets that are part of this pattern.
        private StreetSegment[] _segments;

        /// Private C'tor.
        private IntersectionPattern(Map map, Type type)
        {
            this.ID = ++_lastAssignedID;
            this.PatternType = type;
            map.IntersectionPatterns.Add(ID, this);
        }

        /// Private C'tor.
        private IntersectionPattern(Map map, Type type, int id)
        {
            this.ID = id;
            this.PatternType = type;
            map.IntersectionPatterns.Add(ID, this);
        }

        /// Initialize with deserialized segment IDs.
        public void Initialize(Serialization.IntersectionPattern pattern, Map map)
        {
            _segments = pattern.SegmentIDs.Select(id => map.GetMapObject<StreetSegment>(id)).ToArray();
        }

        /*
                Pattern: Two-Way Road x Two-Way Road
                
                            R4
                        |   |   |
                        |   |   |
                        | Y |   |
                 -------         -------
                                 X
             R1  -------         ------- R3
                       X            
                 -------         -------
                        |   | Y |
                        |   |   |
                        |   |   |
                            R2

                Traffic lights:
                    X: R1, R3
                    Y: R2, R4
        */
        public enum TwoWayByTwoWayStreets
        {
            R1 = 0, R3, R2, R4
        }

        public static IntersectionPattern CreateTwoWayByTwoWay(Map map, StreetSegment R1, StreetSegment R3,
                                                               StreetSegment R2, StreetSegment R4)
        {
            return new IntersectionPattern(map, Type.TwoWayByTwoWay)
            {
                _segments = new[] {R1, R3, R2, R4},
            };
        }

        public StreetSegment GetStreet(TwoWayByTwoWayStreets n)
        {
            Debug.Assert(PatternType == Type.TwoWayByTwoWay);
            return _segments[(int) n];
        }

        /*
                Pattern: Double One-Way Road x Two-Way Road

                            R4
                        |    |    |
                        |    |    |
                        | Y  |    |
                 -------    ---    -------
             R3B   <--             X <--   R3A
                 -------     |     -------
                        |   R5    |
                 -------     |     ------- 
             R1A   --> X             -->   R1B
                 -------           -------
                        |    |  Y |
                        |    |    |
                        |    |    |
                            R2

                Traffic lights:
                    X: R1A, R3A
                    Y: R2, R4
        */
        public enum DoubleOneWayByTwoWayStreets
        {
            R1A = 0, R1B, R2, R4, R3A, R3B
        }

        public static IntersectionPattern CreateDoubleOneWayByTwoWay(Map map, StreetSegment R1A, StreetSegment R1B,
                                                                     StreetSegment R2, StreetSegment R4,
                                                                     StreetSegment R3A, StreetSegment R3B)
        {
            return new IntersectionPattern(map, Type.DoubleOneWayByTwoWay)
            {
                _segments = new[] {R1A, R1B, R2, R4, R3A, R3B},
            };
        }
        
        public StreetSegment GetStreet(DoubleOneWayByTwoWayStreets n)
        {
            Debug.Assert(PatternType == Type.DoubleOneWayByTwoWay);
            return _segments[(int) n];
        }
        
        /*
                Pattern: Double One-Way Road x Double One-Way Road
                
                         R4A   R2B
                        | | | | A |
                        | V | | | |
                        |   | |   |
                 -------  Y ---    -------
             R3B   <--      <R8    X <--   R3A
                 -------    |-| A  -------
                        | R5| |R6 |
                 -------  V |-|    ------- 
             R1A   --> X    R7>      -->   R1B
                 -------    --- Y  -------
                        |   | |   |
                        | | | | A |
                        | V | | | |
                         R4B   R2A

                Traffic lights:
                    X: R1A (end), R3A (end)
                    Y: R2A (end), R4A (end)
        */
        public enum DoubleOneWayByDoubleOneWayStreets
        {
            R1A = 0, R1B, R2A, R2B, R3A, R3B, R4A, R4B, R5, R6, R7, R8,
        }
        
        public static IntersectionPattern CreateDoubleOneWayByDoubleOneWay(Map map, StreetSegment R1A, StreetSegment R1B,
                                                                           StreetSegment R2A, StreetSegment R2B,
                                                                           StreetSegment R3A, StreetSegment R3B,
                                                                           StreetSegment R4A, StreetSegment R4B,
                                                                           StreetSegment R5, StreetSegment R6,
                                                                           StreetSegment R7, StreetSegment R8)
        {
            return new IntersectionPattern(map, Type.DoubleOneWayByDoubleOneWay)
            {
                _segments = new[] {R1A, R1B, R2A, R2B, R3A, R3B, R4A, R4B, R5, R6, R7, R8},
            };
        }
        
        public StreetSegment GetStreet(DoubleOneWayByDoubleOneWayStreets n)
        {
            Debug.Assert(PatternType == Type.DoubleOneWayByDoubleOneWay);
            return _segments[(int) n];
        }

        public Serialization.IntersectionPattern Serialize()
        {
            var result = new Serialization.IntersectionPattern
            {
                ID = ID,
                Type = (Serialization.IntersectionPattern.Types.Type)PatternType,
            };

            result.SegmentIDs.AddRange(_segments.Select(s => s?.id ?? 0));
            return result;
        }

        public static IntersectionPattern Deserialize(Serialization.IntersectionPattern pattern, Map map)
        {
            var result = new IntersectionPattern(map, (Type)pattern.Type, pattern.ID);
            return result;
        }
    }

    public class StreetIntersection : StaticMapObject, IStop
    {
        /// Position of the intersection.
        public Vector3 Position => centroid;

        /// Intersecting streets.
        public List<StreetSegment> IntersectingStreets;

        /// Angles of intersecting streets.
        private Dictionary<StreetSegment, float> _streetAngles;

        /// Relative positions of intersecting streets; clockwise and paired based on
        /// traffic light placement.
        private Dictionary<StreetSegment, int> _relativePositions;

        /// The intersection pattern, or null if the pattern is unknown.
        private IntersectionPattern _pattern;
        public IntersectionPattern Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;

                if (value?.PatternType == IntersectionPattern.Type.TwoWayByTwoWay)
                {
                    var R1 = value.GetStreet(IntersectionPattern.TwoWayByTwoWayStreets.R1);
                    var R2 = value.GetStreet(IntersectionPattern.TwoWayByTwoWayStreets.R2);
                    var R3 = value.GetStreet(IntersectionPattern.TwoWayByTwoWayStreets.R3);
                    var R4 = value.GetStreet(IntersectionPattern.TwoWayByTwoWayStreets.R4);

                    IntersectingStreets.Clear();
                    if (R1 != null)
                        IntersectingStreets.Add(R1);
                    if (R2 != null)
                        IntersectingStreets.Add(R2);
                    if (R3 != null)
                        IntersectingStreets.Add(R3);
                    if (R4 != null)
                        IntersectingStreets.Add(R4);

                    if (_relativePositions == null)
                    {
                        _relativePositions = new Dictionary<StreetSegment, int>();
                        if (R1 != null)
                            _relativePositions.Add(R1, 0);
                        if (R2 != null)
                            _relativePositions.Add(R2, 1);
                        if (R3 != null)
                            _relativePositions.Add(R3, 2);
                        if (R4 != null)
                            _relativePositions.Add(R4, 3);
                    }
                    else
                    {
                        if (R1 != null)
                            _relativePositions[R1] = 0;
                        if (R2 != null)
                            _relativePositions[R2] = 1;
                        if (R3 != null)
                            _relativePositions[R3] = 2;
                        if (R4 != null)
                            _relativePositions[R4] = 3;
                    }
                }
            }
        }

        public void Initialize(int id, Vector3 pos)
        {
            base.Initialize(MapObjectKind.StreetIntersection, id);

            this.id = id;
            this.name = "";
            this.centroid = pos;
            this.IntersectingStreets = new List<StreetSegment>();
            this._streetAngles = new Dictionary<StreetSegment, float>();
        }

        public new Serialization.StreetIntersection ToProtobuf()
        {
            return new Serialization.StreetIntersection
            {
                MapObject = base.ToProtobuf(),
                PatternID = Pattern?.ID ?? 0,
            };
        }

        public static StreetIntersection Deserialize(Serialization.StreetIntersection inter, Map map)
        {
            var obj = map.CreateIntersection(inter.MapObject.Centroid.Deserialize(), (int)inter.MapObject.Id);
            obj.Deserialize(inter.MapObject);

            if (inter.PatternID != 0)
            {
                obj._pattern = map.IntersectionPatterns[inter.PatternID];
            }

            return obj;
        }

        public void DeleteSegment(StreetSegment seg)
        {
            IntersectingStreets.Remove(seg);
        }

        public Vector2 Location => Position;

        public IEnumerable<IRoute> Routes
        {
            get
            {
                return IntersectingStreets.Select(s => s as IRoute);
            }
        }

        public bool IsGoalReached(IStop goal)
        {
            switch (goal)
            {
                case StreetIntersection intersection:
                    return intersection == this;
                case PointOnStreet pos:
                    return IntersectingStreets.Contains(pos.street);
            }

            return false;
        }

        public bool uTurnAllowed => IntersectingStreets.Count == 1;

        public IEnumerable<StreetSegment> IncomingStreets
        {
            get
            {
                return IntersectingStreets.Where(s => !s.IsOneWay || s.endIntersection == this);
            }
        }

        public IEnumerable<StreetSegment> OutgoingStreets
        {
            get
            {
                return IntersectingStreets.Where(s => !s.IsOneWay || s.startIntersection == this);
            }
        }

        public float GetAngle(StreetSegment seg)
        {
            return _streetAngles[seg];
        }

        public int RelativePosition(StreetSegment seg)
        {
            if (_relativePositions == null || !_relativePositions.ContainsKey(seg))
            {
                CalculateRelativePositions();
            }

            return _relativePositions[seg];
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
            var vec = (nextPos - Position).normalized;

            return Position + vec * offset;
        }

        public void AddIntersectingStreet(StreetSegment seg)
        {
            IntersectingStreets.Add(seg);
        }

        public void CalculateRelativePositions()
        {
            if (_relativePositions == null)
            {
                _relativePositions = new Dictionary<StreetSegment, int>();
            }
            else
            {
                _relativePositions.Clear();
                _streetAngles.Clear();
            }

            var baseSegment = IntersectingStreets[0];
            _relativePositions.Add(baseSegment, 0);

            var angles = new Tuple<StreetSegment, float>[IntersectingStreets.Count - 1];
            var baseDirection = baseSegment.RelativeDirection(this);

            _streetAngles.Add(baseSegment, Math.DirectionalAngleRad(baseDirection, baseDirection) * Mathf.Rad2Deg);

            for (var i = 1; i < IntersectingStreets.Count; ++i)
            {
                var otherSegment = IntersectingStreets[i];
                var otherDirection = otherSegment.RelativeDirection(this);

                var angle = Math.DirectionalAngleRad(baseDirection, otherDirection);
                _streetAngles.Add(otherSegment, angle * Mathf.Rad2Deg);

                angles[i - 1] = Tuple.Create(otherSegment, angle);
            }

            Array.Sort(angles, (v1, v2) => v1.Item2.CompareTo(v2.Item2));

            for (var i = 1; i < IntersectingStreets.Count; ++i)
            {
                var (seg, _) = angles[i - 1];
                IntersectingStreets[i] = seg;
                _relativePositions.Add(seg, i);
            }
        }

        public StreetSegment GetStreetAtSlot(int slot)
        {
            foreach (var seg in IntersectingStreets)
            {
                if (_relativePositions[seg] == slot)
                    return seg;
            }

            return null;
        }

        /// Return a unique index for every intersection path.
        public int GetIndexForIntersectionPath(StreetSegment from, StreetSegment to)
        {
            var fromPos = RelativePosition(from);
            var toPos = RelativePosition(to);
            var n = Pattern?.PatternType == IntersectionPattern.Type.TwoWayByTwoWay ? 4 : IntersectingStreets.Count;

            return (fromPos * n) + toPos;
        }

#if DEBUG
        public void CreateTrafficLightSprites()
        {
            var map = SaveManager.loadedMap;
            foreach (var seg in IntersectingStreets)
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

        private List<LineRenderer> _paths;

        public void RenderPaths()
        {
            if (_paths == null)
            {
                _paths = new List<LineRenderer>();
            }
            else
            {
                _paths.Clear();
            }

            var builder = GameController.instance.sim.trafficSim.StreetPathBuilder;
            var points = new List<Vector2>();

            foreach (var incoming in IncomingStreets)
            {
                foreach (var outgoing in OutgoingStreets)
                {
                    var path = builder.GetIntersectionPath(this, incoming, outgoing);
                    path.AddPoints(points, 5);

                    var obj = Utility.DrawLine(points.ToArray(), 1f, Color.green, Map.Layer(MapLayer.Foreground), false, true);
                    _paths.Add(obj.GetComponent<LineRenderer>());
                    
                    points.Clear();
                }
            }
        }

        public void UpdateOccupation(ulong mask)
        {
            if (_paths == null)
                return;

            var i = 0;
            foreach (var incoming in IncomingStreets)
            {
                foreach (var outgoing in OutgoingStreets)
                {
                    var offset = GetIndexForIntersectionPath(incoming, outgoing);
                    var isOccupied = (mask & (0b1111ul << (offset * 4))) != 0;

                    var color = isOccupied ? Color.red : Color.green;
                    _paths[i].material = GameController.instance.GetUnlitMaterial(color);
                    _paths[i].startColor = color;
                    _paths[i].endColor = color;

                    ++i;
                }
            }
        }
#endif
    }
}