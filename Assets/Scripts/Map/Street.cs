using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Street
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

    /// Name of the street.
    public string name;

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
    }

    int GetDefaultLanes()
    {
        switch (type)
        {
            case Type.Primary:
            case Type.Secondary:
                return isOneWay ? 2 : 4;
            case Type.Tertiary:
            case Type.Residential:
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

    public void AddSegment(List<Vector3> path,
                           StreetIntersection startIntersection,
                           StreetIntersection endIntersection)
    {
        var seg = new StreetSegment();
        seg.Initialize(this, segments.Count, path,
                       startIntersection, endIntersection);

        map.RegisterSegment(seg);
        segments.Add(seg);
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
                       map.streetIntersectionIDMap[seg.endIntersectionID]);
        }
    }
}
