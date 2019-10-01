using OsmSharp;
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

        public int thresholdTime = 1000;
        public bool loadGame = true;

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
        public string country;
        public int tileX, tileY;
        public int tilesX, tilesY;
        public bool singleTile;
        public bool done;

        float cosCenterLat;
        static readonly float earthRadius = 6371000f * Map.Meters;

        public static readonly float maxTileSize = 999999f; // 6000f;

        async void Start()
        {
            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = Instantiate(mapPrefab);

            map = mapObj.GetComponent<Map>();
            map.name = this.area.ToString();
            map.input = GameController.instance.input;

            await this.ImportArea();

            tilesX = (int)Mathf.Ceil(width / maxTileSize);
            tilesY = (int)Mathf.Ceil(height / maxTileSize);
            singleTile = tilesX == 1 && tilesY == 1;

            if (singleTile)
            {

                StartCoroutine(LoadFeatures());
                return;
            }

            StartCoroutine(LoadTiles());
        }

        IEnumerator LoadTiles()
        {
            for (var x = 0; x < tilesX; ++x)
            {
                for (var y = 0; y < tilesY; ++y)
                {
                    tileX = x;
                    tileY = y;
                    done = false;

                    StartCoroutine(LoadFeatures());

                    while (!done)
                    {
                        yield return null;
                    }

                    map.ResetAll();
                }
            }

            Debug.Log("Done!");
        }

        bool IsMainImporter
        {
            get
            {
                return tileX == 0 && tileY == 0;
            }
        }

        bool ShouldImport(Node node)
        {
            if (singleTile)
            {
                return true;
            }

            var pos = Project(node);
            var x = (int)Mathf.Floor(pos.x / maxTileSize);
            var y = (int)Mathf.Floor(pos.y / maxTileSize);

            return x == tileX && y == tileY;
        }

        bool ShouldImport(Way way)
        {
            if (singleTile)
            {
                return true;
            }

            foreach (var nodeId in way.Nodes)
            {
                var node = FindNode(nodeId);
                if (node != null)
                {
                    return ShouldImport(node);
                }
            }

            return false;
        }

        bool ShouldImport(Relation relation)
        {
            if (singleTile)
            {
                return true;
            }

            foreach (var member in relation.Members)
            {
                if (member.Type == OsmGeoType.Node)
                {
                    var node = FindNode(member.Id);
                    if (node != null)
                    {
                        return ShouldImport(node);
                    }
                }
                else if (member.Type == OsmGeoType.Way)
                {
                    var way = FindWay(member.Id);
                    if (way != null)
                    {
                        return ShouldImport(way);
                    }
                }
            }

            return false;
        }

        bool ShouldImport(OsmGeo geo)
        {
            if (singleTile)
            {
                return true;
            }

            if (geo.Type == OsmGeoType.Node)
            {
                return ShouldImport(geo as Node);
            }
            if (geo.Type == OsmGeoType.Way)
            {
                return ShouldImport(geo as Way);
            }

            return ShouldImport(geo as Relation);
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
            await Task.Factory.StartNew(() =>
            {
                var _ = new OSMImportHelper(this, areaName, country);
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
            if (IsMainImporter)
            {
                LoadBoundary();
                Debug.Log("loaded boundary");
                yield return null;
            }

            yield return LoadParks();
            Debug.Log("loaded natural features");

            yield return LoadStreets();
            Debug.Log("loaded streets");

            yield return LoadBuildings();
            Debug.Log("loaded buildings");

            if (singleTile)
            {
                yield return map.DoFinalize(thresholdTime);
            }
            else
            {
                yield return map.DoFinalize(thresholdTime, tileX, tileY);
            }

            // yield return LoadTransitLines();
            // Debug.Log("loaded transit lines");

            Texture2D backgroundTex;
            if (singleTile)
            {
                backgroundTex = ScreenShotMaker.Instance.MakeScreenshotSingle(map);
                SaveManager.SaveMapLayout(map, backgroundTex.EncodeToPNG());
            }
            else
            {
                backgroundTex = ScreenShotMaker.Instance.MakeScreenshotSingle(map, tileX, tileY);
                SaveManager.SaveMapLayout(map, backgroundTex.EncodeToPNG(), tileX, tileY);
            }

            Destroy(backgroundTex);
            Resources.UnloadUnusedAssets();

            if (singleTile)
            {
                SaveManager.SaveMapData(map);
            }
            else
            {
                SaveManager.SaveMapData(map, tileX, tileY);
            }

            if (singleTile)
            {
                Debug.Log("Done!");
            }
            else
            {
                Debug.Log("Done with tile " + tileX + ", " + tileY + "!");
            }

            done = true;

            if (loadGame && singleTile)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
            }
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
                                      float z = 0f, bool stopAfterFirst = false,
                                      bool reportMissing = false)
        {
            var wayNodes = new HashSet<WayNode>();
            foreach (var member in relation.Members)
            {
                if (role != string.Empty && member.Role != role)
                    continue;

                Way way = FindWay(member.Id);
                if (way == null)
                {
                    if (reportMissing)
                    {
                        Debug.LogWarning("missing way " + member.Id);
                    }

                    continue;
                }

                var wayNode = new WayNode { way = way };
                foreach (var nodeId in way.Nodes)
                {
                    if (!nodes.TryGetValue(nodeId, out Node node))
                    {
                        if (reportMissing)
                        {
                            Debug.LogWarning("missing node " + nodeId);
                        }

                        continue;
                    }

                    var loc = Project(node);
                    wayNode.positions.Add(new Vector3(loc.x, loc.y, z));
                }

                if (wayNode.positions.Count > 0)
                {
                    // Some borders have disjointed exclaves (e.g. Berlin), exclude those.
                    if (!wayNode.positions.First().Equals(wayNode.positions.Last()))
                    {
                        wayNodes.Add(wayNode);

                        if (stopAfterFirst)
                            break;
                    }
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

            Vector2[] boundaryPositions;
            if (boundary != null)
            {
                var positions = GetWayPositions(boundary, "outer", 0f, false, true);
                if (!positions.First().Equals(positions.Last()))
                {
                    positions.Add(positions.First());
                }

                var positionsToKeep = new List<Vector2>();
                LineUtility.Simplify(positions.Select(v => (Vector2)v).ToList(),
                                     5f, positionsToKeep);

                Debug.Log("reduced boundary from " + positions.Count + " to "
                    + positionsToKeep.Count + " verts");

                boundaryPositions = positionsToKeep.ToArray();
                // boundaryPositions = positions.Select(v => (Vector2)v).ToArray();

#if false
                var i = 0;
                foreach (var pos in boundaryPositions)
                {
                    var txt = map.CreateText(new Vector3(pos.x, pos.y, Map.Layer(MapLayer.Cursor)), i.ToString(), Color.red, 20);
                    ++i;
                }

                // Debug.Break();
#endif
            }
            else
            {
                boundaryPositions = new Vector2[]
                {
                    new Vector2(map.minX, map.minY), // bottom left
                    new Vector2(map.minX, map.maxY), // top left
                    new Vector2(map.maxX, map.maxY), // top right
                    new Vector2(map.maxX, map.minY), // bottom right
                    new Vector2(map.minX, map.minY), // bottom left
                };
            }

            map.UpdateBoundary(boundaryPositions);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            for (int x = 0; x < tilesX; ++x)
            {
                for (int y = 0; y < tilesY; ++y)
                {
                    var baseX = map.minX + x * maxTileSize;
                    var baseY = map.minY + y * maxTileSize;

                    Gizmos.DrawLine(new Vector3(baseX, baseY, -13f),
                                    new Vector3(baseX + maxTileSize, baseY, -13f));

                    Gizmos.DrawLine(new Vector3(baseX, baseY, -13f),
                                    new Vector3(baseX, baseY + maxTileSize, -13f));

                    Gizmos.DrawLine(new Vector3(baseX, baseY + maxTileSize, -13f),
                                    new Vector3(baseX + maxTileSize, baseY + maxTileSize, -13f));

                    Gizmos.DrawLine(new Vector3(baseX + maxTileSize, baseY, -13f),
                                    new Vector3(baseX + maxTileSize, baseY + maxTileSize, -13f));
                }
            }
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
                        positions = GameController.instance.sim.trafficSim.GetCompletePath(
                            result, segmentInfo);
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

            return s;
        }

        IEnumerator LoadTransitLines()
        {
            foreach (var linePair in lines)
            {
                if (linePair.Value.type != TransitType.Bus
                /*|| (linePair.Value.inbound.Tags.GetValue("ref") != "M19"
                && linePair.Value.inbound.Tags.GetValue("ref") != "M29")*/)
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

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
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

        IEnumerator LoadStreets()
        {
            var namelessStreets = 0;
            var partialStreets = new List<PartialStreet>();
            var nodeCount = new Dictionary<Node, int>();
            var intersections = new Dictionary<Node, StreetIntersection>();
            streetSegmentMap.Clear();

            foreach (var street in streets)
            {
                if (!ShouldImport(street.Item1))
                {
                    continue;
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

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

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

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
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

            var removedVerts = 0;

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
                    var segPositions = MeshBuilder.RemoveDetailByDistance(
                        segPositionsStream.ToArray(), 2.5f);

                    segPositions = MeshBuilder.RemoveDetailByAngle(segPositions, 5f);

                    removedVerts += startSeg.positions.Count - segPositions.Count;

                    var street = map.CreateStreet(startSeg.name, startSeg.type, startSeg.lit,
                                                  startSeg.oneway, startSeg.maxspeed,
                                                  startSeg.lanes);

                    var seg = street.AddSegment(segPositions,
                                                map.streetIntersectionMap[Project(
                                                    startSeg.start.position)],
                                                map.streetIntersectionMap[Project(
                                                    startSeg.end.position)]);

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
                            var nextSegPositions = MeshBuilder.RemoveDetailByDistance(
                                nextSegPositionsStream.ToArray(), 2.5f);

                            nextSegPositions = MeshBuilder.RemoveDetailByAngle(
                                nextSegPositions, 5f);

                            removedVerts += nextSeg.positions.Count - nextSegPositions.Count;

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

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            Debug.Log("removed " + removedVerts + " street verts");
        }

        public void LoadPolygon(PSLG pslg, Relation rel,
                                float simplificationThresholdAngle = 0f,
                                string outer = "outer",
                                string inner = "inner")
        {
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
                    if (simplificationThresholdAngle > 0f)
                    {
                        wayPositions = MeshBuilder.RemoveDetailByAngle(
                            wayPositions, simplificationThresholdAngle);
                    }

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
                        if (simplificationThresholdAngle > 0f)
                        {
                            wayPositions = MeshBuilder.RemoveDetailByAngle(
                                wayPositions, simplificationThresholdAngle);
                        }

                        pslg.AddHole(wayPositions);
                    }
                }
            }
        }

        public void LoadPolygon(Relation rel,
                                float simplificationThresholdAngle = 0f,
                                string outer = "outer",
                                string inner = "inner")
        {
            var pslg = new PSLG();
            LoadPolygon(pslg, rel, simplificationThresholdAngle, outer, inner);
        }

        static readonly float ParkThresholdSize = 150f;

        IEnumerator LoadParks()
        {
            var totalVerts = 0;
            var smallParks = 0;

            foreach (var feature in naturalFeatures)
            {
                if (!ShouldImport(feature.Item1))
                {
                    continue;
                }

                try
                {
                    var featureName = feature.Item1.Tags.GetValue("name");
                    Vector2[][] outlinePositions = null;
                    float area = 0f;
                    Vector2 centroid = Vector2.zero;

                    Debug.Log("loading natural feature '" + featureName + "'...");

                    Mesh mesh;
                    if (feature.Item1.Type == OsmGeoType.Relation)
                    {
                        var pslg = new PSLG();
                        LoadPolygon(pslg, feature.Item1 as Relation);

                        mesh = TriangleAPI.CreateMesh(pslg);
                        outlinePositions = pslg?.Outlines;
                        area = pslg?.Area ?? 0f;
                        centroid = pslg?.Centroid ?? Vector2.zero;
                    }
                    else
                    {
                        var way = feature.Item1 as Way;
                        var wayPositions = GetWayPositions(way);

                        if (wayPositions.Count < 3)
                        {
                            continue;
                        }

                        var wayPositionsArr = wayPositions.ToArray();
                        mesh = MeshBuilder.PointsToMeshFast(wayPositionsArr);
                        MeshBuilder.FixWindingOrder(mesh);

                        outlinePositions = new Vector2[][]
                        {
                        wayPositionsArr.Select(v => (Vector2)v).ToArray(),
                        };

                        area = Math.GetAreaOfPolygon(wayPositionsArr);
                        centroid = Math.GetCentroid(wayPositionsArr);
                    }

                    if (area < ParkThresholdSize)
                    {
                        ++smallParks;
                        continue;
                    }

                    totalVerts += mesh?.triangles.Length ?? 0;

                    var f = map.CreateFeature(featureName, feature.Item2, mesh, area, centroid);
                    if (outlinePositions != null)
                    {
                        outlinePositions = outlinePositions.Select(
                            arr => MeshBuilder.RemoveDetailByDistance(arr, 1f).ToArray()).ToArray();

                        f.outlinePositions = outlinePositions;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                    continue;
                }

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            Debug.Log("feature verts: " + totalVerts);
            Debug.Log("yeeted " + smallParks + " parks for being too god damn small");
            // yield return LoadFootpaths();
        }

        IEnumerator LoadFootpaths()
        {
            var verts = 0;
            foreach (var street in streets)
            {
                var tags = street.Item1.Tags;
                var name = tags.GetValue("name");
                Debug.Log("loading path '" + name + "'...");

                var positions = GetWayPositions(street.Item1);
                positions = MeshBuilder.RemoveDetailByAngle(positions.ToArray(), 5f);

                verts += positions.Count;

                var mesh = MeshBuilder.CreateSmoothLine(positions,
                                                        2 * StreetSegment.laneWidth * 0.3f,
                                                        10);

                map.CreateFeature(name, NaturalFeature.Type.Footpath, mesh, 0f, Vector2.zero);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            Debug.Log("footpath verts: " + verts);
        }

        void BuildEquivalenceClass(Building building,
                                   HashSet<Building> equivalenceClass,
                                   HashSet<Building> checkedBuildings,
                                   Dictionary<long, HashSet<Building>> nodeMap,
                                   Dictionary<Building, long[]> nodesMap)
        {
            if (!checkedBuildings.Add(building))
            {
                return;
            }
            if (!nodesMap.TryGetValue(building, out long[] nodeIds))
            {
                return;
            }

            equivalenceClass.Add(building);

            foreach (var nodeId in nodeIds)
            {
                if (!nodeMap.TryGetValue(nodeId, out HashSet<Building> buildings))
                {
                    continue;
                }

                foreach (var nextBuilding in buildings)
                {
                    BuildEquivalenceClass(nextBuilding, equivalenceClass, checkedBuildings, nodeMap, nodesMap);
                }
            }
        }

        class PartialBuilding
        {
            internal Tuple<OsmGeo, Building.Type> buildingData;
            internal PSLG pslg;
            internal Vector2 centroid;
        }

        struct Edge
        {
            internal Vector2 begin;
            internal Vector2 end;

            public Edge(Tuple<Vector2, Vector2> unorderedEdge) : this(unorderedEdge.Item1, unorderedEdge.Item2)
            {
                
            }

            public Edge(Vector2 v1, Vector2 v2)
            {
                if (v1.x <= v2.x)
                {
                    begin = v1;
                    end = v2;
                }
                else
                {
                    begin = v2;
                    end = v1;
                }
            }
        }

        bool CheckEquivalence(int idToCheck, int currentID, Dictionary<int,
                              HashSet<int>> equivalences,
                              HashSet<int> checkedEquivalences)
        {
            if (!checkedEquivalences.Add(currentID))
                return false;

            if (currentID == idToCheck)
                return true;

            foreach (var equivalentID in equivalences[currentID])
            {
                if (CheckEquivalence(idToCheck, equivalentID, equivalences, checkedEquivalences))
                    return true;
            }

            return false;
        }

        List<PartialBuilding> MergeBuildings(List<PartialBuilding> partialBuildings)
        {
            var singleEdgeMap = new Dictionary<Edge, Vector2>();
            var sharedEdgeMap = new Dictionary<Edge, HashSet<Vector2>>();

            foreach (var building in partialBuildings)
            {
                foreach (var unorderedEdge in building.pslg.Edges)
                {
                    var edge = new Edge(unorderedEdge);
                    if (sharedEdgeMap.TryGetValue(edge, out HashSet<Vector2> set))
                    {
                        set.Add(building.centroid);
                        continue;
                    }
                    if (singleEdgeMap.TryGetValue(edge, out Vector2 otherBuilding))
                    {
                        set = new HashSet<Vector2> { otherBuilding, building.centroid };
                        sharedEdgeMap.Add(edge, set);
                        singleEdgeMap.Remove(edge);

                        continue;
                    }

                    singleEdgeMap.Add(edge, building.centroid);
                }
            }

            var nextEquivalenceClass = 0;
            var equivalences = new Dictionary<int, HashSet<int>>();
            var equivalenceClasses = new Dictionary<Edge, int>();
            var assignments = new Dictionary<Vector2, int>();
            var mergedBuildings = new List<PartialBuilding>();
            var visitedBuildings = new HashSet<Vector2>();
            var buildingTypes = new Dictionary<int, Building.Type>();

            foreach (var building in partialBuildings)
            {
                var equivalenceClass = -1;
                foreach (var unorderedEdge in building.pslg.Edges)
                {
                    var edge = new Edge(unorderedEdge);
                    if (equivalenceClasses.TryGetValue(edge, out int classID))
                    {
                        if (equivalenceClass == -1)
                        {
                            equivalenceClass = classID;
                        }
                        else
                        {
                            equivalences[equivalenceClass].Add(classID);
                            equivalences[classID].Add(equivalenceClass);
                        }
                    }
                    else if (sharedEdgeMap.TryGetValue(edge, out HashSet<Vector2> _))
                    {
                        classID = nextEquivalenceClass++;
                        equivalenceClasses.Add(edge, classID);
                        equivalences.Add(classID, new HashSet<int>());
                        buildingTypes.Add(classID, building.buildingData.Item2);

                        if (equivalenceClass == -1)
                        {
                            equivalenceClass = classID;
                        }
                        else
                        {
                            equivalences[equivalenceClass].Add(classID);
                            equivalences[classID].Add(equivalenceClass);
                        }
                    }
                }

                if (equivalenceClass == -1)
                {
                    mergedBuildings.Add(building);
                    visitedBuildings.Add(building.centroid);
                    continue;
                }

                if (assignments.TryGetValue(building.centroid, out int id))
                {
                    Debug.LogWarning("duplicate centroid: " + building.centroid);

                    mergedBuildings.Add(building);
                    visitedBuildings.Add(building.centroid);

                    continue;
                }

                assignments.Add(building.centroid, equivalenceClass);
            }

            var buildingsToMerge = new List<Vector2>();
            var checkedEquivalences = new HashSet<int>();
            var pslgs = new List<PSLG>();

            for (var id = 0; id < nextEquivalenceClass; ++id)
            {
                if (visitedBuildings.Count == partialBuildings.Count)
                    break;

                var type = buildingTypes[id];
                foreach (var building in partialBuildings)
                {
                    if (building.buildingData.Item2 != type || visitedBuildings.Contains(building.centroid))
                        continue;

                    if (!assignments.TryGetValue(building.centroid, out int classID))
                    {
                        continue;
                    }

                    if (CheckEquivalence(classID, id, equivalences, checkedEquivalences))
                    {
                        buildingsToMerge.Add(building.centroid);
                        pslgs.Add(building.pslg);
                        visitedBuildings.Add(building.centroid);
                    }

                    checkedEquivalences.Clear();
                }

                if (buildingsToMerge.Count > 0)
                {
                    var mergedBuilding = new PartialBuilding()
                    {
                        buildingData = new Tuple<OsmGeo, Building.Type>(null, type),
                    };

                    mergedBuilding.pslg = Math.PolygonUnion(pslgs);
                    mergedBuilding.centroid = mergedBuilding.pslg?.Centroid ?? Vector2.zero;
                    mergedBuildings.Add(mergedBuilding);
                }

                buildingsToMerge.Clear();
                pslgs.Clear();
            }

            return mergedBuildings;
        }

        /// <summary>
        ///  Buildings with an area lower than this will not be imported. (in m^2)
        /// </summary>
        static readonly float ThresholdArea = 100f;
        Dictionary<StreetSegment, int> assignedNumbers = new Dictionary<StreetSegment, int>();

        // TODO: Generate sensible building names.
        string GetBuildingName(PartialBuilding building, float area)
        {
            var tags = building.buildingData.Item1?.Tags;
            if (tags?.ContainsKey("name") ?? false)
            {
                return tags.GetValue("name");
            }

            //var capacity = Building.GetDefaultCapacity(building.buildingData.Item2, area);
            var closestStreet = map.GetClosestStreet(building.centroid);
            if (closestStreet == null)
            {
                return "";
            }

            if (assignedNumbers.TryGetValue(closestStreet.seg, out int number))
            {
                assignedNumbers[closestStreet.seg]++;
                return closestStreet.seg.street.name + " " + number;
            }

            assignedNumbers.Add(closestStreet.seg, 1);
            return closestStreet.seg.street.name + " 1";
        }

        IEnumerator LoadBuildings()
        {
            var totalVerts = 0;
            var partialBuildings = new List<PartialBuilding>();

            foreach (var building in buildings)
            {
                if (!ShouldImport(building.Item1))
                {
                    continue;
                }

                Debug.Log(building.Item2);

                var pslg = new PSLG();
                try
                {
                    if (building.Item1.Type == OsmGeoType.Relation)
                    {
                        LoadPolygon(pslg, building.Item1 as Relation);
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
                        }

                        if (wayPositions.Count < 3)
                        {
                            continue;
                        }

                        pslg.AddVertexLoop(wayPositions);
                    }

                    var centroid = pslg.Centroid;
                    partialBuildings.Add(new PartialBuilding
                    {
                        buildingData = building,
                        pslg = pslg,
                        centroid = centroid,
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                    continue;
                }
            }

            var mergedBuildings = MergeBuildings(partialBuildings);
            Debug.Log("reduced from " + partialBuildings.Count + " to " + mergedBuildings.Count + " buildings");

            var smallBuildings = 0;
            foreach (var building in mergedBuildings)
            {
                var type = building.buildingData.Item2;
                var outlinePositions = building.pslg?.Outlines;
                var centroid = building.centroid;
                var area = building.pslg?.Area ?? 0f;

                if (area < ThresholdArea)
                {
                    ++smallBuildings;
                    continue;
                }

                var mesh = TriangleAPI.CreateMesh(building.pslg);
                totalVerts += mesh?.triangles.Length ?? 0;

                var b = map.CreateBuilding(type, mesh, GetBuildingName(building, area), "", area, centroid);
                if (outlinePositions != null)
                {
                    outlinePositions = outlinePositions.Select(
                        arr => MeshBuilder.RemoveDetailByDistance(arr, 1f).ToArray()).ToArray();

                    b.outlinePositions = outlinePositions;
                }

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            Debug.Log("yeeted " + smallBuildings + " buildings because of small area");

            //foreach (var building in buildings)
            //{
            //    if (!ShouldImport(building.Item1))
            //    {
            //        continue;
            //    }

            //    try
            //    {
            //        var buildingName = building.Item1.Tags.GetValue("name");
            //        var numberStr = building.Item1.Tags.GetValue("addr:housenumber");

            //        if (string.IsNullOrEmpty(buildingName))
            //        {
            //            var streetName = building.Item1.Tags.GetValue("addr:street");
            //            buildingName = streetName;

            //            if (!string.IsNullOrEmpty(numberStr))
            //            {
            //                buildingName += " ";
            //                buildingName += numberStr;
            //            }
            //        }

            //        Debug.Log("loading building '" + buildingName + "'...");

            //        var type = building.Item2;
            //        Vector2[][] outlinePositions = null;
            //        float area = 0f;
            //        Vector2 centroid = Vector2.zero;

            //        Mesh mesh;
            //        if (building.Item1.Type == OsmGeoType.Relation)
            //        {
            //            var pslg = new PSLG();

            //            mesh = TriangleAPI.CreateMesh(pslg);
            //            outlinePositions = pslg?.Outlines;
            //            area = pslg?.Area ?? 0f;
            //            centroid = pslg?.Centroid ?? Vector2.zero;
            //        }
            //        else
            //        {
            //            var way = building.Item1 as Way;
            //            var wayPositions = new List<Vector3>();

            //            foreach (var nodeId in way.Nodes)
            //            {
            //                var node = FindNode(nodeId);
            //                if (node == null)
            //                {
            //                    continue;
            //                }

            //                var loc = Project(node);
            //                wayPositions.Add(loc);

            //                if (node.Tags != null)
            //                {
            //                    if (string.IsNullOrEmpty(numberStr))
            //                    {
            //                        numberStr = node.Tags.GetValue("addr:housenumber");
            //                    }

            //                    if (node.Tags.ContainsKey("shop"))
            //                    {
            //                        var val = node.Tags.GetValue("shop");
            //                        if (val == "convenience" || val == "supermarket")
            //                        {
            //                            type = Building.Type.GroceryStore;
            //                        }
            //                        else
            //                        {
            //                            type = Building.Type.Shop;
            //                        }
            //                    }
            //                    else if (node.Tags.ContainsKey("office"))
            //                    {
            //                        type = Building.Type.Office;
            //                    }
            //                }
            //            }

            //            if (wayPositions.Count < 3)
            //            {
            //                continue;
            //            }

            //            var wayPositionsArr = wayPositions.ToArray();
            //            mesh = MeshBuilder.PointsToMeshFast(wayPositionsArr);
            //            MeshBuilder.FixWindingOrder(mesh);

            //            outlinePositions = new Vector2[][]
            //            {
            //                wayPositionsArr.Select(v => (Vector2)v).ToArray(),
            //            };

            //            area = Math.GetAreaOfPolygon(wayPositionsArr);
            //            centroid = Math.GetCentroid(wayPositionsArr);
            //        }

            //        totalVerts += mesh?.triangles.Length ?? 0;

            //        var b = map.CreateBuilding(type, mesh, buildingName, numberStr, area, centroid);
            //        if (outlinePositions != null)
            //        {
            //            outlinePositions = outlinePositions.Select(
            //                arr => MeshBuilder.RemoveDetailByDistance(arr, 1f).ToArray()).ToArray();

            //            b.outlinePositions = outlinePositions;
            //        }

            //        // if (building.Item1.Type == OsmGeoType.Way)
            //        // {
            //        //     var nodes = (building.Item1 as Way).Nodes;
            //        //     nodesMap.Add(b, nodes);

            //        //     foreach (var nodeId in nodes)
            //        //     {
            //        //         addNode(nodeId, b);
            //        //     }
            //        // }
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.LogWarning(e.Message);
            //        continue;
            //    }

            //    if (FrameTimer.instance.FrameDuration >= thresholdTime)
            //    {
            //        yield return null;
            //    }
            //}

            Debug.Log("building verts: " + totalVerts);
        }
    }
}
