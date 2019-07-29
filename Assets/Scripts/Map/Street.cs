using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class Street : MonoBehaviour
    {
        public enum Type
        {
            Primary,
            Secondary,
            Tertiary,
            Residential,
            Path,
            River,
        }

        [System.Serializable]
        public struct SerializedStreet
        {
            public string name;
            public Type type;
            public StreetSegment.SerializedStreetSegment[] segments;
            public bool lit;
            public bool oneway;
            public int maxspeed;
            public int lanes;
        }

        /// ID of the street.
        public int id;

        /// Reference to the map.
        public Map map;

        /// The street type.
        public Type type = Type.Residential;

        /// The (ordered) partial streets of this street.
        public List<StreetSegment> segments;

        /// <summary>
        /// Whether or not this street is lit.
        /// </summary>
        public bool lit;

        /// <summary>
        /// Whether or not this street is onw-way only.
        /// </summary>
        public bool isOneWay;

        /// <summary>
        /// The maximum speed (in kmh) of this street.
        /// </summary>
        public int maxspeed;

        /// The number of lanes on the road.
        public int lanes;

        /// The length of this street.
        public float length;

        public void Initialize(Map map, Type type, string name, bool lit, bool isOneWay, int maxspeed, int lanes)
        {
            this.map = map;
            this.type = type;
            this.name = name;
            this.lit = lit;
            this.isOneWay = isOneWay;
            this.maxspeed = maxspeed != 0 ? maxspeed : GetDefaultMaxSpeed();
            this.lanes = lanes != 0 ? lanes : GetDefaultLanes();
            this.segments = new List<StreetSegment>();
            this.length = 0f;
        }

        public float MaxSpeedMetersPerSecond
        {
            get
            {
                return maxspeed * (1f / 3.6f);
            }
        }

        int GetDefaultLanes()
        {
            switch (type)
            {
                case Type.Primary:
                case Type.Secondary:
                    return 2;
                case Type.Tertiary:
                case Type.Residential:
                    return 2;
                case Type.Path:
                    return isOneWay ? 1 : 2;
                case Type.River:
                    return 2;
                default:
                    return 0;
            }
        }

        int GetDefaultMaxSpeed()
        {
            switch (type)
            {
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

        public int LanesPerDirection
        {
            get
            {
                if (isOneWay)
                {
                    return lanes;
                }

                return lanes / 2;
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

            Vector3 pos = Vector3.zero;
            var halfWidth = minWidth / 2f;
            var len = 0f;
            var middle = seg.length / 2f;
            var angle = 0f;
            var thresholdAngle = 7f;
            bool isPositionUsable = false;
            var direction = Vector3.zero;

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
                        var pointAngle = Math.PointAngle(p0, seg.positions[j]);
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
                        var pointAngle = Math.PointAngle(p1, seg.positions[j]);
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
                    angle = GetEffectiveAngle(Math.PointAngle(leftPoint, rightPoint));
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
            if (!seg.street.isOneWay || seg.length < 30f * Map.Meters)
            {
                return;
            }

            if (seg.directionArrow == null)
            {
                seg.directionArrow = map.Game.CreateSprite(
                    GameController.streetArrowSprite);

                seg.directionArrow.GetComponent<SpriteRenderer>().color = new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            seg.directionArrow.transform.position = new Vector3(posOnStreet.pos.x, posOnStreet.pos.y,
                                                                Map.Layer(MapLayer.StreetMarkings));

            seg.directionArrow.transform.rotation = Quaternion.FromToRotation(Vector3.up,
                                                                              posOnStreet.direction);
        }

        void GenerateDirectionalArrow(StreetSegment seg)
        {
            if (!seg.street.isOneWay || seg.length < 30f * Map.Meters)
            {
                return;
            }

            var sprite = GameController.streetArrowSprite;
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
            var txt = map.CreateText(Vector3.zero, name, new Color(0.3f, 0.3f, 0.3f, 1f));
            txt.textMesh.autoSizeTextContainer = true;
            txt.textMesh.fontSize = segments.First().GetFontSize(InputController.maxZoom);
            txt.textMesh.alignment = TMPro.TextAlignmentOptions.Center;
            txt.textMesh.ForceMeshUpdate();

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
                txt.transform.position = new Vector3(posAndAngle.pos.x,
                                                     posAndAngle.pos.y,
                                                     Map.Layer(MapLayer.StreetNames));

                spaceSinceLastLabel = 0f;

                txt.transform.rotation = Quaternion.AngleAxis(posAndAngle.angle, Vector3.forward);
                seg.streetName = txt;

                GenerateDirectionalArrow(seg, posAndAngle);
            }

            if (!placedText)
            {
                Destroy(txt.gameObject);
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

        public StreetSegment AddSegment(List<Vector3> path,
                                        StreetIntersection startIntersection,
                                        StreetIntersection endIntersection,
                                        int atPosition = -1,
                                        bool hasTramTracks = false)
        {
            var segObj = Instantiate(map.streetSegmentPrefab);
            segObj.transform.SetParent(this.transform);

            var seg = segObj.GetComponent<StreetSegment>();
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

            seg.Initialize(this, pos, path, startIntersection, endIntersection, hasTramTracks);
            map.RegisterSegment(seg);

            return seg;
        }

        public Tuple<StreetSegment, StreetSegment>
        SplitSegment(StreetSegment seg, StreetIntersection newIntersection)
        {
            seg.DeleteSegment();

            var splitPt = newIntersection.Location;
            var dist = (seg.positions.First() - splitPt).magnitude;

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

        public SerializedStreet Serialize()
        {
            return new SerializedStreet
            {
                name = name,
                type = type,

                segments = segments.Select(s => s.Serialize()).ToArray(),
                lit = lit,
                oneway = isOneWay,
                maxspeed = maxspeed,
                lanes = lanes
            };
        }

        public void Deserialize(SerializedStreet s)
        {
            foreach (var seg in s.segments)
            {
                AddSegment(seg.positions.Select(v => v.ToVector()).ToList(),
                           map.streetIntersectionIDMap[seg.startIntersectionID],
                           map.streetIntersectionIDMap[seg.endIntersectionID],
                           -1, seg.hasTramTracks);
            }

            CalculateLength();
            CreateTextMeshes();
        }
    }
}
