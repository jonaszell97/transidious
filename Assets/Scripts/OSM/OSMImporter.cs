using OsmSharp;
using OsmSharp.Streams;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class OSMImporter : MonoBehaviour
{
    public Map map;
    public bool reload = false;

    public string fileName = "Assets/Resources/OSM/berlin-latest.osm.pbf";
    public PBFOsmStreamSource sourceStream;

    struct LineMeta
    {

    }

    class TransitLine
    {
        internal Line.TransitType type;
        internal Relation inbound;
        internal Relation outbound;

        void Add(Relation line)
        {
            if (inbound == null)
            {
                inbound = line;
            }
            else
            {
                outbound = line;
            }
        }
    }

    Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
    Dictionary<long, Node> stops = new Dictionary<long, Node>();

    double minLat = double.PositiveInfinity;
    double minLng = double.PositiveInfinity;
    double maxLat = 0;
    double maxLng = 0;

    float ratioLng = 1f;
    float ratioLat = 1f;

    static readonly float oneDegreeLat = 11132f;

    Vector2 Project(Node node)
    {
        float oneDegreeLng = 4007500f * (Mathf.Cos((float)node.Latitude) / 360);
        return new Vector2(-(float)((node.Longitude - minLng) / maxLng) * oneDegreeLng * ratioLng,
                           (float)((node.Latitude - minLat) / maxLat) * oneDegreeLat * ratioLat);
    }

    void Awake()
    {
        Stream input;
        if (!reload)
        {
            input = File.OpenRead("Assets/Resources/OSM/XML/berlin.xml");
        }
        else
        {
            input = File.OpenRead(fileName);
        }

        if (input == null)
        {
            Debug.LogError("opening stream failed");
            return;
        }

        sourceStream = new PBFOsmStreamSource(input);
        foreach (var geo in sourceStream)
        {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
                case OsmGeoType.Relation:
                    if (tags.Contains("type", "route"))
                    {
                        var route = tags.GetValue("route");

                        Line.TransitType type;
                        if (route == "subway")
                        {
                            type = Line.TransitType.Subway;
                        }
                        else if (route == "train" && tags.Contains("line", "light_rail"))
                        {
                            type = Line.TransitType.STrain;
                        }
                        else
                        {
                            continue;
                        }

                        var lineName = tags.GetValue("ref");
                        if (lines.TryGetValue(lineName, out TransitLine pair))
                        {
                            pair.outbound = geo as Relation;
                        }
                        else
                        {
                            lines.Add(lineName, new TransitLine {
                                inbound = geo as Relation,
                                type = type
                            });
                        }
                    }

                    break;
                case OsmGeoType.Node:
                    if (tags.Contains("railway", "stop")
                        && (tags.Contains("subway", "yes") || tags.Contains("light_rail", "yes")))
                    {
                        var node = geo as Node;
                        if (geo.Tags.GetValue("name") != "S Ostkreuz"
                        && geo.Tags.GetValue("name") != "S Treptower Park"
                        && geo.Tags.GetValue("name") != "S Warschauer Straße"
                        && geo.Tags.GetValue("name") != "S Sonnenallee"
                        && geo.Tags.GetValue("name") != "S Plänterwald"
                        && geo.Tags.GetValue("name") != "S Frankfurter Allee")
                        {
                            continue;
                        }
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        stops.Add(geo.Id.Value, node);

                        double lat = node.Latitude.Value;
                        double lng = node.Longitude.Value;

                        minLat = System.Math.Min(lat, minLat);
                        minLng = System.Math.Min(lng, minLng);

                        maxLat = System.Math.Max(lat, maxLat);
                        maxLng = System.Math.Max(lng, maxLng);
                    }

                    break;
                default:
                    break;
            }
        }

        double width = maxLat - minLat;
        double height = maxLng - minLng;

        if (width > height)
        {
            ratioLng = (float)(width / height);
        }
        else if (width < height)
        {
            ratioLat = (float)(height / width);
        }

        if (!reload)
            return;

        using (var output = File.Open("Assets/Resources/OSM/XML/berlin.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            var writer = new PBFOsmStreamTarget(output);
            writer.Initialize();

            foreach (var line in lines)
            {
                if (line.Value.inbound != null)
                {
                    writer.AddRelation(line.Value.inbound);
                }

                if (line.Value.outbound != null)
                {
                    writer.AddRelation(line.Value.outbound);
                }
            }
            foreach (var stop in stops)
            {
                writer.AddNode(stop.Value);
            }

            writer.Flush();
            writer.Close();
        }
    }

    // Use this for initialization
    void Start()
    {
        var stopMap = new Dictionary<long, Stop>();
        foreach (var stopPair in stops)
        {
            Node stop = stopPair.Value;

            var loc = Project(stop);
            var stopName = stop.Tags.GetValue("name");

            var s = map.GetOrCreateStop(stopName, loc);
            stopMap.Add(stop.Id.Value, s);
        }

        foreach (var linePair in lines)
        {
            var l1 = linePair.Value.inbound;
            var l2 = linePair.Value.outbound;

            Color color;
            if (ColorUtility.TryParseHtmlString(l1.Tags.GetValue("colour"), out Color c))
            {
                color = c;
            }
            else
            {
                color = Map.defaultLineColors[Line.TransitType.Subway];
            }

            var l = map.CreateLine(linePair.Value.type, l1.Tags.GetValue("ref"), color).Finish();
            Stop lastStop = null;

            foreach (var member in l1.Members)
            {
                if (!member.Role.StartsWith("stop"))
                {
                    continue;
                }

                if (stopMap.TryGetValue(member.Id, out Stop s))
                {
                    if (lastStop)
                    {
                        l.AddRoute(lastStop, s, null, true, false);
                    }
                    else
                    {
                        l.depot = s;
                    }

                    lastStop = s;
                }
            }

            if (l2 == null)
            {
                continue;
            }

            lastStop = null;
            foreach (var member in l2.Members)
            {
                if (!member.Role.StartsWith("stop"))
                {
                    continue;
                }

                if (stopMap.TryGetValue(member.Id, out Stop s))
                {
                    if (lastStop)
                    {
                        l.AddRoute(lastStop, s, null, true, true);
                    }

                    lastStop = s;
                }
            }
        }

        map.done = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
