using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Text;

namespace Transidious
{
    public enum MapLayer : int
    {
        Background = 0,

        NatureBackground,
        Parks,

        RiverOutlines,
        Rivers,

        LakeOutlines,
        Lakes,

        Buildings,

        StreetOutlines,
        Streets,
        StreetMarkings,
        StreetNames,

        Cars,

        TransitLines,
        TransitStops,

        Foreground,
        Cursor,
    }

    public class MapTile
    {
        [System.Serializable]
        public struct SerializableMapTile
        {
            public int[] streetSegmentIDs;
            public int[] stopIDs;
        }

        public readonly int x, y;
        public HashSet<StreetSegment> streetSegments;
        public HashSet<Stop> transitStops;

        public MapTile(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.streetSegments = new HashSet<StreetSegment>();
            this.transitStops = new HashSet<Stop>();
        }

        public SerializableMapTile Serialize()
        {
            return new SerializableMapTile
            {
                streetSegmentIDs = streetSegments.Select(s => s.id).ToArray(),
                stopIDs = transitStops.Select(s => s.id).ToArray(),
            };
        }

        public void Deserialize(Map map, SerializableMapTile tile)
        {
            foreach (var id in tile.streetSegmentIDs)
            {
                this.streetSegments.Add(map.streetSegmentIDMap[id]);
            }

            foreach (var id in tile.stopIDs)
            {
                this.transitStops.Add(map.transitStopIDMap[id]);
            }
        }
    }

    public class Map : MonoBehaviour
    {
        public static readonly Dictionary<Line.TransitType, Color> defaultLineColors = new Dictionary<Line.TransitType, Color>
    {
        { Line.TransitType.Bus, new Color(0.58f, 0.0f, 0.83f)  },
        { Line.TransitType.Tram, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.Subway, new Color(0.09f, 0.02f, 0.69f)  },
        { Line.TransitType.STrain, new Color(37f/255f, 102f/255f, 10f/255f)  },
        { Line.TransitType.RegionalTrain, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.LongDistanceTrain, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.Ferry, new Color(0.14f, 0.66f, 0.79f)  }
    };

        /// Reference to the input controller.
        public InputController input;

        /// The map tiles.
        public MapTile[][] tiles;
        public int tilesHeight, tilesWidth;

        /// The width of the map (in meters).
        public int width;

        /// The height of the map (in meters).
        public int height;

        /// Starting position of the camera.
        public Vector3 startingCameraPos;
        public float minX, maxX, minY, maxY;

        /// The object carrying the boundary mesh.
        public GameObject boundaryBackgroundObj;

        /// The object carrying the boundary mesh.
        public GameObject boundaryOutlineObj;

        /// The object carrying the boundary mask.
        public GameObject boundarymaskObj;

        /// List of all streets.
        public List<Street> streets;

        /// Map of streets indexed by name.
        public Dictionary<string, Street> streetMap;

        /// Map of streets indexed by ID.
        public Dictionary<int, Street> streetIDMap;

        /// List of streets segemnts.
        public List<StreetSegment> streetSegments;

        /// Map of street segments indexed by ID.
        public Dictionary<int, StreetSegment> streetSegmentIDMap;

        /// List of all intersections.
        public List<StreetIntersection> streetIntersections;

        /// Map of streets indexed by position.
        public Dictionary<Vector3, StreetIntersection> streetIntersectionMap;

        /// Map of streets indexed by ID.
        public Dictionary<int, StreetIntersection> streetIntersectionIDMap;

        /// List of all public transit stops.
        public List<Stop> transitStops;

        /// List of all public transit routes.
        public List<Route> transitRoutes;

        /// Map of transit routes by ID.
        public Dictionary<int, Route> transitRouteIDMap;

        /// Map of transit stops by name.
        public Dictionary<string, Stop> transitStopMap;

        /// Map of transit stops by ID.
        public Dictionary<int, Stop> transitStopIDMap;

        /// List of all public transit lines.
        public List<Line> transitLines;

        /// Map of transit lines by name.
        public Dictionary<string, Line> transitLineMap;

        /// Map of transit lines by ID.
        public Dictionary<int, Line> transitLineIDMap;

        /// List of natural features.
        public List<NaturalFeature> naturalFeatures;

        /// List of buildings.
        public List<Building> buildings;
        /// Map of buildings by type.
        public Dictionary<Building.Type, List<Building>> buildingsByType;

        /// The canvas covering the entire map.
        public Canvas canvas;

        /// The 'triangle' API instance.
        public TriangleAPI triangleAPI;

        /// The street mesh builder.
        public MultiMesh streetMesh;

        /// The building mesh.
        public MultiMesh buildingMesh;

        /// The nature mesh.
        public MultiMesh natureMesh;

        /// Prefab for creating multimesh objects.
        public GameObject multiMeshPrefab;

        /// Prefab for creating mesh objects.
        public GameObject meshPrefab;

        /// Prefab for creating streets.
        public GameObject streetPrefab;

        /// Prefab for creating street segments.
        public GameObject streetSegmentPrefab;

        /// Prefab for creating stops.
        public GameObject stopPrefab;

        /// Prefab for creating lines.
        public GameObject linePrefab;

        /// Prefab for creating routes.
        public GameObject routePrefab;

        /// Prefab for creating text.
        public GameObject textPrefab;

        /// Prefab for creating features.
        public GameObject screenshotMakerPrefab;

        /// The scale of one meter.
        public static readonly float Meters = 1f;

        /// Dimensions of a tile on the map.
        public static readonly float tileSize = 2000f * Meters;

#if DEBUG
        /// Whether or not to render traffic lights.
        public bool renderTrafficLights = true;
        public bool renderStreetOrder = true;
#endif

        public void Initialize(int width, int height)
        {
            this.width = width;
            this.height = height;

            this.streets = new List<Street>();
            this.streetMap = new Dictionary<string, Street>();
            this.streetIDMap = new Dictionary<int, Street>();
            this.streetSegments = new List<StreetSegment>();
            this.streetSegmentIDMap = new Dictionary<int, StreetSegment>();
            this.streetIntersections = new List<StreetIntersection>();
            this.streetIntersectionMap = new Dictionary<Vector3, StreetIntersection>();
            this.streetIntersectionIDMap = new Dictionary<int, StreetIntersection>();
            this.transitRoutes = new List<Route>();
            this.transitRouteIDMap = new Dictionary<int, Route>();
            this.transitStops = new List<Stop>();
            this.transitStopMap = new Dictionary<string, Stop>();
            this.transitStopIDMap = new Dictionary<int, Stop>();
            this.transitLines = new List<Line>();
            this.transitLineMap = new Dictionary<string, Line>();
            this.transitLineIDMap = new Dictionary<int, Line>();
            this.naturalFeatures = new List<NaturalFeature>();
            this.buildings = new List<Building>();
            this.buildingsByType = new Dictionary<Building.Type, List<Building>>();

            streetIDMap[0] = null;
            streetSegmentIDMap[0] = null;
            streetIntersectionIDMap[0] = null;
            transitRouteIDMap[0] = null;
            transitStopIDMap[0] = null;
            transitLineIDMap[0] = null;

            this.triangleAPI = new TriangleAPI();

            var sm = Instantiate(multiMeshPrefab);
            sm.transform.SetParent(this.transform);
            sm.name = "Streets";

            this.streetMesh = sm.GetComponent<MultiMesh>();
            this.streetMesh.map = this;

            var bm = Instantiate(multiMeshPrefab);
            bm.transform.SetParent(this.transform);
            bm.name = "Buildings";

            this.buildingMesh = bm.GetComponent<MultiMesh>();
            this.buildingMesh.map = this;

            var nm = Instantiate(multiMeshPrefab);
            nm.transform.SetParent(this.transform);
            nm.name = "Nature";

            this.natureMesh = nm.GetComponent<MultiMesh>();
            this.natureMesh.map = this;

            this.canvas = this.gameObject.AddComponent<Canvas>();
            this.canvas.sortingLayerName = "Foreground";
        }

        public MultiMesh CreateMultiMesh()
        {
            var obj = Instantiate(multiMeshPrefab);
            obj.transform.SetParent(this.transform);

            var mm = obj.GetComponent<MultiMesh>();
            mm.map = this;

            return mm;
        }

        public static float Layer(MapLayer l, int orderInLayer = 0)
        {
            Debug.Assert(orderInLayer < 10, "invalid layer order");
            return -((float)(int)l) - (orderInLayer * .1f);
        }

        public void UpdateBoundary(Vector3[] positions)
        {
            maxX = 0f;
            maxY = 0f;

            minX = float.PositiveInfinity;
            minY = float.PositiveInfinity;

            foreach (var pos in positions)
            {
                maxX = Mathf.Max(maxX, pos.x);
                maxY = Mathf.Max(maxY, pos.y);

                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.y);
            }

            input.minX = minX - 1000f;
            input.maxX = maxX + 1000f;
            input.minY = minY - 1000f;
            input.maxY = maxY + 1000f;

            var pslg = new PSLG();
            pslg.AddOrderedVertices(positions);

            var backgroundMesh = triangleAPI.CreateMesh(pslg);
            var positionList = positions.ToList();

            var boundaryMesh = MeshBuilder.CreateSmoothLine(positionList, input.boundaryWidth, 10);
            float halfWidth = (input.maxX - input.minX) / 2f;
            float halfHeight = (input.maxY - input.minY);

            var halfViewportWidth = Camera.main.rect.width;
            var halfViewportHeight = Camera.main.rect.height;

            pslg = new PSLG();
            pslg.AddOrderedVertices(new Vector3[]
            {
                new Vector3(input.minX - halfViewportWidth, input.minY - halfViewportHeight),
                new Vector3(input.minX - halfViewportWidth, input.maxY + halfViewportHeight),
                new Vector3(input.maxX + halfViewportWidth, input.maxY + halfViewportHeight),
                new Vector3(input.maxX + halfViewportWidth, input.minY - halfViewportHeight)
            });

            pslg.AddHole(positionList);

            var maskMesh = triangleAPI.CreateMesh(pslg);
            UpdateBoundary(backgroundMesh, boundaryMesh, maskMesh, minX, maxX,
                           minY, maxY);
        }

        public void UpdateBoundary(Mesh backgroundMesh, Mesh outlineMesh, Mesh maskMesh,
                                   float minX, float maxX, float minY, float maxY)
        {
            startingCameraPos = new Vector3(minX + (maxX - minX) / 2f,
                                            minY + (maxY - minY) / 2f,
                                            Camera.main.transform.position.z);

            Camera.main.transform.position = startingCameraPos;
            input.minX = minX - 1000f;
            input.maxX = maxX + 1000f;
            input.minY = minY - 1000f;
            input.maxY = maxY + 1000f;

            input.UpdateZoomLevels();

            var canvasTransform = canvas.GetComponent<RectTransform>();
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxX - minX);
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxY - minY);
            canvasTransform.position = startingCameraPos;

            if (boundaryBackgroundObj == null)
            {
                boundaryBackgroundObj = Instantiate(meshPrefab);
                boundaryBackgroundObj.transform.SetParent(this.transform);

                boundaryOutlineObj = Instantiate(meshPrefab);
                boundaryOutlineObj.transform.SetParent(this.transform);

                boundarymaskObj = Instantiate(meshPrefab);
                boundarymaskObj.transform.SetParent(this.transform);
            }

            // Create the background mesh.
            {
                var filter = boundaryBackgroundObj.GetComponent<MeshFilter>();
                filter.mesh = backgroundMesh;

                ResetBackgroundColor();

                boundaryBackgroundObj.transform.position = new Vector3(boundaryBackgroundObj.transform.position.x,
                                                             boundaryBackgroundObj.transform.position.y,
                                                             Layer(MapLayer.Background));
            }

            // Create the boundary mesh.
            {
                var filter = boundaryOutlineObj.GetComponent<MeshFilter>();
                var meshRenderer = boundaryOutlineObj.GetComponent<MeshRenderer>();

                filter.mesh = outlineMesh;
                meshRenderer.material.color = Color.black;
                boundaryOutlineObj.transform.position = new Vector3(boundaryOutlineObj.transform.position.x,
                                                                    boundaryOutlineObj.transform.position.y,
                                                                    Layer(MapLayer.Foreground, 1));
            }

            // Create the "mask" to make sure only things inside the boundary are visible.
            {
                var filter = boundarymaskObj.GetComponent<MeshFilter>();
                var meshRenderer = boundarymaskObj.GetComponent<MeshRenderer>();

                filter.mesh = maskMesh;
                meshRenderer.material.color = Color.white;
                boundarymaskObj.transform.position = new Vector3(boundarymaskObj.transform.position.x,
                                                                 boundarymaskObj.transform.position.y,
                                                                 Layer(MapLayer.Foreground, 0));
            }

            var width = maxX - minX;
            var height = maxY - minY;
            var neededTilesX = (int)Mathf.Ceil(width / tileSize);
            var neededTilesY = (int)Mathf.Ceil(height / tileSize);

            tiles = new MapTile[neededTilesX][];

            for (int x = 0; x < neededTilesX; ++x)
            {
                tiles[x] = new MapTile[neededTilesY];

                for (int y = 0; y < neededTilesY; ++y)
                {
                    tiles[x][y] = new MapTile(x, y);
                }
            }

            tilesWidth = neededTilesX;
            tilesHeight = neededTilesY;
        }

        public void SetBackgroundColor(Color c)
        {
            var meshRenderer = boundaryBackgroundObj.GetComponent<MeshRenderer>();
            meshRenderer.material = input.controller.GetUnlitMaterial(c);
        }

        public void ResetBackgroundColor()
        {
            SetBackgroundColor(new Color(249f / 255f, 245f / 255f, 237f / 255f, 1));
        }

        public MapTile GetTile(Vector3 pos)
        {
            float x = pos.x - minX;
            float y = pos.y - minY;

            if (x < 0 || y < 0)
                return null;

            int tileX = (int)Mathf.Floor(x / tileSize);
            int tileY = (int)Mathf.Floor(y / tileSize);

            if (tileX >= tilesWidth)
                return null;

            if (tileY >= tilesHeight)
                return null;

            return tiles[tileX][tileY];
        }

        public class PointOnStreet
        {
            public StreetSegment seg;
            public Vector3 pos;
            public int prevIdx;
        }

        PointOnStreet GetClosestStreet(Vector3 position, int radius,
                                       int tileX, int tileY,
                                       float minDist, StreetSegment minSeg,
                                       Vector3 minPnt, int prevIdx,
                                       bool disregardRivers)
        {
            bool foundNew = false;

            var x = -radius;
            while (x <= radius)
            {
                var newTileX = tileX + x;
                if (newTileX >= 0 && newTileX < tilesWidth)
                {
                    var y = -radius;
                    while (y <= radius)
                    {
                        var newTileY = tileY + y;
                        if (newTileY >= 0 && newTileY < tilesHeight)
                        {
                            var tile = tiles[newTileX][newTileY];
                            foundNew = true;

                            foreach (var seg in tile.streetSegments)
                            {
                                if (disregardRivers && seg.street.type == Street.Type.River)
                                    continue;

                                for (int i = 1; i < seg.positions.Count; ++i)
                                {
                                    var p0 = seg.positions[i - 1];
                                    var p1 = seg.positions[i];

                                    var closestPt = Math.NearestPointOnLine(p0, p1, position);

                                    var sqrDist = (closestPt - position).sqrMagnitude;
                                    if (sqrDist < minDist)
                                    {
                                        minDist = sqrDist;
                                        minSeg = seg;
                                        minPnt = closestPt;
                                        prevIdx = i - 1;
                                    }
                                }
                            }
                        }

                        if (x == -radius || x == radius)
                        {
                            ++y;
                        }
                        else
                        {
                            y += 2 * radius;
                        }
                    }
                }

                ++x;
            }


            if (foundNew && minSeg == null)
            {
                return GetClosestStreet(position, radius + 1, tileX, tileY,
                                        minDist, minSeg, minPnt, prevIdx,
                                        disregardRivers);
            }

            return new PointOnStreet { seg = minSeg, pos = minPnt, prevIdx = prevIdx };
        }

        public PointOnStreet GetClosestStreet(Vector3 position, bool disregardRivers = true)
        {
            var tile = GetTile(position);
            if (tile == null)
                return null;

            float minDist = float.PositiveInfinity;
            StreetSegment minSeg = null;
            Vector3 minPnt = Vector3.zero;
            int prevIdx = 0;

            foreach (var seg in tile.streetSegments)
            {
                if (disregardRivers && seg.street.type == Street.Type.River)
                    continue;

                for (int i = 1; i < seg.positions.Count; ++i)
                {
                    var p0 = seg.positions[i - 1];
                    var p1 = seg.positions[i];

                    var closestPt = Math.NearestPointOnLine(p0, p1, position);
                    var sqrDist = (closestPt - position).sqrMagnitude;

                    if (sqrDist < minDist)
                    {
                        minDist = sqrDist;
                        minSeg = seg;
                        minPnt = closestPt;
                        prevIdx = i - 1;
                    }
                }
            }

            return GetClosestStreet(position, 1, tile.x, tile.y, minDist, minSeg,
                                    minPnt, prevIdx, disregardRivers);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            for (int x = 0; x < tilesWidth; ++x)
            {
                for (int y = 0; y < tilesWidth; ++y)
                {
                    var baseX = minX + x * tileSize;
                    var baseY = minY + y * tileSize;

                    Gizmos.DrawLine(new Vector3(baseX, baseY, -13f),
                                    new Vector3(baseX + tileSize, baseY, -13f));

                    Gizmos.DrawLine(new Vector3(baseX, baseY, -13f),
                                    new Vector3(baseX, baseY + tileSize, -13f));

                    Gizmos.DrawLine(new Vector3(baseX, baseY + tileSize, -13f),
                                    new Vector3(baseX + tileSize, baseY + tileSize, -13f));

                    Gizmos.DrawLine(new Vector3(baseX + tileSize, baseY, -13f),
                                    new Vector3(baseX + tileSize, baseY + tileSize, -13f));
                }
            }
        }

        /*void OnDrawGizmosSelected()
        {
            if (boundaryPositions == null)
                return;

            Gizmos.color = Color.red;
            Vector3 prev = Vector3.positiveInfinity;

            foreach (var pos in boundaryPositions)
            {
                if (prev != Vector3.positiveInfinity)
                {
                    Gizmos.DrawLine(prev, pos);
                }

                Gizmos.DrawSphere(pos, 0.05f);
                prev = pos;
            }
        }*/

        public Line GetLine(string name)
        {
            if (transitLineMap.TryGetValue(name, out Line l))
            {
                return l;
            }

            return null;

        }

        public class LineBuilder
        {
            Line line;
            Stop lastAddedStop;

            internal LineBuilder(Line line)
            {
                this.line = line;
                this.lastAddedStop = null;
            }

            public LineBuilder AddStop(string name, Vector2 position,
                                       bool oneWay = false, bool isBackRoute = false,
                                       List<Vector3> positions = null)
            {
                return AddStop(line.map.GetOrCreateStop(name, position), oneWay, isBackRoute, positions);
            }

            public LineBuilder AddStop(Stop stop, bool oneWay = false,
                                       bool isBackRoute = false, List<Vector3> positions = null)
            {
                Debug.Assert(stop != null, "stop is null!");
                Debug.Assert(oneWay || !isBackRoute, "can't have a two-way back route!");

                if (lastAddedStop == null)
                {
                    lastAddedStop = stop;
                    line.depot = stop;

                    return this;
                }

                line.AddRoute(lastAddedStop, stop, positions, oneWay, isBackRoute);
                lastAddedStop = stop;

                return this;
            }

            public Line Finish()
            {
                return line;
            }
        }

        /// Create a new public transit line.
        public LineBuilder CreateLine(Line.TransitType type, string name, Color color)
        {
            GameObject lineObject = Instantiate(linePrefab);
            Line line = lineObject.GetComponent<Line>();
            line.transform.SetParent(this.transform);

            line.map = this;
            line.name = name;
            line.type = type;
            line.color = color;

            RegisterLine(line);
            return new LineBuilder(line);
        }

        /// Create a new bus line.
        public LineBuilder CreateBusLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.Bus, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Bus]);
        }

        /// Create a new tram line.
        public LineBuilder CreateTramLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.Tram, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Tram]);
        }

        /// Create a new subway line.
        public LineBuilder CreateSubwayLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.Subway, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Subway]);
        }

        /// Create a new S-Train line.
        public LineBuilder CreateSTrainLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.STrain, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.STrain]);
        }

        /// Create a new regional train line.
        public LineBuilder CreateRegionalTrainLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.RegionalTrain, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.RegionalTrain]);
        }

        /// Create a new long distance train line.
        public LineBuilder CreateLongDistanceTrainLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.LongDistanceTrain, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.LongDistanceTrain]);
        }

        /// Create a new ferry line.
        public LineBuilder CreateFerryLine(string name, Color color = new Color())
        {
            return CreateLine(Line.TransitType.Ferry, name,
                              color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Ferry]);
        }

        /// Create a route.
        public Route CreateRoute()
        {
            var obj = Instantiate(routePrefab);
            var route = obj.GetComponent<Route>();
            RegisterRoute(route);

            return route;
        }

        /// Register a new public transit line.
        public void RegisterLine(Line line)
        {
            line.id = transitLines.Count + 1;

            transitLines.Add(line);
            transitLineMap.Add(line.name, line);
            transitLineIDMap.Add(line.id, line);
        }

        /// Register a new public transit line.
        public void RegisterStop(Stop stop)
        {
            stop.id = transitStops.Count + 1;

            transitStops.Add(stop);
            transitStopMap.Add(stop.name, stop);
            transitStopIDMap.Add(stop.id, stop);
        }

        /// Register a new public transit route.
        public void RegisterRoute(Route route)
        {
            route.id = transitRoutes.Count + 1;

            transitRoutes.Add(route);
            transitRouteIDMap.Add(route.id, route);
        }

        /// Register a new public transit stop.
        public Stop GetOrCreateStop(string name, Vector2 location)
        {
            if (transitStopMap.TryGetValue(name, out Stop stop))
            {
                return stop;
            }

            GameObject stopObject = Instantiate(stopPrefab);
            stop = stopObject.GetComponent<Stop>();
            stop.transform.SetParent(this.transform);

            stop.map = this;
            stop.name = name;
            stop.transform.position = location;

            RegisterStop(stop);
            return stop;
        }

        /// <summary>
        ///  Create a street.
        /// </summary>
        public Street CreateStreet(string name, Street.Type type, bool lit,
                                   bool oneWay, int maxspeed, int lanes)
        {
            var streetObj = Instantiate(streetPrefab);
            streetObj.transform.SetParent(this.transform);

            var street = streetObj.GetComponent<Street>();
            street.id = streets.Count + 1;
            street.Initialize(this, type, name, lit, oneWay, maxspeed, lanes);

            streets.Add(street);
            streetIDMap.Add(street.id, street);

            if (!streetMap.ContainsKey(name))
            {
                streetMap.Add(name, street);
            }

            return street;
        }

        public StreetIntersection CreateIntersection(Vector3 pos)
        {
            var inter = new StreetIntersection
            (
                streetIntersections.Count + 1,
                pos
            );

            streetIntersections.Add(inter);
            streetIntersectionMap.Add(pos, inter);
            streetIntersectionIDMap.Add(inter.id, inter);

            return inter;
        }

        public void RegisterSegment(StreetSegment s)
        {
            s.id = streetSegments.Count + 1;
            streetSegments.Add(s);
            streetSegmentIDMap.Add(s.id, s);

            foreach (var pos in s.positions)
            {
                var tile = GetTile(pos);
                if (tile != null)
                {
                    tile.streetSegments.Add(s);
                }
            }
        }

        public Text CreateText(Vector3 position, string txt = "", Color c = default, float fontSize = .01f)
        {
            var obj = Instantiate(textPrefab);
            obj.transform.position = position;
            obj.transform.SetParent(canvas.transform);

            var t = obj.GetComponent<Text>();
            t.SetText(txt);
            t.SetColor(c);
            t.SetFontSize(fontSize);

            return t;
        }

        public NaturalFeature CreateFeature(string name, NaturalFeature.Type type, Mesh mesh)
        {
            var nf = new NaturalFeature();
            nf.Initialize(this, name, type, mesh);

            naturalFeatures.Add(nf);
            return nf;
        }

        public Building CreateBuilding(Building.Type type, Mesh mesh,
                                       string name, string numberStr, Vector3? position)
        {
            var building = new Building(this, type, null, numberStr, mesh, name, position);
            buildings.Add(building);

            if (!buildingsByType.TryGetValue(type, out List<Building> buildingList))
            {
                buildingList = new List<Building>();
                buildingsByType.Add(type, buildingList);
            }

            buildingList.Add(building);
            return building;
        }

        public void UpdateTextScale()
        {
            if (!input.combineStreetMeshes)
            {
                foreach (var street in streets)
                {
                    foreach (var seg in street.segments)
                    {
                        seg.UpdateTextScale(input.renderingDistance);
                    }
                }
            }
        }

        public void UpdateScale()
        {
            buildingMesh.UpdateScale(input.renderingDistance);
            natureMesh.UpdateScale(input.renderingDistance);

            foreach (var stop in transitStops)
            {
                stop.UpdateScale();
            }

            foreach (var route in transitRoutes)
            {
                route.UpdateScale();
            }

            if (input.combineStreetMeshes)
            {
                streetMesh.UpdateScale(input.renderingDistance);
            }
            else
            {
                foreach (var street in streets)
                {
                    foreach (var seg in street.segments)
                    {
                        seg.UpdateScale(input.renderingDistance);
                    }
                }
            }
        }

        // Use this for initialization
        void Awake()
        {
            Initialize(1000, 1000);
        }

        public bool done = false;

        // Use this for initialization
        void Start()
        {
            /*Stop kaiserdamm = GetOrCreateStop("Kaiserdamm", new Vector2(0, 0));
            Stop zoo = GetOrCreateStop("Zoologischer Garten", new Vector2(5f, -1f));
            Stop uhlandstrasse = GetOrCreateStop("Uhlandstraße", new Vector2(2.5f, -1.5f));
            Stop hbf = GetOrCreateStop("Hauptbahnhof", new Vector2(6.0f, 1.0f));
            Stop friedrichstrasse = GetOrCreateStop("Friedrichstraße", new Vector2(7.0f, 0.8f));
            Stop alexanderplatz = GetOrCreateStop("Alexanderplatz", new Vector2(8f, 0.4f));
            Stop memhardstrasse = GetOrCreateStop("Memhardstraße", new Vector2(8.1f, 0.5f));

            Stop bornholmerStrasse = GetOrCreateStop("Bornholmer Straße", new Vector2(5.75f, 2.5f));
            Stop gesundbrunnen = GetOrCreateStop("Gesundbrunnen", new Vector2(5.0f, 2.0f));
            Stop schoenhauserAllee = GetOrCreateStop("Schönhauser Allee", new Vector2(6.0f, 2.0f));
            Stop prenzlauerAllee = GetOrCreateStop("Prenzlauer Allee", new Vector2(8.0f, 2.0f));
            Stop landsbergerAllee = GetOrCreateStop("Landsberger Allee", new Vector2(10.0f, 2.0f));

            Path erpZooPath = new Path(new List<PathSegment> {
                new PathSegment(new Vector2(4.0f, 0), new Vector2(4.2f, 0)),
                new PathSegment(new Vector2(4.2f, 0), new Vector2(5.0f, -1.0f)),
            });

            CreateSTrainLine("S8", new Color(103f / 255f, 184f / 255f, 93f / 255f))
                .AddStop(bornholmerStrasse)
                .AddStop(schoenhauserAllee)
                .AddStop(prenzlauerAllee)
                .AddStop(landsbergerAllee);

            CreateSTrainLine("S41", new Color(169f / 255f, 73f / 255f, 50f / 255f))
                .AddStop(kaiserdamm, true)
                .AddStop(gesundbrunnen, true)
                .AddStop(schoenhauserAllee, true)
                .AddStop(prenzlauerAllee, true)
                .AddStop(landsbergerAllee, true);

            CreateSTrainLine("S42", new Color(182f / 255f, 111f / 255f, 50f / 255f))
                .AddStop(landsbergerAllee)
                .AddStop(prenzlauerAllee, true)
                .AddStop(schoenhauserAllee, true)
                .AddStop(gesundbrunnen, true)
                .AddStop(kaiserdamm, true);

            CreateSubwayLine("U2", new Color(243f/255f, 87f/255f, 33f/255f))
               .AddStop(kaiserdamm)
               .AddStop("Sophie-Charlotte-Platz", new Vector2(1f, 0))
               .AddStop("Bismarckstraße", new Vector2(2f, 0))
               .AddStop("Deutsche Oper", new Vector2(2.3f, 0))
               .AddStop("Ernst-Reuter-Platz", new Vector2(4f, 0))
               .AddStop(zoo, false, erpZooPath)
               .AddStop(schoenhauserAllee)
               .AddStop("Pankow", new Vector2(6f, 3f));

            CreateSubwayLine("U1", new Color(103f/255f, 184f/255f, 93f/255f))
                .AddStop(uhlandstrasse)
                .AddStop("Kurfürstendamm", new Vector2(3.75f, -1.5f))
                .AddStop(zoo);

            CreateRegionalTrainLine("RE1", new Color(236f / 255f, 124f / 255f, 102f / 255f))
                .AddStop(zoo)
                .AddStop(hbf)
                .AddStop(friedrichstrasse)
                .AddStop(alexanderplatz);

            CreateSTrainLine("S3", new Color(3f / 255f, 110f / 255f, 178f / 255f))
                .AddStop("Spandau", new Vector2(-3f, 1f))
                .AddStop("Charlottenburg", new Vector2(0.3f, -1f))
                .AddStop(zoo);

            CreateTramLine("M2")
                .AddStop(alexanderplatz)
                .AddStop(memhardstrasse)
                .AddStop(prenzlauerAllee);

            var options = new PathPlanningOptions
            {
                start = kaiserdamm,
                goal = schoenhauserAllee,
                time = DateTime.Now
            };

            var planner = new PathPlanner(options);
            //Debug.Log(planner.GetPath());
            */
            //done = true;
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var stop in transitStops)
            {
                if (stop.wasModified)
                {
                    stop.UpdateMesh();
                }
            }

            foreach (var line in transitLines)
            {
                if (line.wasModified)
                {
                    line.UpdateMesh();
                }
            }
        }

        public bool saveOnExit = false;
        public bool saveScene = false;

        public void DoFinalize()
        {
            streetMesh.CreateMeshes();

            buildingMesh.CreateMeshes();
            buildingMesh.CopyData(InputController.RenderingDistance.Near,
                                  InputController.RenderingDistance.Far);

            natureMesh.CreateMeshes();
            natureMesh.CopyData(InputController.RenderingDistance.Near,
                                InputController.RenderingDistance.Far);
            natureMesh.CopyData(InputController.RenderingDistance.Near,
                                InputController.RenderingDistance.VeryFar);
            natureMesh.CopyData(InputController.RenderingDistance.Near,
                                InputController.RenderingDistance.Farthest);

            UpdateScale();
            UpdateTextScale();

            foreach (var i in streetIntersections)
            {
                i.GenerateTrafficLights(this);
            }

#if DEBUG && UNITY_EDITOR
            if (saveOnExit && input.controller.reload)
            {
                SaveToFile(name);
            }
#endif
        }

        [Serializable]
        public struct SerializedMap
        {
            public float minX, maxX, minY, maxY;
            public MapTile.SerializableMapTile[][] tiles;
            public SerializableVector3 cameraStartingPos;

            public SerializableMesh[] boundaryMeshes;

            public Street.SerializedStreet[] streets;
            public StreetIntersection.SerializedStreetIntersection[] streetIntersections;
            public StreetSegment.SerializedStreetSegment[] streetSegments;
            public Route.SerializedRoute[] transitRoutes;
            public Stop.SerializedStop[] transitStops;
            public Line.SerializedLine[] transitLines;
            public NaturalFeature.SerializedFeature[] naturalFeatures;
            public Building.SerializableBuilding[] buildings;
        }

        SerializedMap GetSerializedMap()
        {
            var stiles = new MapTile.SerializableMapTile[tiles.Length][];
            for (int x = 0; x < tiles.Length; ++x)
            {
                stiles[x] = new MapTile.SerializableMapTile[tiles[x].Length];

                for (int y = 0; y < tiles[x].Length; ++y)
                {
                    stiles[x][y] = tiles[x][y].Serialize();
                }
            }

            return new SerializedMap
            {
                minX = minX,
                maxX = maxX,
                minY = minY,
                maxY = maxY,
                tiles = stiles,
                cameraStartingPos = new SerializableVector3(startingCameraPos),
                boundaryMeshes = new SerializableMesh[]
                {
                    new SerializableMesh(boundaryBackgroundObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh(boundaryOutlineObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh(boundarymaskObj.GetComponent<MeshFilter>().mesh),
                },
                streets = streets.Select(s => s.Serialize()).ToArray(),
                streetIntersections = streetIntersections.Select(s => s.Serialize()).ToArray(),
                streetSegments = streetSegments.Select(s => s.Serialize()).ToArray(),
                transitRoutes = transitRoutes.Select(r => r.Serialize()).ToArray(),
                transitStops = transitStops.Select(r => r.Serialize()).ToArray(),
                transitLines = transitLines.Select(r => r.Serialize()).ToArray(),

                naturalFeatures = naturalFeatures.Select(r => r.Serialize()).ToArray(),
                buildings = buildings.Select(r => r.Serialize()).ToArray(),
            };
        }

        public void LoadFromFile(string saveName)
        {
            name = saveName;

            var fileResource = (TextAsset)Resources.Load("Maps/" + saveName);
            using (var stream = new MemoryStream())
            {
                stream.Write(fileResource.bytes, 0, fileResource.bytes.Length);
                stream.Position = 0;

                var formatter = new BinaryFormatter();
                var map = (SerializedMap)formatter.Deserialize(stream);

                minX = map.minX;
                maxX = map.maxX;
                minY = map.minY;
                maxY = map.maxY;

                UpdateBoundary(map.boundaryMeshes[0].GetMesh(),
                               map.boundaryMeshes[1].GetMesh(),
                               map.boundaryMeshes[2].GetMesh(),
                               map.minX, map.maxX, map.minY, map.maxY);

                Camera.main.transform.position = map.cameraStartingPos.ToVector();
                startingCameraPos = Camera.main.transform.position;

                foreach (var inter in map.streetIntersections)
                {
                    CreateIntersection(inter.position.ToVector());
                }
                foreach (var street in map.streets)
                {
                    var s = CreateStreet(street.name, street.type, street.lit,
                                         street.oneway, street.maxspeed,
                                         street.lanes);

                    s.Deserialize(street);
                }

                foreach (var route in map.transitRoutes)
                {
                    CreateRoute();
                }
                foreach (var stop in map.transitStops)
                {
                    GetOrCreateStop(stop.name, stop.position.ToVector());
                }
                foreach (var line in map.transitLines)
                {
                    CreateLine(line.type, line.name, line.color.ToColor()).Finish();
                }

                foreach (var stop in map.transitStops)
                {
                    transitStopIDMap[stop.id].Deserialize(stop, this);
                }
                foreach (var line in map.transitLines)
                {
                    transitLineIDMap[line.id].Deserialize(line, this);
                }
                foreach (var route in map.transitRoutes)
                {
                    transitRouteIDMap[route.id].Deserialize(route, this);
                }

                foreach (var f in map.naturalFeatures)
                {
                    CreateFeature(f.name, f.type, f.mesh.GetMesh());
                }
                foreach (var b in map.buildings)
                {
                    var building = CreateBuilding(b.type, b.mesh.GetMesh(), b.name, b.number,
                                                  b.position.ToVector());
                    building.street = streetSegmentIDMap[b.streetID];
                    building.number = b.number;
                }

                for (int x = 0; x < tiles.Length; ++x)
                {
                    for (int y = 0; y < tiles[x].Length; ++y)
                    {
                        tiles[x][y].Deserialize(this, map.tiles[x][y]);
                    }
                }

                DoFinalize();
            }
        }

        public void SaveToFile(string saveName)
        {
            string fileName = "Assets/Resources/Maps/";
            fileName += saveName;
            fileName += ".txt";

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                formatter.Serialize(stream, GetSerializedMap());
            }
        }

        public void SaveToXMLFile(string saveName)
        {
            string fileName = "Assets/Resources/Maps/";
            fileName += saveName;
            fileName += ".xml";

            XmlSerializer xsSubmit = new XmlSerializer(typeof(SerializedMap));
            using (Stream stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                xsSubmit.Serialize(stream, GetSerializedMap());
            }
        }
    }
}