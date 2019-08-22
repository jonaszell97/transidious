using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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

        static SerializedMap GetSerializedMap(Map map, byte[] backgroundImage)
        {
            return new SerializedMap
            {
                boundaryPositions = map.boundaryPositions.Select(
                    p => new SerializableVector2(p)).ToArray(),
                minX = map.minX,
                maxX = map.maxX,
                minY = map.minY,
                maxY = map.maxY,
                cameraStartingPos = new SerializableVector3(map.startingCameraPos),
                boundaryMeshes = new SerializableMesh2D[]
                {
                    new SerializableMesh2D(map.boundaryBackgroundObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh2D(map.boundaryOutlineObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh2D(map.boundaryMaskObj.GetComponent<MeshFilter>().mesh),
                },
                naturalFeatures = map.naturalFeatures.Select(r => r.Serialize()).ToArray(),
                backgroundImage = backgroundImage,
            };
        }

        static SaveFile GetSaveFile(Map map)
        {
            var stiles = new MapTile.SerializableMapTile[map.tiles.Length][];
            for (int x = 0; x < map.tiles.Length; ++x)
            {
                stiles[x] = new MapTile.SerializableMapTile[map.tiles[x].Length];

                for (int y = 0; y < map.tiles[x].Length; ++y)
                {
                    stiles[x][y] = map.tiles[x][y].Serialize();
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

        static void SaveMapToFile(Map map, byte[] backgroundImage)
        {
            string fileName = "Assets/Resources/Maps/";
            fileName += map.name;
            fileName += ".bytes";

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                var serializedMap = GetSerializedMap(map, backgroundImage);
                SerializeToStream(stream, serializedMap);
            }
        }

        public static void SaveMapLayout(Map map, byte[] backgroundImage = null)
        {
            SaveMapToFile(map, backgroundImage);
        }

        public static void SaveMapData(Map map)
        {
            string fileName = "Assets/Resources/Saves/";
            fileName += map.name;
            // fileName += "_";
            // fileName += (new DateTime()).ToString();
            fileName += ".bytes";

            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                SerializeToStream(stream, GetSaveFile(map));
            }
        }

        static IEnumerator LoadMap(Map map, string mapName)
        {
            map.name = mapName;

            var asyncResource = Resources.LoadAsync("Maps/" + mapName);
            yield return asyncResource;

            var fileResource = (TextAsset)asyncResource.asset;
            var serializedMap = DeserializeFromStream<SerializedMap>(fileResource);

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

            // Deserialize natural features.
            foreach (var feature in serializedMap.naturalFeatures)
            {
                NaturalFeature.Deserialize(map, feature);
            }

            LoadBackgroundSprite(map, serializedMap.backgroundImage,
                                 MapDisplayMode.Day,
                                 ref map.backgroundSpriteDay);

            LoadBackgroundSprite(map, serializedMap.backgroundImage,
                                 MapDisplayMode.Night,
                                 ref map.backgroundSpriteNight);

            // yield return LoadTiles(map, mapName, serializedMap.screenShotInfo);
        }

        static void LoadBackgroundSprite(Map map, byte[] bytes,
                                         MapDisplayMode mode,
                                         ref GameObject gameObject)
        {
            var tex = new Texture2D(0, 0, TextureFormat.RGB24, false);
            if (!tex.LoadImage(bytes))
            {
                Debug.LogError("corrupted PNG file!");
                return;
            }

            var buildingColor = Building.DefaultColor(mode);
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

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0, 0), 100f);

            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
            gameObject.name = "LOD Background (" + mode.ToString() + ")";

            var transform = gameObject.transform;
            var spriteBounds = sprite.bounds;
            var desiredWidth = map.width;
            var desiredHeight = map.height;

            transform.position = new Vector3(map.minX, map.minY, Map.Layer(MapLayer.Parks));
            transform.localScale = new Vector3(desiredWidth / spriteBounds.size.x,
                                               desiredHeight / spriteBounds.size.y,
                                               1f);

            gameObject.SetActive(false);
        }

        static IEnumerator LoadTiles(Map map, string mapName,
                                     ScreenShotMaker.ScreenShotInfo screenShotInfo)
        {
            var minX = map.minX - 100f;
            var maxX = map.maxX + 100f;
            var minY = map.minY - 100f;
            var maxY = map.maxY + 100f;

            float tileSizeUnits = screenShotInfo.tileSizeUnits;
            float tileSizePixels = screenShotInfo.tileSizePixels;
            float pixelsPerUnit = tileSizePixels / tileSizeUnits;

            string path = "Assets/Resources/Maps/" + mapName + "/";
            for (int x = 0; x < screenShotInfo.xTiles; ++x)
            {
                for (int y = 0; y < screenShotInfo.yTiles; ++y)
                {
                    var asyncSprite = new DataCoroutine<Sprite>(SpriteLoader.LoadNewSpriteAsync(
                        path + x + "_" + y + ".png", pixelsPerUnit));

                    yield return asyncSprite.coroutine;

                    var sprite = asyncSprite.result;
                    if (!sprite)
                    {
                        continue;
                    }

                    var spriteObj = map.Game.CreateSprite(sprite);
                    spriteObj.transform.position = new Vector3(
                        minX + (x * tileSizeUnits) + tileSizeUnits / 2,
                        minY + (y * tileSizeUnits) + tileSizeUnits / 2,
                        Map.Layer(MapLayer.Parks));
                }
            }
        }

        static IEnumerator LoadSave(Map map, string saveName)
        {
            var asyncResource = Resources.LoadAsync("Saves/" + saveName);
            yield return asyncResource;

            var fileResource = (TextAsset)asyncResource.asset;
            if (fileResource == null)
            {
                yield return map.DoFinalize();
                yield break;
            }

            var saveFile = DeserializeFromStream<SaveFile>(fileResource);
#if UNITY_EDITOR
            var thresholdTime = 500;
#else
            var thresholdTime = 30;
#endif

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

            // Deserialize streets along with their segments.
            foreach (var street in saveFile.streets)
            {
                Street.Deserialize(map, street);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
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
                    map.tiles[x][y].Deserialize(map, saveFile.tiles[x][y]);
                }
            }

            // Finalize the map.
            yield return map.DoFinalize(thresholdTime);
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
