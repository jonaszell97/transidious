﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transidious
{
    public enum MapLayer : int
    {
        Background = -30,

        NatureBackground = 0,
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

        TransitLines,
        TemporaryLines,
        Cars,
        TransitStops,

        Boundary,
        Foreground,
        Cursor,
    }

    public class Map : MonoBehaviour
    {
        public static readonly Dictionary<TransitType, Color> defaultLineColors = new Dictionary<TransitType, Color>
        {
            { TransitType.Bus, new Color(0.58f, 0.0f, 0.83f)  },
            { TransitType.Tram, new Color(1.0f, 0.0f, 0.0f)  },
            { TransitType.Subway, new Color(0.09f, 0.02f, 0.69f)  },
            { TransitType.LightRail, new Color(37f/255f, 102f/255f, 10f/255f)  },
            { TransitType.IntercityRail, new Color(1.0f, 0.0f, 0.0f)  },
            { TransitType.Ferry, new Color(0.14f, 0.66f, 0.79f)  }
        };

        /// Reference to the input controller.
        public InputController input;

        /// The map tiles.
        public MapTile[][] tiles;
        public int tilesHeight, tilesWidth;

        public bool isLoadedFromSaveFile;

        /// The width of the map (in meters).
        public float width;

        /// The height of the map (in meters).
        public float height;

        /// Starting position of the camera.
        public Vector3 startingCameraPos;
        public float minX, maxX, minY, maxY;

        /// The map's boundary.
        public Vector2[] boundaryPositions;

        /// The object carrying the boundary mesh.
        public GameObject boundaryBackgroundObj;

        /// The object carrying the boundary mesh.
        public GameObject boundaryOutlineObj;

        /// The object carrying the boundary mask.
        public GameObject boundaryMaskObj;

        /// The map background LOD sprite (day).
        public GameObject backgroundSpriteDay;

        /// The map background LOD sprite (night).
        public GameObject backgroundSpriteNight;

        /// Map objects indexed by name.
        Dictionary<string, MapObject> mapObjectMap;

        /// Map objects indexed by ID.
        Dictionary<int, MapObject> mapObjectIDMap;

        int lastAssignedMapObjectID = 1;

        /// List of streets segemnts.
        public List<StreetSegment> streetSegments;

        /// List of all streets.
        public List<Street> streets;

        /// List of all intersections.
        public List<StreetIntersection> streetIntersections;

        /// Map of streets indexed by position.
        public Dictionary<Vector3, StreetIntersection> streetIntersectionMap;

        /// List of all public transit stops.
        public List<Stop> transitStops;

        /// List of all public transit routes.
        public List<Route> transitRoutes;

        /// List of all public transit lines.
        public List<Line> transitLines;

        /// List of natural features.
        public List<NaturalFeature> naturalFeatures;

        /// List of buildings.
        public List<Building> buildings;
        /// Map of buildings by type.
        public Dictionary<Building.Type, List<Building>> buildingsByType;

        /// The canvas covering the entire map.
        public Canvas canvas;

        /// Prefab for creating map tiles.
        public GameObject mapTilePrefab;

        /// Prefab for creating buildings.
        public GameObject buildingPrefab;

        /// Prefab for creating natural features.
        public GameObject naturalFeaturePrefab;

        /// Prefab for creating multimesh objects.
        public GameObject multiMeshPrefab;

        /// Prefab for creating mesh objects.
        public GameObject meshPrefab;

        /// Prefab for creating streets.
        public GameObject streetPrefab;

        /// Prefab for creating street segments.
        public GameObject streetSegmentPrefab;

        /// Prefab for creating street intersections.
        public GameObject streetIntersectionPrefab;

        /// Prefab for creating stops.
        public GameObject stopPrefab;

        /// Prefab for creating lines.
        public GameObject linePrefab;

        /// Prefab for creating routes.
        public GameObject routePrefab;

        /// Prefab for creating a canvas.
        public GameObject canvasPrefab;

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

        public GameController Game
        {
            get
            {
                return input?.controller;
            }
        }

        public void Initialize()
        {
            this.mapObjectMap = new Dictionary<string, MapObject>();
            this.mapObjectIDMap = new Dictionary<int, MapObject>();

            this.streets = new List<Street>();
            this.streetSegments = new List<StreetSegment>();
            this.streetIntersections = new List<StreetIntersection>();
            this.streetIntersectionMap = new Dictionary<Vector3, StreetIntersection>();
            this.transitRoutes = new List<Route>();
            this.transitStops = new List<Stop>();
            this.transitLines = new List<Line>();
            this.naturalFeatures = new List<NaturalFeature>();
            this.buildings = new List<Building>();
            this.buildingsByType = new Dictionary<Building.Type, List<Building>>();

            this.tilesToShow = new HashSet<MapTile>();

            mapObjectIDMap[0] = null;

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

        public void RegisterMapObject(MapObject obj, Vector3 position, int id = -1)
        {
            if (!isLoadedFromSaveFile)
            {
                var tile = GetTile(position);
                if (tile != null)
                {
                    tile.mapObjects.Add(obj);

                    // If the object belongs to a single map tile, parent it.
                    obj.transform.SetParent(tile.transform);
                    obj.uniqueTile = tile;
                }
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(MapObject obj, IEnumerable<Vector3> positions,
                                      int id = -1)
        {
            if (!isLoadedFromSaveFile && positions != null)
            {
                var tiles = new HashSet<MapTile>();
                foreach (var pos in positions)
                {
                    var tile = GetTile(pos);
                    if (tile != null)
                    {
                        tile.mapObjects.Add(obj);
                        tiles.Add(tile);
                    }
                }

                // If the object belongs to a single map tile, parent it.
                if (tiles.Count == 1)
                {
                    // If the object belongs to a single map tile, parent it.
                    obj.transform.SetParent(tiles.First().transform);
                    obj.uniqueTile = tiles.First();
                }
                else
                {
                    foreach (var tile in tiles)
                    {
                        tile.AddOrphanedObject(obj);
                    }
                }
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(MapObject obj, int id = -1)
        {
            if (id == -1)
            {
                id = lastAssignedMapObjectID++;
                obj.id = id;
            }
            else if (id > lastAssignedMapObjectID)
            {
                lastAssignedMapObjectID = id + 1;
            }

            mapObjectIDMap.Add(id, obj);

            if (obj.name.Length == 0 || obj.name.EndsWith("Clone)"))
            {
                return;
            }

#if DEBUG
            if (mapObjectMap.ContainsKey(obj.name))
            {
                Debug.LogWarning("duplicate map object name " + obj.name);
                return;
            }
#endif

            mapObjectMap.Add(obj.name, obj);
        }

        public void DeleteMapObject(MapObject obj)
        {
            mapObjectIDMap.Remove(obj.id);
            mapObjectMap.Remove(obj.name);

            foreach (var tileArr in tiles)
            {
                foreach (var tile in tileArr)
                {
                    tile.mapObjects.Remove(obj);
                }
            }

            Destroy(obj.gameObject);
        }

        public bool HasMapObject(int id)
        {
            return mapObjectIDMap.ContainsKey(id);
        }

        public bool HasMapObject(string name)
        {
            return mapObjectMap.ContainsKey(name);
        }

        public MapObject GetMapObject(int id)
        {
            if (mapObjectIDMap.TryGetValue(id, out MapObject val))
            {
                return val;
            }

            return null;
        }

        public T GetMapObject<T>(int id) where T : MapObject
        {
            if (mapObjectIDMap.TryGetValue(id, out MapObject val))
            {
                return val as T;
            }

            return null;
        }

        public MapObject GetMapObject(string name)
        {
            if (mapObjectMap.TryGetValue(name, out MapObject val))
            {
                return val;
            }

            return null;
        }

        public T GetMapObject<T>(string name) where T : MapObject
        {
            if (mapObjectMap.TryGetValue(name, out MapObject val))
            {
                return val as T;
            }

            return null;
        }

        public TileIterator GetTilesForObject(MapObject obj)
        {
            return new TileIterator(this, obj);
        }

        public TileIterator AllTiles
        {
            get
            {
                return new TileIterator(this);
            }
        }

        public static float Layer(MapLayer l, int orderInLayer = 0)
        {
            Debug.Assert(orderInLayer < 10, "invalid layer order");
            return -((float)((int)l * 10)) - (orderInLayer * 1f);
        }

        public void UpdateBoundary(Vector2[] positions)
        {
            this.boundaryPositions = positions;

            var minX = this.minX - 1000f;
            var maxX = this.maxX + 1000f;
            var minY = this.minY - 1000f;
            var maxY = this.maxY + 1000f;

            if (input != null)
            {
                input.minX = minX;
                input.maxX = maxX;
                input.minY = minY;
                input.maxY = maxY;
            }

            var positions3d = positions.Select(v => (Vector3)v).ToArray();

            var pslg = new PSLG(Map.Layer(MapLayer.Boundary));
            pslg.AddOrderedVertices(positions3d);

            var backgroundMesh = TriangleAPI.CreateMesh(pslg);
            var positionList = positions3d.ToList();

            var boundaryMesh = MeshBuilder.CreateSmoothLine(
                positionList, 10, 10, Map.Layer(MapLayer.Foreground));

            float halfWidth = (maxX - minX) / 2f;
            float halfHeight = (maxY - minY);

            Camera.main.orthographicSize = InputController.maxZoom;

            var halfViewportWidth = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.rect.width, 0, 0)).x;
            var halfViewportHeight = halfViewportWidth;

            pslg = new PSLG(Map.Layer(MapLayer.Boundary));
            pslg.AddOrderedVertices(new Vector3[]
            {
                new Vector3(minX - halfViewportWidth, minY - halfViewportHeight),
                new Vector3(minX - halfViewportWidth, maxY + halfViewportHeight),
                new Vector3(maxX + halfViewportWidth, maxY + halfViewportHeight),
                new Vector3(maxX + halfViewportWidth, minY - halfViewportHeight)
            });

            pslg.AddHole(positionList);

            var maskMesh = TriangleAPI.CreateMesh(pslg);
            UpdateBoundary(backgroundMesh, boundaryMesh, maskMesh, this.minX, this.maxX,
                           this.minY, this.maxY);
        }

        public void UpdateBoundary(Mesh backgroundMesh, Mesh outlineMesh, Mesh maskMesh,
                                   float minX, float maxX, float minY, float maxY)
        {
#if DEBUG
            Debug.Log("background verts: " + backgroundMesh.triangles.Length);
            Debug.Log("outline verts: " + outlineMesh.triangles.Length);
            Debug.Log("mask verts: " + maskMesh.triangles.Length);
#endif

            startingCameraPos = new Vector3(minX + (maxX - minX) / 2f,
                                            minY + (maxY - minY) / 2f,
                                            Camera.main.transform.position.z);

            this.width = this.maxX - this.minX;
            this.height = this.maxY - this.minY;

            Camera.main.transform.position = startingCameraPos;

            if (input != null)
            {
                input.minX = minX - 1000f;
                input.maxX = maxX + 1000f;
                input.minY = minY - 1000f;
                input.maxY = maxY + 1000f;
                input.UpdateZoomLevels();
            }

            var canvasTransform = canvas.GetComponent<RectTransform>();
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxX - minX);
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxY - minY);

            // Create the background mesh.
            {
                var filter = boundaryBackgroundObj.GetComponent<MeshFilter>();
                filter.mesh = backgroundMesh;

                ResetBackgroundColor();

                boundaryBackgroundObj.transform.position = new Vector3(
                    boundaryBackgroundObj.transform.position.x,
                    boundaryBackgroundObj.transform.position.y,
                    Layer(MapLayer.Background));
            }

            // Create the boundary mesh.
            {
                var filter = boundaryOutlineObj.GetComponent<MeshFilter>();
                var meshRenderer = boundaryOutlineObj.GetComponent<MeshRenderer>();

                filter.mesh = outlineMesh;
                meshRenderer.material.color = Color.black;
                boundaryOutlineObj.transform.position = new Vector3(
                    boundaryOutlineObj.transform.position.x,
                    boundaryOutlineObj.transform.position.y,
                    Layer(MapLayer.Foreground, 1));
            }

            // Create the "mask" to make sure only things inside the boundary are visible.
            {
                var filter = boundaryMaskObj.GetComponent<MeshFilter>();
                var meshRenderer = boundaryMaskObj.GetComponent<MeshRenderer>();

                filter.mesh = maskMesh;
                meshRenderer.material.color = Color.white;
                boundaryMaskObj.transform.position = new Vector3(
                    boundaryMaskObj.transform.position.x,
                    boundaryMaskObj.transform.position.y,
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
                    tiles[x][y] = Instantiate(mapTilePrefab).GetComponent<MapTile>();
                    tiles[x][y].Initialize(this, x, y);
                }
            }

            tilesWidth = neededTilesX;
            tilesHeight = neededTilesY;
        }

        public bool IsPointOnMap(Vector2 pt)
        {
            return pt.x >= minX && pt.x <= maxX
                && pt.y >= minY && pt.y <= maxY;

            // var b = new Vector2(pt.x, maxY);
            // var ray = new Ray2D(pt, Vector2.right);
            // var intersections = 0;

            // for (var i = 1; i < boundaryPositions.Length; ++i)
            // {
            //     var p0 = boundaryPositions[i - 1];
            //     var p1 = boundaryPositions[i];

            //     Math.GetIntersectionPoint(pt, b, p0, p1, out bool found);

            //     if (found)
            //     {
            //         ++intersections;
            //     }
            // }

            // return (intersections & 1) == 1;
        }

        public void SetBackgroundColor(Color c)
        {
            var meshRenderer = boundaryBackgroundObj.GetComponent<MeshRenderer>();
            meshRenderer.material = GameController.GetUnlitMaterial(c);
        }

        public void SetBorderColor(Color c)
        {
            var meshRenderer = boundaryOutlineObj.GetComponent<MeshRenderer>();
            meshRenderer.material = GameController.GetUnlitMaterial(c);
        }

        public void SetMaskColor(Color c)
        {
            var meshRenderer = boundaryMaskObj.GetComponent<MeshRenderer>();
            meshRenderer.material = GameController.GetUnlitMaterial(c);
        }

        public void MakeBackgroundTransparent()
        {
            var meshRenderer = boundaryBackgroundObj.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
        }

        public Color GetDefaultBackgroundColor(MapDisplayMode mode)
        {
            switch (mode)
            {
            case MapDisplayMode.Day:
            default:
                return new Color(249f / 255f, 245f / 255f, 237f / 255f, 1);
            case MapDisplayMode.Night:
                return new Color(.167f, .178f, .183f);
                // return new Color(.6f, .6f, .6f, 1f);
            }
        }

        public void ResetBackgroundColor()
        {
            SetBackgroundColor(GetDefaultBackgroundColor(Game.displayMode));
        }

        public Color GetDefaultBorderColor(MapDisplayMode mode)
        {
            switch (mode)
            {
            case MapDisplayMode.Day:
            default:
                return Color.black;
            case MapDisplayMode.Night:
                return new Color(255f / 255f, 59f / 126f, 0f / 255f, 1);
            }
        }

        void ResetBorderColor()
        {
            SetBorderColor(GetDefaultBorderColor(Game.displayMode));
        }

        public Color GetDefaultMaskColor(MapDisplayMode mode)
        {
            switch (mode)
            {
            case MapDisplayMode.Day:
            default:
                return Color.white;
            case MapDisplayMode.Night:
                return new Color(.32f, .37f, .43f, 1);
            }
        }

        void ResetMaskColor()
        {
            SetMaskColor(GetDefaultMaskColor(Game.displayMode));
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

                                for (int i = 1; i < seg.drivablePositions.Count; ++i)
                                {
                                    var p0 = seg.drivablePositions[i - 1];
                                    var p1 = seg.drivablePositions[i];

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

        public PointOnStreet GetClosestStreet(Vector3 position,
                                              bool mustBeOnMap = true,
                                              bool disregardRivers = true)
        {
            var tile = GetTile(position);
            if (tile == null)
            {
                if (mustBeOnMap)
                {
                    return null;
                }

                position = new Vector3(Mathf.Clamp(position.x, minX, maxX),
                                       Mathf.Clamp(position.y, minY, maxY),
                                       0f);

                tile = GetTile(position);
            }

            if (mustBeOnMap && !IsPointOnMap(position))
            {
                return null;
            }

            float minDist = float.PositiveInfinity;
            StreetSegment minSeg = null;
            Vector3 minPnt = Vector3.zero;
            int prevIdx = 0;

            foreach (var seg in tile.streetSegments)
            {
                if (disregardRivers && seg.street.type == Street.Type.River)
                    continue;

                for (int i = 1; i < seg.drivablePositions.Count; ++i)
                {
                    var p0 = seg.drivablePositions[i - 1];
                    var p1 = seg.drivablePositions[i];

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

            Gizmos.color = Color.blue;
            foreach (var pos in boundaryPositions)
            {
                Gizmos.DrawSphere(new Vector3(pos.x, pos.y, -160f), 5f);
            }
        }

        public Line GetLine(string name)
        {
            return GetMapObject<Line>(name);
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
        public LineBuilder CreateLine(TransitType type, string name, Color color, int id = -1)
        {
            string currentName = name;

            var i = 1;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            GameObject lineObject = Instantiate(linePrefab);
            Line line = lineObject.GetComponent<Line>();
            line.transform.SetParent(this.transform);
            line.Initialize(this, currentName, type, color, id);

            RegisterLine(line, id);
            return new LineBuilder(line);
        }

        /// Create a new bus line.
        public LineBuilder CreateBusLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.Bus, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.Bus]);
        }

        /// Create a new tram line.
        public LineBuilder CreateTramLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.Tram, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.Tram]);
        }

        /// Create a new subway line.
        public LineBuilder CreateSubwayLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.Subway, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.Subway]);
        }

        /// Create a new S-Train line.
        public LineBuilder CreateSTrainLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.LightRail, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.LightRail]);
        }

        /// Create a new regional train line.
        public LineBuilder CreateRegionalTrainLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.IntercityRail, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.IntercityRail]);
        }

        /// Create a new ferry line.
        public LineBuilder CreateFerryLine(string name, Color color = new Color())
        {
            return CreateLine(TransitType.Ferry, name,
                              color.a > 0.0f ? color : defaultLineColors[TransitType.Ferry]);
        }

        /// Create a route.
        public Route CreateRoute(int id = -1)
        {
            var obj = Instantiate(routePrefab);
            var route = obj.GetComponent<Route>();
            RegisterRoute(route, id);

            return route;
        }

        /// Register a new public transit line.
        public void RegisterLine(Line line, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;
            line.id = id;

            transitLines.Add(line);
            RegisterMapObject(line, id);
        }

        /// Register a new public transit line.
        public void RegisterStop(Stop stop, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;
            stop.id = id;

            transitStops.Add(stop);
            RegisterMapObject(stop, stop.location, id);
        }

        /// Register a new public transit route.
        public void RegisterRoute(Route route, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;
            route.id = id;

            transitRoutes.Add(route);
            RegisterMapObject(route, route.positions, id);
        }

        public Stop CreateStop(string name, Vector2 location, int id = -1)
        {
            string currentName = name;

            var i = 1;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            return GetOrCreateStop(currentName, location, id);
        }

        /// Register a new public transit stop.
        public Stop GetOrCreateStop(string name, Vector2 location, int id = -1)
        {
            var existingStop = GetMapObject<Stop>(name);
            if (existingStop != null)
            {
                return existingStop;
            }

            GameObject stopObject = Instantiate(stopPrefab);
            var stop = stopObject.GetComponent<Stop>();
            stop.transform.SetParent(this.transform);
            stop.Initialize(this, name, location, id);

            RegisterStop(stop, id);
            return stop;
        }

        /// <summary>
        ///  Create a street.
        /// </summary>
        public Street CreateStreet(string name, Street.Type type, bool lit,
                                   bool oneWay, int maxspeed, int lanes,
                                   int id = -1)
        {
            string currentName = name;

            var i = 1;
            var modifiedName = false;

            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                modifiedName = true;
                ++i;
            }

            id = id == -1 ? lastAssignedMapObjectID++ : id;

            var streetObj = Instantiate(streetPrefab);
            streetObj.transform.SetParent(this.transform);

            var street = streetObj.GetComponent<Street>();
            streets.Add(street);

            street.id = id;
            street.Initialize(this, type, currentName, lit, oneWay, maxspeed, lanes);
            RegisterMapObject(street, id);

            if (modifiedName)
            {
                street.displayName = name;
            }

            return street;
        }

        public StreetIntersection CreateIntersection(Vector3 pos, int id = -1)
        {
            if (streetIntersectionMap.TryGetValue(pos, out StreetIntersection val))
            {
                return val;
            }

            id = id == -1 ? lastAssignedMapObjectID++ : id;

            var obj = Instantiate(streetIntersectionPrefab);
            var inter = obj.GetComponent<StreetIntersection>();
            streetIntersections.Add(inter);
            streetIntersectionMap.Add(pos, inter);

            inter.Initialize(id, pos);
            RegisterMapObject(inter, pos, id);

            return inter;
        }

        public void RegisterSegment(StreetSegment s, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;

            s.id = id;
            streetSegments.Add(s);
            RegisterMapObject(s, s.positions, id);
        }

        public Text CreateText(Vector3 position, string txt = "",
                               Color c = default, float fontSize = 10f)
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

        public NaturalFeature CreateFeature(string name, NaturalFeature.Type type,
                                            Mesh mesh, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;

            var i = 1;
            var currentName = name;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            var obj = Instantiate(naturalFeaturePrefab);
            var nf = obj.GetComponent<NaturalFeature>();
            nf.Initialize(this, currentName, type, mesh, id);
            RegisterMapObject(nf, mesh?.vertices, id);

            naturalFeatures.Add(nf);
            return nf;
        }

        public Building CreateBuilding(Building.Type type, Mesh mesh,
                                       string name, string numberStr, Vector3? position,
                                       int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;

            var i = 1;
            var currentName = name;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            var obj = Instantiate(this.buildingPrefab);
            var building = obj.GetComponent<Building>();

            building.Initialize(this, type, null, numberStr, mesh,
                                currentName, position, id);

            RegisterMapObject(building, mesh?.vertices, id);

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
            var renderingDistance = input?.renderingDistance ?? RenderingDistance.Near;

            foreach (var street in streets)
            {
                foreach (var seg in street.segments)
                {
                    seg.UpdateTextScale(renderingDistance);
                }
            }
        }

        Rect? prevCameraRect;

        void GetTilePosition(Vector2 pos, out int tileX, out int tileY)
        {
            // We only need to update tiles if the camera rect crossed a new tile boundary.
            float x = pos.x - minX;
            float y = pos.y - minY;

            if (x < 0 || y < 0)
            {
                tileX = -1;
                tileY = -1;

                return;
            }

            tileX = (int)Mathf.Floor(x / tileSize);
            tileY = (int)Mathf.Floor(y / tileSize);
        }

        bool ShouldUpdateTiles(Rect prevRect, Rect newRect)
        {
            int prevX, prevY;
            int newX, newY;

            // Check if bottom left corner is in new tile.
            GetTilePosition(new Vector2(prevRect.x, prevRect.y), out prevX, out prevY);
            GetTilePosition(new Vector2(newRect.x, newRect.y), out newX, out newY);

            if (prevX != newX || prevY != newY)
            {
                return true;
            }

            // Check if top left corner is in new tile.
            GetTilePosition(new Vector2(prevRect.x, prevRect.y + prevRect.height),
                out prevX, out prevY);
            GetTilePosition(new Vector2(newRect.x, newRect.y + newRect.height),
                out newX, out newY);

            if (prevX != newX || prevY != newY)
            {
                return true;
            }

            // Check if top right corner is in new tile.
            GetTilePosition(new Vector2(prevRect.x + prevRect.width, prevRect.y + prevRect.height),
                out prevX, out prevY);
            GetTilePosition(new Vector2(newRect.x + newRect.width, newRect.y + newRect.height),
                out newX, out newY);

            if (prevX != newX || prevY != newY)
            {
                return true;
            }

            // Check if bottom right corner is in new tile.
            GetTilePosition(new Vector2(prevRect.x + prevRect.width, prevRect.y),
                out prevX, out prevY);
            GetTilePosition(new Vector2(newRect.x + newRect.width, newRect.y),
                out newX, out newY);

            if (prevX != newX || prevY != newY)
            {
                return true;
            }

            return false;
        }

        HashSet<MapTile> tilesToShow;

        public void UpdateVisibleTiles()
        {
            if (tiles == null)
            {
                return;
            }

            if (input.renderingDistance >= RenderingDistance.Far)
            {
                foreach (var tile in AllTiles)
                {
                    tile.Hide();
                }

                return;
            }

#if DEBUG
            if (Game.ImportingMap)
            {
                foreach (var tile in AllTiles)
                {
                    tile.Show(RenderingDistance.Near);
                }

                return;
            }
#endif

            var bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f));
            var topRight = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f));
            var cameraRect = new Rect(bottomLeft.x, bottomLeft.y,
                                      topRight.x - bottomLeft.x,
                                      topRight.y - bottomLeft.y);

            if (prevCameraRect != null)
            {
                if (!ShouldUpdateTiles(prevCameraRect.Value, cameraRect))
                {
                    return;
                }
            }

            var x = 0;
            var y = 0;
            tilesToShow.Clear();

            var renderingDistance = input?.renderingDistance ?? RenderingDistance.Near;
            foreach (var tileArr in tiles)
            {
                var endX = (x + 1) * tileSize;
                if (cameraRect.x > endX)
                {
                    foreach (var tile in tileArr)
                    {
                        tile.Hide();
                    }

                    ++x;
                    continue;
                }

                foreach (var tile in tileArr)
                {
                    var endY = (y + 1) * tileSize;
                    if (cameraRect.y > endY)
                    {
                        ++y;
                        tile.Hide();
                        continue;
                    }

                    var tileRect = tile.rect;
                    if (cameraRect.Overlaps(tileRect))
                    {
                        tile.Show(renderingDistance);
                        tilesToShow.Add(tile);
                    }
                    else
                    {
                        tile.Hide();
                    }

                    ++y;
                }

                ++x;
            }

            foreach (var tile in tilesToShow)
            {
                if (tile.orphanedObjects == null)
                {
                    continue;
                }

                foreach (var obj in tile.orphanedObjects)
                {
                    obj.Show(renderingDistance);
                }
            }

            prevCameraRect = cameraRect;
        }

        public void HideBackgroundSprite()
        {
            // Do it next frame to guarantee that the tiles are visible again.
            this.RunNextFrame(() =>
            {
                backgroundSpriteDay?.SetActive(false);
                backgroundSpriteNight?.SetActive(false);
            });
        }

        public void ShowBackgroundSprite()
        {
            switch (Game.displayMode)
            {
            case MapDisplayMode.Day:
                backgroundSpriteDay?.SetActive(true);
                break;
            case MapDisplayMode.Night:
                backgroundSpriteNight?.SetActive(true);
                break;
            }

            foreach (var tile in AllTiles)
            {
                tile.Hide();
            }
        }

        public void UpdateScale()
        {
            foreach (var stop in transitStops)
            {
                stop.UpdateScale();
            }

            foreach (var seg in streetSegments)
            {
                seg.UpdateScale(input.renderingDistance);
            }

            var dist = input.renderingDistance;
            switch (dist)
            {
            case RenderingDistance.Near:
                UpdateVisibleTiles();
                HideBackgroundSprite();
                break;
            case RenderingDistance.VeryFar:
            case RenderingDistance.Farthest:
            case RenderingDistance.Far:
                ShowBackgroundSprite();
                prevCameraRect = null;

                break;
            }
        }

        public void Reset()
        {
            foreach (var route in transitRoutes)
            {
                DeleteMapObject(route);
            }
            foreach (var line in transitLines)
            {
                DeleteMapObject(line);
            }
            foreach (var stop in transitStops)
            {
                DeleteMapObject(stop);
            }

            this.transitRoutes.Clear();
            this.transitStops.Clear();
            this.transitLines.Clear();
        }

        // Use this for initialization
        void Awake()
        {
            Initialize();
        }

        // Use this for initialization
        void Start()
        {
            boundaryBackgroundObj = Instantiate(meshPrefab);
            // boundaryBackgroundObj.transform.SetParent(this.transform);
            boundaryBackgroundObj.name = "Boundary Background";

            boundaryOutlineObj = Instantiate(meshPrefab);
            // boundaryOutlineObj.transform.SetParent(this.transform);
            boundaryOutlineObj.name = "Boundary Outline";

            boundaryMaskObj = Instantiate(meshPrefab);
            // boundarymaskObj.transform.SetParent(this.transform);
            boundaryMaskObj.name = "Boundary Mask";

            this.backgroundSpriteDay = Instantiate(GameController.instance.spritePrefab);
            this.backgroundSpriteDay.transform.SetParent(this.transform);
            this.backgroundSpriteNight = Instantiate(GameController.instance.spritePrefab);
            this.backgroundSpriteNight.transform.SetParent(this.transform);

            input.RegisterEventListener(InputEvent.Zoom, _ =>
            {
                this.UpdateVisibleTiles();
            });

            input.RegisterEventListener(InputEvent.Pan, _ =>
            {
                this.UpdateVisibleTiles();
            });

            input.RegisterEventListener(InputEvent.ScaleChange, _ =>
            {
                this.UpdateScale();
            });

            input.RegisterEventListener(InputEvent.DisplayModeChange, _ =>
            {
                var mode = Game.displayMode;
                switch (mode)
                {
                case MapDisplayMode.Day:
                    if (backgroundSpriteNight?.activeSelf ?? false)
                    {
                        backgroundSpriteNight.SetActive(false);
                        backgroundSpriteDay.SetActive(true);
                    }

                    break;
                case MapDisplayMode.Night:
                    if (backgroundSpriteDay?.activeSelf ?? false)
                    {
                        backgroundSpriteDay.SetActive(false);
                        backgroundSpriteNight.SetActive(true);
                    }

                    break;
                }

                ResetBackgroundColor();
                ResetMaskColor();
                ResetBorderColor();

                if (Game.Loading)
                {
                    return;
                }

                foreach (var building in buildings)
                {
                    building.UpdateColor(mode);
                }
                foreach (var seg in streetSegments)
                {
                    seg.UpdateColor(mode);
                }
            });
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

        public IEnumerator DoFinalize(float thresholdTime = 50)
        {
            foreach (var building in buildings)
            {
                building.UpdateMesh(this);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            foreach (var feature in naturalFeatures)
            {
                feature.UpdateMesh(this);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            foreach (var seg in streetSegments)
            {
                seg.UpdateMesh();

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

#if DEBUG
            Debug.Log("street verts: " + StreetSegment.totalVerts);
            Debug.Log("quad verts: " + MeshBuilder.quadVerts);
            Debug.Log("circle verts: " + MeshBuilder.circleVerts);
#endif

            foreach (var tile in AllTiles)
            {
                tile.mesh.CreateMeshes();

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            UpdateScale();
            UpdateTextScale();
            UpdateVisibleTiles();

            if (isLoadedFromSaveFile)
            {
                foreach (var i in streetIntersections)
                {
                    i.GenerateTrafficLights(this);
                }
            }

            if (isLoadedFromSaveFile)
            {
                Game.transitEditor.InitOverlappingRoutes();
            }
        }
    }
}