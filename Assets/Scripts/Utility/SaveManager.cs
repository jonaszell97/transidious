using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;

namespace Transidious
{
    public class SaveManager
    {
        [Serializable]
        public struct SerializedMap
        {
            public float maxTileSize;
            public SerializableVector2[] boundaryPositions;
            public float minX, maxX, minY, maxY;
            public SerializableVector3 cameraStartingPos;
            public SerializableMesh2D[] boundaryMeshes;
            public NaturalFeature.SerializedFeature[] naturalFeatures;
            public byte[] backgroundImage;
        }

        [Serializable]
        public struct SaveFile
        {
            public MapTile.SerializableMapTile[][] tiles;
            public Building.SerializableBuilding[] buildings;
            public Street.SerializedStreet[] streets;
            public StreetIntersection.SerializedStreetIntersection[] streetIntersections;
            public Route.SerializedRoute[] transitRoutes;
            public Stop.SerializedStop[] transitStops;
            public Line.SerializedLine[] transitLines;
        }

        public static Map loadedMap;

#if UNITY_EDITOR
        public static readonly int thresholdTime = 500;
#else
        public static readonly int thresholdTime = 30;
#endif

        static SerializedMap GetSerializedMap(Map map, byte[] backgroundImage, bool includeBoundary)
        {
            var result = new SerializedMap
            {
                maxTileSize = OSMImporter.maxTileSize,
                minX = map.minX,
                maxX = map.maxX,
                minY = map.minY,
                maxY = map.maxY,
                cameraStartingPos = new SerializableVector3(map.startingCameraPos),
                naturalFeatures = map.naturalFeatures.Select(r => r.Serialize()).ToArray(),
                backgroundImage = backgroundImage,
            };

            if (includeBoundary)
            {
                result.boundaryPositions = map.boundaryPositions?.Select(
                    p => new SerializableVector2(p)).ToArray();

                result.boundaryMeshes = new SerializableMesh2D[]
                {
                    new SerializableMesh2D(map.boundaryBackgroundObj?.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh2D(map.boundaryOutlineObj?.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh2D(map.boundaryMaskObj?.GetComponent<MeshFilter>().mesh),
                };
            }

            return result;
        }

        static SaveFile GetSaveFile(Map map, int tileX, int tileY)
        {
            int minX, maxX;
            if (tileX == -1)
            {
                minX = 0;
                maxX = map.tiles.Length;
            }
            else
            {
                minX = (int)Mathf.Floor(tileX * (OSMImporter.maxTileSize / Map.tileSize));
                maxX = Mathf.Min(minX + (int)(OSMImporter.maxTileSize / Map.tileSize), map.tiles.Length);
            }

            var stiles = new MapTile.SerializableMapTile[maxX - minX][];
            for (int x = minX; x < maxX; ++x)
            {
                int minY, maxY;
                if (tileX == -1)
                {
                    minY = 0;
                    maxY = map.tiles[x].Length;
                }
                else
                {
                    minY = (int)Mathf.Floor(tileY * (OSMImporter.maxTileSize / Map.tileSize));
                    maxY = Mathf.Min(minY + (int)(OSMImporter.maxTileSize / Map.tileSize), map.tiles[x].Length);
                }

                stiles[x - minX] = new MapTile.SerializableMapTile[maxY - minY];

                for (int y = minY; y < maxY; ++y)
                {
                    stiles[x - minX][y - minY] = map.tiles[x][y].Serialize();
                }
            }

            return new SaveFile
            {
                tiles = stiles,
                buildings = map.buildings.Select(r => r.Serialize()).ToArray(),
                streets = map.streets.Select(s => s.Serialize()).ToArray(),
                streetIntersections = map.streetIntersections.Select(
                    s => s.Serialize()).ToArray(),
                transitRoutes = map.transitRoutes.Select(r => r.Serialize()).ToArray(),
                transitStops = map.transitStops.Select(r => r.Serialize()).ToArray(),
                transitLines = map.transitLines.Select(r => r.Serialize()).ToArray(),
            };
        }

        static void SerializeToStream(Stream stream, object value)
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, value);
                ms.Seek(0, SeekOrigin.Begin);

                GZip.Compress(ms, stream, false);
            }
        }

        static T DeserializeFromStream<T>(TextAsset asset)
        {
            var formatter = new BinaryFormatter();
            using (var compressed = new MemoryStream(asset.bytes))
            {
                using (var decompressed = new MemoryStream())
                {
                    GZip.Decompress(compressed, decompressed, false);
                    decompressed.Seek(0, SeekOrigin.Begin);
                    return (T)formatter.Deserialize(decompressed);
                }
            }
        }

        public static void SaveMapLayout(Map map, byte[] backgroundImage, int tileX = -1, int tileY = -1)
        {
            string fileName;
            if (tileX == -1 && tileY == -1)
            {
                fileName = "Assets/Resources/Maps/";
                fileName += map.name;

                if (System.IO.Directory.Exists(fileName))
                {
                    var dir = new System.IO.DirectoryInfo(fileName);
                    foreach (var file in dir.GetFiles())
                    {
                        file.Delete();
                    }

                    dir.Delete();
                }

                fileName += ".bytes";
            }
            else
            {
                var directoryPath = "Assets/Resources/Maps/";
                directoryPath += map.name;

                if (System.IO.File.Exists(directoryPath + ".bytes"))
                {
                    System.IO.File.Delete(directoryPath + ".bytes");
                }

                var dir = System.IO.Directory.CreateDirectory(directoryPath);
                if (tileX == 0 && tileY == 0)
                {
                    foreach (var file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                }

                fileName = directoryPath + "/" + tileX + "_" + tileY + ".bytes";
            }

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                var serializedMap = GetSerializedMap(map, backgroundImage, tileX == -1 || (tileX == 0 && tileY == 0));
                SerializeToStream(stream, serializedMap);
            }
        }

        public static void SaveMapData(Map map, int tileX = -1, int tileY = -1)
        {
            string fileName;
            if (tileX == -1 && tileY == -1)
            {
                fileName = "Assets/Resources/Saves/";
                fileName += map.name;

                if (System.IO.Directory.Exists(fileName))
                {
                    var dir = new System.IO.DirectoryInfo(fileName);
                    foreach (var file in dir.GetFiles())
                    {
                        file.Delete();
                    }

                    dir.Delete();
                }

                // fileName += "_";
                // fileName += (new DateTime()).ToString();
                fileName += ".bytes";
            }
            else
            {
                var directoryPath = "Assets/Resources/Saves/";
                directoryPath += map.name;

                if (System.IO.File.Exists(directoryPath + ".bytes"))
                {
                    System.IO.File.Delete(directoryPath + ".bytes");
                }

                var dir = System.IO.Directory.CreateDirectory(directoryPath);
                if (tileX == 0 && tileY == 0)
                {
                    foreach (var file in dir.GetFiles())
                    {
                        file.Delete();
                    }
                }

                fileName = directoryPath + "/" + tileX + "_" + tileY + ".bytes";
            }

            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                SerializeToStream(stream, GetSaveFile(map, tileX, tileY));
            }
        }

        static IEnumerator LoadMap(Map map, string mapName)
        {
            var resourceName = "Maps/" + mapName;

            var asyncResource = Resources.LoadAsync(resourceName);
            yield return asyncResource;

            if (asyncResource.asset != null)
            {
                yield return LoadMap(map, mapName, asyncResource, -1, -1);
            }
            else
            {
                for (int x = 0; ; ++x)
                {
                    for (int y = 0; ; ++y)
                    {
                        var file = resourceName + "/" + x + "_" + y;
                        asyncResource = Resources.LoadAsync(file);
                        yield return asyncResource;

                        if (asyncResource.asset != null)
                        {
                            yield return LoadMap(map, mapName, asyncResource, x, y);
                        }
                        else if (y == 0)
                        {
                            if (x == 0)
                            {
                                Debug.LogError("no save file");
                            }

                            yield break;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        static IEnumerator LoadMap(Map map, string mapName, ResourceRequest asyncResource, int tileX, int tileY)
        {
            map.name = mapName;

            var fileResource = (TextAsset)asyncResource.asset;
            var serializedMap = DeserializeFromStream<SerializedMap>(fileResource);
            var isBaseTile = tileX == -1 || (tileX == 0 && tileY == 0);

            if (isBaseTile)
            {
                map.minX = serializedMap.minX;
                map.maxX = serializedMap.maxX;
                map.minY = serializedMap.minY;
                map.maxY = serializedMap.maxY;

                map.UpdateBoundary(
                    serializedMap.boundaryMeshes[0].GetMesh(Map.Layer(MapLayer.Boundary)),
                    serializedMap.boundaryMeshes[1].GetMesh(Map.Layer(MapLayer.Foreground)),
                    serializedMap.boundaryMeshes[2].GetMesh(Map.Layer(MapLayer.Boundary)),
                    serializedMap.minX, serializedMap.maxX,
                    serializedMap.minY, serializedMap.maxY);

                map.boundaryPositions = serializedMap.boundaryPositions.Select(
                    p => (Vector2)p).ToArray();

                Camera.main.transform.position = new Vector3(
                    serializedMap.minX + (serializedMap.maxX - serializedMap.minX) / 2f,
                    serializedMap.minY + (serializedMap.maxY - serializedMap.minY) / 2f,
                    Camera.main.transform.position.z);

                map.startingCameraPos = Camera.main.transform.position;
            }

            // Deserialize natural features.
            foreach (var feature in serializedMap.naturalFeatures)
            {
                NaturalFeature.Deserialize(map, feature);
            }

            LoadBackgroundSprite(map, serializedMap.backgroundImage,
                                 MapDisplayMode.Day,
                                 ref map.backgroundSpriteDay,
                                 tileX, tileY, serializedMap.maxTileSize);

            LoadBackgroundSprite(map, serializedMap.backgroundImage,
                                 MapDisplayMode.Night,
                                 ref map.backgroundSpriteNight,
                                 tileX, tileY, serializedMap.maxTileSize);

            yield return null;
        }

        static void LoadBackgroundSprite(Map map, byte[] bytes,
                                         MapDisplayMode mode,
                                         ref GameObject gameObject,
                                         int tileX, int tileY, float maxTileSize)
        {
            var tex = new Texture2D(0, 0, TextureFormat.RGB24, false);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogError("corrupted PNG file!");
                return;
            }

            var buildingColor = Building.GetColor(Building.Type.Residential, mode);
            var bgColor = map.GetDefaultBackgroundColor(mode);
            var streetColor = StreetSegment.GetStreetColor(Street.Type.Primary,
                RenderingDistance.Near, mode);
            var streetOutlineColor = StreetSegment.GetBorderColor(Street.Type.Primary,
                RenderingDistance.Near, mode);

            for (var x = 0; x < tex.width; ++x)
            {
                for (var y = 0; y < tex.height; ++y)
                {
                    var pixel = tex.GetPixel(x, y);
                    if (pixel.Equals(Color.white))
                    {
                        tex.SetPixel(x, y, bgColor);
                    }
                    else if (pixel.Equals(Color.black))
                    {
                        tex.SetPixel(x, y, buildingColor);
                    }
                    else if (pixel.Equals(Color.blue))
                    {
                        tex.SetPixel(x, y, streetColor);
                    }
                    else if (pixel.Equals(Color.red))
                    {
                        tex.SetPixel(x, y, streetOutlineColor);
                    }
                }
            }

            tex.Apply();

            int minX, minY;
            if (tileX == -1)
            {
                minX = 0;
                minY = 0;
            }
            else
            {
                minX = (int)maxTileSize * tileX;
                minY = (int)maxTileSize * tileY;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0, 0), 100f);

            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
            gameObject.name = "LOD Background (" + mode.ToString() + ")";

            var transform = gameObject.transform;
            var spriteBounds = sprite.bounds;
            var desiredWidth = Mathf.Min(map.width, maxTileSize);
            var desiredHeight = Mathf.Min(map.height, maxTileSize);

            transform.position = new Vector3(map.minX + minX, map.minY + minY, Map.Layer(MapLayer.Parks));
            transform.localScale = new Vector3(desiredWidth / spriteBounds.size.x,
                                               desiredHeight / spriteBounds.size.y,
                                               1f);

            gameObject.SetActive(false);
        }

        static IEnumerator LoadSave(Map map, string mapName)
        {
            var resourceName = "Saves/" + mapName;
            var asyncResource = Resources.LoadAsync(resourceName);
            yield return asyncResource;

            if (asyncResource.asset != null)
            {
                yield return LoadSave(map, asyncResource);
            }
            else
            {
                var done = false;
                var tiles = new List<SaveFile>();

                for (int x = 0; !done; ++x)
                {
                    for (int y = 0; ; ++y)
                    {
                        var file = resourceName + "/" + x + "_" + y;

                        asyncResource = Resources.LoadAsync(file);
                        yield return asyncResource;

                        if (asyncResource.asset != null)
                        {
                            var fileResource = (TextAsset)asyncResource.asset;
                            var saveFile = DeserializeFromStream<SaveFile>(fileResource);
                            tiles.Add(saveFile);
                        }
                        else if (y == 0)
                        {
                            done = true;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                foreach (var tile in tiles)
                {
                    Debug.Log("deserializing");
                    yield return DeserializeGameObjects(map, tile);
                }

                foreach (var tile in tiles)
                {
                    Debug.Log("initializing");
                    yield return InitializeGameObjects(map, tile);
                }
            }

            // Finalize the map.
            yield return map.DoFinalize(thresholdTime);
        }

        static IEnumerator LoadSave(Map map, ResourceRequest asyncResource)
        {
            var fileResource = (TextAsset)asyncResource.asset;
            if (fileResource == null)
            {
                yield break;
            }

            var saveFile = DeserializeFromStream<SaveFile>(fileResource);

            yield return DeserializeGameObjects(map, saveFile);
            yield return InitializeGameObjects(map, saveFile);
        }

        static IEnumerator DeserializeGameObjects(Map map, SaveFile saveFile)
        {
            // Deserialize buildings.
            foreach (var b in saveFile.buildings)
            {
                Building.Deserialize(map, b);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Deserialize raw intersections without their intersecting streets.
            foreach (var inter in saveFile.streetIntersections)
            {
                StreetIntersection.Deserialize(map, inter);
            }

            // Deserialize raw routes without stops or lines.
            foreach (var route in saveFile.transitRoutes)
            {
                map.CreateRoute(route.mapObject.id);
            }

            // Deserialize raw routes without routes.
            foreach (var stop in saveFile.transitStops)
            {
                map.GetOrCreateStop(stop.mapObject.name, stop.position,
                                    stop.mapObject.id);
            }

            // Deserialize raw routes without stops or routes.
            foreach (var line in saveFile.transitLines)
            {
                map.CreateLine(line.type, line.mapObject.name, line.color,
                               line.mapObject.id).Finish();
            }
        }

        static IEnumerator InitializeGameObjects(Map map, SaveFile saveFile)
        {
            // Deserialize streets along with their segments.
            foreach (var street in saveFile.streets)
            {
                Street.Deserialize(map, street);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Connect stops with their lines.
            foreach (var stop in saveFile.transitStops)
            {
                map.GetMapObject<Stop>(stop.mapObject.id).Deserialize(stop, map);
            }

            // Connect routes with streets and initialize the meshes.
            foreach (var route in saveFile.transitRoutes)
            {
                map.GetMapObject<Route>(route.mapObject.id).Deserialize(route, map);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Connect lines with their routes.
            foreach (var line in saveFile.transitLines)
            {
                var createdLine = map.GetMapObject<Line>(line.mapObject.id);
                createdLine.Deserialize(line, map);
            }

            // Connect buildings to their streets.
            foreach (var building in map.buildings)
            {
                building.street = map.GetMapObject<StreetSegment>(building.streetID);
            }

            // Initialize the map tiles.
            for (int x = 0; x < saveFile.tiles.Length; ++x)
            {
                for (int y = 0; y < saveFile.tiles[x].Length; ++y)
                {
                    var tile = saveFile.tiles[x][y];
                    map.tiles[tile.x][tile.y].Deserialize(map, tile);
                }
            }
        }

        public static IEnumerator LoadSave(GameController game, string saveName)
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = GameObject.Instantiate(mapPrefab);

            var map = mapObj.GetComponent<Map>();
            map.input = game.input;
            map.isLoadedFromSaveFile = true;
            loadedMap = map;

            yield return LoadMap(map, saveName);
            yield return LoadSave(map, saveName);
        }
    }
}
