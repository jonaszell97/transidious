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

namespace Transidious
{
    public class OSMImporter
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
        static readonly float earthRadius = 6371000f * Map.Meters;

        public OSMImporter(Map map, OSMImportHelper.Area area)
        {
            this.map = map;
            this.area = area;
        }

        Vector3 Project(Node node)
        {
            return Project((float)node.Longitude, (float)node.Latitude);
        }

        Vector3 Project(float lng, float lat)
        {
            var x = earthRadius * Math.toRadians(lng) * cosCenterLat;
            var y = earthRadius * Math.toRadians(lat);

            return new Vector3((x - minX), (y - minY));
        }

        public void ImportArea()
        {
            var areaName = area.ToString();
            var importHelper = new OSMImportHelper(this, areaName);
            importHelper.ImportArea();

            FindMinLngAndLat();

            map.name = areaName;
            cosCenterLat = Mathf.Cos(Math.toRadians((float)(minLat + (maxLat - minLat) * 0.5f)));

            minX = earthRadius * Math.toRadians((float)minLng) * cosCenterLat;
            minY = earthRadius * Math.toRadians((float)minLat);

            maxX = earthRadius * Math.toRadians((float)maxLng) * cosCenterLat;
            maxY = earthRadius * Math.toRadians((float)maxLat);

            width = maxX - minX;
            height = maxY - minY;

            LoadBoundary();
            // LoadTransitLines();
            LoadStreets();
            LoadParks();
            LoadBuildings();

            map.DoFinalize();
        }

        void FindMinLngAndLat()
        {
            if (boundary == null)
            {
                return;
            }

            minLng = double.PositiveInfinity;
            minLat = double.PositiveInfinity;
            maxLng = 0;
            maxLat = 0;

            foreach (var member in boundary.Members)
            {
                Way way = FindWay(member.Id);
                if (way == null)
                {
                    continue;
                }

                foreach (var nodeId in way.Nodes)
                {
                    if (!nodes.TryGetValue(nodeId, out Node node))
                    {
                        continue;
                    }

                    minLng = System.Math.Min(minLng, node.Longitude.Value);
                    minLat = System.Math.Min(minLat, node.Latitude.Value);

                    maxLng = System.Math.Max(maxLng, node.Longitude.Value);
                    maxLat = System.Math.Max(maxLat, node.Latitude.Value);
                }
            }
        }

        Way FindWay(long id)
        {
            if (ways.ContainsKey(id))
            {
                return ways[id];
            }
            else
            {
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

                boundaryPositions = MeshBuilder.RemoveDetail(MeshBuilder.DistributeEvenly(positions, 20f), 20f).ToArray();
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

                var wayPositions = new List<List<Vector3>>();
                var currentPositions = new List<Vector3>();

                int i = 0;
                foreach (var member in l1.Members)
                {
                    if (!string.IsNullOrEmpty(member.Role))
                        continue;

                    var way = FindWay(member.Id);
                    if (way == null)
                    {
                        continue;
                    }

                    foreach (var nodeId in way.Nodes)
                    {
                        var node = FindNode(nodeId);
                        if (node == null)
                            continue;

                        currentPositions.Add(Project(node));

                        if (i != 0 && node.Tags != null && node.Tags.GetValue("railway").StartsWith("stop"))
                        {
                            wayPositions.Add(currentPositions);
                            currentPositions = new List<Vector3>();
                        }
                    }

                    ++i;
                }

                i = 0;
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
                            List<Vector3> positions = null;
                            if (i <= wayPositions.Count && wayPositions[i - 1] != null)
                            {
                                positions = wayPositions[i - 1];
                            }

                            l.AddRoute(lastStop, s, positions, true, false);
                        }
                        else
                        {
                            l.depot = s;
                        }

                        lastStop = s;
                    }

                    ++i;
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
            internal Node position;
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

            internal List<Node> positions;
            internal StreetIntersection start;
            internal StreetIntersection end;
        }

        void LoadStreets()
        {
            var namelessStreets = 0;
            var partialStreets = new List<PartialStreet>();
            var nodeCount = new Dictionary<Node, int>();
            var intersections = new Dictionary<Node, StreetIntersection>();

            var totalVerts = 0;

            foreach (var street in streets)
            {
                var tags = street.Item1.Tags;
                var streetName = tags.GetValue("name");

                if (streetName == string.Empty)
                {
                    streetName = street.Item1.Id?.ToString() ?? (namelessStreets++).ToString();
                }

                var positions = new List<Node>();
                foreach (var pos in street.Item1.Nodes)
                {
                    var node = FindNode(pos);
                    if (node != null)
                        positions.Add(node);
                }

                // GetWayPositions(street.Item1, positions);
                // positions = MeshBuilder.RemoveDetail(positions.ToArray(), 5f);

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

                int lanes = 0;
                // int.TryParse(tags.GetValue("lanes"), out int lanes);

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
                    intersections.Add(node.Key, new StreetIntersection
                    {
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
                map.CreateIntersection(Project(inter.Key));
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

                    var segPositionsStream = startSeg.positions.Select(n => this.Project(n));
                    var segPositions = MeshBuilder.RemoveDetail(segPositionsStream.ToArray(), 5f);

                    var street = map.CreateStreet(startSeg.name, startSeg.type, startSeg.lit,
                                                  startSeg.oneway, startSeg.maxspeed,
                                                  startSeg.lanes);

                    var seg = street.AddSegment(segPositions,
                                                map.streetIntersectionMap[Project(startSeg.start.position)],
                                                map.streetIntersectionMap[Project(startSeg.end.position)]);

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

                            var nextSegPositionsStream = nextSeg.positions.Select(n => this.Project(n));
                            var nextSegPositions = MeshBuilder.RemoveDetail(nextSegPositionsStream.ToArray(), 5f);

                            seg = street.AddSegment(nextSegPositions,
                                                    map.streetIntersectionMap[Project(nextSeg.start.position)],
                                                    map.streetIntersectionMap[Project(nextSeg.end.position)]);

                            done = false;
                            currSeg = nextSeg;
                            currInter = nextInter;
                            streetSet.Remove(nextSeg);

                            break;
                        }
                    }

                    street.CalculateLength();
                    street.CreateTextMeshes();
                }

                startingStreetCandidates.Clear();
            }
        }

        public Mesh LoadPolygon(Relation rel, string outer = "outer", string inner = "inner")
        {
            var pslg = new PSLG();
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

                    if (wayPositions.Count < 3)
                    {
                        continue;
                    }

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
                var numberStr = building.Item1.Tags.GetValue("addr:housenumber");
                var type = building.Item2;

                Vector3? position = null;

                Mesh mesh;
                if (building.Item1.Type == OsmGeoType.Relation)
                {
                    mesh = LoadPolygon(building.Item1 as Relation);
                }
                else
                {
                    var way = building.Item1 as Way;
                    var wayPositions = new List<Vector3>();

                    foreach (var nodeId in way.Nodes)
                    {
                        var node = FindNode(nodeId);
                        if (node == null)
                        {
                            continue;
                        }

                        var loc = Project(node);
                        wayPositions.Add(loc);

                        if (node.Tags != null)
                        {
                            if (string.IsNullOrEmpty(numberStr))
                            {
                                numberStr = node.Tags.GetValue("addr:housenumber");
                            }

                            if (node.Tags.ContainsKey("shop"))
                            {
                                var val = node.Tags.GetValue("shop");
                                if (val == "convenience" || val == "supermarket")
                                {
                                    type = Building.Type.GroceryStore;
                                }
                                else
                                {
                                    type = Building.Type.Shop;
                                }
                            }
                            else if (node.Tags.ContainsKey("office"))
                            {
                                type = Building.Type.Office;
                            }
                        }
                    }

                    if (wayPositions.Count < 3)
                    {
                        continue;
                    }

                    mesh = MeshBuilder.PointsToMesh(wayPositions.ToArray());
                    MeshBuilder.FixWindingOrder(mesh);
                }

                map.CreateBuilding(type, mesh, buildingName, numberStr, position);
            }
        }
    }
}
