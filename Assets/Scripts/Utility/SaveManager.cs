using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Transidious
{
    public class SaveManager
    {
        [Serializable]
        public struct SerializedMap
        {
            public SerializableVector3[] boundaryPositions;
            public float minX, maxX, minY, maxY;
            public ScreenShotMaker.ScreenShotInfo screenShotInfo;
            public SerializableVector3 cameraStartingPos;
            public SerializableMesh[] boundaryMeshes;
            public NaturalFeature.SerializedFeature[] naturalFeatures;
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

        static SerializedMap GetSerializedMap(Map map, ScreenShotMaker.ScreenShotInfo screenShotInfo)
        {
            return new SerializedMap
            {
                boundaryPositions = map.boundaryPositions.Select(p => new SerializableVector3(p)).ToArray(),
                minX = map.minX,
                maxX = map.maxX,
                minY = map.minY,
                maxY = map.maxY,
                screenShotInfo = screenShotInfo,
                cameraStartingPos = new SerializableVector3(map.startingCameraPos),
                boundaryMeshes = new SerializableMesh[]
                {
                    new SerializableMesh(map.boundaryBackgroundObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh(map.boundaryOutlineObj.GetComponent<MeshFilter>().mesh),
                    new SerializableMesh(map.boundarymaskObj.GetComponent<MeshFilter>().mesh),
                },
                naturalFeatures = map.naturalFeatures.Select(r => r.Serialize()).ToArray(),
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
                streets = map.streets.Where(s => s.type != Street.Type.FootPath).Select(s => s.Serialize()).ToArray(),
                streetIntersections = map.streetIntersections.Select(s => s.Serialize()).ToArray(),
                transitRoutes = map.transitRoutes.Select(r => r.Serialize()).ToArray(),
                transitStops = map.transitStops.Select(r => r.Serialize()).ToArray(),
                transitLines = map.transitLines.Select(r => r.Serialize()).ToArray(),
            };
        }

        static void SerializeToStream(Stream stream, object value)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, value);

                var b64str = Convert.ToBase64String(ms.ToArray());
                var buffer = Encoding.ASCII.GetBytes(b64str);
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        static object DeserializeFromStream(TextAsset fileResource)
        {
            var b64str = Encoding.ASCII.GetString(fileResource.bytes, 0, fileResource.bytes.Length);
            var bytes = Convert.FromBase64String(b64str);

            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                var formatter = new BinaryFormatter();
                return formatter.Deserialize(stream);
            }
        }

        static void SaveMapToFile(Map map,
                                  ScreenShotMaker.ScreenShotInfo screenShotInfo)
        {
            string fileName = "Assets/Resources/Maps/";
            fileName += map.name;
            fileName += ".txt";

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                SerializeToStream(stream, GetSerializedMap(map, screenShotInfo));
            }
        }

        static ScreenShotMaker.ScreenShotInfo SaveMapScreenshot(Map map)
        {
            return ScreenShotMaker.Instance.MakeScreenshot(map);
        }

        public static void SaveMapLayout(Map map)
        {
            // var screenShotInfo = SaveMapScreenshot(map);
            SaveMapToFile(map, new ScreenShotMaker.ScreenShotInfo());
        }

        public static void SaveMapData(Map map)
        {
            string fileName = "Assets/Resources/Saves/";
            fileName += map.name;
            // fileName += "_";
            // fileName += (new DateTime()).ToString();
            fileName += ".txt";

            Debug.Log("serialized " + map.buildings.Count + " buildings");

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
            var serializedMap = (SerializedMap)DeserializeFromStream(fileResource);

            map.UpdateBoundary(serializedMap.boundaryMeshes[0].GetMesh(),
                               serializedMap.boundaryMeshes[1].GetMesh(),
                               serializedMap.boundaryMeshes[2].GetMesh(),
                               serializedMap.minX, serializedMap.maxX,
                               serializedMap.minY, serializedMap.maxY);

            map.boundaryPositions = serializedMap.boundaryPositions.Select(p => p.ToVector()).ToArray();

            Camera.main.transform.position = new Vector3(
                serializedMap.minX + (serializedMap.maxX - serializedMap.minX) / 2f,
                serializedMap.minY + (serializedMap.maxY - serializedMap.minY) / 2f,
                Camera.main.transform.position.z);

            map.startingCameraPos = Camera.main.transform.position;

            // yield return LoadTiles(map, mapName, serializedMap.screenShotInfo);
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
                map.DoFinalize();
                yield break;
            }

            var saveFile = (SaveFile)DeserializeFromStream(fileResource);
            var thresholdTime = 500; // 12;

            // Deserialize buildings.
            foreach (var b in saveFile.buildings)
            {
                Mesh mesh = b.mesh.GetMesh();
                var building = map.CreateBuilding(b.type, mesh, b.name, b.number,
                                                  b.position.ToVector());

                building.streetID = b.streetID;
                building.number = b.number;

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Deserialize raw intersections without their intersecting streets.
            foreach (var inter in saveFile.streetIntersections)
            {
                map.CreateIntersection(inter.position.ToVector(), inter.id);
            }

            // Deserialize streets along with their segments.
            foreach (var street in saveFile.streets)
            {
                var s = map.CreateStreet(street.name, street.type, street.lit,
                                         street.oneway, street.maxspeed,
                                         street.lanes, street.id);

                s.Deserialize(street);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Deserialize raw routes without stops or lines.
            foreach (var route in saveFile.transitRoutes)
            {
                map.CreateRoute(route.id);
            }

            // Deserialize raw routes without routes.
            foreach (var stop in saveFile.transitStops)
            {
                map.GetOrCreateStop(stop.name, stop.position.ToVector(), stop.id);
            }

            // Deserialize raw routes without stops or routes.
            foreach (var line in saveFile.transitLines)
            {
                map.CreateLine(line.type, line.name, line.color.ToColor(), line.id).Finish();
            }

            // Connect stops with their lines.
            foreach (var stop in saveFile.transitStops)
            {
                map.transitStopIDMap[stop.id].Deserialize(stop, map);
            }

            // Connect routes with streets and initialize the meshes.
            foreach (var route in saveFile.transitRoutes)
            {
                map.transitRouteIDMap[route.id].Deserialize(route, map);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Connect lines with their routes.
            foreach (var line in saveFile.transitLines)
            {
                var createdLine = map.transitLineIDMap[line.id];
                createdLine.Deserialize(line, map);
            }

            // Connect buildings to their streets.
            foreach (var building in map.buildings)
            {
                building.street = map.streetSegmentIDMap[building.streetID];
            }

            // Initialize the map tiles.
            for (int x = 0; x < saveFile.tiles.Length; ++x)
            {
                for (int y = 0; y < saveFile.tiles[x].Length; ++y)
                {
                    map.tiles[x][y].Deserialize(map, saveFile.tiles[x][y]);
                }
            }

            yield return null;

            // Finalize the map.
            map.DoFinalize();
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
            loadedMap = map;

            yield return LoadMap(map, saveName);
            yield return LoadSave(map, saveName);
        }
    }
}
