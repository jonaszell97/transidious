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

    public class TransitLine
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

    public Dictionary<long, Node> nodes = new Dictionary<long, Node>();
    public List<Tuple<Way, Street.Type>> streets = new List<Tuple<Way, Street.Type>>();
    
    public Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
    public Dictionary<long, Node> stops = new Dictionary<long, Node>();

    public Relation boundary = null;
    public Dictionary<long, Way> ways = new Dictionary<long, Way>();

    public List<Tuple<OsmGeo, NaturalFeature.Type>> naturalFeatures = new List<Tuple<OsmGeo, NaturalFeature.Type>>();
    public List<Tuple<OsmGeo, Building.Type>> buildings = new List<Tuple<OsmGeo, Building.Type>>();

    public double minLat = double.PositiveInfinity;
    public double minLng = double.PositiveInfinity;
    public double maxLat = 0;
    public double maxLng = 0;

    public float minUsedLat = float.PositiveInfinity;
    public float minUsedLng = float.PositiveInfinity;

    public float maxX;
    public float minX;
    public float width;

    public float maxY;
    public float minY;
    public float height;

    public OSMImportHelper.Area area;

    float cosCenterLat;
    static readonly float earthRadius = 6371.00f;

    float ratioLng = 2f;
    float ratioLat = 2f;

    Vector2 Project(Node node)
    {
        return Project((float)node.Longitude, (float)node.Latitude);
    }

    Vector2 Project(float lng, float lat)
    {
        var x = earthRadius * Transidious.Math.toRadians(lng) * cosCenterLat;
        var y = earthRadius * Transidious.Math.toRadians(lat);

        return new Vector2((x - minX) * ratioLng, (y - minY) * ratioLat);
    }

    void Awake()
    {
        var areaName = area.ToString();
        var importHelper = new OSMImportHelper(this, areaName);
        importHelper.ImportArea();

        map.name = areaName;
        cosCenterLat = Mathf.Cos(Transidious.Math.toRadians((float)(minLat + (maxLat - minLat) * 0.5f)));

        minX = earthRadius * Transidious.Math.toRadians((float)minLng) * cosCenterLat;
        maxX = earthRadius * Transidious.Math.toRadians((float)maxLng);

        minY = earthRadius * Transidious.Math.toRadians((float)minLat) * cosCenterLat;
        maxY = earthRadius * Transidious.Math.toRadians((float)minLat);

        width = maxX - minX;
        height = maxY - minY;
    }

    Way FindWay(long id)
    {
        if (ways.ContainsKey(id))
        {
            return ways[id];
        }
        else
        {
            Debug.Log("way " + id + " not found");
            return null;
        }
    }

    Node FindNode(long id)
    {
        if (nodes.ContainsKey(id))
        {
            return nodes[id];
        }
        else
        {
            Debug.Log("node " + id + " not found");
            return null;
        }
    }

    class WayNode
    {
        internal Way way;
        internal List<Vector3> positions = new List<Vector3>();
        internal bool reversed = false;
    }

    List<Vector3> GetWayPositions(Relation relation, string role = "",
                                  float z = 0f, bool stopAfterFirst = false)
    {
        var wayNodes = new HashSet<WayNode>();
        foreach (var member in relation.Members)
        {
            if (role != string.Empty && member.Role != role)
                continue;

            Way way = FindWay(member.Id);
            if (way == null)
            {
                continue;
            }

            var wayNode = new WayNode { way = way };
            foreach (var nodeId in way.Nodes)
            {
                if (!nodes.TryGetValue(nodeId, out Node node))
                {
                    //Debug.Log("node " + nodeId + " not found");
                    continue;
                }

                var loc = Project(node);
                wayNode.positions.Add(new Vector3(loc.x, loc.y, z));
            }

            if (wayNode.positions.Count > 0)
            {
                wayNodes.Add(wayNode);

                if (stopAfterFirst)
                    break;
            }
        }

        var positions = new List<Vector3>();
        if (wayNodes.Count == 0)
        {
            return positions;
        }

        WayNode startNode = wayNodes.First();
        wayNodes.Remove(startNode);
        positions.AddRange(startNode.positions);

        WayNode currentNode = startNode;
        while (wayNodes.Count != 0)
        {
            float minDist = float.PositiveInfinity;
            WayNode minNode = null;
            bool isStart = false;

            foreach (var otherNode in wayNodes)
            {
                var startDist = (otherNode.positions.First() - currentNode.positions.Last()).magnitude;
                if (startDist < minDist)
                {
                    minDist = startDist;
                    minNode = otherNode;
                    isStart = true;
                }

                var endDist = (otherNode.positions.Last() - currentNode.positions.Last()).magnitude;
                if (endDist < minDist)
                {
                    minDist = endDist;
                    minNode = otherNode;
                    isStart = false;
                }
            }

            if (!isStart)
            {
                minNode.positions.Reverse();
                minNode.reversed = true;
            }

            wayNodes.Remove(minNode);
            positions.AddRange(minNode.positions);

            currentNode = minNode;
        }

        return positions;
    }

    List<Vector3> GetWayPositions(Way way)
    {
        var positions = new List<Vector3>();
        GetWayPositions(way, positions);

        /*if (forceClockwise && positions.Count != 0)
        {
            Vector3 p0 = positions.First();
            for (int i = 1; i < positions.Count; ++i)
            {
                Vector3 p1 = positions[i];
                int cmp = p0.x.CompareTo(p1.x);

                if (cmp == 0)
                {
                    continue;
                }

                if (cmp == 1)
                {
                    positions.Reverse();
                }

                break;
            }
        }*/

        return positions;
    }

    void GetWayPositions(Way way, List<Vector3> positions, float z = 0)
    {
        foreach (var nodeId in way.Nodes)
        {
            if (!nodes.TryGetValue(nodeId, out Node node))
            {
                continue;
            }

            var loc = Project(node);
            positions.Add(new Vector3(loc.x, loc.y, z));
        }
    }

    Vector3[] boundaryPositions;
    void LoadBoundary()
    {
        if (boundary != null)
        {
            var positions = GetWayPositions(boundary, "outer", 0f);
            if (positions.First() != positions.Last())
                positions.Add(positions.First());

            boundaryPositions = MeshBuilder.RemoveDetail(MeshBuilder.DistributeEvenly(positions)).ToArray();
        }
        else
        {
            boundaryPositions = new Vector3[]
            {
                new Vector3(minX, maxY),
                new Vector3(maxX, maxY),
                new Vector3(minX, minY),
                new Vector3(maxX, minY)
            };
        }

        map.UpdateBoundary(boundaryPositions);
    }

    void OnDrawGizmosSelected()
    {
        if (boundaryPositions == null)
            return;

        for (int i = 0; i < boundaryPositions.Length; ++i)
        {
            Gizmos.color = new Color(255f, 0f, 0f);
            Gizmos.DrawSphere(boundaryPositions[i], .1f);
        }
    }

    void LoadTransitLines()
    {
        var stopMap = new Dictionary<long, Stop>();
        foreach (var stopPair in stops)
        {
            Node stop = stopPair.Value;

            var loc = Project(stop);
            var stopName = stop.Tags.GetValue("name");

            stopName = stopName.Replace("S ", "");
            stopName = stopName.Replace("U ", "");
            stopName = stopName.Replace("S+U ", "");
            stopName = stopName.Replace("Berlin ", "");
            stopName = stopName.Replace("Berlin-", "");

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
    }

    class StreetIntersection
    {
        internal Vector3 position;
        internal List<PartialStreet> intersectingStreets;
    }

    class PartialStreet
    {
        internal string name;
        internal Street.Type type;
        internal bool lit;
        internal bool oneway;
        internal int maxspeed;
        internal int lanes;

        internal bool isRoundabout;

        internal List<Vector3> positions;
        internal StreetIntersection start;
        internal StreetIntersection end;
    }

    void LoadStreets()
    {
        var namelessStreets = 0;
        var partialStreets = new List<PartialStreet>();
        var nodeCount = new Dictionary<Vector3, int>();
        var intersections = new Dictionary<Vector3, StreetIntersection>();

        var totalVerts = 0;

        foreach (var street in streets)
        {
            var tags = street.Item1.Tags;
            var streetName = tags.GetValue("name");

            if (streetName == string.Empty)
            {
                streetName = street.Item1.Id?.ToString() ?? (namelessStreets++).ToString();
            }

            var positions = new List<Vector3>();
            GetWayPositions(street.Item1, positions);

            positions = MeshBuilder.RemoveDetail(positions.ToArray(), 5f);
            totalVerts += positions.Count;

            // Find intersections.
            foreach (var pos in positions)
            {
                if (nodeCount.ContainsKey(pos))
                {
                    ++nodeCount[pos];
                }
                else
                {
                    nodeCount.Add(pos, 1);
                }
            }

            ++nodeCount[positions.First()];
            ++nodeCount[positions.Last()];

            int.TryParse(tags.GetValue("maxspeed"), out int maxspeed);
            int.TryParse(tags.GetValue("lanes"), out int lanes);

            var lit = tags.Contains("lit", "yes");
            var oneway = tags.Contains("oneway", "yes");

            var ps = new PartialStreet
            {
                name = streetName,
                type = street.Item2,
                lit = lit,
                oneway = oneway,
                maxspeed = maxspeed,
                lanes = lanes,
                positions = positions,
                start = null,
                end = null,
                isRoundabout = tags.Contains("junction", "roundabout")
            };

            partialStreets.Add(ps);
        }

        Debug.Log(totalVerts.ToString() + " total verts");

        foreach (var node in nodeCount)
        {
            if (node.Value > 1)
            {
                intersections.Add(node.Key, new StreetIntersection {
                    position = node.Key,
                    intersectingStreets = new List<PartialStreet>()
                });
            }
        }

        var streetSet = new HashSet<PartialStreet>();
        foreach (var street in partialStreets)
        {
            var startPos = street.positions.First();
            var startInter = intersections[startPos];
            var startIdx = 0;
            var roundabout = street.isRoundabout ||
                street.positions.First() == street.positions.Last();

            for (int i = 1; i < street.positions.Count; ++i)
            {
                var endPos = street.positions[i];
                if (!intersections.TryGetValue(endPos, out StreetIntersection endInter))
                {
                    continue;
                }

                PartialStreet ps;
                if (i < street.positions.Count - 1 || startIdx > 0)
                {
                    ps = new PartialStreet
                    {
                        name = street.name,
                        type = street.type,
                        lit = street.lit,
                        oneway = street.oneway,
                        maxspeed = street.maxspeed,
                        positions = street.positions.GetRange(startIdx, i - startIdx + 1),
                        start = startInter,
                        end = endInter,
                        isRoundabout = roundabout
                    };
                }
                else
                {
                    ps = street;
                    ps.start = startInter;
                    ps.end = endInter;
                }

                streetSet.Add(ps);
                startInter.intersectingStreets.Add(ps);
                endInter.intersectingStreets.Add(ps);

                startPos = endPos;
                startInter = endInter;
                startIdx = i;
            }
        }

        foreach (var inter in intersections)
        {
            map.CreateIntersection(inter.Key);
        }

        var startingStreetCandidates = new HashSet<string>();
        foreach (var inter in intersections)
        {
            // Check for streets that start at this intersection.
            foreach (var ps in inter.Value.intersectingStreets)
            {
                if (!startingStreetCandidates.Add(ps.name) && !ps.isRoundabout)
                {
                    startingStreetCandidates.Remove(ps.name);
                }
            }

            foreach (var startSeg in inter.Value.intersectingStreets)
            {
                // This street was already built.
                if (!startingStreetCandidates.Contains(startSeg.name)
                    || !streetSet.Contains(startSeg))
                {
                    continue;
                }

                var street = map.CreateStreet(startSeg.name, startSeg.type, startSeg.lit,
                                              startSeg.oneway, startSeg.maxspeed,
                                              startSeg.lanes);

                street.AddSegment(startSeg.positions,
                                  map.streetIntersectionMap[startSeg.start.position],
                                  map.streetIntersectionMap[startSeg.end.position]);

                streetSet.Remove(startSeg);

                var done = false;
                var currSeg = startSeg;
                var currInter = inter.Value;
    
                while (!done)
                {
                    StreetIntersection nextInter;
                    if (currSeg.start == currInter)
                    {
                        nextInter = currSeg.end;
                    }
                    else
                    {
                        nextInter = currSeg.start;
                    }

                    if (nextInter == null)
                    {
                        continue;
                    }

                    done = true;

                    foreach (var nextSeg in nextInter.intersectingStreets)
                    {
                        if (nextSeg.name != currSeg.name || !streetSet.Contains(nextSeg))
                        {
                            continue;
                        }

                        street.AddSegment(nextSeg.positions,
                                          map.streetIntersectionMap[nextSeg.start.position],
                                          map.streetIntersectionMap[nextSeg.end.position]);

                        done = false;
                        currSeg = nextSeg;
                        currInter = nextInter;
                        streetSet.Remove(nextSeg);

                        break;
                    }
                }
            }

            startingStreetCandidates.Clear();
        }
    }

    public Mesh LoadPolygon(Relation rel, string outer = "outer", string inner = "inner")
    {
        var pslg = new Transidious.PSLG();
        foreach (var member in rel.Members)
        {
            if (!String.IsNullOrEmpty(outer) && member.Role == outer)
            {
                var way = FindWay(member.Id);
                if (way == null)
                {
                    continue;
                }

                var wayPositions = GetWayPositions(way);
                pslg.AddVertexLoop(wayPositions);
            }
            else if (!String.IsNullOrEmpty(inner) && member.Role == inner)
            {
                var way = FindWay(member.Id);
                if (way == null)
                {
                    continue;
                }

                var wayPositions = GetWayPositions(way);
                if (wayPositions.Count == way.Nodes.Length)
                {
                    pslg.AddHole(wayPositions);
                }
            }
        }

        if (pslg.Empty)
        {
            return null;
        }
        else
        {
            var polygon = map.triangleAPI.Triangulate(pslg);
            var mesh = new Mesh
            {
                vertices = polygon.vertices,
                triangles = polygon.triangles
            };

            MeshBuilder.FixWindingOrder(mesh);
            return mesh;
        }
    }

    void LoadParks()
    {
        foreach (var feature in naturalFeatures)
        {
            var featureName = feature.Item1.Tags.GetValue("name");

            Mesh mesh;
            if (feature.Item1.Type == OsmGeoType.Relation)
            {
                mesh = LoadPolygon(feature.Item1 as Relation);
            }
            else
            {
                var way = feature.Item1 as Way;
                var wayPositions = GetWayPositions(way);

                mesh = MeshBuilder.PointsToMesh(wayPositions.ToArray());
                MeshBuilder.FixWindingOrder(mesh);
            }

            map.CreateFeature(featureName, feature.Item2, mesh);
        }
    }

    void LoadBuildings()
    {
        foreach (var building in buildings)
        {
            var buildingName = building.Item1.Tags.GetValue("name");

            Mesh mesh;
            if (building.Item1.Type == OsmGeoType.Relation)
            {
                mesh = LoadPolygon(building.Item1 as Relation);
            }
            else
            {
                var way = building.Item1 as Way;
                var wayPositions = GetWayPositions(way);

                mesh = MeshBuilder.PointsToMesh(wayPositions.ToArray());
                MeshBuilder.FixWindingOrder(mesh);
            }

            map.CreateBuilding(building.Item2, mesh, buildingName);
        }
    }

    // Use this for initialization
    void Start()
    {
        LoadBoundary();
        LoadTransitLines();
        LoadStreets();
        LoadParks();
        LoadBuildings();

        map.DoFinalize();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
