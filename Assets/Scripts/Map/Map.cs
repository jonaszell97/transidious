﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Transidious.PathPlanning;
using UnityEditor;

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

        Citizens,

        TransitLines,
        TemporaryLines,
        Cars,
        
        Grid,
        TransitStops,

        Boundary,
        Foreground,
        Cursor,
    }

    public class Map : MonoBehaviour
    {
        /// Reference to the input controller.
        public InputController input;

        /// The map tiles.
        public MapTile[] tiles;
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

        /// The map background LOD sprite (day).
        public GameObject backgroundSpriteDay;

        /// The map background LOD sprite (night).
        public GameObject backgroundSpriteNight;

        /// Map objects indexed by name.
        Dictionary<string, IMapObject> mapObjectMap;

        /// Map objects indexed by ID.
        Dictionary<int, IMapObject> mapObjectIDMap;

        int lastAssignedMapObjectID = 1;

        /// List of streets segemnts.
        public List<StreetSegment> streetSegments;

        /// List of all streets.
        public List<Street> streets;

        /// List of all intersections.
        public List<StreetIntersection> streetIntersections;

        /// List of all traffic lights.
        public Dictionary<int, TrafficLight> TrafficLights => Game.sim.trafficSim.trafficLights;
        
        /// Map of streets indexed by position.
        public Dictionary<Vector3, StreetIntersection> streetIntersectionMap;

        /// Map of intersection patterns.
        public Dictionary<int, IntersectionPattern> IntersectionPatterns;

        /// List of all public transit stops.
        public List<Stop> transitStops;

        /// Set of grid points occupied by a stop.
        public HashSet<Vector2> occupiedGridPoints;

        /// List of all public transit routes.
        public List<Route> transitRoutes;

        /// List of all public transit lines.
        public List<Line> transitLines;

        /// List of natural features.
        public List<NaturalFeature> naturalFeatures;

        /// List of buildings.
        public List<Building> buildings;

        /// Total building capacity for every building type.
        public Dictionary<Tuple<Building.Type, OccupancyKind>, int> buildingCapacity;

        /// Total building capacity for every building type.
        public Dictionary<Tuple<Building.Type, OccupancyKind>, int> buildingOccupation;

        /// Map of buildings by type.
        public Dictionary<Building.Type, List<Building>> buildingsByType;

        /// Map from map object IDs to their occupant lists.
        public Dictionary<Tuple<int, OccupancyKind>, SortedSet<Citizen>> occupancyMap;

        /// The canvas covering the entire map.
        public Canvas canvas;

        /// The heatmap covering the entire map.
        public Heatmap heatmap;

        /// The line construction grid.
        public MeshFilter grid;

        /// The grid material.
        public Material gridMaterial;

        /// Prefab for creating map tiles.
        public GameObject mapTilePrefab;

        /// Prefab for creating multimesh objects.
        public GameObject multiMeshPrefab;

        /// Prefab for creating mesh objects.
        public GameObject meshPrefab;

        /// Prefab for creating stops.
        public GameObject stopPrefab;

        /// Prefab for creating routes.
        public GameObject routePrefab;

        /// Prefab for creating a canvas.
        public GameObject canvasPrefab;

        /// Prefab for creating text.
        public GameObject textPrefab;

        /// The default map tile size.
        public static readonly float defaultTileSize = 2000f;

        /// Dimensions of a tile on the map.
        public float tileSize;

        public GameController Game => GameController.instance;

        public void Initialize(string name, InputController input, float tileSize = -1f, bool resetIDs = true)
        {
            if (tileSize < 0f)
            {
                tileSize = defaultTileSize;
            }

            this.name = name;
            this.input = input;

            this.boundaryPositions = null;
            this.mapObjectMap = new Dictionary<string, IMapObject>();
            this.mapObjectIDMap = new Dictionary<int, IMapObject>();

            this.streets = new List<Street>();
            this.streetSegments = new List<StreetSegment>();
            this.streetIntersections = new List<StreetIntersection>();
            this.streetIntersectionMap = new Dictionary<Vector3, StreetIntersection>();
            this.IntersectionPatterns = new Dictionary<int, IntersectionPattern>();
            this.transitRoutes = new List<Route>();
            this.transitStops = new List<Stop>();
            this.occupiedGridPoints = new HashSet<Vector2>();
            this.transitLines = new List<Line>();
            this.naturalFeatures = new List<NaturalFeature>();
            this.buildings = new List<Building>();
            this.buildingsByType = new Dictionary<Building.Type, List<Building>>();
            this.occupancyMap = new Dictionary<Tuple<int, OccupancyKind>, SortedSet<Citizen>>();
            this.buildingCapacity = new Dictionary<Tuple<Building.Type, OccupancyKind>, int>();
            this.buildingOccupation = new Dictionary<Tuple<Building.Type, OccupancyKind>, int>();

            this.tileSize = tileSize;
            this._tilesToShow = new MapTile[4];
            mapObjectIDMap[0] = null;

            if (resetIDs)
            {
                this.lastAssignedMapObjectID = 1;
            }

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

        public void RegisterMapObject(IMapObject obj, Vector3 position, int id = -1)
        {
            var tile = GetTile(position);
            if (tile != null && tile.mapObjects.Add(obj))
            {
                // If the object belongs to a single map tile, parent it.
                obj.transform?.SetParent(tile.transform);
                obj.UniqueTile = tile;
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(IMapObject obj,
                                      IEnumerable<Vector2[]> positions,
                                      int id = -1)
        {
            if (!isLoadedFromSaveFile && positions != null)
            {
                var tiles = new HashSet<MapTile>();
                foreach (var arr in positions)
                {
                    foreach (var pos in arr)
                    {
                        var tile = GetTile(pos);
                        if (tile != null)
                        {
                            tile.mapObjects.Add(obj);
                            tiles.Add(tile);
                        }
                    }
                }

                // If the object belongs to a single map tile, parent it.
                if (tiles.Count == 1)
                {
                    // If the object belongs to a single map tile, parent it.
                    obj.transform?.SetParent(tiles.First().transform);
                    obj.UniqueTile = tiles.First();
                }
                else
                {
                    foreach (var tile in tiles)
                    {
                        tile.mapObjects.Add(obj);
                    }

                    obj.transform?.SetParent(this.transform);
                }
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(IMapObject obj,
                                      IEnumerable<Vector2> positions,
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
                    obj.transform?.SetParent(tiles.First().transform);
                    obj.UniqueTile = tiles.First();
                }
                else
                {
                    foreach (var tile in tiles)
                    {
                        tile.mapObjects.Add(obj);
                    }

                    obj.transform?.SetParent(this.transform);
                }
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(IMapObject obj,
                                      IEnumerable<Vector3> positions,
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
                    obj.transform?.SetParent(tiles.First().transform);
                    obj.UniqueTile = tiles.First();
                }
                else
                {
                    foreach (var tile in tiles)
                    {
                        tile.mapObjects.Add(obj);
                    }

                    obj.transform?.SetParent(this.transform);
                }
            }

            RegisterMapObject(obj, id);
        }

        public void RegisterMapObject(IMapObject obj, int id = -1)
        {
            if (obj.UniqueTile == null)
            {
                obj.transform?.SetParent(this.transform);
            }

            if (id == -1)
            {
                id = lastAssignedMapObjectID++;
                obj.Id = id;
            }
            else if (id >= lastAssignedMapObjectID)
            {
                lastAssignedMapObjectID = id + 1;
            }

            mapObjectIDMap.Add(id, obj);

            var name = obj.Name;
            if (name.Length == 0 || name.EndsWith("Clone)"))
            {
                return;
            }

#if DEBUG
            if (mapObjectMap.ContainsKey(name))
            {
                Debug.LogWarning("duplicate map object name " + name);
                return;
            }
#endif

            mapObjectMap.Add(name, obj);
        }

        public void DeleteMapObject(IMapObject obj)
        {
            if (obj == null)
            {
                return;
            }

            mapObjectIDMap.Remove(obj.Id);
            mapObjectMap.Remove(obj.Name);

            foreach (var tile in tiles)
            {
                tile.mapObjects.Remove(obj);
            }

            obj.Destroy();
            Destroy(obj.transform?.gameObject);
        }

        public bool HasMapObject(int id)
        {
            return mapObjectIDMap.ContainsKey(id);
        }

        public bool HasMapObject(string name)
        {
            return mapObjectMap.ContainsKey(name);
        }

        public IMapObject GetMapObject(int id)
        {
            if (mapObjectIDMap.TryGetValue(id, out IMapObject val))
            {
                return val;
            }

            return null;
        }

        public T GetMapObject<T>(int id) where T : class, IMapObject
        {
            if (mapObjectIDMap.TryGetValue(id, out IMapObject val))
            {
                return val as T;
            }

            return null;
        }

        public IMapObject GetMapObject(string name)
        {
            if (mapObjectMap.TryGetValue(name, out IMapObject val))
            {
                return val;
            }

            return null;
        }

        public T GetMapObject<T>(string name) where T : class, IMapObject
        {
            if (mapObjectMap.TryGetValue(name, out IMapObject val))
            {
                return val as T;
            }

            return null;
        }

        public TrafficLight GetTrafficLight(int id)
        {
            if (TrafficLights.TryGetValue(id, out var tl))
            {
                return tl;
            }

            return null;
        }

        public TileIterator GetTilesForObject(IMapObject obj)
        {
            return new TileIterator(this, obj);
        }

        public TileIterator AllTiles => new TileIterator(this);

        public TileIterator ActiveTiles => new TileIterator(this, null, true);

        public static float Layer(MapLayer l, int orderInLayer = 0)
        {
            Debug.Assert(orderInLayer < 10 || l == MapLayer.Foreground, "invalid layer order");
            return -((float)((int)l * 10)) - (orderInLayer * 1f);
        }

        public void UpdateBoundary(Vector2[] positions)
        {
            this.boundaryPositions = positions;
            
            var pslg = new PSLG(Layer(MapLayer.Boundary));
            pslg.AddOrderedVertices(new []
            {
                new Vector2(minX, minY), 
                new Vector2(minX, maxY),
                new Vector2(maxX, maxY),
                new Vector2(maxX, minY),
                new Vector2(minX, minY),
            });

            var backgroundMesh = TriangleAPI.CreateMesh(pslg);

            var fgLayer = Layer(MapLayer.Foreground);
            var boundaryMesh = MeshBuilder.CreateBakedLineMesh(positions, 10, fgLayer, null, 10, 10);

            UpdateBoundary(backgroundMesh, boundaryMesh, this.minX, this.maxX,
                           this.minY, this.maxY);
        }

        void UpdateBoundary(Mesh backgroundMesh, Mesh outlineMesh,
                            float minX, float maxX, float minY, float maxY)
        {
            var cam = input.camera;

            startingCameraPos = new Vector3(minX + (maxX - minX) / 2f,
                                            minY + (maxY - minY) / 2f,
                                            cam.transform.position.z);

            this.width = this.maxX - this.minX;
            this.height = this.maxY - this.minY;

            cam.transform.position = startingCameraPos;

            if (input != null)
            {
                input.UpdateZoomLevels(this);
            }

            var canvasTransform = canvas.GetComponent<RectTransform>();
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxX - minX);
            canvasTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxY - minY);

            heatmap.transform.localScale = new Vector3(maxX - minX, maxY - minY, 1f);
            heatmap.transform.position = new Vector3(minX + (maxX - minX) * .5f,
                                                     minY + (maxY - minY) * .5f,
                                                     Layer(MapLayer.Foreground));

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

                ResetBorderColor();
                boundaryOutlineObj.transform.position = new Vector3(
                    boundaryOutlineObj.transform.position.x,
                    boundaryOutlineObj.transform.position.y,
                    Layer(MapLayer.Foreground, 1));
            }

            width = maxX - minX;
            height = maxY - minY;

            tilesWidth = (int)Mathf.Ceil(width / tileSize);
            tilesHeight = (int)Mathf.Ceil(height / tileSize);
            tiles = new MapTile[tilesWidth * tilesHeight];

            for (var x = 0; x < tilesWidth; ++x)
            {
                for (var y = 0; y < tilesHeight; ++y)
                {
                    tiles[(x * tilesHeight) + y] = Instantiate(mapTilePrefab).GetComponent<MapTile>();
                    tiles[(x * tilesHeight) + y].transform.SetParent(this.transform, false);
                    tiles[(x * tilesHeight) + y].Initialize(this, x, y);
                }
            }
        }
        
        public bool IsPointOnMap(Vector2 pt)
        {
            return Math.IsPointInPolygon(pt, boundaryPositions);
            // return pt.x >= minX && pt.x <= maxX && pt.y >= minY && pt.y <= maxY;
        }

        public void SetBackgroundColor(Color c)
        {
            var meshRenderer = boundaryBackgroundObj.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GameController.instance.GetUnlitMaterial(c);
        }

        public void SetBorderColor(Color c)
        {
            var meshRenderer = boundaryOutlineObj.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GameController.instance.GetUnlitMaterial(c);
        }

        public Color GetDefaultBackgroundColor(MapDisplayMode mode)
        {
            switch (mode)
            {
            default:
                return Colors.GetColor("map.backgroundDay");
            case MapDisplayMode.Night:
                return Colors.GetColor("map.backgroundNight");
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
            default:
                return Colors.GetColor("map.boundaryDay");
            case MapDisplayMode.Night:
                return Colors.GetColor("map.boundaryNight");
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
            default:
                return Colors.GetColor("map.voidDay");
            case MapDisplayMode.Night:
                return Colors.GetColor("map.voidNight");
            }
        }

        public MapTile GetTile(int x, int y)
        {
            Debug.Assert(x >= 0 && y >= 0 && x < tilesWidth && y < tilesHeight);
            return tiles[x * tilesHeight + y];
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

            return GetTile(tileX, tileY);
        }

        public HashSet<MapTile> GetTiles(IEnumerable<Vector3> positions)
        {
            var tiles = new HashSet<MapTile>();
            foreach (var pos in positions)
            {
                var tile = GetTile(pos);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }
        
        public HashSet<MapTile> GetTiles(IEnumerable<Vector2> positions)
        {
            var tiles = new HashSet<MapTile>();
            foreach (var pos in positions)
            {
                var tile = GetTile(pos);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }
        
        public HashSet<MapTile> GetTiles(IEnumerable<Vector2[]> positions)
        {
            var tiles = new HashSet<MapTile>();
            foreach (var arr in positions)
            {
                foreach (var pos in arr)
                {
                    var tile = GetTile(pos);
                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        PointOnStreet GetClosestStreet(Vector2 position, int radius,
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
                            var tile = GetTile(newTileX, newTileY);
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

            if (minSeg == null)
            {
                return null;
            }

            return new PointOnStreet { street = minSeg, pos = minPnt, prevIdx = prevIdx };
        }

        public PointOnStreet GetClosestStreet(Vector2 position,
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

                position = new Vector2(Mathf.Clamp(position.x, minX, maxX),
                                       Mathf.Clamp(position.y, minY, maxY));

                tile = GetTile(position);
            }

            if (mustBeOnMap && !IsPointOnMap(position))
            {
                return null;
            }

            float minDist = float.PositiveInfinity;
            StreetSegment minSeg = null;
            Vector2 minPnt = Vector2.zero;
            int prevIdx = 0;

            foreach (var seg in tile.streetSegments)
            {
                if (disregardRivers == (seg.street.type == Street.Type.River))
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

        public T[] GetMapObjectsInRadius<T>(Vector2 position, float radius,
                                            bool sortByDistance = true,
                                            Func<T, bool> filter = null) where T: IMapObject
        {
            if (radius < 0f)
            {
                radius = Mathf.Max(width, height) / 2f;
            }

            var minPos = new Vector2(Mathf.Max(position.x - radius, minX), Mathf.Max(position.y - radius, minY));
            var maxPos = new Vector2(Mathf.Min(position.x + radius, maxX - 1f), Mathf.Min(position.y + radius, maxY - 1f));

            var minTile = GetTile(minPos);
            var maxTile = GetTile(maxPos);

            var sqrDist = radius * radius;
            var results = new List<Tuple<float, T>>();

            for (var x = minTile.x; x <= maxTile.x; ++x)
            {
                for (var y = minTile.y; y <= maxTile.y; ++y)
                {
                    var tile = GetTile(x, y);
                    foreach (var obj in tile.mapObjects.OfType<T>())
                    {
                        var dst = (obj.Centroid - position).sqrMagnitude;
                        if (dst <= sqrDist)
                        {
                            if (filter != null)
                            {
                                if (!filter(obj))
                                {
                                    continue;
                                }
                            }
                            
                            results.Add(Tuple.Create(dst, obj));
                        }
                    }
                }
            }

            if (sortByDistance)
            {
                results.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
            }

            return results.Select(t => t.Item2).ToArray();
        }

        public Stop[] GetStopsInRadius(Vector2 position, float radius)
        {
            if (radius < 0f)
            {
                radius = Mathf.Max(width, height) / 2f;
            }

            var minPos = new Vector2(Mathf.Max(position.x - radius, minX), Mathf.Max(position.y - radius, minY));
            var maxPos = new Vector2(Mathf.Min(position.x + radius, maxX - 1f), Mathf.Min(position.y + radius, maxY - 1f));

            var minTile = GetTile(minPos);
            var maxTile = GetTile(maxPos);

            var sqrDist = radius * radius;
            var results = new List<Tuple<float, Stop>>();

            for (var x = minTile.x; x <= maxTile.x; ++x)
            {
                for (var y = minTile.y; y <= maxTile.y; ++y)
                {
                    var tile = GetTile(x, y);
                    foreach (var obj in tile.mapObjects.OfType<Stop>())
                    {
                        var dst = (obj.Centroid - position).sqrMagnitude;
                        if (dst <= sqrDist)
                        {
                            results.Add(Tuple.Create(dst, obj));

                            if (obj.oppositeStop != null)
                            {
                                dst = (obj.oppositeStop.Centroid - position).sqrMagnitude;
                                if (dst > sqrDist)
                                {
                                    results.Add(Tuple.Create(dst, obj.oppositeStop));
                                }
                            }
                        }
                    }
                }
            }

            results.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
            return results.Select(t => t.Item2).ToArray();
        }

        void FindInTile<T>(ref T result, ref bool found, ref float minDistance,
                           MapTile tile, Vector2 position, Func<T, bool> filter) where T : IMapObject
        {
            foreach (var obj in tile.mapObjects.OfType<T>())
            {
                if (filter != null && !filter(obj))
                    continue;

                var dst = (obj.Centroid - position).sqrMagnitude;
                if (dst < minDistance)
                {
                    found = true;
                    minDistance = dst;
                    result = obj;
                }
            }
        }
        
        void FindInSearchRadius<T>(ref T result, ref bool found, ref float minDistance,
                                   MapTile centerTile, int searchRadius,
                                   Vector2 position, Func<T, bool> filter) where T : IMapObject
        {
            var minX = System.Math.Max(centerTile.x - searchRadius, 0);
            var maxX = System.Math.Min(centerTile.x + searchRadius, tilesWidth - 1);
            
            var minY = System.Math.Max(centerTile.y - searchRadius, 0);
            var maxY = System.Math.Min(centerTile.y + searchRadius, tilesHeight - 1);

            for (var x = minX; x <= maxX; ++x)
            {
                for (var y = minY; y <= maxY; ++y)
                {
                    if (searchRadius > 1)
                    {
                        // Only look in the outermost tiles.
                        if ((x > 0 && x < centerTile.x + searchRadius - 1)
                            && (y > 0 && y < centerTile.y + searchRadius - 1))
                        {
                            continue;
                        }
                    }

                    var tile = GetTile(x, y);
                    if (tile == null)
                        continue;

                    FindInTile(ref result, ref found, ref minDistance, tile, position, filter);
                }
            }
        }

        public bool FindClosest<T>(out T result, Vector2 position,
                                   Func<T, bool> filter = null) where T : IMapObject
        {
            result = default(T);

            var centerTile = GetTile(position);
            if (centerTile == null)
            {
                return false;
            }

            var found = false;
            var minDistance = float.PositiveInfinity;

            // We can't just look on the current tile since something on another tile may be closer.
            var searchRadius = 1;
            FindInSearchRadius(ref result, ref found, ref minDistance, centerTile, searchRadius, position, filter);

            if (found)
            {
                return true;
            }

            // Increase the search radius until we find something.
            var maxRadius = System.Math.Max(tilesWidth, tilesHeight);
            while (++searchRadius < maxRadius)
            {
                FindInSearchRadius(ref result, ref found, ref minDistance, centerTile, searchRadius, position, filter);

                if (found)
                {
                    return true;
                }
            }

            return false;
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
            public Line line;
            Stop lastAddedStop;
            private Stop.StopType _type;

            internal LineBuilder(Line line)
            {
                this.line = line;
                this.lastAddedStop = null;
                _type = Stop.GetStopType(line.type);
            }

            public LineBuilder AddStop(string name, Vector2 position,
                                       bool isBackRoute = false,
                                       List<Vector2> positions = null,
                                       bool automaticPath = false)
            {
                return AddStop(line.map.GetOrCreateStop(_type, name, position), isBackRoute, positions, automaticPath);
            }

            public LineBuilder AddStop(Stop stop, bool isBackRoute = false, List<Vector2> positions = null,
                                       bool automaticPath = false)
            {
                Debug.Assert(stop != null, "stop is null!");
                if (lastAddedStop == null)
                {
                    lastAddedStop = stop;
                    line.depot = stop;

                    return this;
                }

                List<TrafficSimulator.PathSegmentInfo> segmentInfo = null;
                if (automaticPath)
                {
                    Debug.Assert(positions == null, "automatic path but positions are given!");

                    var stopType = Stop.GetStopType(line.type);
                    switch (stopType)
                    {
                        case Stop.StopType.StreetBound:
                        {

                            var options = new PathPlanning.PathPlanningOptions { allowWalk = false };
                            var planner = new PathPlanning.PathPlanner(options);
                            var result = planner.FindClosestDrive(line.map, lastAddedStop.transform.position,
                                stop.transform.position);

                            if (result != null)
                            {
                                segmentInfo = new List<TrafficSimulator.PathSegmentInfo>();
                                positions = GameController.instance.sim.trafficSim.GetCompletePath(result, segmentInfo);
                            }
                            else
                            {
                                Debug.LogWarning("no path found from " + lastAddedStop.name + " to " + stop.name);
                            }

                            break;
                        }
                        default:
                            positions = new List<Vector2> { lastAddedStop.location, stop.location };
                            break;
                    }
                }

                var route = line.AddRoute(lastAddedStop, stop, positions, isBackRoute);
                lastAddedStop = stop;

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

                return this;
            }

            public LineBuilder Loop()
            {
                for (var i = line.routes.Count - 1; i >= 0; --i)
                {
                    var route = line.routes[i];
                    AddStop(route.beginStop, true, route.positions.AsEnumerable().Reverse().ToList());
                }

                return this;
            }

            public Line Finish(Schedule sched = null)
            {
                if (sched != null)
                {
                    line.schedule = sched;
                }

                line.FinalizeLine();
                return line;
            }
        }

        /// Create a new public transit line.
        public LineBuilder CreateLine(TransitType type, string name, Color? color = null, int id = -1)
        {
            string currentName = name;

            var i = 1;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            Line line = new Line(this, currentName, type, color ?? Colors.GetColor($"transit.default{type}"), id);
            RegisterLine(line, id);

            return new LineBuilder(line);
        }

        /// Create a new bus line.
        public LineBuilder CreateBusLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.Bus, name, color ?? Colors.GetColor("transit.defaultBus"));
        }

        /// Create a new tram line.
        public LineBuilder CreateTramLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.Tram, name, color ?? Colors.GetColor("transit.defaultTram"));
        }

        /// Create a new subway line.
        public LineBuilder CreateSubwayLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.Subway, name, color ?? Colors.GetColor("transit.defaultSubway"));
        }

        /// Create a new S-Train line.
        public LineBuilder CreateSTrainLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.LightRail, name, color ?? Colors.GetColor("transit.defaultLightRail"));
        }

        /// Create a new regional train line.
        public LineBuilder CreateRegionalTrainLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.IntercityRail, name, color ?? Colors.GetColor("transit.defaultIntercity"));
        }

        /// Create a new ferry line.
        public LineBuilder CreateFerryLine(string name, Color? color = null)
        {
            return CreateLine(TransitType.Ferry, name, color ?? Colors.GetColor("transit.defaultFerry"));
        }

#if DEBUG
        int lastBusLine = 100;
        int lastMetroBusLine = 9;
        int lastSubwayLine = 1;
        int lastTramLine = 10;
        int lastIntercityLine = 1;
        int lastFerryLine = 1;

        public string DefaultLineName(TransitType type)
        {
            switch (type)
            {
                case TransitType.Bus:
                default:
                    if (RNG.value < .5f)
                    {
                        return "M" + (lastMetroBusLine++);
                    }
                    else
                    {
                        return (lastBusLine++).ToString();
                    }
                case TransitType.Tram:
                    return (lastTramLine++).ToString();
                case TransitType.Subway:
                    return "U" + (lastSubwayLine++);
                case TransitType.IntercityRail:
                    return "R" + (lastIntercityLine++);
                case TransitType.Ferry:
                    return "F" + (lastFerryLine++);
            }
        }

        Tuple<Stop, Stop, StreetSegment, StreetIntersection> GetRandomStopPair(
            Tuple<Stop, Stop, StreetSegment, StreetIntersection> previousStop = null)
        {
            // Max 200m distance
            float maxDistance = 200f;

            // Min 50m distance
            float minSqrDistance = 2500f;

            Vector2? pt = null;
            if (previousStop == null)
            {
                pt = new Vector2(RNG.Next(minX, maxX), RNG.Next(minY, maxY));
            }
            else if (previousStop.Item4.IntersectingStreets.Count == 1)
            {
                var attempts = 0;
                var loc = previousStop.Item1.location;

                while (true)
                {
                    pt = new Vector2(
                        RNG.Next(loc.x - maxDistance, loc.x + maxDistance),
                        RNG.Next(loc.y - maxDistance, loc.y + maxDistance));

                    if (IsPointOnMap(pt.Value) && (pt.Value - loc).sqrMagnitude >= minSqrDistance)
                    {
                        break;
                    }

                    if (attempts++ > 1000)
                    {
                        Debug.LogError("can't find a close point on the map!");
                        pt = new Vector2(RNG.Next(minX, maxX), RNG.Next(minY, maxY));

                        break;
                    }
                }
            }

            StreetSegment street;
            StreetIntersection nextIntersection;
            Stop fwd, bwd;

            if (pt.HasValue)
            {
                var closestPt = GetClosestStreet(pt.Value);
                if (closestPt.street.OneWay)
                {
                    return GetRandomStopPair(previousStop);
                }

                street = closestPt.street;
                nextIntersection = closestPt.street.endIntersection;

                {
                    var positionsFwd = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                        street, street.RightmostLane).Points;

                    var newPtPos = StreetSegment.GetClosestPointAndPosition(closestPt.pos, positionsFwd);
                    fwd = GetOrCreateStop(Stop.StopType.StreetBound, closestPt.street.street.name, newPtPos.Item1);
                }
                {
                    var positionsBwd = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                        street, street.LeftmostLane).Points;

                    var newPtPos = StreetSegment.GetClosestPointAndPosition(closestPt.pos, positionsBwd);
                    bwd = GetOrCreateStop(Stop.StopType.StreetBound, closestPt.street.street.name, newPtPos.Item1);
                }
            }
            else {
                var rnd = RNG.Next((float) 0, previousStop.Item4.IntersectingStreets.Count - 1);
                var i = 0;
                street = null;
                
                foreach (var s in previousStop.Item4.IntersectingStreets)
                {
                    if (s == previousStop.Item3)
                    {
                        continue;
                    }

                    if (i++ == rnd)
                    {
                        street = s;
                        break;
                    }
                }

                var backward = false;
                if (street.endIntersection == previousStop.Item4)
                {
                    backward = true;
                    nextIntersection = street.startIntersection;
                }
                else
                {
                    nextIntersection = street.endIntersection;
                }

                Vector2 randomPt = street.RandomPoint;
                var dist = (previousStop.Item1.location - randomPt).sqrMagnitude;

                if (street.OneWay || dist < minSqrDistance)
                {
                    return GetRandomStopPair(
                        Tuple.Create(previousStop.Item1, previousStop.Item2,
                                     street, nextIntersection));
                }

                {
                    var positionsFwd = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                        street, backward ? street.LeftmostLane : street.RightmostLane).Points;

                    var newPtPos = StreetSegment.GetClosestPointAndPosition(randomPt, positionsFwd);
                    fwd = CreateStop(Stop.StopType.StreetBound, street.name, newPtPos.Item1);
                }
                {
                    var positionsBwd = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                        street, backward ? street.RightmostLane : street.LeftmostLane).Points;

                    var newPtPos = StreetSegment.GetClosestPointAndPosition(randomPt, positionsBwd);
                    bwd = CreateStop(Stop.StopType.StreetBound, street.name, newPtPos.Item1);
                }
            }

            return Tuple.Create(fwd, bwd, street, nextIntersection);
        }

        Stop GetRandomStop(HashSet<StreetSegment> usedSegments, Stop previousStop = null)
        {
            // Max 200m distance
            float maxDistance = 200f;

            // Min 50m distance
            float minSqrDistance = 2500f;
            
            Vector2 pt;
            if (previousStop == null)
            {
                pt = new Vector2(RNG.Next(minX, maxX), RNG.Next(minY, maxY));
            }
            else
            {
                var attempts = 0;
                while (true)
                {
                    pt = new Vector2(
                        RNG.Next(previousStop.location.x - maxDistance, previousStop.location.x + maxDistance),
                        RNG.Next(previousStop.location.y - maxDistance, previousStop.location.y + maxDistance));

                    if (IsPointOnMap(pt) && (pt - previousStop.location).sqrMagnitude >= minSqrDistance)
                    {
                        break;
                    }

                    if (attempts++ > 1000)
                    {
                        Debug.LogError("can't find a close point on the map!");
                        pt = new Vector2(RNG.Next(minX, maxX), RNG.Next(minY, maxY));

                        break;
                    }
                }

            }

            var closestPt = GetClosestStreet(pt);
            var street = closestPt.street;

            if (usedSegments.Contains(street))
            {
                return GetRandomStop(usedSegments, previousStop);
            }

            usedSegments.Add(street);

            var closestPtAndPos = street.GetClosestPointAndPosition(closestPt.pos);
            var positions = GameController.instance.sim.trafficSim.StreetPathBuilder.GetPath(
                street, (closestPtAndPos.Item2 == Math.PointPosition.Right || street.IsOneWay)
                    ? street.RightmostLane
                    : street.LeftmostLane).Points;

            var newClosestPtAndPos = StreetSegment.GetClosestPointAndPosition(closestPt.pos, positions);
            closestPt.pos = newClosestPtAndPos.Item1;

            return GetOrCreateStop(Stop.StopType.StreetBound, closestPt.street.street.name, closestPt.pos);
        }

        public Line CreateRandomizedLine(TransitType type, string name = null, int stops = 2)
        {
            var builder = CreateLine(type, name ?? DefaultLineName(type), RNG.RandomColor);
            Stop firstStop = null;
            Tuple<Stop, Stop, StreetSegment, StreetIntersection> previousStop = null;
            
            var bwdStops = new List<Stop>();
            for (var i = 0; i < stops; ++i)
            {
                var nextStops = GetRandomStopPair(previousStop);
                bwdStops.Add(nextStops.Item2);
                previousStop = nextStops;

                builder.AddStop(nextStops.Item1, false, null, type == TransitType.Bus);

                if (firstStop == null)
                {
                    firstStop = nextStops.Item1;
                }
            }

            for (var i = bwdStops.Count - 1; i >= 0; --i)
            {
                builder.AddStop(bwdStops[i], false, null, type == TransitType.Bus);
            }

            if (firstStop != null)
            {
                builder.AddStop(firstStop, false, null, type == TransitType.Bus);
            }

            return builder.Finish();
        }
#endif

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

        public Stop CreateStop(Stop.StopType type, string name, Vector2 location, int id = -1)
        {
            string currentName = name;

            var i = 1;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            return GetOrCreateStop(type, currentName, location, id);
        }

        /// Register a new public transit stop.
        public Stop GetOrCreateStop(Stop.StopType type, string name, Vector2 location, int id = -1)
        {
            var existingStop = GetMapObject<Stop>(name);
            if (existingStop != null)
            {
                return existingStop;
            }

            GameObject stopObject = Instantiate(stopPrefab);
            var stop = stopObject.GetComponent<Stop>();
            stop.transform.SetParent(this.transform, false);
            stop.Initialize(this, type, name, location, id);

            if (GetNearestGridPt(location).Equals(location))
            {
                occupiedGridPoints.Add(location);
            }

            RegisterStop(stop, id);
            return stop;
        }

        ///  Create a street.
        public Street CreateStreet(string name, Street.Type type, bool lit,
                                   int maxspeed, int lanes, int id = -1)
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

            var street = new Street();
            streets.Add(street);

            street.Initialize(this, type, currentName, lit, maxspeed, lanes, id);
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

            var inter = new StreetIntersection();
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

            var tf = obj.transform;
            tf.position = new Vector3(position.x, position.y, Layer(MapLayer.Foreground));
            tf.SetParent(canvas.transform);

            var t = obj.GetComponent<Text>();
            t.SetText(txt);
            t.SetColor(c);
            t.SetFontSize(fontSize);

            return t;
        }

        public NaturalFeature CreateFeature(string name, NaturalFeature.Type type,
                                            IEnumerable<Vector2[]> outlines,
                                            float area,
                                            Vector2 centroid, int id = -1)
        {
            id = id == -1 ? lastAssignedMapObjectID++ : id;

            var i = 1;
            var currentName = name;
            while (HasMapObject(currentName))
            {
                currentName = name + " (" + i.ToString() + ")";
                ++i;
            }

            var nf = new NaturalFeature();
            nf.Initialize(this, currentName, type, null, area, centroid, id);
            RegisterMapObject(nf, outlines, id);

            naturalFeatures.Add(nf);
            return nf;
        }

        public Building CreateBuilding(Building.Type type,
                                       IEnumerable<Vector2[]> outlines,
                                       string name, string numberStr,
                                       float area, Vector2 centroid,
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

            var building = new Building();
            building.Initialize(this, type, null, numberStr, null,
                                area, currentName, centroid, id);

            RegisterMapObject(building, outlines, id);

            buildings.Add(building);
            if (!buildingsByType.TryGetValue(type, out List<Building> buildingList))
            {
                buildingList = new List<Building>();
                buildingsByType.Add(type, buildingList);
            }

            for (var k = 0; k < (int)OccupancyKind._Last; ++k)
            {
                var kind = (OccupancyKind) k;
                var key = Tuple.Create(building.type, kind);

                if (buildingCapacity.TryGetValue(key, out var cap))
                {
                    buildingCapacity[key] = cap + building.GetCapacity(kind);
                }
                else
                {
                    buildingCapacity.Add(key, building.GetCapacity(kind));
                }
            }

            buildingList.Add(building);
            return building;
        }
        
        public int GetOccupancyCount(int objId, OccupancyKind kind)
        {
            if (occupancyMap.TryGetValue(Tuple.Create(objId, kind), out var set))
            {
                return set.Count;
            }

            return 0;
        }

        public SortedSet<Citizen> GetOccupants(int objId, OccupancyKind kind)
        {
            if (occupancyMap.TryGetValue(Tuple.Create(objId, kind), out var set))
            {
                return set;
            }

            return null;
        }

        public void AddOccupant(IMapObject obj, OccupancyKind kind, Citizen c)
        {
            var key = Tuple.Create(obj.Id, kind);
            if (!occupancyMap.TryGetValue(key, out var set))
            {
                set = new SortedSet<Citizen>();
                occupancyMap[key] = set;
            }

            if (obj is Building b)
            {
                var otherKey = Tuple.Create(b.type, kind);
                if (buildingOccupation.TryGetValue(otherKey, out var data))
                {
                    buildingOccupation[otherKey] = data + 1;
                }
                else
                {
                    buildingOccupation.Add(otherKey, 1);
                }
            }

            set.Add(c);
        }

        public void RemoveOccupant(int objId, OccupancyKind kind, Citizen c)
        {
            if (occupancyMap.TryGetValue(Tuple.Create(objId, kind), out var set))
            {
                set.Remove(c);
            }
        }

        public void UpdateTextScale()
        {
            var renderingDistance = input?.renderingDistance ?? RenderingDistance.Near;

            foreach (var street in streets)
            {
                if (!street.Active)
                {
                    continue;
                }

                foreach (var seg in street.segments)
                {
                    if (!seg.Active)
                    {
                        continue;
                    }

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

        private MapTile[] _tilesToShow;

        public void UpdateVisibleTiles()
        {
            if (tiles == null || input.renderingDistance == RenderingDistance.Far)
            {
                return;
            }

            var bottomLeft = input.camera.ViewportToWorldPoint(new Vector3(0f, 0f));
            var topRight = input.camera.ViewportToWorldPoint(new Vector3(1f, 1f));
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

            var topLeft = input.camera.ViewportToWorldPoint(new Vector3(0f, 1f));
            var bottomRight = input.camera.ViewportToWorldPoint(new Vector3(1f, 0f));

            var visibleTile1 = GetTile(bottomLeft);
            var visibleTile2 = GetTile(topRight);
            var visibleTile3 = GetTile(bottomRight);
            var visibleTile4 = GetTile(topLeft);

            for (var i = 0; i < 4; ++i)
            {
                var tile = _tilesToShow[i];
                if (tile == null | tile == visibleTile1 | tile == visibleTile2 | tile == visibleTile3 | tile == visibleTile4)
                {
                    continue;
                }

                tile.Hide();
            }

            var renderingDistance = input?.renderingDistance ?? RenderingDistance.Near;
            visibleTile1?.Show(renderingDistance);
            visibleTile2?.Show(renderingDistance);
            visibleTile3?.Show(renderingDistance);
            visibleTile4?.Show(renderingDistance);

            _tilesToShow[0] = visibleTile1;
            _tilesToShow[1] = visibleTile2;
            _tilesToShow[2] = visibleTile3;
            _tilesToShow[3] = visibleTile4;

            /*var renderingDistance = input?.renderingDistance ?? RenderingDistance.Near;
            for (var x = 0; x < tilesWidth; ++x)
            {
                var endX = (x + 1) * tileSize;
                if (cameraRect.x > endX)
                {
                    for (var y = 0; y < tilesHeight; ++y)
                    {
                        GetTile(x, y).Hide();
                    }

                    ++x;
                    continue;
                }

                for (var y = 0; y < tilesHeight; ++y)
                {
                    var endY = (y + 1) * tileSize;
                    var tile = GetTile(x, y);

                    if (cameraRect.y > endY)
                    {
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
                }
            }*/

            prevCameraRect = cameraRect;
        }

        public void ShowAllTiles()
        {
            foreach (var tile in AllTiles)
            {
                tile.Show(RenderingDistance.Near);
            }
        }

        void UpdateStreetNameScale(RenderingDistance dist)
        {
            if (tiles == null)
            {
                return;
            }

            if (dist >= RenderingDistance.Far)
            {
                return;
            }

            foreach (var tile in ActiveTiles)
            {
                foreach (var seg in tile.streetSegments)
                {
                    seg.UpdateTextScale(dist);
                }
            }
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

        private void UpdateScale()
        {
            if (input.renderingDistance == RenderingDistance.Far)
            {
                foreach (var tile in AllTiles)
                {
                    tile.Show(RenderingDistance.Far);
                }

                prevCameraRect = null;
            }
            else
            {
                foreach (var tile in AllTiles)
                {
                    tile.Hide();
                }

                prevCameraRect = null;
                UpdateVisibleTiles();
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

        public void ResetAll()
        {
            var mapObjects = mapObjectIDMap.Values.ToArray();
            foreach (var obj in mapObjects)
            {
                DeleteMapObject(obj);
            }

            mapObjects = null;

            foreach (var tile in AllTiles)
            {
                tile.Reset();
            }

            Initialize(name, input, tileSize, false);

            // Might be placebo, whatever
            GC.Collect();
        }

        // Use this for initialization
        void Start()
        {
            input.RegisterEventListener(InputEvent.Zoom, _ =>
            {
                this.UpdateVisibleTiles();
                this.UpdateStreetNameScale(input.renderingDistance);

                if (grid.gameObject.activeSelf)
                {
                    UpdateGridScale();
                }
            });

            input.RegisterEventListener(InputEvent.Pan, _ =>
            {
                this.UpdateVisibleTiles();
                
                if (grid.gameObject.activeSelf)
                {
                    UpdateGridPosition();
                }
            });

            input.RegisterEventListener(InputEvent.ScaleChange, _ =>
            {
                this.UpdateScale();
            });
        }

        public IEnumerator FinalizeStreetMeshes(float thresholdTime = 50)
        {
            foreach (var street in streets)
            {
                street.CreateTextMeshes();

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }
        }

        public IEnumerator DoFinalize(float thresholdTime = 50)
        {
            foreach (var building in buildings)
            {
                building.UpdateMesh(this);
            }

            foreach (var feature in naturalFeatures)
            {
                feature.UpdateMesh(this);
            }

            foreach (var seg in streetSegments)
            {
                seg.UpdateMesh();
            }

            foreach (var tile in tiles)
            {
                tile.FinalizeTile();
            }

            UpdateScale();
            UpdateTextScale();
            UpdateVisibleTiles();

#if DEBUG
            if (Game.sim.trafficSim.renderTrafficLights)
            {
                foreach (var inter in streetIntersections)
                {
                    inter.CreateTrafficLightSprites();
                }
            }
#endif

            if (isLoadedFromSaveFile)
            {
                Game.transitEditor.InitOverlappingRoutes();
                Game.TransitMap.Initialize();
            }

            yield break;
        }

        public static readonly float GridCellSize = 7.5f;
        private static readonly int BaseGridScale = 8;

        private Vector2 _baseGridTiling;
        private float _gridScale = 1f;

        public Vector2 GetNearestGridPt(Vector2 worldPos)
        {
            return new Vector2(Mathf.Round(worldPos.x / GridCellSize) * GridCellSize,
                               Mathf.Round(worldPos.y / GridCellSize) * GridCellSize);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void CreateGridTexture()
        {
            var gridTex = new Texture2D(256, 256);
            Color32 resetColor = new Color32(255, 255, 255, 0);
            Color32[] resetColorArray = gridTex.GetPixels32();

            for (int i = 0; i < resetColorArray.Length; i++) {
                resetColorArray[i] = resetColor;
            }

            gridTex.SetPixels32(resetColorArray);

            var gridColor = Color.white;
            var cellSize = 256 / BaseGridScale;
            for (var x = 0; x < BaseGridScale; ++x)
            {
                var baseX = x * cellSize;
                for (var y = 0; y < BaseGridScale; ++y)
                {
                    var baseY = y * cellSize;

                    for (var i = 0; i < cellSize; ++i)
                    {
                        gridTex.SetPixel(baseX + i, baseY, gridColor);
                        gridTex.SetPixel(baseX, baseY + i, gridColor);
                    }
                }
            }

            gridTex.Apply();

            var data = gridTex.EncodeToPNG();
            System.IO.File.WriteAllBytes("Assets/Resources/Sprites/grid.png", data);
        }
#endif

        void InitializeGrid()
        {
            gridMaterial = grid.GetComponent<MeshRenderer>().material;
            grid.transform.localScale = Vector3.one;

            var layer = Layer(MapLayer.Grid);
            grid.transform.position = new Vector3(0f, 0f, layer);

            var aspect = Camera.main.aspect;
            var minOrthoSize = InputController.minZoom;
            var halfHeight = minOrthoSize;
            var halfWidth = aspect * halfHeight;

            halfHeight = Mathf.Ceil(((halfHeight * 2f) / GridCellSize) + 2) * GridCellSize * .5f;
            halfWidth = Mathf.Ceil(((halfWidth * 2f) / GridCellSize) + 2) * GridCellSize * .5f;

            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-halfWidth, -halfHeight, 0f), 
                    new Vector3(-halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, halfHeight, 0f),
                    new Vector3(halfWidth, -halfHeight, 0f),
                },
                triangles = new[]
                {
                    0, 1, 2, 0, 2, 3
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                },
            };

            var minTilingX = (halfWidth * 2f) / GridCellSize;
            var minTilingY = (halfHeight * 2f) / GridCellSize;

            grid.GetComponent<MeshFilter>().sharedMesh = mesh;

            _baseGridTiling = new Vector2(minTilingX / BaseGridScale, minTilingY / BaseGridScale);
            gridMaterial.SetTextureScale("_MainTex", _baseGridTiling);
        }

        void UpdateGridPosition()
        {
            var mainCamera = Camera.main;
            Debug.Assert(mainCamera != null);

            var cameraRectScreen = mainCamera.pixelRect;
            Vector2 minPtWorld = mainCamera.ScreenToWorldPoint(cameraRectScreen.min);

            var nearestGridPt = new Vector2(Mathf.Floor(minPtWorld.x / GridCellSize) * GridCellSize,
                                            Mathf.Floor(minPtWorld.y / GridCellSize) * GridCellSize);

            var neededPt = nearestGridPt + new Vector2(_baseGridTiling.x * _gridScale * GridCellSize * .5f * BaseGridScale,
                                                       _baseGridTiling.y * _gridScale * GridCellSize * .5f * BaseGridScale);

            grid.transform.SetPositionInLayer(neededPt);
        }

        void UpdateGridScale()
        {
            var mr = grid.GetComponent<MeshRenderer>();
            var orthoSize = Camera.main.orthographicSize;
            if (orthoSize >= 120f)
            {
                var percentage = (orthoSize - 120) / 60f;
                if (percentage >= 1f)
                {
                    mr.enabled = false;
                    return;
                }

                var c = gridMaterial.color;
                gridMaterial.color = new Color(c.r, c.g, c.b, 1f - percentage);
            }
            else
            {
                var c = gridMaterial.color;
                gridMaterial.color = new Color(c.r, c.g, c.b, 1f);
            }

            mr.enabled = true;

            _gridScale = orthoSize / InputController.minZoom;
            grid.transform.localScale = new Vector3(_gridScale, _gridScale, 1f);

            var xFactor = _baseGridTiling.x * _gridScale;
            var yFactor = _baseGridTiling.y * _gridScale;

            gridMaterial.SetTextureScale("_MainTex", new Vector2(xFactor, yFactor));
            UpdateGridPosition();
        }

        public void ToggleGrid()
        {
            if (grid.gameObject.activeSelf)
            {
                DisableGrid();
            }
            else
            {
                EnableGrid();
            }
        }

        public void EnableGrid()
        {
            if (grid.sharedMesh == null)
            {
                InitializeGrid();
            }

            UpdateGridScale();
            grid.gameObject.SetActive(true);
        }

        public void DisableGrid()
        {
            grid.gameObject.SetActive(false);
        }
    }
}