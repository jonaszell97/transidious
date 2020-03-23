using Google.Protobuf;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using ICSharpCode.SharpZipLib.GZip;

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
            };

            result.Buildings.AddRange(map.buildings.Select(b => b.ToProtobuf()));
            result.Streets.AddRange(map.streets.Select(b => b.ToProtobuf()));
            result.StreetIntersections.AddRange(map.streetIntersections.Select(b => b.ToProtobuf()));
            result.NaturalFeatures.AddRange(map.naturalFeatures.Select(b => b.ToProtobuf()));

            result.BoundaryPositions.AddRange(map.boundaryPositions?.Select(v => v.ToProtobuf()));
            result.BoundaryMeshes.Add(map.boundaryBackgroundObj?.GetComponent<MeshFilter>().mesh.ToProtobuf2D());
            result.BoundaryMeshes.Add(map.boundaryOutlineObj?.GetComponent<MeshFilter>().mesh.ToProtobuf2D());
            result.BoundaryMeshes.Add(map.boundaryMaskObj?.GetComponent<MeshFilter>().mesh.ToProtobuf2D());

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

                    if (c.Value.activePath != null)
                    {
                        saveFile.ActivePaths.Add(c.Value.activePath.Serialize());
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
            string fileName = $"Assets/Resources/Maps/{map.name}.bytes";

            var sm = GetProtobufMap(map);
            using (Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                CompressGZip(stream, sm);
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
            var resourceName = "Maps/" + mapName;
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

            //map.minX = serializedMap.MinX;
            //map.maxX = serializedMap.MaxX;
            //map.minY = serializedMap.MinY;
            //map.maxY = serializedMap.MaxY;
            //map.width = map.maxX - map.minX;
            //map.height = map.maxY - map.minY;

            //map.UpdateBoundary(
            //    serializedMap.BoundaryMeshes[0].Deserialize(Map.Layer(MapLayer.Boundary)),
            //    serializedMap.BoundaryMeshes[1].Deserialize(Map.Layer(MapLayer.Foreground)),
            //    serializedMap.BoundaryMeshes[2].Deserialize(Map.Layer(MapLayer.Boundary)),
            //    serializedMap.MinX, serializedMap.MaxX,
            //    serializedMap.MinY, serializedMap.MaxY);

            //map.boundaryPositions = serializedMap.BoundaryPositions.Select(
            //    p => p.Deserialize()).ToArray();

            //for (var x = 0; x < map.tilesWidth; ++x)
            //{
            //    for (var y = 0; y < map.tilesHeight; ++y)
            //    {

            //    }
            //}

            // Initialize the map tiles.
            for (int x = 0; x < map.tilesWidth; ++x)
            {
                for (int y = 0; y < map.tilesHeight; ++y)
                {
                    map.GetTile(x, y).Initialize(map, x, y);
                }
            }

            map.sharedTile.Initialize(map, -1, -1);

            Camera.main.transform.position = map.startingCameraPos;
            map.input.minX = map.minX - 1000f;
            map.input.maxX = map.maxX + 1000f;
            map.input.minY = map.minY - 1000f;
            map.input.maxY = map.maxY + 1000f;
            map.input.UpdateZoomLevels();

            //Camera.main.transform.position = new Vector3(
            //    serializedMap.MinX + (serializedMap.MaxX - serializedMap.MinX) / 2f,
            //    serializedMap.MinY + (serializedMap.MaxY - serializedMap.MinY) / 2f,
            //    Camera.main.transform.position.z);

            // map.input.UpdateZoomLevels();
            //map.startingCameraPos = Camera.main.transform.position;

            yield return DeserializeGameObjects(map, serializedMap);
            yield return InitializeGameObjects(map, serializedMap);

            // LoadBackgroundSprite(map, ref map.backgroundSpriteDay);
            LoadMiniMapSprite(map);
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

            // Deserialize raw intersections without their intersecting streets.
            foreach (var inter in serializedMap.StreetIntersections)
            {
                StreetIntersection.Deserialize(inter, map);
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

            // Connect buildings to their streets.
            foreach (var building in map.buildings)
            {
                building.street = map.GetMapObject<StreetSegment>(building.streetID);
            }
        }

        static void LoadMiniMapSprite(Map map)
        {
            var sprite = SpriteManager.GetSprite($"Maps/{map.name}/minimap");
            UIMiniMap.mapSprite = sprite;
        }

#if false
        static void LoadBackgroundSprite(Map map, ref GameObject gameObject)
        {
            var sprite = SpriteManager.GetSprite($"Maps/{map.name}/LOD");

            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
            gameObject.name = "LOD Background";

            var transform = gameObject.transform;
            var spriteBounds = sprite.bounds;
            var desiredWidth = map.width;
            var desiredHeight = map.height;

            transform.position = new Vector3(map.minX + map.width * .5f,
                                             map.minY + map.height * .5f,
                                             Map.Layer(MapLayer.Parks));

            transform.localScale = new Vector3(desiredWidth / spriteBounds.size.x,
                                               desiredHeight / spriteBounds.size.y,
                                               1f);

            gameObject.SetActive(false);
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

            int minX = 0, minY = 0;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0, 0), 100f);

            gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
            gameObject.name = "LOD Background (" + mode.ToString() + ")";

            var transform = gameObject.transform;
            var spriteBounds = sprite.bounds;
            var desiredWidth = map.width;
            var desiredHeight = map.height;

            transform.position = new Vector3(map.minX + minX, map.minY + minY, Map.Layer(MapLayer.Parks));
            transform.localScale = new Vector3(desiredWidth / spriteBounds.size.x,
                                               desiredHeight / spriteBounds.size.y,
                                               1f);

            gameObject.SetActive(false);
        }
#endif

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
                map.GetOrCreateStop(stop.MapObject.Name, stop.Position.Deserialize(),
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
                GameController.instance.sim.CreateCitizen(citizen);
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
            for (int x = 0; x < saveFile.Tiles.Count; ++x)
            {
                for (int y = 0; y < saveFile.Tiles[x].Tiles.Count; ++y)
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

            yield break;
        }

        public static Map CreateMap(GameController game, string saveName)
        {
            var mapPrefab = Resources.Load($"Prefabs/Maps/{saveName}") as GameObject;
            var mapObj = GameObject.Instantiate(mapPrefab);

            var map = mapObj.GetComponent<Map>();
            map.name = saveName;
            map.input = game.input;
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
