#if UNITY_EDITOR

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Transidious.Serialization.OSM;
using UnityEditor;

namespace Transidious
{
    public class OSMImporterProxy
    {
        public class TransitLine
        {
            internal TransitType type;
            internal OsmSharp.Relation inbound;
            internal OsmSharp.Relation outbound;

            void Add(OsmSharp.Relation line)
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

        public Dictionary<long, OsmSharp.Node> nodes = new Dictionary<long, OsmSharp.Node>();
        public List<Tuple<OsmSharp.Way, Street.Type>> streets = new List<Tuple<OsmSharp.Way, Street.Type>>();

        HashSet<OsmSharp.Relation> referencedRelations = new HashSet<OsmSharp.Relation>();

        public Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
        public Dictionary<long, OsmSharp.Node> stops = new Dictionary<long, OsmSharp.Node>();

        public HashSet<long> visualOnlyFeatures = new HashSet<long>();
        public List<Tuple<OsmSharp.OsmGeo, NaturalFeature.Type>> naturalFeatures = new List<Tuple<OsmSharp.OsmGeo, NaturalFeature.Type>>();
        public List<Tuple<OsmSharp.OsmGeo, Building.Type>> buildings = new List<Tuple<OsmSharp.OsmGeo, Building.Type>>();

        public OsmSharp.Relation boundary = null;
        public Dictionary<long, OsmSharp.Way> ways = new Dictionary<long, OsmSharp.Way>();

        public double minLat = double.PositiveInfinity;
        public double minLng = double.PositiveInfinity;
        public double maxLat = 0;
        public double maxLng = 0;

        public float minUsedLat = float.PositiveInfinity;
        public float minUsedLng = float.PositiveInfinity;

        public float maxX;
        public float minX;

        public float maxY;
        public float minY;

        float cosCenterLat;
        private static readonly float earthRadius = 6371000f;

        public void ImportArea(string areaName)
        {
            {
                var _ = new OSMImportHelper(this, areaName);
            }

            FindMinLngAndLat();

            cosCenterLat = Mathf.Cos((float)(minLat + (maxLat - minLat) * 0.5f) * Mathf.Deg2Rad);

            minX = earthRadius * ((float)minLng * Mathf.Deg2Rad) * cosCenterLat;
            minY = earthRadius * ((float)minLat * Mathf.Deg2Rad);

            maxX = earthRadius * ((float)maxLng * Mathf.Deg2Rad) * cosCenterLat;
            maxY = earthRadius * ((float)maxLat * Mathf.Deg2Rad);
        }

        public Area Deserialize(string fileName)
        {
            return SaveManager.Decompress(fileName, Area.Parser);
        }

        public void Load(Area area, OSMImporter importer)
        {
            importer.minX = area.MinX;
            importer.maxX = area.MaxX;
            importer.minY = area.MinY;
            importer.maxY = area.MaxY;

            foreach (var node in area.Nodes)
            {
                importer.nodes.Add(node.Geo.Id, node);
            }
            foreach (var way in area.Ways)
            {
                importer.ways.Add(way.Geo.Id, way);
            }
            foreach (var rel in area.Relations)
            {
                importer.relations.Add(rel.Geo.Id, rel);
            }

            importer.boundary = importer.relations[area.Boundary];

            foreach (var street in area.Streets)
            {
                importer.streets.Add(Tuple.Create(importer.ways[street.WayId], (Street.Type)street.Type));
            }
            foreach (var feature in area.Features)
            {
                importer.naturalFeatures.Add(Tuple.Create(feature.GeoId, (NaturalFeature.Type)feature.Type));
                if (feature.VisualOnly)
                    importer.visualOnlyGeos.Add(feature.GeoId);
            }
            foreach (var building in area.Buildings)
            {
                importer.buildings.Add(Tuple.Create(building.GeoId, (Building.Type)building.Type));
            }

            foreach (var line in area.Lines)
            {
                importer.lines.Add(line.Name, new OSMImporter.TransitLine
                {
                    type = (TransitType)line.Type,
                    inbound = line.InboundId != 0 ? importer.relations[line.InboundId] : null,
                    outbound = line.OutboundId != 0 ? importer.relations[line.OutboundId] : null,
                });
            }
        }

        Vector2 Project(OsmSharp.Node node)
        {
            return Project((float)node.Longitude, (float)node.Latitude);
        }

        Vector2 Project(float lng, float lat)
        {
            var x = earthRadius * (lng * Mathf.Deg2Rad) * cosCenterLat;
            var y = earthRadius * (lat * Mathf.Deg2Rad);

            return new Vector2((x - minX), (y - minY));
        }

        OsmSharp.Way FindWay(long id)
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

        OsmSharp.Node FindNode(long id)
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

        void FindMinLngAndLat()
        {
            if (boundary == null)
            {
                return;
            }

            minLng = double.PositiveInfinity;
            minLat = double.PositiveInfinity;
            maxLng = double.NegativeInfinity;
            maxLat = double.NegativeInfinity;

            foreach (var member in boundary.Members)
            {
                var way = FindWay(member.Id);
                if (way == null)
                {
                    continue;
                }

                foreach (var nodeId in way.Nodes)
                {
                    if (!nodes.TryGetValue(nodeId, out OsmSharp.Node node))
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

        OsmGeo Serialize(OsmSharp.OsmGeo geo)
        {
            var result = new OsmGeo
            {
                Id = (ulong)geo.Id.Value,
                Type = (OsmGeo.Types.Type)geo.Type,
            };

            foreach (var tag in geo.Tags)
            {
                result.Tags.Add(tag.Key, tag.Value);
            }

            return result;
        }

        Node Serialize(OsmSharp.Node node)
        {
            return new Node
            {
                Geo = Serialize((OsmSharp.OsmGeo)node),
                Position = Project(node).ToProtobuf(),
            };
        }

        Way Serialize(OsmSharp.Way way)
        {
            var result = new Way
            {
                Geo = Serialize((OsmSharp.OsmGeo)way),
            };

            foreach (var id in way.Nodes)
            {
                result.Nodes.Add((ulong)id);
            }

            return result;
        }

        Relation Serialize(OsmSharp.Relation rel)
        {
            var result = new Relation
            {
                Geo = Serialize((OsmSharp.OsmGeo)rel),
            };

            foreach (var member in rel.Members)
            {
                result.Members.Add(new Relation.Types.Member
                {
                    Id = (ulong)member.Id,
                    Type = (OsmGeo.Types.Type)member.Type,
                    Role = member.Role,
                });
            }

            return result;
        }

        ulong AddRelation(OsmSharp.Relation rel)
        {
            if (rel == null)
            {
                return 0;
            }

            referencedRelations.Add(rel);
            return (ulong)rel.Id.Value;
        }

        public Area Serialize()
        {
            var maxValues = Project((float)maxLng, (float)maxLat);
            var area = new Area
            {
                MinX = 0f,
                MaxX = maxValues.x,
                MinY = 0f,
                MaxY = maxValues.y,
            };
            
            foreach (var node in nodes)
            {
                area.Nodes.Add(Serialize(node.Value));
            }

            foreach (var way in ways)
            {
                area.Ways.Add(Serialize(way.Value));
            }

            area.Boundary = AddRelation(boundary);

            foreach (var street in streets)
            {
                area.Streets.Add(new Area.Types.Street
                {
                    WayId = (ulong)street.Item1.Id.Value,
                    Type = (Serialization.Street.Types.Type)street.Item2,
                });
            }

            foreach (var building in buildings)
            {
                if (building.Item1.Type == OsmSharp.OsmGeoType.Relation)
                {
                    AddRelation(building.Item1 as OsmSharp.Relation);
                }

                area.Buildings.Add(new Area.Types.Building
                {
                    GeoId = (ulong)building.Item1.Id.Value,
                    Type = (Serialization.Building.Types.Type)building.Item2,
                });
            }
            
            foreach (var feature in naturalFeatures)
            {
                if (feature.Item1.Type == OsmSharp.OsmGeoType.Relation)
                {
                    AddRelation(feature.Item1 as OsmSharp.Relation);
                }

                area.Features.Add(new Area.Types.NaturalFeature
                {
                    GeoId = (ulong)feature.Item1.Id.Value,
                    Type = (Serialization.NaturalFeature.Types.Type)feature.Item2,
                    VisualOnly = visualOnlyFeatures.Contains(feature.Item1.Id.Value),
                });
            }

            foreach (var line in lines)
            {
                if (line.Value.inbound != null)
                {
                    AddRelation(line.Value.inbound);
                }
                if (line.Value.outbound != null)
                {
                    AddRelation(line.Value.outbound);
                }

                area.Lines.Add(new Area.Types.TransitLine
                {
                    Name = line.Key,
                    Type = (Serialization.TransitType)line.Value.type,
                    InboundId = (ulong)(line.Value.inbound?.Id.Value ?? 0),
                    OutboundId = (ulong)(line.Value.outbound?.Id.Value ?? 0),
                });
            }

            foreach (var rel in referencedRelations)
            {
                area.Relations.Add(Serialize(rel));
            }

            return area;
        }

        public Area Save(string fileName)
        {
            var area = this.Serialize();
            SaveManager.CompressGZip<Area>(fileName, area);

            return area;
        }
    }

    public class OSMImporter : MonoBehaviour
    {
        public enum ImportType
        {
            Fast,
            Complete,
        }

        public enum MapExportType
        {
            Mesh,
            Picture,
        }

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

        class BuildingMesh
        {
            internal int id;
            internal Vector3 centroid;
            internal float area;
            internal Mesh mesh;
        }

        public Map map;
        public ImportType importType = ImportType.Complete;
        public MapExportType exportType = MapExportType.Picture;

        public MapExporter exporter;

        public int thresholdTime = 1000;
        public bool loadGame = true;
        public bool importOnly = false;
        public bool forceReload = false;
        public int resolution = 8192;

        public Relation boundary = null;
        public Dictionary<ulong, Node> nodes = new Dictionary<ulong, Node>();
        public Dictionary<ulong, Way> ways = new Dictionary<ulong, Way>();
        public Dictionary<ulong, Relation> relations = new Dictionary<ulong, Relation>();

        public List<Tuple<Way, Street.Type>> streets = new List<Tuple<Way, Street.Type>>();

        public Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
        public Dictionary<ulong, Node> stops = new Dictionary<ulong, Node>();
        Dictionary<ulong, Stop> stopMap = new Dictionary<ulong, Stop>();

        public Dictionary<Vector2, StreetSegment> streetSegmentMap = new Dictionary<Vector2, StreetSegment>();
        public Dictionary<Vector2, Transidious.StreetIntersection> streetIntersectionMap = new Dictionary<Vector2, Transidious.StreetIntersection>();

        public HashSet<ulong> visualOnlyGeos = new HashSet<ulong>();
        public List<Tuple<ulong, NaturalFeature.Type>> naturalFeatures = new List<Tuple<ulong, NaturalFeature.Type>>();
        public List<Tuple<ulong, Building.Type>> buildings = new List<Tuple<ulong, Building.Type>>();

        public int maxUniqueBuildings = 100;
        public int registeredUniqueBuildings = 0;
        List<BuildingMesh> uniqueBuildings = new List<BuildingMesh>();

        public float maxX;
        public float minX;
        public float maxY;
        public float minY;

        public OSMImportHelper.Area area;
        public bool loadTransitLines;
        public string[] linesToLoad;
        public bool done;

        public static readonly float maxTileSize = 999999f; // 6000f;

        async void Start()
        {
            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = Instantiate(mapPrefab);

            map = mapObj.GetComponent<Map>();
            map.name = this.area.ToString();
            map.input = GameController.instance.input;
            SaveManager.loadedMap = map;

            this.exporter = new MapExporter(map, resolution);
            this.ImportArea();

            if (importOnly)
            {
                Debug.Log("Done!");
                return;
            }

            StartCoroutine(LoadFeatures());
        }

        void ImportArea()
        {
            var areaName = this.area.ToString();

            string fileName = "Resources/Areas/";
            fileName += areaName;
            fileName += ".bytes";

            var proxy = new OSMImporterProxy();

            Area area;
            if (!System.IO.File.Exists(fileName) || forceReload)
            {
                proxy.ImportArea(areaName);
                area = proxy.Save(fileName);
            }
            else
            {
                area = proxy.Deserialize(fileName);
            }

            if (importOnly)
            {
                return;
            }

            proxy.Load(area, this);
        }

        IEnumerator LoadFeatures()
        {
            using (this.CreateTimer("Boundary"))
            {
                LoadBoundary();
            }

            using (this.CreateTimer("Features"))
            {
                yield return LoadParks();
            }

            using (this.CreateTimer("Streets"))
            {
                yield return LoadStreets();
            }

            using (this.CreateTimer("Buildings"))
            {
                yield return LoadBuildings();
            }

            using (this.CreateTimer("Create Street Names"))
            {
                yield return map.FinalizeStreetMeshes(thresholdTime);
            }

            if (exportType == MapExportType.Mesh)
            {
                using (this.CreateTimer("Create Prefab Meshes"))
                {
                    yield return exporter.CreatePrefabMeshes(
                        importType == ImportType.Fast,
                        exportType == MapExportType.Mesh,
                        9999999);
                }
            }

            using (this.CreateTimer("Finalize"))
            {
                yield return map.DoFinalize(thresholdTime);
            }

            if (loadTransitLines)
            {
                using (this.CreateTimer("Transit Lines"))
                {
                    yield return LoadTransitLines();
                }
            }

            using (this.CreateTimer("Save Map"))
            {
                SaveManager.SaveMapLayout(map);
                SaveManager.SaveMapData(map, map.name);
            }

            using (this.CreateTimer("Screenshots"))
            {
                exporter.ExportMap(map.name);
            }

            if (exportType == MapExportType.Mesh)
            {
                using (this.CreateTimer("Map Prefab"))
                {
                    exporter.ExportMapPrefab();
                }
            }

            using (this.CreateTimer("Minimap"))
            {
                exporter.ExportMinimap(map.name);
            }

            using (this.CreateTimer("Save Assets"))
            {
                UnityEditor.AssetDatabase.SaveAssets();
            }

            exporter.PrintStats();
            Debug.Log("Done!");

            done = true;

            if (loadGame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
            }
        }

        Vector2 Project(Node node)
        {
            return node.Position.Deserialize();
        }

        Way FindWay(ulong id)
        {
            if (ways.TryGetValue(id, out Way result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        Relation FindRelation(ulong id)
        {
            if (relations.TryGetValue(id, out Relation rel))
            {
                return rel;
            }
            else
            {
                return null;
            }
        }

        Node FindNode(ulong id)
        {
            if (nodes.TryGetValue(id, out Node node))
            {
                return node;
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
            var boundaries = relation.Members.Where(m => m.Role == role).Count();

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

                    wayNode.positions.Add(node.Position.Deserialize());
                }

                if (wayNode.positions.Count > 0)
                {
                    // Some borders have disjointed exclaves (e.g. Berlin), exclude those.
                    if (!wayNode.positions.First().Equals(wayNode.positions.Last()) || boundaries == 1)
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

                positions.Add(node.Position.Deserialize());
            }
        }

        void LoadBoundary()
        {
            map.minX = 0;
            map.minY = 0;

            map.maxX = maxX;
            map.maxY = maxY;

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

        void AddStopToLine(Map.LineBuilder l, Stop s, Stop previousStop, int i, List<List<Vector3>> wayPositions,
                           bool setDepot = true)
        {
            if (s == previousStop)
            {
                return;
            }

            if (previousStop != null)
            {
                List<Vector3> positions = null;
                List<TrafficSimulator.PathSegmentInfo> segmentInfo = null;

                if (l.line.type == TransitType.Bus)
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

                l.AddStop(s, true, false, positions);
                if (segmentInfo != null)
                {
                    // Note which streets this route passes over.
                    var route = l.line.routes.Last();
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
                l.line.depot = s;
            }
        }

        Stop GetOrCreateStop(Node stopNode, bool projectOntoStreet = false)
        {
            if (stopNode == null)
            {
                return null;
            }

            if (stopMap.TryGetValue(stopNode.Geo.Id, out Stop stop))
            {
                return stop;
            }

            var loc = Project(stopNode);
            if (projectOntoStreet)
            {
                var street = map.GetClosestStreet(loc)?.seg;
                if (street == null)
                {
                    Debug.LogWarning("'" + stopNode.Geo.Tags.GetValue("name") + "' is not on map");
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

            var stopName = stopNode.Geo.Tags.GetValue("name");
            stopName = stopName.Replace("S+U ", "");
            stopName = stopName.Replace("S ", "");
            stopName = stopName.Replace("U ", "");
            stopName = stopName.Replace("Berlin-", "");
            stopName = stopName.Replace("Berlin ", "");

            var s = map.CreateStop(stopName, loc);
            stopMap.Add(stopNode.Geo.Id, s);

            return s;
        }

        IEnumerator LoadTransitLines()
        {
            foreach (var linePair in lines)
            {
                if (linesToLoad.Length > 0 && !linesToLoad.Contains(linePair.Value.inbound.Geo.Tags["ref"]))
                {
                    continue;
                }

                var l1 = linePair.Value.inbound;
                var l2 = linePair.Value.outbound;

                Color? color = null;
                if (ColorUtility.TryParseHtmlString(l1.Geo.Tags.GetValue("colour"), out Color c))
                {
                    color = c;
                }

                var name = l1.Geo.Tags.GetValue("ref");
                var type = linePair.Value.type;
                var l = map.CreateLine(type, name, color);

                var members = l1.Members.ToList();
                if (l2 != null)
                {
                    members.AddRange(l2.Members);
                }

                int i = 0;
                List<Vector3> wayPositions = null;
                List<Vector3> currentPositions = null;

                if (type != TransitType.Bus)
                {
                    wayPositions = new List<Vector3>();
                    currentPositions = new List<Vector3>();
                    
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

                            if (i != 0 && node.Geo.Tags != null
                            && (node.Geo.Tags.GetValue("railway").StartsWith("stop", StringComparison.InvariantCulture)
                            || node.Geo.Tags.GetValue("public_transport").StartsWith("stop", StringComparison.InvariantCulture)
                            || node.Geo.Tags.GetValue("highway").Contains("stop")))
                            {
                                wayPositions.AddRange(currentPositions);
                                currentPositions.Clear();
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

                    if (type == TransitType.Bus && platform != null)
                    {
                        foreach (var nodeId in platform.Nodes)
                        {
                            var node = FindNode(nodeId);
                            if (node == null)
                            {
                                continue;
                            }

                            if (node.Geo.Tags.Contains("highway", "bus_stop"))
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
                        l.AddStop(s, true, false, wayPositions, true);
                        previousStop = s;
                    }

                    ++i;
                    ++n;
                }
                
                if (l.line.stops.Count != 0 && previousStop != l.line.stops.First())
                {
                    l.AddStop(l.line.stops.First(), true, false, null, true);
                }

                l.Finish();

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
                var tags = street.Item1.Geo.Tags;
                var streetName = tags.GetValue("name");
                // Debug.Log("loading street '" + streetName + "'...");

                if (string.IsNullOrEmpty(streetName))
                {
                    streetName = street.Item1.Geo.Id.ToString() ?? (namelessStreets++).ToString();
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

                    var segPositionsStream = startSeg.positions.Select(n => (Vector3)this.Project(n));
                    var segPositions = MeshBuilder.RemoveDetailByDistance(
                        segPositionsStream.ToArray(), 2.5f);

                    segPositions = MeshBuilder.RemoveDetailByAngle(segPositions, 5f);

                    removedVerts += startSeg.positions.Count - segPositions.Count;

                    var street = map.CreateStreet(startSeg.name, startSeg.type, startSeg.lit,
                                                  startSeg.oneway, startSeg.maxspeed,
                                                  startSeg.lanes);

                    var seg = street.AddSegment(
                        segPositions,
                        map.streetIntersectionMap[Project(startSeg.start.position)],
                        map.streetIntersectionMap[Project(startSeg.end.position)]);

                    if (exportType == MapExportType.Mesh)
                    {
                        exporter.RegisterMesh(seg, (PSLG)null,
                            (int)Map.Layer(MapLayer.Buildings),
                            Color.black);
                    }

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

                            var nextSegPositionsStream = nextSeg.positions.Select(n => (Vector3)this.Project(n));
                            var nextSegPositions = MeshBuilder.RemoveDetailByDistance(
                                nextSegPositionsStream.ToArray(), 2.5f);

                            nextSegPositions = MeshBuilder.RemoveDetailByAngle(
                                nextSegPositions, 5f);

                            removedVerts += nextSeg.positions.Count - nextSegPositions.Count;

                            seg = street.AddSegment(
                                nextSegPositions,
                                map.streetIntersectionMap[Project(nextSeg.start.position)],
                                map.streetIntersectionMap[Project(nextSeg.end.position)]);

                            if (exportType == MapExportType.Mesh)
                            {
                                exporter.RegisterMesh(seg, (PSLG)null,
                                    (int)Map.Layer(MapLayer.Buildings),
                                    Color.black);
                            }

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
                if (!string.IsNullOrEmpty(outer) && member.Role == outer)
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
                else if (!string.IsNullOrEmpty(inner) && member.Role == inner)
                {
                    var way = FindWay(member.Id);
                    if (way == null)
                    {
                        continue;
                    }

                    var wayPositions = GetWayPositions(way);
                    if (wayPositions.Count == way.Nodes.Count)
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

        private static readonly float inclusionThresholdArea = 250f;
        private static readonly float interactableThresholdArea = 1000f;

        IEnumerator LoadParks()
        {
            var tinyParks = 0;
            var smallParks = 0;

            foreach (var feature in naturalFeatures)
            {
                try
                {
                    var way = FindWay(feature.Item1);
                    var rel = FindRelation(feature.Item1);

                    OsmGeo geo;
                    if (way != null)
                    {
                        geo = way.Geo;
                    }
                    else
                    {
                        geo = rel.Geo;
                    }

                    var featureName = geo.Tags.GetValue("name") ?? string.Empty;
                    Vector2[][] outlinePositions = null;
                    float area = 0f;
                    Vector2 centroid = Vector2.zero;

                    //Debug.Log("loading natural feature '" + featureName + "'...");

                    PSLG pslg = null;
                    Vector3[] wayPositionsArr = null;

                    if (rel != null)
                    {
                        pslg = new PSLG();
                        LoadPolygon(pslg, rel);

                        outlinePositions = pslg?.Outlines;
                        area = pslg?.Area ?? 0f;
                        centroid = pslg?.Centroid ?? Vector2.zero;
                    }
                    else
                    {
                        var wayPositions = GetWayPositions(way);
                        if (wayPositions.Count < 3)
                        {
                            continue;
                        }

                        wayPositionsArr = wayPositions.ToArray();

                        outlinePositions = new Vector2[][]
                        {
                            wayPositionsArr.Select(v => (Vector2)v).ToArray(),
                        };

                        area = Math.GetAreaOfPolygon(wayPositionsArr);
                        centroid = Math.GetCentroid(wayPositionsArr);
                    }

                    var visualOnly = visualOnlyGeos.Contains(feature.Item1);
                    if (area < inclusionThresholdArea && feature.Item2 != NaturalFeature.Type.Parking)
                    {
                        ++tinyParks;
                        continue;
                    }
                    if (area < interactableThresholdArea && feature.Item2 != NaturalFeature.Type.Parking)
                    {
                        ++smallParks;
                        visualOnly = true;
                    }

                    if (visualOnly)
                    {
                        var layer = NaturalFeature.GetLayer(feature.Item2);
                        var color = NaturalFeature.GetColor(feature.Item2);

                        if (pslg != null)
                        {
                            exporter.RegisterMesh(pslg, layer, color);
                        }
                        else if (wayPositionsArr != null)
                        {
                            exporter.RegisterMesh(wayPositionsArr, layer, color);
                        }

                        continue;
                    }

                    var f = map.CreateFeature(featureName, feature.Item2, outlinePositions, area, centroid);
                    if (outlinePositions != null)
                    {
                        outlinePositions = outlinePositions.Select(
                            arr => MeshBuilder.RemoveDetailByDistance(arr, 1f).ToArray()).ToArray();

                        f.outlinePositions = outlinePositions;
                    }

                    if (pslg != null)
                    {
                        exporter.RegisterMesh(f, pslg, f.GetLayer(),  f.GetColor());
                    }
                    else if (wayPositionsArr != null)
                    {
                        exporter.RegisterMesh(f, wayPositionsArr, f.GetLayer(), f.GetColor());
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

            Debug.Log($"tiny features: {tinyParks}, small features: {smallParks}");
            // yield return LoadFootpaths();
        }

        IEnumerator LoadFootpaths()
        {
            var verts = 0;
            foreach (var street in streets)
            {
                var tags = street.Item1.Geo.Tags;
                var name = tags.GetValue("name");
                Debug.Log("loading path '" + name + "'...");

                var positions = GetWayPositions(street.Item1);
                positions = MeshBuilder.RemoveDetailByAngle(positions.ToArray(), 5f);

                verts += positions.Count;

                var mesh = MeshBuilder.CreateSmoothLine(positions,
                                                        2 * StreetSegment.laneWidth * 0.3f,
                                                        10);

                map.CreateFeature(name, NaturalFeature.Type.Footpath, null, 0f, Vector2.zero);

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
            internal OsmGeo geo;
            internal Tuple<ulong, Building.Type> buildingData;
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
            var originalBuildings = new Dictionary<int, PartialBuilding>();

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
                        originalBuildings.Add(classID, building);

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
            var thresholdDistance = 200f * 200f;

            for (var id = 0; id < nextEquivalenceClass; ++id)
            {
                if (visitedBuildings.Count == partialBuildings.Count)
                    break;

                //Debug.Log("[MergeBuildings] " + (int)((float)visitedBuildings.Count / (float)partialBuildings.Count * 100f) + "%");

                var original = originalBuildings[id];
                var type = original.buildingData.Item2;

                foreach (var building in partialBuildings)
                {
                    if (building.buildingData.Item2 != type || visitedBuildings.Contains(building.centroid))
                    {
                        continue;
                    }

                    if ((building.centroid - original.centroid).sqrMagnitude > thresholdDistance)
                    {
                        continue;
                    }

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
                        geo = original.geo,
                        buildingData = new Tuple<ulong, Building.Type>(0, type),
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
        Dictionary<Street, int> assignedNumbers = new Dictionary<Street, int>();

        // TODO: Generate sensible building names.
        string GetBuildingName(PartialBuilding building, float area)
        {
            var tags = building.geo?.Tags;
            if (tags.ContainsKey("name"))
            {
                return tags.GetValue("name");
            }

            //var capacity = Building.GetDefaultCapacity(building.buildingData.Item2, area);
            var closestStreet = map.GetClosestStreet(building.centroid);
            if (closestStreet == null)
            {
                return "";
            }

            var street = closestStreet.seg.street;
            if (!string.IsNullOrEmpty(street.displayName) && street.displayName != street.name)
            {
                street = map.GetMapObject<Street>(street.displayName) ?? street;
            }

            if (assignedNumbers.TryGetValue(street, out int number))
            {
                assignedNumbers[street]++;
                return street.name + " " + number;
            }

            assignedNumbers.Add(street, 2);
            return street.name + " 1";
        }

        Mesh ModifyBuildingMesh(Mesh mesh, Vector3 centroid, BuildingMesh existingMesh)
        {
            var minAngle = 0f;
            var _ = MeshBuilder.GetSmallestSurroundingRect(mesh, ref minAngle);
            var rot = Quaternion.Euler(0f, 0f, minAngle * Mathf.Rad2Deg);
            var diff = centroid - existingMesh.centroid;

            var vertices = existingMesh.mesh.vertices;
            for (var i = 0; i < vertices.Length; ++i)
            {
                var v = vertices[i];
                vertices[i] = new Vector3(v.x + diff.x, v.y + diff.y, v.z);
            }

            var m = new Mesh
            {
                vertices = vertices,
                triangles = existingMesh.mesh.triangles,
            };

            return MeshBuilder.RotateMesh(m, centroid, rot);
        }

        Mesh GetBuildingMesh(PartialBuilding building, Vector2 centroid)
        {
            Mesh mesh = TriangleAPI.CreateMesh(building.pslg, importType == ImportType.Fast);
            return mesh;
            /*
            var minAngle = 0f;
            var rect = MeshBuilder.GetSmallestSurroundingRect(mesh, ref minAngle);
            var rectArea = (rect[0] - rect[1]).magnitude * (rect[2] - rect[1]).magnitude;
            var rectCentroid = rect[0] + ((rect[2] - rect[0]) * .5f);

            // Don't replace special buildings.
            if (building.geo?.Tags.ContainsKey("name") ?? false)
            {
                return mesh;
            }

            float threshold = 25f;
            if (registeredUniqueBuildings >= maxUniqueBuildings)
            {
                foreach (var b in uniqueBuildings)
                {
                    if (b.area < rectArea && (rectArea - b.area) < threshold)
                    {
                        this.gameObject.transform.position = centroid;
                        this.gameObject.DrawCircle(10f, 1f, Color.red);

                        return ModifyBuildingMesh(mesh, centroid, b);
                    }
                }
            }
            
            // Register a new unique building.
            var rot = Quaternion.Euler(0f, 0f, minAngle * Mathf.Rad2Deg);

            var id = ++registeredUniqueBuildings;
            var rotated = MeshBuilder.RotateMesh(mesh, centroid, rot);

            uniqueBuildings.Add(new BuildingMesh
            {
                id = id,
                area = rectArea,
                mesh = rotated,
                centroid = centroid,
            });

            uniqueBuildings.Sort((BuildingMesh b1, BuildingMesh b2) => b2.area.CompareTo(b1.area));
            return mesh;*/
        }

        IEnumerator LoadBuildings()
        {
            var partialBuildings = new List<PartialBuilding>();
            foreach (var building in buildings)
            {
                var pslg = new PSLG();
                try
                {
                    var way = FindWay(building.Item1);
                    var rel = FindRelation(building.Item1);

                    OsmGeo geo;
                    if (way != null)
                    {
                        geo = way.Geo;
                    }
                    else
                    {
                        geo = rel.Geo;
                    }

                    if (rel != null)
                    {
                        LoadPolygon(pslg, rel);
                    }
                    else
                    {
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
                        geo = geo,
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

            if (importType == ImportType.Complete)
            {
                List<PartialBuilding> mergedBuildings = null;
                this.RunTimer("MergeBuildings", () =>
                {
                    mergedBuildings = MergeBuildings(partialBuildings);
                });

                Debug.Log("reduced from " + partialBuildings.Count + " to " + mergedBuildings.Count + " buildings");
                partialBuildings = mergedBuildings;
            }

            var tinyBuildings = 0;
            var smallBuildings = 0;
            var n = 0;

            foreach (var building in partialBuildings)
            {
                //Debug.Log("[TriangulateBuildings] " + (int)((float)n / (float)partialBuildings.Count * 100f) + "%");

                var type = building.buildingData.Item2;
                var outlinePositions = building.pslg?.Outlines;
                var centroid = building.centroid;
                var area = building.pslg?.Area ?? 0f;

                var visualOnly = visualOnlyGeos.Contains(building.geo.Id);
                if (area < inclusionThresholdArea)
                {
                    ++tinyBuildings;
                    continue;
                }
                if (area < interactableThresholdArea)
                {
                    ++smallBuildings;
                    visualOnly = true;
                }

                if (visualOnly)
                {
                    var layer = Building.GetLayer(building.buildingData.Item2);
                    var color = Building.GetColor(building.buildingData.Item2);

                    if (building.pslg != null)
                    {
                        exporter.RegisterMesh(building.pslg, layer, color);
                    }

                    ++n;
                    continue;
                }

                var b = map.CreateBuilding(type, outlinePositions, GetBuildingName(building, area),
                                         "", area, centroid);

                if (outlinePositions != null)
                {
                    outlinePositions = outlinePositions.Select(
                        arr => MeshBuilder.RemoveDetailByDistance(arr, 1f).ToArray()).ToArray();

                    b.outlinePositions = outlinePositions;
                }

                if (building.pslg != null)
                {
                    exporter.RegisterMesh(b, building.pslg, b.GetLayer(), b.GetColor());
                }

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }

                ++n;
            }

            Debug.Log($"tiny buildings: {tinyBuildings}, small buildings: {smallBuildings}");
            Debug.Log("'nique buildings: " + uniqueBuildings.Count);
        }
    }
}

#endif