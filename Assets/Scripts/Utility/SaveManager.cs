using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;
using Transidious.Serialization;

namespace Transidious
{
    [Serializable]
    public struct VersionTriple : IEquatable<VersionTriple>
    {
        public short major;
        public short minor;
        public short patch;

        public VersionTriple(short major, short minor, short patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }

        public Serialization.VersionTriple ToProtobuf()
        {
            return new Serialization.VersionTriple
            {
                Major = (uint)major,
                Minor = (uint)minor,
                Patch = (uint)patch,
            };
        }

        public override bool Equals(object obj)
        {
            return obj is VersionTriple triple && Equals(triple);
        }

        public bool Equals(VersionTriple other)
        {
            return major == other.major &&
                   minor == other.minor &&
                   patch == other.patch;
        }

        public override int GetHashCode()
        {
            var hashCode = -704854903;
            hashCode = hashCode * -1521134295 + major.GetHashCode();
            hashCode = hashCode * -1521134295 + minor.GetHashCode();
            hashCode = hashCode * -1521134295 + patch.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(VersionTriple left, VersionTriple right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VersionTriple left, VersionTriple right)
        {
            return !(left == right);
        }
    }

    public class SaveManager
    {
        public static readonly VersionTriple SaveFormatVersion = new VersionTriple(0, 0, 3);
        public static Map loadedMap;

#if UNITY_EDITOR
        public static readonly int thresholdTime = 500;
#else
        public static readonly int thresholdTime = 30;
#endif

        static Serialization.Map GetProtobufMap(Map map)
        {
            var result = new Serialization.Map
            {
                Triple = SaveFormatVersion.ToProtobuf(),
                MinX = map.minX,
                MaxX = map.maxX,
                MinY = map.minY,
                MaxY = map.maxY,
                StartingCameraPos = map.startingCameraPos.ToProtobuf(),
                TileSize = map.tileSize,
            };

            result.Buildings.AddRange(map.buildings.Select(b => b.ToProtobuf()));
            result.NaturalFeatures.AddRange(map.naturalFeatures.Select(b => b.ToProtobuf()));

            result.Streets.AddRange(map.streets.Select(b => b.ToProtobuf()));
            result.StreetIntersections.AddRange(map.streetIntersections.Select(b => b.ToProtobuf()));
            result.IntersectionPatterns.AddRange(map.IntersectionPatterns.Select(pair => pair.Value.Serialize()));
            result.TrafficLights.AddRange(map.TrafficLights.Select(tl => tl.Value.Serialize()));

            result.BoundaryPositions.AddRange(map.boundaryPositions?.Select(v => v.ToProtobuf()));
            result.BoundaryMeshes.Add(map.boundaryBackgroundObj.GetComponent<MeshFilter>().mesh.ToProtobuf2D());
            result.BoundaryMeshes.Add(map.boundaryOutlineObj.GetComponent<MeshFilter>().mesh.ToProtobuf2D());

            return result;
        }

        static Serialization.SaveFile GetProtobufSaveFile(Map map)
        {
            var sim = GameController.instance.sim;
            var saveFile = new Serialization.SaveFile
            {
                Triple = new Serialization.VersionTriple
                {
                    Major = (uint)SaveFormatVersion.major,
                    Minor = (uint)SaveFormatVersion.minor,
                    Patch = (uint)SaveFormatVersion.patch,
                },
                GameTime = (ulong)sim.GameTime.Ticks,
                Finances = GameController.instance.financeController?.ToProtobuf(),
            };

            for (var x = 0; x < map.tilesWidth; ++x)
            {
                var tileProtoBuf = new Serialization.SaveFile.Types.MapTiles();
                for (var y = 0; y < map.tilesHeight; ++y)
                {
                    var tile = map.GetTile(x, y);
                    tileProtoBuf.Tiles.Add(tile.ToProtobuf());
                }

                saveFile.Tiles.Add(tileProtoBuf);
            }

            saveFile.Stops.AddRange(map.transitStops.Select(b => b.ToProtobuf()));
            saveFile.Routes.AddRange(map.transitRoutes.Select(b => b.ToProtobuf()));
            saveFile.Lines.AddRange(map.transitLines.Select(b => b.ToProtobuf()));

            if (sim.citizens != null)
            {
                saveFile.Cars.AddRange(
                    sim.cars.Select(c => c.Value.ToProtobuf()));

                foreach (var c in sim.citizens)
                {
                    saveFile.Citizens.Add(c.Value.ToProtobuf());

                    if (c.Value.ActivePath != null)
                    {
                        saveFile.ActivePaths.Add(c.Value.ActivePath.Serialize());
                    }
                }
            }

            return saveFile;
        }

        public static void CompressGZip<T>(string fileName, IMessage msg)
        {
            using (var stream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                CompressGZip(stream, msg);
            }
        }
        
        public static void CompressGZip(Stream stream, IMessage msg)
        {
            using (var ms = new MemoryStream())
            {
                msg.WriteTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                GZip.Compress(ms, stream, false, 512, 9);
            }
        }

        static T DeserializeFromStream<T>(TextAsset asset) where T: IMessage
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

        public static T Decompress<T>(string fileName, MessageParser<T> parser) where T : IMessage<T>
        {
            using (var compressed = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var decompressed = new MemoryStream())
                {
                    GZip.Decompress(compressed, decompressed, false);
                    decompressed.Seek(0, SeekOrigin.Begin);

                    return parser.ParseFrom(decompressed);
                }
            }
        }

        public static T Decompress<T>(TextAsset asset, MessageParser<T> parser) where T: IMessage<T>
        {
            using (var compressed = new MemoryStream(asset.bytes))
            {
                using (var decompressed = new MemoryStream())
                {
                    GZip.Decompress(compressed, decompressed, false);
                    decompressed.Seek(0, SeekOrigin.Begin);

                    return parser.ParseFrom(decompressed);
                }
            }
        }

        public static void SaveMapLayout(Map map)
        {
            var dst = $"Assets/Resources/Maps/{map.name}";
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(dst))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Maps", map.name);
            }
#endif

            string fileName = $"{dst}/MapData.bytes";

            var sm = GetProtobufMap(map);
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                CompressGZip(stream, sm);
            }
        }

        public static IEnumerator UpdateMapBackground(Map map)
        {
            var resourceName = $"Maps/{map.name}/MapData";
            var asyncResource = Resources.LoadAsync(resourceName);

            yield return asyncResource;

            if (asyncResource.asset == null)
            {
                yield break;
            }

            var fileResource = (TextAsset)asyncResource.asset;
            var serializedMap = Decompress(fileResource, Serialization.Map.Parser);
            if (serializedMap.Triple.Deserialize() != SaveFormatVersion)
            {
                Debug.LogError("Incompatible save format version!");
                yield break;
            }

            serializedMap.BackgroundTileSize = map.tileSize;
            serializedMap.BackgroundMin = new Serialization.Vector2
            {
                X = (map.tilesWidth * map.tileSize) - map.width,
                Y = (map.tilesHeight * map.tileSize) - map.height,
            };
            serializedMap.BackgroundSize = new Serialization.Vector2
            {
                X = map.tilesWidth,
                Y = map.tilesHeight,
            };

            var fileName = $"Assets/Resources/Maps/{map.name}/MapData.bytes";
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                CompressGZip(stream, serializedMap);
            }
        }

        public static void SaveMapData(Map map, string saveFile = null)
        {
            var fileName = "Assets/Resources/Saves";

            if (!string.IsNullOrEmpty(GameController.instance.missionToLoad))
            {
                fileName += $"/{GameController.instance.missionToLoad}";
            }

            if (!Directory.Exists(fileName))
            {
                Directory.CreateDirectory(fileName);
            }

            if (saveFile != null)
            {
                fileName += $"/{saveFile}";
            }
            else
            {
                fileName += $"/{DateTime.Now.Ticks}";
            }

            fileName += ".bytes";

            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                CompressGZip(stream, GetProtobufSaveFile(map));
            }
        }

        static IEnumerator LoadMap(Map map, string mapName)
        {
            var resourceName = $"Maps/{mapName}/MapData";
            var asyncResource = Resources.LoadAsync(resourceName);

            yield return asyncResource;
            yield return LoadMap(map, mapName, asyncResource);
        }

        static IEnumerator LoadMap(Map map, string mapName, ResourceRequest asyncResource)
        {
            if (asyncResource.asset == null)
            {
                yield break;
            }

            var fileResource = (TextAsset)asyncResource.asset;
            var serializedMap = Decompress(fileResource, Serialization.Map.Parser);
            if (serializedMap.Triple.Deserialize() != SaveFormatVersion)
            {
                Debug.LogError("Incompatible save format version!");
                yield break;
            }

            map.boundaryPositions = serializedMap.BoundaryPositions.Select(
                p => p.Deserialize()).ToArray();

            // Initialize the map tiles.
            for (int x = 0; x < map.tilesWidth; ++x)
            {
                for (int y = 0; y < map.tilesHeight; ++y)
                {
                    map.GetTile(x, y).Initialize(map, x, y);
                }
            }

            map.input.camera.transform.position = map.startingCameraPos;
            map.input.UpdateZoomLevels(map);

            yield return DeserializeGameObjects(map, serializedMap);
            yield return InitializeGameObjects(map, serializedMap);
        }

        static IEnumerator DeserializeGameObjects(Map map, Serialization.Map serializedMap)
        {
            // Deserialize buildings.
            foreach (var b in serializedMap.Buildings)
            {
                Building.Deserialize(b, map);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Deserialize intersection patterns without their segments.
            foreach (var pattern in serializedMap.IntersectionPatterns)
            {
                IntersectionPattern.Deserialize(pattern, map);
            }

            // Deserialize raw intersections without their intersecting streets.
            foreach (var inter in serializedMap.StreetIntersections)
            {
                StreetIntersection.Deserialize(inter, map);
            }

            // Deserialize traffic lights.
            var trafficSim = GameController.instance.sim.trafficSim;
            foreach (var data in serializedMap.TrafficLights)
            {
                var tl = new TrafficLight(data);
                trafficSim.trafficLights.Add(tl.Id, tl);
            }

            // Deserialize natural features.
            foreach (var feature in serializedMap.NaturalFeatures)
            {
                NaturalFeature.Deserialize(feature, map);
            }
        }

        static IEnumerator InitializeGameObjects(Map map, Serialization.Map serializedMap)
        {
            // Deserialize streets along with their segments.
            foreach (var street in serializedMap.Streets)
            {
                Street.Deserialize(street, map);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Initialize patterns.
            foreach (var pattern in serializedMap.IntersectionPatterns)
            {
                map.IntersectionPatterns[pattern.ID].Initialize(pattern, map);
            }

            // Connect buildings to their streets.
            foreach (var building in map.buildings)
            {
                building.street = map.GetMapObject<StreetSegment>(building.streetID);
            }
        }

        public static IEnumerator LoadSave(Map map, string mapName, bool pauseAfterLoad = false)
        {
            bool wasPaused = GameController.instance.Paused;
            if (!wasPaused)
            {
                GameController.instance.EnterPause();
            }

            var resourceName = "Saves/" + mapName;
            var asyncResource = Resources.LoadAsync(resourceName);

            yield return asyncResource;
            yield return LoadSave(map, asyncResource);
            yield return map.DoFinalize(thresholdTime);

            if (!wasPaused && !pauseAfterLoad)
            {
                GameController.instance.ExitPause();
            }
        }

        static IEnumerator LoadSave(Map map, ResourceRequest asyncResource)
        {
            var fileResource = (TextAsset)asyncResource.asset;
            if (fileResource == null)
            {
                yield break;
            }

            var saveFile = Decompress(fileResource, Serialization.SaveFile.Parser);
            if (saveFile.Triple.Deserialize() != SaveFormatVersion)
            {
                Debug.LogError("Incompatible save format version!");
                yield break;
            }

            yield return DeserializeGameObjects(map, saveFile);
            yield return InitializeGameObjects(map, saveFile);

            var sim = GameController.instance.sim;
            if (saveFile.GameTime != 0)
            {
                sim.GameTime = new DateTime((long)saveFile.GameTime);
            }

            if (saveFile.Finances != null)
            {
                var finances = GameController.instance.financeController;
                finances.Money = saveFile.Finances.Money.Deserialize();

                foreach (var expense in saveFile.Finances.ExpenseItems)
                {
                    finances.AddExpense(expense.Description, expense.Amount.Deserialize());
                }

                foreach (var earning in saveFile.Finances.EarningItems)
                {
                    finances.AddEarning(earning.Description, earning.Amount.Deserialize());
                }
            }
        }

        static IEnumerator DeserializeGameObjects(Map map, Serialization.SaveFile saveFile)
        {
            // Deserialize raw routes without stops or lines.
            foreach (var route in saveFile.Routes)
            {
                map.CreateRoute((int)route.MapObject.Id);
            }

            // Deserialize raw routes without routes.
            foreach (var stop in saveFile.Stops)
            {
                map.GetOrCreateStop((Stop.StopType)stop.Type, stop.MapObject.Name, stop.Position.Deserialize(),
                                    (int)stop.MapObject.Id);
            }

            // Deserialize raw lines without stops or routes.
            foreach (var line in saveFile.Lines)
            {
                map.CreateLine((TransitType)line.Type, line.MapObject.Name, line.Color.Deserialize(),
                               (int)line.MapObject.Id).Finish();
            }

            // Deserialize raw citizens.
            foreach (var citizen in saveFile.Citizens)
            {
                var c = new Citizen(GameController.instance.sim, citizen);
                c.Initialize();
            }

            // Deserialize raw cars.
            foreach (var car in saveFile.Cars)
            {
                GameController.instance.sim.CreateCar(car);
            }

            // Deserialize active paths.
            foreach (var path in saveFile.ActivePaths)
            {
                var ap = ActivePath.Deserialize(path);
                ap.gameObject.SetActive(true);
                ap.ContinuePath();
            }

            yield break;
        }

        static IEnumerator InitializeGameObjects(Map map, Serialization.SaveFile saveFile)
        {
            // Connect stops with their lines.
            foreach (var stop in saveFile.Stops)
            {
                map.GetMapObject<Stop>((int)stop.MapObject.Id).Deserialize(stop, map);
            }

            // Connect routes with streets and initialize the meshes.
            foreach (var route in saveFile.Routes)
            {
                map.GetMapObject<Route>((int)route.MapObject.Id).Deserialize(route, map);

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            // Connect lines with their routes.
            foreach (var line in saveFile.Lines)
            {
                var createdLine = map.GetMapObject<Line>((int)line.MapObject.Id);
                createdLine.Deserialize(line, map);
            }

            // Initialize the map tiles.
            for (var x = 0; x < saveFile.Tiles.Count; ++x)
            {
                for (var y = 0; y < saveFile.Tiles[x].Tiles.Count; ++y)
                {
                    var tile = saveFile.Tiles[x].Tiles[y];
                    map.GetTile((int)tile.X, (int)tile.Y).Deserialize(tile);
                }
            }

            // Initialize citizens.
            foreach (var citizen in saveFile.Citizens)
            {
                GameController.instance.sim.citizens[citizen.Id].Finalize(citizen);
            }
        }

        public static Map CreateMap(GameController game, string saveName)
        {
            var mapPrefab = Resources.Load($"Maps/{saveName}/{saveName}") as GameObject;
            var mapObj = GameObject.Instantiate(mapPrefab);

            var map = mapObj.GetComponent<Map>();
            map.Initialize(saveName, game.input);
            map.isLoadedFromSaveFile = true;
            loadedMap = map;

            return map;
        }

        public static IEnumerator LoadSave(GameController game, Map map, string saveName = "")
        {
            yield return LoadMap(map, map.name);
            yield return LoadSave(map, string.IsNullOrEmpty(saveName) ? map.name : saveName);
        }
    }
}
