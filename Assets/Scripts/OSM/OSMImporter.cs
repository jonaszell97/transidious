﻿using OsmSharp;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transidious
{
    public class OSMImporter : MonoBehaviour
    {
        public class TransitLine
        {
            internal TransitType type;
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

        public Map map;
        public Dictionary<long, Node> nodes = new Dictionary<long, Node>();
        public List<Tuple<Way, Street.Type>> streets = new List<Tuple<Way, Street.Type>>();

        public Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
        public Dictionary<long, Node> stops = new Dictionary<long, Node>();
        Dictionary<long, Stop> stopMap = new Dictionary<long, Stop>();

        public Dictionary<Vector2, StreetSegment> streetSegmentMap = new Dictionary<Vector2, StreetSegment>();
        public Dictionary<Vector2, Transidious.StreetIntersection> streetIntersectionMap = new Dictionary<Vector2, Transidious.StreetIntersection>();

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

        async void Start()
        {
            GameController.instance.status = GameController.GameStatus.Paused;

            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = Instantiate(mapPrefab);

            map = mapObj.GetComponent<Map>();
            map.name = this.area.ToString();
            map.input = GameController.instance.input;

            await this.ImportArea();
            StartCoroutine(LoadFeatures());
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

        public async Task ImportArea()
        {
            var areaName = area.ToString();
            OSMImportHelper importHelper = null;

            await Task.Factory.StartNew(() =>
            {
                importHelper = new OSMImportHelper(this, areaName);
                importHelper.ImportArea();
            });

            FindMinLngAndLat();

            cosCenterLat = Mathf.Cos(Math.toRadians((float)(minLat + (maxLat - minLat) * 0.5f)));

            minX = earthRadius * Math.toRadians((float)minLng) * cosCenterLat;
            minY = earthRadius * Math.toRadians((float)minLat);

            maxX = earthRadius * Math.toRadians((float)maxLng) * cosCenterLat;
            maxY = earthRadius * Math.toRadians((float)maxLat);

            width = maxX - minX;
            height = maxY - minY;
        }

        IEnumerator LoadFeatures()
        {
            LoadBoundary();
            Debug.Log("loaded boundary");

            yield return null;

            yield return LoadParks();
            Debug.Log("loaded natural features");

            yield return LoadStreets(true);
            Debug.Log("loaded paths");

            map.DoFinalize(false);
            SaveManager.SaveMapLayout(map);

            yield return LoadStreets(false);
            Debug.Log("loaded streets");

            yield return LoadTransitLines();
            Debug.Log("loaded transit lines");

            yield return LoadBuildings();
            Debug.Log("loaded buildings");

            SaveManager.SaveMapData(map);
            Debug.Log("Done!");

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
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


        void LoadBoundary()
        {
            map.minX = 0;
            map.minY = 0;

            var maxValues = Project((float)maxLng, (float)maxLat);
            map.maxX = maxValues.x;
            map.maxY = maxValues.y;

            Vector3[] boundaryPositions;
            if (boundary != null)
            {
                var positions = GetWayPositions(boundary, "outer", 0f);
                if (!positions.First().Equals(positions.Last()))
                {
                    positions.Add(positions.First());
                }

                boundaryPositions = MeshBuilder.RemoveDetail(positions, 20f).ToArray();
            }
            else
            {
                boundaryPositions = new Vector3[]
                {
                    new Vector3(map.minX, map.maxY),
                    new Vector3(map.maxX, map.maxY),
                    new Vector3(map.minX, map.minY),
                    new Vector3(map.maxX, map.minY)
                };
            }

            map.UpdateBoundary(boundaryPositions);
            map.boundaryOutlineObj.SetActive(false);
        }

        void AddStopToLine(Line l, Stop s, Stop previousStop, int i, List<List<Vector3>> wayPositions,
                           bool setDepot = true)
        {
            if (s == previousStop)
            {
                return;
            }

            if (previousStop)
            {
                List<Vector3> positions = null;
                List<TrafficSimulator.PathSegmentInfo> segmentInfo = null;

                if (l.type == TransitType.Bus)
                {
                    var options = new PathPlanning.PathPlanningOptions { allowWalk = false };
                    var planner = new PathPlanning.PathPlanner(options);
                    var result = planner.FindClosestDrive(map, previousStop.transform.position,
                                                          s.transform.position);

                    if (result != null)
                    {
                        segmentInfo = new List<TrafficSimulator.PathSegmentInfo>();
                        positions = GameController.instance.sim.trafficSim.GetCompletePath(result, segmentInfo);
                    }
                    else
                    {
                        Debug.LogWarning("no path found from " + previousStop.name + " to " + s.name);
                    }
                }
                else if (i <= wayPositions.Count && wayPositions[i - 1] != null)
                {
                    positions = wayPositions[i - 1];
                }

                var route = l.AddRoute(previousStop, s, positions, true, false);
                if (segmentInfo != null)
                {
                    // Note which streets this route passes over.
                    foreach (var segAndLane in segmentInfo)
                    {
                        var routesOnSegment = segAndLane.segment.GetTransitRoutes(segAndLane.lane);
                        routesOnSegment.Add(route);
                        route.AddStreetSegmentOffset(segAndLane);
                    }
                }
            }
            else if (setDepot)
            {
                l.depot = s;
            }
        }

        Stop GetOrCreateStop(Node stopNode, bool projectOntoStreet = false)
        {
            if (stopNode == null)
            {
                return null;
            }

            if (stopMap.TryGetValue(stopNode.Id.Value, out Stop stop))
            {
                return stop;
            } 

            var loc = Project(stopNode);
            if (projectOntoStreet)
            {
                var street = map.GetClosestStreet(loc)?.seg;
                if (street == null)
                {
                    Debug.LogWarning("'" + stopNode.Tags.GetValue("name") + "' is not on map");
                    return null;
                }

                var closestPtAndPos = street.GetClosestPointAndPosition(loc);
                var positions = GameController.instance.sim.trafficSim.GetPath(
                    street, (closestPtAndPos.Item2 == Math.PointPosition.Right || street.street.isOneWay)
                        ? street.RightmostLane 
                        : street.LeftmostLane);

                closestPtAndPos = StreetSegment.GetClosestPointAndPosition(loc, positions);
                loc = closestPtAndPos.Item1;
            }

            var stopName = stopNode.Tags.GetValue("name");
            Debug.Log("loading stop '" + stopName + "'...");

            stopName = stopName.Replace("S+U ", "");
            stopName = stopName.Replace("S ", "");
            stopName = stopName.Replace("U ", "");
            stopName = stopName.Replace("Berlin-", "");
            stopName = stopName.Replace("Berlin ", "");

            var s = map.CreateStop(stopName, loc);
            stopMap.Add(stopNode.Id.Value, s);
            Debug.Log("done");

            return s;
        }

        IEnumerator LoadTransitLines()
        {
            var counter = 0;
            foreach (var linePair in lines)
            {
                if (linePair.Value.type != TransitType.Bus||linePair.Value.inbound.Tags.GetValue("ref")!="109")
                {
                    continue;
                }

                var l1 = linePair.Value.inbound;
                var l2 = linePair.Value.outbound;

                Color color;
                if (ColorUtility.TryParseHtmlString(l1.Tags.GetValue("colour"), out Color c))
                {
                    color = c;
                }
                else
                {
                    color = Map.defaultLineColors[linePair.Value.type];
                }

                var l = map.CreateLine(linePair.Value.type, l1.Tags.GetValue("ref"), color).Finish();
                Debug.Log("loading line '" + l.name + "'...");

                var members = l1.Members.ToList();
                if (l2 != null)
                {
                    members.AddRange(l2.Members);
                }

                int i = 0;
                var wayPositions = new List<List<Vector3>>();
                var currentPositions = new List<Vector3>();

                if (l.type != TransitType.Bus)
                {
                    foreach (var member in members)
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

                            if (i != 0 && node.Tags != null
                            && (node.Tags.GetValue("railway").StartsWith("stop")
                            || node.Tags.GetValue("public_transport").StartsWith("stop")
                            || node.Tags.GetValue("highway").Contains("stop")))
                            {
                                wayPositions.Add(currentPositions);
                                currentPositions = new List<Vector3>();
                            }
                        }

                        ++i;
                    }
                }

                i = 0;
                var n = 0;

                Stop previousStop = null;
                foreach (var member in members)
                {
                    if (!member.Role.StartsWith("stop"))
                    {
                        ++n;
                        continue;
                    }

                    Stop s = null;

                    Way platform = null;
                    if (n < members.Count - 1 && members[n + 1].Role.StartsWith("platform"))
                    {
                        platform = FindWay(members[n + 1].Id);
                    }
                    
                    if (l.type == TransitType.Bus && platform != null)
                    {
                        foreach (var nodeId in platform.Nodes)
                        {
                            var node = FindNode(nodeId);
                            if (node == null)
                            {
                                continue;
                            }

                            if (node.Tags.Contains("highway", "bus_stop"))
                            {
                                s = GetOrCreateStop(node, true);
                                break;
                            }
                        }
                    }
                    
                    if (s == null)
                    {
                        s = GetOrCreateStop(FindNode(member.Id));
                    }

                    if (s != null)
                    {
                        AddStopToLine(l, s, previousStop, i, wayPositions);
                        previousStop = s;
                    }

                    ++i;
                    ++n;
                }

                if (l.stops.Count != 0 && previousStop != l.stops.First())
                {
                    AddStopToLine(l, l.stops.First(), previousStop, i, wayPositions);
                }

                Debug.Log("done");

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
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

        void UpdateSegmentPositions(StreetSegment segment)
        {
            var numPositions = segment.positions.Count;
            for (var i = 1; i < numPositions - 2; ++i)
            {
                var pos = segment.positions[i];
                if (streetSegmentMap.ContainsKey(pos))
                {
                    Debug.LogWarning("position " + pos + " is on both '"
                        + streetSegmentMap[pos].name + "' and '" + segment.name + "'");
                }
                else
                {
                    streetSegmentMap.Add(pos, segment);
                }
            }
        }

        IEnumerator LoadStreets(bool onlyFootpaths)
        {
            var namelessStreets = 0;
            var partialStreets = new List<PartialStreet>();
            var nodeCount = new Dictionary<Node, int>();
            var intersections = new Dictionary<Node, StreetIntersection>();

            var totalVerts = 0;
            var counter = 0;

            foreach (var street in streets)
            {
                switch (street.Item2)
                {
                case Street.Type.FootPath:
                    if (!onlyFootpaths)
                    {
                        continue;
                    }

                    break;
                default:
                    if (onlyFootpaths)
                    {
                        continue;
                    }

                    break;
                }

                var tags = street.Item1.Tags;
                var streetName = tags.GetValue("name");
                Debug.Log("loading street '" + streetName + "'...");

                if (streetName == string.Empty)
                {
                    streetName = street.Item1.Id?.ToString() ?? (namelessStreets++).ToString();
                }

                var positions = new List<Node>();
                foreach (var pos in street.Item1.Nodes)
                {
                    var node = FindNode(pos);
                    if (node != null)
                    {
                        positions.Add(node);
                    }
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
                Debug.Log("done");

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
                }
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

            counter = 0;

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

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
                }
            }

            foreach (var inter in intersections)
            {
                var position = Project(inter.Key);
                var intersection = map.CreateIntersection(position);

                if (!streetIntersectionMap.ContainsKey(position))
                {
                    streetIntersectionMap.Add(position, intersection);
                }
            }

            counter = 0;

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

                    UpdateSegmentPositions(seg);
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

                            UpdateSegmentPositions(seg);
                            done = false;
                            currSeg = nextSeg;
                            currInter = nextInter;
                            streetSet.Remove(nextSeg);

                            break;
                        }
                    }

                    street.CalculateLength();
                    // street.CreateTextMeshes();
                }

                startingStreetCandidates.Clear();

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
                }
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

        IEnumerator LoadParks()
        {
            var counter = 0;
            foreach (var feature in naturalFeatures)
            {
                var featureName = feature.Item1.Tags.GetValue("name");
                Debug.Log("loading natural feature '" + featureName + "'...");

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
                Debug.Log("done");

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
                }
            }
        }

        IEnumerator LoadBuildings()
        {
            var counter = 0;
            foreach (var building in buildings)
            {
                var buildingName = building.Item1.Tags.GetValue("name");
                Debug.Log("loading building '" + buildingName + "'...");

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
                Debug.Log("done");

                if (counter++ == 300)
                {
                    yield return null;
                    counter = 0;
                }
            }
        }
    }
}
