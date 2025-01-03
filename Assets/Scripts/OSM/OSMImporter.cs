﻿#if UNITY_EDITOR

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

        public void ImportArea(string areaName, string fileName, bool bg)
        {
            {
                var _ = new OSMImportHelper(this, areaName, fileName);
            }

            if (!bg)
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

            importer.relations.TryGetValue(area.Boundary, out importer.boundary);

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

            return null;
        }

        OsmSharp.Node FindNode(long id)
        {
            if (nodes.ContainsKey(id))
            {
                return nodes[id];
            }

            return null;
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
            Background,
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
        public float mapTileSize = Map.defaultTileSize;
        public int backgroundBlur = 10;
        public bool batchTriangulations;

        public Relation boundary = null;
        public Dictionary<ulong, Node> nodes = new Dictionary<ulong, Node>();
        public Dictionary<ulong, Way> ways = new Dictionary<ulong, Way>();
        public Dictionary<ulong, Relation> relations = new Dictionary<ulong, Relation>();

        public List<Tuple<Way, Street.Type>> streets = new List<Tuple<Way, Street.Type>>();

        public Dictionary<string, TransitLine> lines = new Dictionary<string, TransitLine>();
        public Dictionary<ulong, Node> stops = new Dictionary<ulong, Node>();
        Dictionary<ulong, Stop> stopMap = new Dictionary<ulong, Stop>();

        public Dictionary<Vector2, StreetIntersection> streetIntersectionMap = new Dictionary<Vector2, StreetIntersection>();

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
        public bool done { get; private set; }

        public bool visualizeIntersections = false;

        public static readonly float maxTileSize = 999999f; // 6000f;

        void Start()
        {
            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = Instantiate(mapPrefab);

            map = mapObj.GetComponent<Map>();
            SaveManager.loadedMap = map;
            
            map.Initialize(this.area.ToString(), GameController.instance.input, mapTileSize);

            this.exporter = new MapExporter(map, resolution, batchTriangulations);
            this.ImportArea();

            if (importOnly)
            {
                Debug.Log("Done!");
                return;
            }

            if (importType == ImportType.Background)
            {
                StartCoroutine(LoadBackground());
            }
            else
            {
                StartCoroutine(LoadFeatures());
            }
        }

        void ImportArea()
        {
            var areaName = this.area.ToString();
            if (importType == ImportType.Background)
            {
                areaName = $"Backgrounds/{areaName}";
            }

            string fileName = "Resources/Areas/";
            fileName += areaName;
            fileName += ".bytes";

            var proxy = new OSMImporterProxy();

            Area area;
            if (!System.IO.File.Exists(fileName) || forceReload)
            {
                proxy.ImportArea(this.area.ToString(), areaName, importType == ImportType.Background);
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
                AssetDatabase.SaveAssets();
            }

            exporter.PrintStats();
            Debug.Log("Done!");

            done = true;

            if (loadGame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
            }
        }

        IEnumerator LoadBackground()
        {
            Vector2[] boundaryPositions;
            using (this.CreateTimer("Boundary"))
            {
                boundaryPositions = LoadBoundary();
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

            using (this.CreateTimer("Update Map File"))
            {
                yield return SaveManager.UpdateMapBackground(map);
            }

            using (this.CreateTimer("Screenshots"))
            {
                exporter.ExportMapBackground(map.name, backgroundBlur);
            }

            using (this.CreateTimer("Save Assets"))
            {
                AssetDatabase.SaveAssets();
            }

            using (this.CreateTimer("Update Map Prefab"))
            {
                exporter.UpdatePrefabBackground();
            }

            exporter.PrintStats();
            Debug.Log("Done!");

            done = true;
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

        List<Vector2> GetWayPositions(Way way)
        {
            var positions = new List<Vector2>();
            GetWayPositions(way, positions);

            return positions;
        }

        void GetWayPositions(Way way, List<Vector2> positions, float z = 0)
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

        Vector2[] LoadBoundary()
        {
            map.minX = 0;
            map.minY = 0;

            map.maxX = (maxX - minX);
            map.maxY = (maxY - minY);

            Vector2[] boundaryPositions = null;
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

            if (boundaryPositions == null || importType == ImportType.Background)
            {
                map.UpdateBoundary(new Vector2[]
                {
                    new Vector2(minX, minY), // bottom left
                    new Vector2(minX, maxY), // top left
                    new Vector2(maxX, maxY), // top right
                    new Vector2(maxX, minY), // bottom right
                    new Vector2(minX, minY), // bottom left
                });
            }
            else
            {
                map.UpdateBoundary(boundaryPositions);
            }

            return boundaryPositions;
        }

        private Dictionary<Tuple<Stop.StopType, string>, Stop> _createdStops =
            new Dictionary<Tuple<Stop.StopType, string>, Stop>();

        Stop GetOrCreateStop(TransitType type, Node stopNode, bool projectOntoStreet = false)
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
                var street = map.GetClosestStreet(loc)?.street;
                if (street == null)
                {
                    Debug.LogWarning("'" + stopNode.Geo.Tags.GetValue("name") + "' is not on map");
                    return null;
                }

                var closestPtAndPos = street.GetClosestPointAndPosition(loc);
                var positions = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                    street, (closestPtAndPos.Item2 == Math.PointPosition.Right || street.IsOneWay)
                        ? street.RightmostLane
                        : street.LeftmostLane).Points;

                closestPtAndPos = StreetSegment.GetClosestPointAndPosition(loc, positions);
                loc = closestPtAndPos.Item1;
            }
            else
            {
                loc = map.GetNearestGridPt(loc);
            }

            var stopName = stopNode.Geo.Tags.GetValue("name");
            stopName = stopName.Replace("S+U ", "");
            stopName = stopName.Replace("S ", "");
            stopName = stopName.Replace("U ", "");
            stopName = stopName.Replace("Berlin-", "");
            stopName = stopName.Replace("Berlin ", "");

            var key = Tuple.Create(Stop.GetStopType(type), stopName);
            _createdStops.TryGetValue(key, out Stop s);

            if (s == null)
            {
                s = map.CreateStop(Stop.GetStopType(type), stopName, loc);
                _createdStops.Add(key, s);
            }

            stopMap.Add(stopNode.Geo.Id, s);
            return s;
        }

        IEnumerator LoadTransitLines()
        {
            foreach (var linePair in lines)
            {
                if (linesToLoad.Length > 0
                    && (!linePair.Value.inbound.Geo.Tags.ContainsKey("ref")
                    || !linesToLoad.Contains(linePair.Value.inbound.Geo.Tags.GetValue("ref"))))
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
                
                if (type == TransitType.Bus && l2 != null)
                {
                    members.AddRange(l2.Members);
                }

                List<Vector2> wayPositions = null;

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
                                s = GetOrCreateStop(type, node, true);
                                break;
                            }
                        }
                    }

                    if (s == null)
                    {
                        s = GetOrCreateStop(type, FindNode(member.Id));
                    }

                    if (s != null)
                    {
                        l.AddStop(s, false, wayPositions, true);
                        previousStop = s;
                    }

                    ++n;
                }

                if (type == TransitType.Bus)
                {
                    if (l.line.stops.Count != 0 && previousStop != l.line.stops.First())
                    {
                        l.AddStop(l.line.stops.First(), false, null, true);
                    }
                }
                else
                {
                    l.Loop();
                }

                l.Finish();

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }
        }

        class RawIntersection
        {
            internal Node position;
            internal List<PartialStreet> intersectingStreets;
        }

        bool CheckTwoWayXTwoWayIntersection(
            StreetIntersection intersection,
            HashSet<StreetIntersection> assigned,
            Dictionary<StreetIntersection, List<StreetSegment>> trafficLightInfo)
        {
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

            if (intersection.IntersectingStreets.Count != 4 && intersection.IntersectingStreets.Count != 3)
            {
                return false;
            }

            var R1 = intersection.IntersectingStreets[0];
            var R2 = intersection.IntersectingStreets[1];
            var R3 = intersection.IntersectingStreets[2];
            var R4 = intersection.IntersectingStreets.TryGet(3);

            intersection.Pattern = IntersectionPattern.CreateTwoWayByTwoWay(map, R1, R3, R2, R4);
            
            if (trafficLightInfo.TryGetValue(intersection, out var trafficLights))
            {
                if (!trafficLights.Contains(R1))
                {
                    R1 = null;
                }
                if (!trafficLights.Contains(R2))
                {
                    R2 = null;
                }
                if (!trafficLights.Contains(R3))
                {
                    R3 = null;
                }
                if (!trafficLights.Contains(R4))
                {
                    R4 = null;
                }
            }

            // Find the combination with minimum angle.
            var all = new[] {R1, R2, R3, R4};
            if (all.Count(r => r != null) <= 1)
            {
                return false;
            }

            var minAngle = float.PositiveInfinity;
            var minPair = new Tuple<StreetSegment, StreetSegment>(null, null);

            foreach (var seg in all)
            {
                if (seg == null)
                    continue;
                
                var dir = seg.RelativeDirection(intersection);
                foreach (var other in all)
                {
                    if (seg == other || other == null)
                        continue;

                    var otherDir = other.RelativeDirection(intersection);
                    var angle = Mathf.Abs(Mathf.PI - Math.DirectionalAngleRad(dir, otherDir));

                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        minPair = Tuple.Create(seg, other);
                    }
                }
            }

            R1 = minPair.Item1;
            R3 = minPair.Item2;
            R2 = all.FirstOrDefault(s => s != R1 && s != R3);
            R4 = all.FirstOrDefault(s => s != R1 && s != R2 && s != R3);

            if (R2 == null && R4 == null)
            {
                if (minAngle * Mathf.Rad2Deg >= 40f)
                {
                    R2 = R3;
                    R3 = null;
                }
            }

            var hasX = R1 != null || R3 != null;
            var hasY = R2 != null || R4 != null;

            if (hasX)
            {
                var tl = new TrafficLight(hasY ? 2 : 1, 0);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);
                
                R1?.SetTrafficLight(intersection, tl);
                R3?.SetTrafficLight(intersection, tl);
            }
            
            if (hasY)
            {
                var tl = new TrafficLight(hasX ? 2 : 1, hasX ? 1 : 0);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);

                R2?.SetTrafficLight(intersection, tl);
                R4?.SetTrafficLight(intersection, tl);
            }

            assigned.Add(intersection);

            if (visualizeIntersections)
                Utility.DrawCircle(intersection.Position, 8f, 3f, Color.blue);
            
            return true;
        }

        bool CheckTwoWayXTwoWayIntersectionNoTrafficLights(StreetIntersection intersection,
                                                           HashSet<StreetIntersection> assigned)
        {
            Debug.Assert(intersection.Pattern == null);
            
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

            if (intersection.IntersectingStreets.Count != 4 && intersection.IntersectingStreets.Count != 3)
            {
                return false;
            }
            
            var R1 = intersection.IntersectingStreets[0];
            var R2 = intersection.IntersectingStreets[1];
            var R3 = intersection.IntersectingStreets[2];
            var R4 = intersection.IntersectingStreets.TryGet(3);

            // Find the combination with minimum angle.
            var all = new[] {R1, R2, R3, R4};
            var minAngle = float.PositiveInfinity;
            var minPair = new Tuple<StreetSegment, StreetSegment>(null, null);

            foreach (var seg in all)
            {
                if (seg == null)
                    continue;
                
                var dir = seg.RelativeDirection(intersection);
                foreach (var other in all)
                {
                    if (seg == other || other == null)
                        continue;

                    var otherDir = other.RelativeDirection(intersection);
                    var angle = Mathf.Abs(Mathf.PI - Math.DirectionalAngleRad(dir, otherDir));

                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        minPair = Tuple.Create(seg, other);
                    }
                }
            }

            R1 = minPair.Item1;
            R3 = minPair.Item2;
            R2 = all.FirstOrDefault(s => s != R1 && s != R3);
            R4 = all.FirstOrDefault(s => s != R1 && s != R2 && s != R3);

            if (R2 == null && R4 == null)
            {
                if (minAngle * Mathf.Rad2Deg >= 40f)
                {
                    R2 = R3;
                    R3 = null;
                }
            }
            
            assigned.Add(intersection);
            intersection.Pattern = IntersectionPattern.CreateTwoWayByTwoWay(map, R1, R3, R2, R4);

            if (visualizeIntersections)
                Utility.DrawCircle(intersection.Position, 8f, 3f, Color.blue);
            
            return true;
        }

        bool CheckDoubleOneWayXTwoWayIntersection(
            StreetIntersection intersection,
            HashSet<StreetIntersection> assigned,
            Dictionary<StreetIntersection, List<StreetSegment>> trafficLightInfo)
        {
            Debug.Assert(intersection.Pattern == null);

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

            bool IsValidIntersection(StreetIntersection si)
            {
                if (si.IntersectingStreets.Count != 4)
                {
                    return false;
                }

                if (si.IncomingStreets.Count() < 2 || si.OutgoingStreets.Count() < 2)
                {
                    return false;
                }

                if (!si.IncomingStreets.Any(s => s.OneWay))
                {
                    return false;
                }
                
                if (!si.OutgoingStreets.Any(s => s.OneWay))
                {
                    return false;
                }

                return true;
            }

            if (!IsValidIntersection(intersection))
            {
                return false;
            }

            // Find the other intersection.
            StreetIntersection otherIntersection = null;
            StreetSegment connector = null;

            var minDist = float.PositiveInfinity;
            foreach (var outgoing in intersection.OutgoingStreets)
            {
                var candidate = outgoing.GetOppositeIntersection(intersection);
                if (!IsValidIntersection(candidate))
                {
                    continue;
                }

                var dist = (candidate.Location - intersection.Location).sqrMagnitude;
                if (dist < minDist)
                {
                    connector = outgoing;
                    otherIntersection = candidate;
                    minDist = dist;
                }
            }

            if (otherIntersection == null || connector.OneWay)
            {
                return false;
            }

            var R1A = intersection.IncomingStreets.First(s => s.OneWay && s != connector);
            var R1B = intersection.OutgoingStreets.First(s => s.OneWay && s != connector);
            var R2  = intersection.IntersectingStreets.First(s => s != R1A && s != R1B && s != connector);

            var R3A = otherIntersection.IncomingStreets.First(s => s.OneWay && s != connector);
            var R3B = otherIntersection.OutgoingStreets.First(s => s.OneWay && s != connector);
            var R4  = otherIntersection.IntersectingStreets.First(s => s != R3A && s != R3B && s != connector);

            var pattern = IntersectionPattern.CreateDoubleOneWayByTwoWay(map, R1A, R1B, R2, R4, R3A, R3B);
            intersection.Pattern = pattern;
            otherIntersection.Pattern = pattern;

            if (trafficLightInfo.TryGetValue(intersection, out var trafficLights))
            {
                if (!trafficLights.Contains(R1A))
                {
                    R1A = null;
                }
                if (!trafficLights.Contains(R2))
                {
                    R2 = null;
                }
            }
            
            if (trafficLightInfo.TryGetValue(otherIntersection, out trafficLights))
            {
                if (!trafficLights.Contains(R3A))
                {
                    R3A = null;
                }
                if (!trafficLights.Contains(R4))
                {
                    R4 = null;
                }
            }
            
            var hasX = R1A != null || R3A != null;
            var hasY = R2  != null || R4  != null;

            if (hasX && hasY)
            {
                var tl = new TrafficLight(2, 0);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);
                
                R1A?.SetTrafficLight(intersection, tl);
                R3A?.SetTrafficLight(otherIntersection, tl);
                
                tl = new TrafficLight(2, 1);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);

                R2?.SetTrafficLight(intersection, tl);
                R4?.SetTrafficLight(otherIntersection, tl);
            }

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = 0f, maxY = 0f;

            foreach (var i in new[] { intersection, otherIntersection })
            {
                assigned.Add(i);

                minX = Mathf.Min(minX, i.Position.x);
                minY = Mathf.Min(minY, i.Position.y);
                maxX = Mathf.Max(maxX, i.Position.x);
                maxY = Mathf.Max(maxY, i.Position.y);
            }

            if (visualizeIntersections)
            {
                Utility.DrawRect(new Vector2(minX, minY), new Vector2(minX, maxY),
                    new Vector2(maxX, maxY), new Vector2(maxX, minY), 2f, Color.green);
            }

            return true;
        }

        bool CheckDoubleOneWayXDoubleOneWayIntersection(
            StreetIntersection intersection,
            HashSet<StreetIntersection> assigned,
            Dictionary<StreetIntersection, List<StreetSegment>> trafficLightInfo)
        {
            Debug.Assert(intersection.Pattern == null);

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

            bool IsValidIntersection(StreetIntersection si)
            {
                if (si.IntersectingStreets.Count < 3)
                {
                    return false;
                }

                if (!si.IncomingStreets.Any() || !si.OutgoingStreets.Any())
                {
                    return false;
                }

                if (!si.IntersectingStreets.All(s => s.OneWay))
                {
                    return false;
                }

                return true;
            }

            if (!IsValidIntersection(intersection))
            {
                return false;
            }

            // Find the 'loop' between the four intersections that form the pattern.
            var intersectionStack = new Stack<Tuple<StreetIntersection, int>>();
            intersectionStack.Push(Tuple.Create(intersection, 0));

            var connectors = new StreetSegment[4];
            while (true)
            {
                if (intersectionStack.Count == 5)
                {
                    if (intersectionStack.Peek().Item1 == intersection)
                    {
                        break;
                    }

                    intersectionStack.Pop();
                    continue;
                }

                if (intersectionStack.Count == 0)
                {
                    return false;
                }

                var top = intersectionStack.Peek();
                var candidates = top.Item1.OutgoingStreets.ToArray();
                
                StreetIntersection next;
                if (top.Item2 < candidates.Length)
                {
                    intersectionStack.Pop();
                    intersectionStack.Push(Tuple.Create(top.Item1, top.Item2 + 1));
                    connectors[intersectionStack.Count - 1] = candidates[top.Item2];
                    next = connectors[intersectionStack.Count - 1].endIntersection;
                }
                else
                {
                    intersectionStack.Pop();
                    continue;
                }

                if (!IsValidIntersection(next))
                {
                    continue;
                }

                intersectionStack.Push(Tuple.Create(next, 0));
            }

            var intersections = intersectionStack
                .Skip(1).Take(4)
                .Select(t => t.Item1).Reverse()
                .ToArray();

            var R1A = intersections[0].IncomingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R2A = intersections[1].IncomingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R3A = intersections[2].IncomingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R4A = intersections[3].IncomingStreets.FirstOrDefault(s => !connectors.Contains(s));
            
            var R1B = intersections[0].OutgoingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R2B = intersections[1].OutgoingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R3B = intersections[2].OutgoingStreets.FirstOrDefault(s => !connectors.Contains(s));
            var R4B = intersections[3].OutgoingStreets.FirstOrDefault(s => !connectors.Contains(s));

            var pattern =
                IntersectionPattern.CreateDoubleOneWayByDoubleOneWay(map, R1A, R1B, R2A, R2B, R3A, R3B, R4A, R4B, 
                    connectors[0], connectors[1], connectors[2], connectors[3]);

            if (trafficLightInfo.TryGetValue(intersections[0], out var trafficLights))
            {
                if (!trafficLights.Contains(R1A))
                {
                    R1A = null;
                }
            }
            if (trafficLightInfo.TryGetValue(intersections[1], out trafficLights))
            {
                if (!trafficLights.Contains(R2A))
                {
                    R2A = null;
                }
            }
            if (trafficLightInfo.TryGetValue(intersections[2], out trafficLights))
            {
                if (!trafficLights.Contains(R3A))
                {
                    R3A = null;
                }
            }
            if (trafficLightInfo.TryGetValue(intersections[3], out trafficLights))
            {
                if (!trafficLights.Contains(R4A))
                {
                    R4A = null;
                }
            }

            var hasX = R1A != null || R3A != null;
            var hasY = R2A != null || R4A != null;
            
            if (hasX)
            {
                var tl = new TrafficLight(hasY ? 2 : 1, 0);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);
                
                R1A?.SetTrafficLight(intersection, tl);
                R3A?.SetTrafficLight(intersection, tl);
            }
            
            if (hasY)
            {
                var tl = new TrafficLight(hasX ? 2 : 1, hasX ? 1 : 0);
                GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);

                R2A?.SetTrafficLight(intersection, tl);
                R4A?.SetTrafficLight(intersection, tl);
            }

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = 0f, maxY = 0f;

            foreach (var i in intersections)
            {
                assigned.Add(i);
                i.Pattern = pattern;

                minX = Mathf.Min(minX, i.Position.x);
                minY = Mathf.Min(minY, i.Position.y);
                maxX = Mathf.Max(maxX, i.Position.x);
                maxY = Mathf.Max(maxY, i.Position.y);
            }

            if (visualizeIntersections)
                Utility.DrawRect(new Vector2(minX, minY), new Vector2(minX, maxY),
                    new Vector2(maxX, maxY), new Vector2(maxX, minY), 2f, Color.red);

            return true;
        }

        bool CheckIntersectionPattern(StreetIntersection intersection,
                                     HashSet<StreetIntersection> assigned,
                                     Dictionary<StreetIntersection, List<StreetSegment>> trafficLightInfo)
        {
            if (CheckDoubleOneWayXDoubleOneWayIntersection(intersection, assigned, trafficLightInfo))
            {
                return true;
            }
            
            if (CheckDoubleOneWayXTwoWayIntersection(intersection, assigned, trafficLightInfo))
            {
                return true;
            }

            if (CheckTwoWayXTwoWayIntersection(intersection, assigned, trafficLightInfo))
            {
                return true;
            }

            // Assign a unique traffic light to every incoming street.
            var trafficLights = trafficLightInfo[intersection];
            var segments = intersection.IncomingStreets.Where(s => trafficLights.Contains(s));
            var numTrafficLights = segments.Count();
            if (numTrafficLights > 1)
            {
                var greenPhase = 0;
                foreach (var s in segments)
                {
                    var tl = new TrafficLight(numTrafficLights, greenPhase++);
                    GameController.instance.sim.trafficSim.trafficLights.Add(tl.Id, tl);

                    s.SetTrafficLight(intersection, tl);
                }
            }

            if (visualizeIntersections)
                Utility.DrawCircle(intersection.Position, 8f, 3f, Color.yellow);

            assigned.Add(intersection);
            return false;
        }

        class PartialStreet
        {
            internal Way way;
            internal string name;
            internal Street.Type type;
            internal bool lit;
            internal bool oneway;
            internal int maxspeed;
            internal int lanes;

            internal bool isBridge;
            internal bool isRoundabout;

            internal List<Node> positions;
            internal RawIntersection start;
            internal RawIntersection end;
        }

        StreetSegment AddSegment(Street street, PartialStreet data, ref int removedVerts,
                                 Dictionary<StreetIntersection, List<StreetSegment>> trafficLights,
                                 HashSet<PartialStreet> streetSet)
        {
            var segPositionsStream = data.positions.Select(Project);
            var segPositions = MeshBuilder.RemoveDetailByDistance(
                segPositionsStream.ToArray(), 2.5f);

            // segPositions = MeshBuilder.RemoveDetailByAngle(segPositions, 3f);
            removedVerts += data.positions.Count - segPositions.Count;

            var startIntersection = map.streetIntersectionMap[Project(data.start.position)];
            var endIntersection = map.streetIntersectionMap[Project(data.end.position)];
            var seg = street.AddSegment(segPositions, startIntersection, endIntersection,
                                                     -1, data.oneway);

            seg.IsBridge = data.isBridge;

            foreach (var node in data.positions)
            {
                if (!node.Geo.Tags.Contains("highway", "traffic_signals"))
                    continue;

                var pos = Project(node);
                var startDiff = (pos - (Vector2)seg.positions.First()).sqrMagnitude;
                var endDiff = (pos - (Vector2)seg.positions.Last()).sqrMagnitude;

                if (startDiff <= endDiff)
                {
                    trafficLights.GetOrPutDefault(startIntersection, () => new List<StreetSegment>()).Add(seg);
                }
                else
                {
                    trafficLights.GetOrPutDefault(endIntersection, () => new List<StreetSegment>()).Add(seg);
                }
            }

            if (exportType == MapExportType.Mesh)
            {
                exporter.RegisterMesh(seg, (PSLG)null,
                    (int)Map.Layer(street.type == Street.Type.River ? MapLayer.Rivers : MapLayer.Streets),
                    Color.black);
            }

            streetSet.Remove(data);
            return seg;
        }

        int CombineAdjacentStreets(List<PartialStreet> candidates,
                                   Dictionary<Node, RawIntersection> intersections,
                                   Dictionary<Node, int> nodeCount,
                                   HashSet<PartialStreet> removed)
        {
            var mergedSmallStreets = 0;
            foreach (var street in candidates)
            {
                // If this street has an intersection with only one other street, we may be able to combine the two.
                bool canCombineStart = nodeCount[street.positions.First()] == 2 
                   && intersections.ContainsKey(street.positions.First());
                bool canCombineEnd = nodeCount[street.positions.Last()] == 2 
                   && intersections.ContainsKey(street.positions.Last());

                if (!canCombineStart && !canCombineEnd)
                {
                    continue;
                }

                var intersection = canCombineStart ? street.start : street.end;
                var oppositeIntersection = canCombineStart ? street.end : street.start;

                if (intersection.intersectingStreets.Count != 2)
                {
                    continue;
                }

                // Get the combination candidate.
                PartialStreet otherStreet = intersection.intersectingStreets[0] == street
                    ? intersection.intersectingStreets[1]
                    : intersection.intersectingStreets[0];

                // Don't combine one-way with two-way streets.
                if (street.oneway != otherStreet.oneway)
                {
                    continue;
                }
                
                Debug.Assert(!removed.Contains(street) && !removed.Contains(otherStreet));
                
                bool endsHere = street.end == intersection;
                Debug.Assert(endsHere || street.start == intersection);

                bool otherEndsHere = otherStreet.end == intersection;
                Debug.Assert(otherEndsHere || otherStreet.start == intersection);

                // Utility.DrawLine(otherStreet.positions.Select(v => (Vector3)Project(v)).ToArray(), 2f, Color.blue);

                if (otherEndsHere)
                {
                    RawIntersection newEnd;

                    //   street      otherStreet
                    // -----------   -----------
                    // - - - -  -> I <-  - - - - 
                    // -----------   -----------
                    if (endsHere)
                    {
                        street.positions.Reverse();
                        newEnd = street.start;
                    }
                    //   street      otherStreet
                    // -----------   -----------
                    // - - - -  <- I <-  - - - - 
                    // -----------   -----------
                    else
                    {
                        newEnd = street.end;
                    }

                    otherStreet.positions.AddRange(street.positions);
                    otherStreet.end = newEnd;
                }
                else
                {
                    RawIntersection newStart;
                    
                    //   street      otherStreet
                    // -----------   -----------
                    // - - - -  <- I ->  - - - - 
                    // -----------   -----------
                    if (!endsHere)
                    {
                        street.positions.Reverse();
                        newStart = street.end;
                    }
                    //   street      otherStreet
                    // -----------   -----------
                    // - - - -  -> I ->  - - - - 
                    // -----------   -----------
                    else
                    {
                        newStart = street.start;
                    }
                    
                    otherStreet.positions.InsertRange(0, street.positions);
                    otherStreet.start = newStart;
                }

                // Debug.Log($"Combined {street.way.Geo.Id} with {otherStreet.way.Geo.Id} => {otherStreet.positions.Count}");
                // Utility.DrawLine(otherStreet.positions.Select(v => (Vector3)Project(v)).ToArray(), 2f, Color.red);

                oppositeIntersection.intersectingStreets.Remove(street);
                oppositeIntersection.intersectingStreets.Add(otherStreet);

                removed.Add(street);

                intersections.Remove(intersection.position);
                ++mergedSmallStreets;
            }

            return mergedSmallStreets;
        }

        IEnumerator LoadStreets()
        {
            var namelessStreets = 0;
            var partialStreets = new List<PartialStreet>();
            var nodeCount = new Dictionary<Node, int>();
            var intersections = new Dictionary<Node, RawIntersection>();
            var combinationCandidates = new List<PartialStreet>();
            var startAndEndPositions = new HashSet<Node>();

            foreach (var street in streets)
            {
                var positions = new List<Node>();
                var tags = street.Item1.Geo.Tags;
                var streetName = tags.GetValue("name");

                if (string.IsNullOrEmpty(streetName))
                {
                    streetName = street.Item1.Geo.Id.ToString() ?? (namelessStreets++).ToString();
                }

                foreach (var pos in street.Item1.Nodes)
                {
                    var node = FindNode(pos);
                    if (node != null)
                    {
                        positions.Add(node);
                    }
                }

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

                startAndEndPositions.Add(positions.First());
                startAndEndPositions.Add(positions.Last());

                if (!int.TryParse(tags.GetValue("maxspeed"), out int maxspeed))
                {
                    maxspeed = 0;
                }

                var ps = new PartialStreet
                {
                    way = street.Item1,
                    name = streetName,
                    type = street.Item2,
                    lit = tags.Contains("lit", "yes"),
                    oneway = tags.Contains("oneway", "yes"),
                    maxspeed = maxspeed,
                    lanes = 0,
                    positions = positions,
                    start = null,
                    end = null,
                    isRoundabout = tags.Contains("junction", "roundabout"),
                    isBridge = tags.Contains("bridge", "yes"),
                };

                partialStreets.Add(ps);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Create intersections.
            foreach (var node in nodeCount)
            {
                if (node.Value > 1 || startAndEndPositions.Contains(node.Key))
                {
                    intersections.Add(node.Key, new RawIntersection
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
                    street.positions.First().Equals(street.positions.Last());

                for (int i = 1; i < street.positions.Count; ++i)
                {
                    var endPos = street.positions[i];
                    if (!intersections.TryGetValue(endPos, out RawIntersection endInter))
                    {
                        continue;
                    }

                    PartialStreet ps;
                    if (i < street.positions.Count - 1 || startIdx > 0)
                    {
                        ps = new PartialStreet
                        {
                            way = street.way,
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
    
                    // Check if we may be able to combine this street with an adjacent one.
                    bool canCombineStart = nodeCount[street.positions.First()] == 2;
                    bool canCombineEnd = nodeCount[street.positions.Last()] == 2;
                    
                    if (canCombineStart || canCombineEnd)
                    {
                        combinationCandidates.Add(ps);
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

            // Try to combine adjacent streets.
            var removed = new HashSet<PartialStreet>();
            var combineAdjacentStreets = CombineAdjacentStreets(combinationCandidates, intersections, nodeCount, removed);
            Debug.Log($"Combined {combineAdjacentStreets} adjacent streets");

            var removedVerts = 0;
            var startingStreetCandidates = new HashSet<string>();
            var trafficLights = new Dictionary<StreetIntersection, List<StreetSegment>>();

            foreach (var inter in intersections)
            {
                // Check for streets that start at this intersection.
                foreach (var ps in inter.Value.intersectingStreets)
                {
                    startingStreetCandidates.Add(ps.name);
                }

                foreach (var startSeg in inter.Value.intersectingStreets)
                {
                    Debug.Assert(!removed.Contains(startSeg));
                    
                    // This street was already built.
                    if (!startingStreetCandidates.Contains(startSeg.name) || !streetSet.Contains(startSeg))
                    {
                        continue;
                    }

                    var street = map.CreateStreet(startSeg.name, startSeg.type, startSeg.lit,
                                                  startSeg.maxspeed,
                                                  startSeg.lanes);

                    var seg = AddSegment(street, startSeg, ref removedVerts, trafficLights, streetSet);
                    var done = false;
                    var currSeg = startSeg;
                    var currInter = inter.Value;

                    while (!done)
                    {
                        RawIntersection nextInter;
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
                            Debug.Assert(!removed.Contains(nextSeg));

                            if (nextSeg.name != currSeg.name || !streetSet.Contains(nextSeg))
                            {
                                continue;
                            }

                            seg = AddSegment(street, nextSeg, ref removedVerts, trafficLights, streetSet);
                            done = false;
                            currSeg = nextSeg;
                            currInter = nextInter;

                            break;
                        }
                    }

                    street.CalculateLength();
                }

                startingStreetCandidates.Clear();

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            var assigned = new HashSet<StreetIntersection>();
            foreach (var inter in map.streetIntersections)
            {
                if (!trafficLights.ContainsKey(inter) || assigned.Contains(inter))
                {
                    continue;
                }

                inter.CalculateRelativePositions();
                CheckIntersectionPattern(inter, assigned, trafficLights);
            }

            foreach (var inter in map.streetIntersections)
            {
                if (!assigned.Contains(inter))
                {
                    CheckTwoWayXTwoWayIntersectionNoTrafficLights(inter, assigned);
                }
            }

            if (visualizeIntersections)
            {
                foreach (var inter in assigned)
                {
                    inter.CreateTrafficLightSprites();
                }

                Debug.Break();
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
                    Vector2[] wayPositionsArr = null;

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
                        if (wayPositions.Count < 3 || !wayPositions.First().Equals(wayPositions.Last()))
                        {
                            continue;
                        }

                        wayPositionsArr = wayPositions.ToArray();

                        area = Math.GetAreaOfPolygon(wayPositionsArr);
                        outlinePositions = new [] { wayPositionsArr };
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

                    if (visualOnly || importType == ImportType.Background)
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
                        f.VisualCenter = Polylabel.GetVisualCenter(pslg, 1f);
                    }
                    else if (wayPositionsArr != null)
                    {
                        exporter.RegisterMesh(f, wayPositionsArr, f.GetLayer(), f.GetColor());
                        f.VisualCenter = Polylabel.GetVisualCenter(wayPositionsArr, 1f);
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
                if (building.buildingData.Item2 == Building.Type.Other)
                {
                    continue;
                }

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
                if (building.buildingData.Item2 == Building.Type.Other)
                {
                    mergedBuildings.Add(building);
                    visitedBuildings.Add(building.centroid);
                    continue;
                }

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

            if (building.buildingData.Item2 == Building.Type.Other)
            {
                return $"Unclassified building: {tags.GetValue("building")}";
            }

            //var capacity = Building.GetDefaultCapacity(building.buildingData.Item2, area);
            var closestStreet = map.GetClosestStreet(building.centroid);
            if (closestStreet == null)
            {
                return "";
            }

            var street = closestStreet.street.street;
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

        IEnumerator LoadBuildings()
        {
            var desiredRatios = new Dictionary<Building.Type, float>();
            var thresholds = new[]
            {
                RandomNameGenerator.GetAgePercentage(CitizenBuilder.ElementarySchoolThresholdAge),
                RandomNameGenerator.GetAgePercentage(CitizenBuilder.HighSchoolThresholdAge),
                RandomNameGenerator.GetAgePercentage(CitizenBuilder.UniversityThresholdAge),
                RandomNameGenerator.GetAgePercentage(CitizenBuilder.WorkerThresholdAge),
                RandomNameGenerator.GetAgePercentage(CitizenBuilder.RetirementThresholdAge),
            };

            // Calculate how many spots we'd like to have for every 100 citizens.
            desiredRatios.Add(Building.Type.Kindergarden, thresholds[0]);
            desiredRatios.Add(Building.Type.ElementarySchool, thresholds[1] - thresholds[0]);
            desiredRatios.Add(Building.Type.HighSchool, thresholds[2] - thresholds[1]);
            desiredRatios.Add(Building.Type.University, thresholds[3] - thresholds[2]);

            var totalWorkers = thresholds[4] - thresholds[3];
            foreach (var (percentage, type) in CitizenBuilder.Workplaces)
            {
                if (desiredRatios.TryGetValue(type, out var ratio))
                {
                    desiredRatios[type] = ratio + percentage * totalWorkers;
                }
                else
                {
                    desiredRatios.Add(type, percentage * totalWorkers);
                }
            }

            var actualCapacity = new Dictionary<Building.Type, int>();
            actualCapacity.Add(Building.Type.Residential, 0);

            foreach (var (type, _) in desiredRatios)
            {
                actualCapacity.Add(type, 0);
            }

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
                        var wayPositions = new List<Vector2>();
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
                
                // Calculate the actual capacities if we didn't change anything.
                foreach (var building in partialBuildings)
                {
                    var type = building.buildingData.Item2;
                    var area = building.pslg?.Area ?? 0f;

                    actualCapacity.GetOrPutDefault(type, 0);
                    actualCapacity[type] += Building.GetDefaultCapacity(type, area);
                }

                // Repurpose residential buildings until we're close to the desired capacity.
                var totalResidents = actualCapacity[Building.Type.Residential];
                foreach (var (type, ratio) in desiredRatios)
                {
                    var have = actualCapacity[type];
                    var want = Mathf.RoundToInt(ratio * totalResidents);

                    while (have < want)
                    {
                        Debug.Assert(totalResidents >= (want - have), "not enough residential capacity!");

                        var needed = want - have;
                        var minDiff = int.MaxValue;
                        var minCapacity = 0;
                        PartialBuilding minBuilding = null;

                        foreach (var building in partialBuildings)
                        {
                            if (building.buildingData.Item2 != Building.Type.Residential)
                            {
                                continue;
                            }

                            var area = building.pslg?.Area ?? 0f;
                            var capacity = Building.GetDefaultCapacity(type, area);

                            var diff = System.Math.Abs(capacity - needed);
                            if (diff < minDiff)
                            {
                                minDiff = diff;
                                minBuilding = building;
                                minCapacity = capacity;
                            }
                        }

                        Debug.Assert(minBuilding != null, "no matching building found");

                        minBuilding.buildingData = Tuple.Create(minBuilding.buildingData.Item1, type);
                        have += minCapacity;
                        totalResidents -= minCapacity;
                    }

                    actualCapacity[type] = have;
                }

                actualCapacity[Building.Type.Residential] = totalResidents;
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

                if (visualOnly || importType == ImportType.Background)
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
                    b.VisualCenter = Polylabel.GetVisualCenter(building.pslg, 1f);
                }
                else
                {
                    b.VisualCenter = b.Centroid;
                }

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }

                ++n;
            }

            Debug.Log($"tiny buildings: {tinyBuildings}, small buildings: {smallBuildings}");
            Debug.Log("'nique buildings: " + uniqueBuildings.Count);

            {
                var totalResidents = actualCapacity[Building.Type.Residential];
                Debug.Log($"Max resident capacity: {totalResidents}");
                foreach (var (type, ratio) in desiredRatios)
                {
                    var want = Mathf.RoundToInt(ratio * totalResidents);
                    var have = actualCapacity[type];
                    Debug.Log($"{type}: want {want}, have {have} ({((float) have / (float) want) * 100f:n0} %)");
                }
            }
        }
    }
}

#endif