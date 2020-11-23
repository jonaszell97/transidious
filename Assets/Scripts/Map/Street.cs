using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class Street : StaticMapObject
    {
        public enum Type
        {
            Highway,
            Primary,
            Secondary,
            Tertiary,
            Residential,
            Link,
            Path,
            River,
        }

        /// The display name of the street. If null, same as name.
        public string displayName;

        /// Reference to the map.
        public Map map;

        /// The street type.
        public Type type = Type.Residential;

        /// The (ordered) partial streets of this street.
        public List<StreetSegment> segments;

        /// Whether or not this street is lit.
        public bool lit;

        /// The maximum speed (in kmh) of this street.
        public Velocity maxspeed;

        /// The number of lanes on the road.
        public int lanes;

        /// The length of this street.
        public float length;

        public void Initialize(Map map, Type type, string name, bool lit,
                               int maxspeed, int lanes, int id = -1)
        {
            base.Initialize(MapObjectKind.Street, id);

            this.map = map;
            this.type = type;
            this.name = name;
            this.lit = lit;
            this.maxspeed = Velocity.FromRealTimeKPH(maxspeed > 0 ? maxspeed : GetDefaultMaxSpeed());
            this.lanes = lanes > 0 ? lanes : GetDefaultLanes();
            this.segments = new List<StreetSegment>();
            this.length = 0f;
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }

                return name;
            }
        }

        public Velocity MaxSpeed => maxspeed;

        public Velocity AverageSpeed => maxspeed;

        int GetDefaultLanes()
        {
            switch (type)
            {
            case Type.Highway:
                return 4;
            case Type.Primary:
            case Type.Secondary:
                return 2;
            case Type.Tertiary:
            case Type.Residential:
                return 2;
            case Type.Path:
                return 2;// return isOneWay ? 1 : 2;
            case Type.River:
                return 2;
            default:
                return 2;
            }
        }

        int GetDefaultMaxSpeed()
        {
            switch (type)
            {
            case Type.Highway:
                return 100;
            case Type.Primary:
                return 70;
            case Type.Secondary:
                return 50;
            case Type.Tertiary:
            case Type.Residential:
            case Type.Path:
                return 30;
            default:
                return 50;
            }
        }

        float GetEffectiveAngle(float angle)
        {
            if (angle > 90)
            {
                angle = -(180f - angle);
            }
            else if (angle < -90)
            {
                angle = 180f + angle;
            }

            return angle;
        }

        class PositionOnStreetSegment
        {
            public Vector3 pos;
            public Vector3 direction;
            public float angle;
        }

        PositionOnStreetSegment GetPositionAndAngle(StreetSegment seg, float minWidth)
        {
            if (seg.length < minWidth)
                return null;

            var pos = Vector2.zero;
            var halfWidth = minWidth / 2f;
            var len = 0f;
            var middle = seg.length / 2f;
            var angle = 0f;
            var thresholdAngle = 7f;
            var isPositionUsable = false;
            var direction = Vector2.zero;

            for (int i = 1; i < seg.positions.Count; ++i)
            {
                var p0 = seg.positions[i - 1];
                var p1 = seg.positions[i];
                var segLen = (p1 - p0).magnitude;

                if (len + segLen >= middle)
                {
                    var dir = (p1 - p0).normalized;
                    var distToMiddle = middle - len;

                    pos = p0 + (dir * distToMiddle);

                    // Check if there is enough space for the text on the left.
                    int j = i - 2;
                    var leftPoint = p0;

                    while (j >= 0)
                    {
                        var pointAngle = Math.PointAngleDeg(p0, seg.positions[j]);
                        if (Mathf.Abs(pointAngle) > thresholdAngle)
                        {
                            break;
                        }

                        leftPoint = seg.positions[j];
                        --j;
                    }

                    var availableSpaceLeft = (pos - leftPoint).magnitude;
                    if (availableSpaceLeft < halfWidth)
                    {
                        continue;
                    }

                    // Check if there is enough space for the text on the right.
                    j = i + 1;
                    var rightPoint = p1;

                    while (j < seg.positions.Count)
                    {
                        var pointAngle = Math.PointAngleDeg(p1, seg.positions[j]);
                        if (Mathf.Abs(pointAngle) > thresholdAngle)
                        {
                            break;
                        }

                        rightPoint = seg.positions[j];
                        ++j;
                    }

                    var availableSpaceRight = (pos - rightPoint).magnitude;
                    if (availableSpaceRight < halfWidth)
                    {
                        continue;
                    }

                    isPositionUsable = true;
                    angle = GetEffectiveAngle(Math.PointAngleDeg(leftPoint, rightPoint));
                    direction = p1 - p0;

                    break;
                }

                len += segLen;
            }

            if (!isPositionUsable)
            {
                return null;
            }

            return new PositionOnStreetSegment
            {
                pos = pos,
                direction = direction,
                angle = angle
            };
        }

        void GenerateDirectionalArrow(StreetSegment seg, PositionOnStreetSegment posOnStreet)
        {
            if (!seg.IsOneWay || seg.length < 50f)
            {
                return;
            }

            if (seg.directionArrow == null)
            {
                seg.directionArrow = SpriteManager.CreateSprite(
                    SpriteManager.instance.streetArrowSprite);

                seg.directionArrow.transform.localScale = new Vector3(5f, 5f, 1f);

                seg.directionArrow.GetComponent<SpriteRenderer>().color =
                    new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            seg.directionArrow.transform.SetParent(seg.uniqueTile?.canvas.transform ?? map.canvas.transform);
            seg.directionArrow.transform.position = new Vector3(posOnStreet.pos.x, posOnStreet.pos.y,
                                                                Map.Layer(MapLayer.StreetMarkings));

            seg.directionArrow.transform.rotation = Quaternion.FromToRotation(Vector3.up,
                                                                              posOnStreet.direction);
        }

        void GenerateDirectionalArrow(StreetSegment seg)
        {
            if (!seg.IsOneWay || seg.length < 30f)
            {
                return;
            }

            var sprite = SpriteManager.instance.streetArrowSprite;
            var neededWidth = sprite.bounds.extents.y;
            var posAndAngle = GetPositionAndAngle(seg, neededWidth);

            if (posAndAngle == null)
            {
                return;
            }

            GenerateDirectionalArrow(seg, posAndAngle);
        }

        public void CreateTextMeshes()
        {
            foreach (var seg in segments)
            {
                GenerateDirectionalArrow(seg);
            }

            return;

            var txt = map.CreateText(Vector3.zero, DisplayName, new Color(0.3f, 0.3f, 0.3f, 1f));
            txt.textMesh.autoSizeTextContainer = true;
            txt.textMesh.fontSize = segments.First().GetFontSize(InputController.MaxZoom);
            txt.textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            txt.textMesh.ForceMeshUpdate();
            txt.gameObject.SetActive(false);

            float neededWidth = txt.textMesh.preferredWidth * 1.1f;
            float spaceBetweenLabels = 2f * neededWidth;
            float spaceSinceLastLabel = 0f;

            var placedText = false;
            var first = true;

            foreach (var seg in segments)
            {
                if (seg.length < neededWidth || (!first && spaceSinceLastLabel < spaceBetweenLabels))
                {
                    spaceSinceLastLabel += seg.length;
                    first = false;
                    GenerateDirectionalArrow(seg);

                    continue;
                }

                first = false;

                var posAndAngle = GetPositionAndAngle(seg, neededWidth);
                if (posAndAngle == null)
                {
                    spaceSinceLastLabel += seg.length;
                    continue;
                }

                placedText = true;
                spaceSinceLastLabel = 0f;

                if (seg.uniqueTile != null)
                {
                    txt.transform.SetParent(seg.uniqueTile.canvas.transform);
                }
                else
                {
                    txt.transform.SetParent(map.canvas.transform);
                }

                txt.transform.position = new Vector3(posAndAngle.pos.x,
                                                     posAndAngle.pos.y,
                                                     Map.Layer(MapLayer.StreetNames));


                txt.transform.rotation = Quaternion.AngleAxis(posAndAngle.angle, Vector3.forward);
                seg.streetName = txt;

                GenerateDirectionalArrow(seg, posAndAngle);
            }

            if (!placedText)
            {
                GameObject.Destroy(txt.gameObject);
            }
        }

        public void CalculateLength()
        {
            length = 0f;

            foreach (var seg in segments)
            {
                seg.CalculateLength();
                length += seg.length;
            }
        }

        public StreetSegment AddSegment(List<Vector2> path,
                                        StreetIntersection startIntersection,
                                        StreetIntersection endIntersection,
                                        int atPosition = -1,
                                        bool isOneWay = false,
                                        bool hasTramTracks = false,
                                        int segId = -1)
        {
            var seg = new StreetSegment();
            int pos;

            if (atPosition == -1)
            {
                pos = segments.Count;
                segments.Add(seg);
            }
            else
            {
                pos = atPosition;
                for (int i = pos; i < segments.Count; ++i)
                {
                    ++segments[i].position;
                }

                segments.Insert(pos, seg);
            }

            seg.Initialize(this, pos, path, startIntersection, endIntersection, 
                           isOneWay, hasTramTracks, segId);

#if DEBUG
            seg.name = this.name + ", Segment #" + this.segments.Count;
#endif

            map.RegisterSegment(seg, segId);
            return seg;
        }

        public void AddSegment(Serialization.StreetSegment seg)
        {
            var newSeg = AddSegment(seg.Positions.Select(v => v.Deserialize()).ToList(),
                                    map.GetMapObject<StreetIntersection>((int)seg.StartIntersectionID),
                                    map.GetMapObject<StreetIntersection>((int)seg.EndIntersectionID),
                                    -1, false,
                                    seg.HasTramTracks, (int)seg.MapObject.Id);

            var flags = (StreetSegment.Flags) seg.Flags;
            newSeg.flags = flags;

            newSeg.startTrafficLight = map.GetTrafficLight(seg.StartTrafficLightID);
            newSeg.endTrafficLight = map.GetTrafficLight(seg.EndTrafficLightID);
            newSeg.Deserialize(seg.MapObject);
        }

        public Tuple<StreetSegment, StreetSegment>
        SplitSegment(StreetSegment seg, StreetIntersection newIntersection)
        {
            seg.DeleteSegment();

            var splitPt = newIntersection.Location;
            var dist = ((Vector2)seg.positions.First() - splitPt).magnitude;

            int i = 1;
            for (; i < seg.positions.Count; ++i)
            {
                var currDist = (seg.positions.First() - seg.positions[i]).magnitude;
                if (currDist >= dist)
                {
                    break;
                }
            }

            var firstSegPositions = seg.positions.GetRange(0, i).ToList();
            firstSegPositions.Add(splitPt);

            var firstSeg = AddSegment(firstSegPositions, seg.startIntersection,
                                      newIntersection, seg.position);

            var secondSegPositions = seg.positions.GetRange(i, seg.positions.Count - i);
            secondSegPositions.Insert(0, splitPt);

            var sndSeg = AddSegment(secondSegPositions, newIntersection,
                                    seg.endIntersection, seg.position + 1);

            return new Tuple<StreetSegment, StreetSegment>(firstSeg, sndSeg);
        }

        public void DeleteSegment(StreetSegment seg)
        {
            length -= seg.length;
            segments.RemoveAt(seg.position);

            for (int i = seg.position; i < segments.Count; ++i)
            {
                --segments[i].position;
            }
        }

        public new Serialization.Street ToProtobuf()
        {
            var result = new Serialization.Street
            {
                MapObject = base.ToProtobuf(),
                DisplayName = displayName ?? string.Empty,
                Lit = lit,
                Maxspeed = (uint)maxspeed.RealTimeKPH,
                Lanes = (uint)lanes,
                Type = (Serialization.Street.Types.Type)type,
            };

            result.Segments.AddRange(segments.Select(s => s.ToProtobuf()));
            return result;
        }

        public static Street Deserialize(Serialization.Street street, Map map)
        {
            var s = map.CreateStreet(street.MapObject.Name, (Type)street.Type, street.Lit,
                                     (int)street.Maxspeed,
                                     (int)street.Lanes, (int)street.MapObject.Id);

            s.Deserialize(street.MapObject);

            foreach (var seg in street.Segments)
            {
                s.AddSegment(seg);
            }

            s.CalculateLength();
            // s.CreateTextMeshes();
            s.displayName = street.DisplayName;

            return s;
        }
    }
}
