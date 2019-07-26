using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Transidious
{
    public class SaveManager
    {
        [Serializable]
        public struct SerializedMap
        {
            public float minX, maxX, minY, maxY;
            public ScreenShotMaker.ScreenShotInfo screenShotInfo;
            public SerializableVector3 cameraStartingPos;

            public SerializableMesh[] boundaryMeshes;

            // public NaturalFeature.SerializedFeature[] naturalFeatures;
            // public Building.SerializableBuilding[] buildings;
        }

        [Serializable]
        public struct SaveFile
        {
            public MapTile.SerializableMapTile[][] tiles;
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
                // naturalFeatures = naturalFeatures.Select(r => r.Serialize()).ToArray(),
                // buildings = buildings.Select(r => r.Serialize()).ToArray(),
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
                streets = map.streets.Select(s => s.Serialize()).ToArray(),
                streetIntersections = map.streetIntersections.Select(s => s.Serialize()).ToArray(),
                transitRoutes = map.transitRoutes.Select(r => r.Serialize()).ToArray(),
                transitStops = map.transitStops.Select(r => r.Serialize()).ToArray(),
                transitLines = map.transitLines.Select(r => r.Serialize()).ToArray(),
            };
        }

        static void SaveMapToFile(Map map,
                                  ScreenShotMaker.ScreenShotInfo screenShotInfo)
        {
            string fileName = "Assets/Resources/Maps/";
            fileName += map.name;
            fileName += ".txt";

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var ms = new MemoryStream())
                {
                    formatter.Serialize(ms, GetSerializedMap(map, screenShotInfo));

                    var b64str = Convert.ToBase64String(ms.ToArray());
                    var buffer = Encoding.ASCII.GetBytes(b64str);
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        static ScreenShotMaker.ScreenShotInfo SaveMapScreenshot(Map map)
        {
            return ScreenShotMaker.Instance.MakeScreenshot(map);
        }

        public static void SaveMapLayout(Map map)
        {
            var screenShotInfo = SaveMapScreenshot(map);
            SaveMapToFile(map, screenShotInfo);
        }

        public static void SaveMapData(Map map)
        {
            string fileName = "Assets/Resources/Saves/";
            fileName += map.name;
            // fileName += "_";
            // fileName += (new DateTime()).ToString();
            fileName += ".txt";

            var formatter = new BinaryFormatter();
            using (Stream stream = File.Open(fileName, FileMode.OpenOrCreate,
                                                       FileAccess.ReadWrite))
            {
                formatter.Serialize(stream, GetSaveFile(map));
            }
        }

        static void LoadMap(Map map, string mapName)
        {
            map.name = mapName;

            var fileResource = (TextAsset)Resources.Load("Maps/" + mapName);
            var b64str = Encoding.ASCII.GetString(fileResource.bytes, 0, fileResource.bytes.Length);
            var bytes = Convert.FromBase64String(b64str);

            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                var formatter = new BinaryFormatter();
                var serializedMap = (SerializedMap)formatter.Deserialize(stream);

                map.UpdateBoundary(serializedMap.boundaryMeshes[0].GetMesh(),
                                   serializedMap.boundaryMeshes[1].GetMesh(),
                                   serializedMap.boundaryMeshes[2].GetMesh(),
                                   serializedMap.minX, serializedMap.maxX,
                                   serializedMap.minY, serializedMap.maxY);

                Camera.main.transform.position = new Vector3(
                    serializedMap.minX + (serializedMap.maxX - serializedMap.minX) / 2f,
                    serializedMap.minY + (serializedMap.maxY - serializedMap.minY) / 2f,
                    Camera.main.transform.position.z);

                map.startingCameraPos = Camera.main.transform.position;

                LoadTiles(map, mapName, serializedMap.screenShotInfo);

                // foreach (var f in map.naturalFeatures)
                // {
                //     CreateFeature(f.name, f.type, f.mesh.GetMesh());
                // }
                // foreach (var b in map.buildings)
                // {
                //     var building = CreateBuilding(b.type, b.mesh.GetMesh(), b.name, b.number,
                //                                   b.position.ToVector());
                //     building.streetID = b.streetID;
                //     building.number = b.number;
                // }
            }
        }

        static void LoadTiles(Map map, string mapName,
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
                    var sprite = SpriteLoader.LoadNewSprite(
                        path + x + "_" + y + ".png", pixelsPerUnit);

                    if (!sprite)
                    {
                        continue;
                    }

                    var spriteObj = map.input.controller.CreateSprite(sprite);
                    spriteObj.transform.position = new Vector3(
                        minX + (x * tileSizeUnits) + tileSizeUnits / 2,
                        minY + (y * tileSizeUnits) + tileSizeUnits / 2,
                        Map.Layer(MapLayer.Parks));
                }
            }
        }

        static void LoadSave(Map map, string saveName)
        {
            var fileResource = (TextAsset)Resources.Load("Saves/" + saveName);
            if (fileResource == null)
            {
                map.DoFinalize();
                return;
            }

            using (var stream = new MemoryStream())
            {
                stream.Write(fileResource.bytes, 0, fileResource.bytes.Length);
                stream.Position = 0;

                var formatter = new BinaryFormatter();
                var saveFile = (SaveFile)formatter.Deserialize(stream);

                foreach (var inter in saveFile.streetIntersections)
                {
                    map.CreateIntersection(inter.position.ToVector());
                }
                foreach (var street in saveFile.streets)
                {
                    var s = map.CreateStreet(street.name, street.type, street.lit,
                                             street.oneway, street.maxspeed,
                                             street.lanes);

                    s.Deserialize(street);
                }

                foreach (var route in saveFile.transitRoutes)
                {
                    map.CreateRoute();
                }
                foreach (var stop in saveFile.transitStops)
                {
                    map.GetOrCreateStop(stop.name, stop.position.ToVector());
                }
                foreach (var line in saveFile.transitLines)
                {
                    map.CreateLine(line.type, line.name, line.color.ToColor()).Finish();
                }

                foreach (var stop in saveFile.transitStops)
                {
                    map.transitStopIDMap[stop.id].Deserialize(stop, map);
                }
                foreach (var line in saveFile.transitLines)
                {
                    map.transitLineIDMap[line.id].Deserialize(line, map);
                }
                foreach (var route in saveFile.transitRoutes)
                {
                    map.transitRouteIDMap[route.id].Deserialize(route, map);
                }

                foreach (var building in map.buildings)
                {
                    building.street = map.streetSegmentIDMap[building.streetID];
                }

                for (int x = 0; x < saveFile.tiles.Length; ++x)
                {
                    for (int y = 0; y < saveFile.tiles[x].Length; ++y)
                    {
                        map.tiles[x][y].Deserialize(map, saveFile.tiles[x][y]);
                    }
                }

                map.DoFinalize();
            }
        }

        public static Map LoadSave(GameController game, string saveName)
        {
            var mapPrefab = Resources.Load("Prefabs/Map") as GameObject;
            var mapObj = GameObject.Instantiate(mapPrefab);

            var map = mapObj.GetComponent<Map>();
            map.input = game.input;

            LoadMap(map, saveName);
            LoadSave(map, saveName);
            loadedMap = map;

            return map;
        }
    }
}
