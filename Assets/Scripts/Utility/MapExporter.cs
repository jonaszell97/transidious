#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using UnityMeshSimplifier;
using Color = UnityEngine.Color;

namespace Transidious
{
    public class MapExporter
    {
        public class MeshInfo
        {
            internal float layer;
            internal UnityEngine.Color color;
            internal PSLG pslg;

            public IEnumerable<Vector2[]> Outlines => pslg.VertexLoops;

            public IEnumerable<Vector2[]> Holes => pslg.Holes;

            public MeshInfo(PSLG pslg, float layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;
                this.pslg = pslg;
            }

            public MeshInfo(Vector2[] poly, float layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;
                this.pslg = new PSLG();
                this.pslg.AddOrderedVertices(poly.Select(v => (Vector3)v).ToArray());
            }

            public MeshInfo(Vector3[] poly, float layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;
                this.pslg = new PSLG();
                this.pslg.AddOrderedVertices(poly);
            }
        }

        public class Statistics
        {
            public Dictionary<string, Tuple<int, int>> totalVerts;

            public Statistics()
            {
                this.totalVerts = new Dictionary<string, Tuple<int, int>>();
            }

            public void AddMesh(string group, Mesh mesh)
            {
                if (totalVerts.TryGetValue(group, out Tuple<int, int> value))
                {
                    totalVerts[group] = Tuple.Create(value.Item1 + mesh.vertexCount,
                        value.Item2 + mesh.triangles.Length);
                }
                else
                {
                    totalVerts.Add(group, Tuple.Create(mesh.vertexCount,
                        mesh.triangles.Length));
                }
            }
        }

        private Map map;
        private int resolution;
        public readonly Dictionary<IMapObject, MeshInfo> meshInfo;
        private readonly List<Tuple<MapTile[], MeshInfo>> _otherMeshes;
        private readonly Dictionary<StreetSegment, Tuple<Mesh, Mesh>> _streetMeshes;
        private readonly Dictionary<MapTile, PSLG> _tileCutouts;
        private readonly Dictionary<MapTile, List<MeshFilter>> _tileMeshes;
        readonly Statistics _stats;

        List<Tuple<IMapObject, MeshInfo>> _sortedMeshes;
        List<Tuple<IMapObject, MeshInfo>> SortedMeshes
        {
            get
            {
                if (_sortedMeshes == null)
                {
                    _sortedMeshes = meshInfo.Select(k => Tuple.Create(k.Key, k.Value)).ToList();
                    _sortedMeshes.Sort((e1, e2) => e2.Item2.layer.CompareTo(e1.Item2.layer));
                }

                return _sortedMeshes;
            }
        }

        public MapExporter(Map map, int resolution = 4096)
        {
            this.map = map;
            this.resolution = resolution;
            this.meshInfo = new Dictionary<IMapObject, MeshInfo>();
            this._stats = new Statistics();
            this._otherMeshes = new List<Tuple<MapTile[], MeshInfo>>();
            this._streetMeshes = new Dictionary<StreetSegment, Tuple<Mesh, Mesh>>();
            this._tileCutouts = new Dictionary<MapTile, PSLG>();
            this._tileMeshes = new Dictionary<MapTile, List<MeshFilter>>();
        }

        public void RegisterMesh(IMapObject obj, PSLG pslg,
                                 float layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj, new MeshInfo(pslg, layer, color));
        }

        public void RegisterMesh(IMapObject obj, Vector2[] poly,
                                 float layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj, new MeshInfo(poly, layer, color));
        }

        public void RegisterMesh(IMapObject obj, Vector3[] poly,
                                 float layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj, new MeshInfo(poly, layer, color));
        }

        public void RegisterMesh(PSLG pslg, float layer, UnityEngine.Color color)
        {
            var tiles = map.GetTiles(pslg.Outlines);
            this._otherMeshes.Add(Tuple.Create(tiles.ToArray(), new MeshInfo(pslg, layer, color)));
        }

        public void RegisterMesh(Vector2[] poly, float layer, UnityEngine.Color color)
        {
            var tiles = map.GetTiles(poly);
            this._otherMeshes.Add(Tuple.Create(tiles.ToArray(), new MeshInfo(poly, layer, color)));
        }

        public void RegisterMesh(Vector3[] poly, float layer, UnityEngine.Color color)
        {
            var tiles = map.GetTiles(poly);
            this._otherMeshes.Add(Tuple.Create(tiles.ToArray(), new MeshInfo(poly, layer, color)));
        }

        Tuple<Mesh, Mesh> GetStreetMesh(StreetSegment seg)
        {
            if (_streetMeshes.TryGetValue(seg, out Tuple<Mesh, Mesh> meshes))
            {
                return meshes;
            }

            meshes = seg.CreateMeshes();
            _streetMeshes.Add(seg, meshes);
            
            return meshes;
        }

        public void ExportMap(string fileName, int backgroundBlur = 0)
        {
            bool isBackground = backgroundBlur != 0;

            // FIXME
            var prevStatus = GameController.instance.status;
            GameController.instance.status = GameController.GameStatus.Disabled;

            if (isBackground)
            {
                System.IO.Directory.CreateDirectory($"Assets/Resources/Maps/{fileName}/Backgrounds");
            }
            else
            {
                System.IO.Directory.CreateDirectory($"Assets/Resources/Maps/{fileName}");
            }

            using (var drawing = new Bitmap(resolution, resolution))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(drawing))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;

                    for (var x = 0; x < map.tilesWidth; ++x)
                    {
                        for (var y = 0; y < map.tilesHeight; ++y)
                        {
                            var backgroundColor = ToDrawingColor(map.GetDefaultBackgroundColor(MapDisplayMode.Day));
                            graphics.FillRectangle(
                                new SolidBrush(backgroundColor),
                                new Rectangle(0, 0, resolution, resolution));

                            var tile = map.GetTile(x, y);
                            if (DrawTile(tile, graphics, false))
                            {
                                if (isBackground)
                                {
                                    var assetName = $"Assets/Resources/Maps/{fileName}/Backgrounds/{x}_{y}.png";
                                    using (var blurred = Blur(drawing, backgroundBlur))
                                    {
                                        blurred.Save(assetName);
                                    }
                                }
                                else
                                {
                                    var assetName = $"Assets/Resources/Maps/{fileName}/{x}_{y}.png";
                                    drawing.Save(assetName);
                                }
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var tile in map.tiles)
            {
                tile.UpdateSprite();
            }

            // FIXME
            GameController.instance.status = prevStatus;
        }

        public void ExportMinimap(string fileName, int resolution = 2048)
        {
            using (var drawing = new Bitmap(resolution, resolution))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(drawing))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.FillRectangle(
                        Brushes.Transparent,
                        new Rectangle(0, 0, resolution, resolution));

                    System.IO.Directory.CreateDirectory($"Assets/Resources/Maps/{fileName}");
                    DrawMinimap(graphics, resolution);

                    var assetName = $"Assets/Resources/Maps/{fileName}/minimap.png";
                    drawing.Save(assetName);
                }
            }
        }

        public void ExportLOD(string fileName, int resolution = 4096)
        {
            using (var drawing = new Bitmap(resolution, resolution))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(drawing))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;

                    var backgroundColor = ToDrawingColor(map.GetDefaultBackgroundColor(MapDisplayMode.Day));
                    graphics.FillRectangle(
                        new SolidBrush(backgroundColor),
                        new Rectangle(0, 0, resolution, resolution));

                    System.IO.Directory.CreateDirectory($"Assets/Resources/Maps/{fileName}");

                    DrawLOD(graphics, resolution);
                    drawing.Save($"Assets/Resources/Maps/{fileName}/LOD.png");
                }
            }
        }

        public System.Drawing.Color ToDrawingColor(UnityEngine.Color c)
        {
            return System.Drawing.Color.FromArgb(
                (int)(c.a * 255f), (int)(c.r * 255f),
                (int)(c.g * 255f), (int)(c.b * 255f));
        }

        PointF GetCoordinate(MapTile tile, Vector3 pos)
        {
            return GetCoordinate(tile, (Vector2)pos);
        }

        PointF GetCoordinate(MapTile tile, Vector2 pos)
        {
            var baseX = tile.x * map.tileSize;
            var baseY = tile.y * map.tileSize;

            return new PointF(
                ((pos.x - baseX) / map.tileSize) * resolution,
                resolution - ((pos.y - baseY) / map.tileSize) * resolution
            );
        }

        static PointF GetGlobalCoordinate(Map map, Vector2 pos, int resolution,
                                          float padding = 0f)
        {
            var aspect = map.width / map.height;
            var resolutionX = aspect >= 1f ? resolution : resolution * aspect;
            var resolutionY = aspect < 1f ? resolution : resolution / aspect;

            var xdiff = resolution - resolutionX;
            var ydiff = resolution - resolutionY;

            var baseX = map.minX - padding;
            var baseY = map.minY - padding;
            var width = map.width + 2 * padding;
            var height = map.height + 2 * padding;

            return new PointF(
                (xdiff * .5f) + ((pos.x - baseX) / width) * resolutionX,
                (ydiff * .5f) + resolutionY - ((pos.y - baseY) / height) * resolutionY
            );
        }

        void FillGlobalPoly(System.Drawing.Graphics g, UnityEngine.Color c, Mesh mesh, int resolution,
                            float padding = 0f)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var brush = new SolidBrush(ToDrawingColor(c));

            var path = new GraphicsPath();
            for (var i = 0; i < tris.Length; i += 3)
            {
                var p0 = verts[tris[i]];
                var p1 = verts[tris[i + 1]];
                var p2 = verts[tris[i + 2]];

                path.AddPolygon(new []
                {
                    GetGlobalCoordinate(map, p0, resolution, padding),
                    GetGlobalCoordinate(map, p1, resolution, padding),
                    GetGlobalCoordinate(map, p2, resolution, padding),
                });
            }

            g.FillPath(brush, path);
        }

        void FillPoly(System.Drawing.Graphics g, UnityEngine.Color c, Mesh mesh, MapTile tile)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var brush = new SolidBrush(ToDrawingColor(c));

            var path = new GraphicsPath();
            for (var i = 0; i < tris.Length; i += 3)
            {
                var p0 = verts[tris[i]];
                var p1 = verts[tris[i + 1]];
                var p2 = verts[tris[i + 2]];

                path.AddPolygon(new []
                {
                    GetCoordinate(tile, p0),
                    GetCoordinate(tile, p1),
                    GetCoordinate(tile, p2),
                });
            }
            
            g.FillPath(brush, path);
        }

        void DrawMesh(System.Drawing.Graphics g, UnityEngine.Color c,
                      MeshInfo info, MapTile tile)
        {
            if (info.pslg == null)
            {
                return;
            }

            var outlines = info.Outlines;
            var holes = info.Holes;

            var path = new GraphicsPath();
            foreach (var outline in outlines)
            {
                path.AddPolygon(outline.Select(v => GetCoordinate(tile, v)).ToArray());
            }

            //var region = new Region(path);
            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    path.AddPolygon(hole.Select(v => GetCoordinate(tile, v)).ToArray());
                }
                //using (var tmpPath = new GraphicsPath())
                //{
                //    foreach (var hole in holes)
                //    {
                //        tmpPath.AddPolygon(hole.Select(v => GetCoordinate(tile, v)).ToArray());
                //    }

                //    try
                //    {
                //        region.Exclude(tmpPath);
                //    }
                //    catch (OutOfMemoryException e)
                //    {
                //        Debug.LogWarning(e.Message);
                //    }
                //}
            }

            g.FillPath(new SolidBrush(ToDrawingColor(c)), path);
            //g.FillRegion(new SolidBrush(ToDrawingColor(c)), region);
        }

        void DrawGlobalMesh(System.Drawing.Graphics g, UnityEngine.Color c,
                            MeshInfo info, int resolution, float padding = 0f)
        {
            if (info.pslg == null)
            {
                return;
            }

            var outlines = info.Outlines;
            var holes = info.Holes;

            var path = new GraphicsPath();
            foreach (var outline in outlines)
            {
                path.AddPolygon(outline.Select(v =>
                    GetGlobalCoordinate(map, v, resolution, padding)).ToArray());
            }

            var region = new Region(path);
            if (holes != null)
            {
                using (var tmpPath = new GraphicsPath())
                {
                    foreach (var hole in holes)
                    {
                        tmpPath.AddPolygon(hole.Select(v =>
                            GetGlobalCoordinate(map, v, resolution, padding)).ToArray());
                    }

                    try
                    {
                        region.Exclude(tmpPath);
                    }
                    catch (OutOfMemoryException e)
                    {
                        Debug.LogWarning(e.Message);
                    }
                }
            }

            g.FillRegion(new SolidBrush(ToDrawingColor(c)), region);
        }

        void DrawGobalLine(System.Drawing.Graphics g, Vector2[] points,
                           UnityEngine.Color c, float width, int resolution)
        {
            var brush = new SolidBrush(ToDrawingColor(c));
            using (var pen = new Pen(brush, width))
            {
                for (var i = 0; i < points.Length - 1; ++i)
                {
                    var p0 = points[i];
                    var p1 = points[i + 1];

                    var coord0 = GetGlobalCoordinate(map, p0, resolution);
                    var coord1 = GetGlobalCoordinate(map, p1, resolution);

                    g.DrawLine(pen, coord0, coord1);
                    g.FillEllipse(brush, new RectangleF(coord0.X - (width * .5f),
                                                        coord0.Y - (width * .5f),
                                                        width, width));
                }
            }
        }

        void EraseBoundary(MapTile tile, Vector2[] boundaryPositions, System.Drawing.Graphics g)
        {
            var path = new GraphicsPath();
            path.AddPolygon(boundaryPositions.Select(v => GetCoordinate(tile, v)).ToArray());
            g.FillPath(new SolidBrush(System.Drawing.Color.Transparent), path);
        }
        
        bool DrawTile(MapTile tile, System.Drawing.Graphics g, bool global)
        {
            var objectsToDraw = new HashSet<IMapObject>();
            var streetsToDraw = new HashSet<StreetSegment>();
            
            foreach (var obj in tile.mapObjects)
            {
                if (!meshInfo.ContainsKey(obj))
                {
                    continue;
                }

                if (obj is StreetSegment seg)
                {
                    streetsToDraw.Add(seg);
                }
                else
                {
                    objectsToDraw.Add(obj);
                }
            }

            var allMeshes = SortedMeshes.ToList();
            allMeshes.AddRange(_otherMeshes.Select(m => Tuple.Create((IMapObject)null, m.Item2)));
            allMeshes.Sort((e1, e2) => e2.Item2.layer.CompareTo(e1.Item2.layer));

            var drewSomething = objectsToDraw.Count > 0 || streetsToDraw.Count > 0;
            foreach (var entry in allMeshes)
            {
                if (entry.Item1 != null && !objectsToDraw.Contains(entry.Item1))
                {
                    continue;
                }

                drewSomething = true;
                
                if (global)
                {
                    DrawGlobalMesh(g, entry.Item2.color, entry.Item2, resolution);
                }
                else
                {
                    DrawMesh(g, entry.Item2.color, entry.Item2, tile);
                }
            }

            foreach (var seg in streetsToDraw)
            {
                var meshes = GetStreetMesh(seg);
                var outlineColor = seg.GetBorderColor();

                if (meshes.Item2 == null)
                    continue;
                
                if (global)
                {
                    FillGlobalPoly(g, outlineColor, meshes.Item2, resolution);
                }
                else
                {
                    FillPoly(g, outlineColor, meshes.Item2, tile);
                }
            }

            foreach (var seg in streetsToDraw)
            {
                var meshes = GetStreetMesh(seg);
                var streetColor = seg.GetStreetColor();
                
                if (global)
                {
                    FillGlobalPoly(g, streetColor, meshes.Item1, resolution);
                }
                else
                {
                    FillPoly(g, streetColor, meshes.Item1, tile);
                }
            }

            return drewSomething;
        }

        void DrawMinimap(System.Drawing.Graphics g, int resolution)
        {
            var boundary = map.boundaryPositions;
            var brush = Brushes.Black;
            var width = resolution / 150f;
            var padding = width * .5f;

            var backgroundColor = ToDrawingColor(map.GetDefaultBackgroundColor(MapDisplayMode.Day));
            g.FillPolygon(
                new SolidBrush(backgroundColor), 
                boundary.Select(v => GetGlobalCoordinate(map, v, resolution, padding)).ToArray());

            var allMeshes = SortedMeshes.Select(m => m.Item2).ToList();
            allMeshes.AddRange(_otherMeshes.Select(m => m.Item2));
            allMeshes.Sort((e1, e2) => e2.layer.CompareTo(e1.layer));

            foreach (var info in allMeshes)
            {
                DrawGlobalMesh(g, info.color, info, resolution, padding);
            }
            
            var path = new GraphicsPath();
            path.AddRectangle(new Rectangle(0, 0, resolution - 1, resolution - 1));

            var boundaryPoints = boundary.Select(p => GetGlobalCoordinate(map, p, resolution, padding)).ToArray();
            path.AddPolygon(boundaryPoints);

            g.FillPath(Brushes.Transparent, path);

            using (var pen = new Pen(brush, width))
            {
                for (var i = 0; i < boundary.Length; ++i)
                {
                    var p0 = boundary[i];
                    var p1 = boundary[(i + 1 == boundary.Length) ? 0 : (i + 1)];

                    var coord0 = GetGlobalCoordinate(map, p0, resolution, padding);
                    var coord1 = GetGlobalCoordinate(map, p1, resolution, padding);

                    g.DrawLine(pen, coord0, coord1);
                    g.FillEllipse(brush, new RectangleF(coord0.X - (width * .5f),
                                                        coord0.Y - (width * .5f),
                                                        width, width));
                }
            }
        }

        void DrawLOD(System.Drawing.Graphics g, int resolution)
        {
            foreach (var info in SortedMeshes)
            {
                DrawGlobalMesh(g, info.Item2.color, info.Item2, resolution);
            }
            
            foreach (var info in _otherMeshes)
            {
                DrawGlobalMesh(g, info.Item2.color, info.Item2, resolution);
            }
        }

        Mesh CreateMesh(MeshInfo info, bool fast)
        {
            var pslg = info.pslg;
            return TriangleAPI.CreateMesh(pslg, fast || pslg.Simple);
        }
        
        Mesh CreateMesh(PSLG pslg, bool fast)
        {
            return TriangleAPI.CreateMesh(pslg, fast || pslg.Simple);
        }

        float GetIntensity(MeshInfo info)
        {
            if (info.pslg == null)
                return 0;

            var n = info.Outlines.Sum(outline => outline.Length);
            n += info.Holes.Sum(hole => hole.Length);

            return n;
        }

        public void AddColorDensity()
        {
            var maxIntensity = meshInfo.Aggregate(0f,
                (current, info) => System.Math.Max(current, GetIntensity(info.Value)));
            maxIntensity = _otherMeshes.Aggregate(maxIntensity,
                (current, info) => System.Math.Max(current, GetIntensity(info.Item2)));

            foreach (var key in meshInfo.Keys)
            {
                var info = meshInfo[key];
                info.color = new UnityEngine.Color(GetIntensity(info) / maxIntensity, 0f, 0f);
            }
            foreach (var info in _otherMeshes)
            {
                info.Item2.color = new UnityEngine.Color(GetIntensity(info.Item2) / maxIntensity, 0f, 0f);
            }
        }

        Mesh GetCutoutMesh(MapTile tile, PSLG pslg, bool fast)
        {
            // Cutouts don't work well for polygons with holes.
            if (!pslg.Simple)
                return CreateMesh(pslg, fast);

            if (!_tileCutouts.TryGetValue(tile, out PSLG cutout))
            {
                cutout = new PSLG();

                var minX = tile.x * map.tileSize;
                var maxX = tile.x * map.tileSize + map.tileSize;
                
                var minY = tile.y * map.tileSize;
                var maxY = tile.y * map.tileSize + map.tileSize;
            
                cutout.AddHole(new List<Vector3>
                {
                    new Vector3(minX, minY),
                    new Vector3(minX, maxY),
                    new Vector3(maxX, maxY),
                    new Vector3(maxX, minY),
                    new Vector3(minX, minY),
                });
            
                minX -= 1000f;
                minY -= 1000f;
                maxX += 1000f;
                maxY += 1000f;
            
                cutout.AddVertexLoop(new List<Vector3>
                {
                    new Vector3(minX, minY),
                    new Vector3(minX, maxY),
                    new Vector3(maxX, maxY),
                    new Vector3(maxX, minY),
                    new Vector3(minX, minY),
                });
            
                _tileCutouts.Add(tile, cutout);
            }
            
            var diff = Math.PolygonDiff(pslg, cutout);
            return CreateMesh(diff, fast);
        }

        public IEnumerator CreatePrefabMeshes(bool fast, bool includeFeatures, float thresholdTime = 1000)
        {
            var meshes = new Dictionary<MapTile, List<Tuple<string, Mesh, UnityEngine.Color, float>>>();
            foreach (var tile in map.AllTiles)
            {
                meshes.Add(tile, new List<Tuple<string, Mesh, Color, float>>());
                _tileMeshes.Add(tile, new List<MeshFilter>());
            }

            foreach (var info in meshInfo)
            {
                if (info.Key is StreetSegment seg)
                {
                    var streetMeshes = GetStreetMesh(seg);
                    var streetColor = seg.GetStreetColor();
                    var streetLayer = Map.Layer(MapLayer.Streets);

                    foreach (var tile in map.GetTilesForObject(info.Key))
                    {
                        meshes[tile].Add(Tuple.Create("Streets", streetMeshes.Item1, streetColor, streetLayer));
                    }

                    _stats.AddMesh("Streets", streetMeshes.Item1);

                    if (streetMeshes.Item2 != null)
                    {
                        var outlineColor = seg.GetBorderColor();
                        var outlineLayer = Map.Layer(MapLayer.StreetOutlines);
                        
                        foreach (var tile in map.GetTilesForObject(info.Key))
                        {
                            meshes[tile].Add(Tuple.Create("Streets", streetMeshes.Item2, outlineColor, outlineLayer));
                        }
                        
                        _stats.AddMesh("StreetOutlines", streetMeshes.Item2);
                    }

                    continue;
                }

                if (!includeFeatures)
                {
                    continue;
                }

                string group;
                UnityEngine.Color color;
                float layer;

                if (info.Key is Building building)
                {
                    group = "Buildings";
                    color = building.GetColor();
                    layer = Map.Layer(MapLayer.Buildings);
                }
                else if (info.Key is NaturalFeature feature)
                {
                    group = "NaturalFeatures";
                    color = feature.GetColor();
                    layer = feature.GetLayer();
                }
                else
                {
                    Debug.LogWarning("unrecognised map object");
                    continue;
                }

                if (info.Key.UniqueTile != null)
                {
                    var mesh = CreateMesh(info.Value.pslg, fast);
                    meshes[info.Key.UniqueTile].Add(Tuple.Create(group, mesh, color, layer));
                    _stats.AddMesh(group, mesh);
                }
                else
                {
                    foreach (var tile in map.GetTilesForObject(info.Key))
                    {
                        var mesh = GetCutoutMesh(tile, info.Value.pslg, fast);
                        meshes[tile].Add(Tuple.Create(group, mesh, color, layer));
                        _stats.AddMesh(group, mesh);
                    }
                }

                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }

            if (includeFeatures)
            {
                foreach (var info in _otherMeshes)
                {
                    if (info.Item1.Length == 1)
                    {
                        var mesh = CreateMesh(info.Item2.pslg, fast);
                        meshes[info.Item1[0]].Add(Tuple.Create("Uncategorised", mesh, info.Item2.color, info.Item2.layer));
                        _stats.AddMesh("Uncategorised", mesh);
                    }
                    else
                    {
                        foreach (var tile in info.Item1)
                        {
                            var mesh = GetCutoutMesh(tile, info.Item2.pslg, fast);
                            meshes[tile].Add(Tuple.Create("Uncategorised", mesh, info.Item2.color, info.Item2.layer));
                            _stats.AddMesh("Uncategorised", mesh);
                        }
                    }

                    if (FrameTimer.instance.FrameDuration >= thresholdTime)
                    {
                        yield return null;
                    }
                }
            }

            var groups = new Dictionary<Tuple<string, UnityEngine.Color, float>, List<Mesh>>();
            foreach (var (tile, meshData) in meshes)
            {
                var rect = new Rect(tile.x * map.tileSize, tile.y * map.tileSize, 
                                    map.tileSize, map.tileSize);
                
                foreach (var data in meshData)
                {
                    var key = Tuple.Create(data.Item1, data.Item3, data.Item4);
                    if (!groups.TryGetValue(key, out List<Mesh> list))
                    {
                        list = new List<Mesh>();
                        groups.Add(key, list);
                    }

                    list.Add(data.Item2);
                }

                foreach (var (key, value) in groups)
                {
                    var obj = new GameObject
                    {
                        name = $"{key.Item1} {ColorUtility.ToHtmlStringRGB(key.Item2)} {key.Item3:n0}"
                    };

                    obj.transform.SetParent(tile.meshes.transform);
                    obj.transform.position = new Vector3(0f, 0f, key.Item3);

                    var mr = obj.AddComponent<MeshRenderer>();
                    mr.material = GameController.instance.GetUnlitMaterial(key.Item2);
                    
                    var combinedMesh = MeshBuilder.CombineMeshes(value);
                    if (combinedMesh.vertexCount >= MultiMesh.maxMeshVertices)
                    {
                        Debug.LogWarning($"too many vertices {combinedMesh.vertexCount}");
                    }
                    
                    var mf = obj.AddComponent<MeshFilter>();
                    mf.sharedMesh = combinedMesh;
                    
                    _tileMeshes[tile].Add(mf);
                }

                groups.Clear();
            }
        }

        void SaveMesh(GameObject obj, string fileName)
        {
            SaveMesh(obj.GetComponent<MeshFilter>().sharedMesh, fileName);
        }
        
        void SaveMesh(Mesh mesh, string fileName)
        {
            AssetDatabase.CreateAsset(mesh, fileName);
        }

        void SaveMaterial(Material mat, string name)
        {
            AssetDatabase.CreateAsset(mat, $"Assets/Resources/Materials/Textures/{name}");
        }

        public void ExportMapPrefab()
        {
            string meshPath = $"Assets/Resources/Maps/{map.name}/Meshes";
            if (!AssetDatabase.IsValidFolder(meshPath))
            {
                AssetDatabase.CreateFolder($"Assets/Resources/Maps/{map.name}", "Meshes");
            }

            // Save multi meshes.
            foreach (var tile in map.AllTiles)
            {
                foreach (var mesh in _tileMeshes[tile])
                {
                    SaveMesh(mesh.sharedMesh, $"{meshPath}/{tile.x}_{tile.y}_{mesh.name}.asset");
                }

                tile.gameObject.SetActive(false);
            }

            // Save boundary outline.
            SaveMesh(map.boundaryOutlineObj, $"{meshPath}/BoundaryOutline.asset");

            // Save boundary background.
            SaveMesh(map.boundaryBackgroundObj, $"{meshPath}/BoundaryBackground.asset");

            PrefabUtility.SaveAsPrefabAsset(
                map.gameObject, $"Assets/Resources/Maps/{map.name}/{map.name}.prefab");
        }

        public void InitializeBackground(Map prefabMap,
                                         float backgroundTileSize,
                                         Vector2 backgroundOffset,
                                         Vector2 backgroundSize)
        {
            string meshPath = $"Assets/Resources/Maps/{map.name}/Meshes";
            
            var xpos = -(backgroundSize.x * backgroundTileSize * .5f) + (prefabMap.width * .5f) + (backgroundOffset.x * .5f);
            var ypos = -(backgroundSize.y * backgroundTileSize * .5f) + (prefabMap.height * .5f) + (backgroundOffset.y * .5f);

            var maxXpos = xpos + backgroundSize.x * backgroundTileSize - backgroundOffset.x;
            var maxYpos = ypos + backgroundSize.y * backgroundTileSize - backgroundOffset.y;

            var sh = Shader.Find("Unlit/Texture");

            var boundaryPSLG = new PSLG(Map.Layer(MapLayer.Foreground));
            boundaryPSLG.AddVertexLoop(prefabMap.boundaryPositions.Select(v => (Vector3)v).ToList());

            var uv = new List<Vector2>();
            for (var x = 0; x < backgroundSize.x; ++x)
            {
                for (var y = 0; y < backgroundSize.y; ++y)
                {
                    uv.Clear();

                    var sprite = SpriteManager.GetSprite($"Maps/{map.name}/Backgrounds/{x}_{y}");
                    var c = Color.white;

                    if (sprite == null)
                    {
                        c = map.GetDefaultBackgroundColor(MapDisplayMode.Day);
                        sprite = Resources.Load<Sprite>("Sprites/ui_square");
                    }

                    var min_x = xpos + x * backgroundTileSize;
                    var max_x = Mathf.Min(maxXpos, min_x + backgroundTileSize);
                    var min_y = ypos + y * backgroundTileSize;
                    var max_y = Mathf.Min(maxYpos, min_y + backgroundTileSize);

                    var basePSLG = new PSLG(Map.Layer(MapLayer.Foreground));
                    basePSLG.AddOrderedVertices(new []
                    {
                        new Vector3(min_x, min_y),
                        new Vector3(min_x, max_y),
                        new Vector3(max_x, max_y),
                        new Vector3(max_x, min_y),
                    });

                    var pslg = Math.PolygonDiff(basePSLG, boundaryPSLG);
                    var mesh = TriangleAPI.CreateMesh(pslg, true);

                    var verts = mesh.vertices;
                    foreach (var v in verts)
                    {
                        uv.Add(new Vector2((v.x - min_x) / backgroundTileSize, (v.y - min_y) / backgroundTileSize));
                    }

                    mesh.uv = uv.ToArray();

                    var name = $"Background {x} {y}";
                    var tf = prefabMap.transform.Find(name);

                    GameObject obj;
                    if (tf == null)
                    {
                        obj = new GameObject();
                        obj.name = $"Background {x} {y}";
                        obj.AddComponent<MeshFilter>();
                        obj.AddComponent<MeshRenderer>();

                        tf = obj.transform;
                    }
                    else
                    {
                        obj = tf.gameObject;
                    }

                    tf.SetParent(prefabMap.transform);
                    tf.SetLayer(MapLayer.Foreground);

                    var mf = obj.GetComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    var mr = obj.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = new Material(sh) {mainTexture = sprite.texture};

                    SaveMesh(mf.sharedMesh, $"{meshPath}/BG_{x}_{y}.asset");
                    SaveMaterial(mr.sharedMaterial, $"{map.name}_{x}_{y}.mat");
                }
            }
        }

        public void UpdatePrefabBackground()
        {
            var prefab = Resources.Load($"Maps/{map.name}/{map.name}") as GameObject;
            var inst = GameObject.Instantiate(prefab);
            var prefabMap = inst.GetComponent<Map>();

            InitializeBackground(prefabMap, map.tileSize, 
                new Vector2(map.tilesWidth * map.tileSize - map.width, map.tilesHeight * map.tileSize - map.height),
                new Vector2(map.tilesWidth, map.tilesHeight));

            PrefabUtility.SaveAsPrefabAsset(
                inst, $"Assets/Resources/Maps/{map.name}/{map.name}.prefab");

            GameObject.Destroy(inst);
        }

        public void PrintStats()
        {
            var statsStr = new System.Text.StringBuilder();

            statsStr.Append("Vertex Count:");
            foreach (var entry in _stats.totalVerts)
            {
                statsStr.Append("\n");
                statsStr.Append($"   {entry.Key}: {entry.Value.Item1} verts, {entry.Value.Item2} tris");
            }

            statsStr.Append("\n");
            Debug.Log(statsStr.ToString());
        }

        private static float[][] GetGaussianKernel2D(Int32 kernelSize, float sigma = 1f)
        {
            var halfKernelSize = kernelSize / 2;
            var sigmaSquared = Mathf.Pow(sigma, 2);
            var twoSigmaSquared = 2f * sigmaSquared;
            var oneOverSigmaSquaredTwoPi = 1f / (sigmaSquared * Math.TwoPI);
            var kernel = new float[kernelSize][];

            var sum = 0f;
            for (var x = 0; x < kernelSize; ++x)
            {
                kernel[x] = new float[kernelSize];

                for (var y = 0; y < kernelSize; ++y)
                {
                    var xSquared = Mathf.Pow(x - halfKernelSize, 2f);
                    var ySquared = Mathf.Pow(y - halfKernelSize, 2f);

                    kernel[x][y] = oneOverSigmaSquaredTwoPi * Mathf.Pow(
                        (float)System.Math.E, 
                        -((xSquared + ySquared) / twoSigmaSquared));

                    sum += kernel[x][y];
                }
            }

            var missing = 1f - sum;
            for (var x = 0; x < kernelSize; ++x)
            {
                for (var y = 0; y < kernelSize; ++y)
                {
                    kernel[x][y] += (kernel[x][y] / sum) * missing;
                }
            }

            return kernel;
        }
        
        private static float[][] GetGaussianKernel1D(Int32 kernelSize, float sigma = 1f)
        {
            var halfKernelSize = kernelSize / 2;
            var sigmaSquared = Mathf.Pow(sigma, 2);
            var twoSigmaSquared = 2f * sigmaSquared;
            var oneOverSigmaSquaredTwoPi = 1f / (sigmaSquared * Math.TwoPI);
            var kernel = new float[kernelSize][];

            var sum = 0f;
            for (var x = 0; x < kernelSize; ++x)
            {
                kernel[x] = new float[1];

                var xSquared = Mathf.Pow(x - halfKernelSize, 2f);
                kernel[x][0] = oneOverSigmaSquaredTwoPi * Mathf.Pow(
                                   (float)System.Math.E, 
                                   -(xSquared / twoSigmaSquared));

                sum += kernel[x][0];
            }

            var missing = 1f - sum;
            for (var x = 0; x < kernelSize; ++x)
            {
                kernel[x][0] += (kernel[x][0] / sum) * missing;
            }
            
            return kernel;
        }

        private static float[][] FlipKernel(float[][] kernel)
        {
            var xsize = kernel.Length;
            var ysize = kernel[0].Length;

            var result = new float[ysize][];
            for (var y = 0; y < ysize; ++y)
            {
                result[y] = new float[xsize];

                for (var x = 0; x < xsize; ++x)
                {
                    result[y][x] = kernel[x][y];
                }
            }

            return result;
        }
        
        private static float[][] GetBoxKernel(Int32 kernelSize)
        {
            var k = 1f / (kernelSize * kernelSize);
            var kernel = new float[kernelSize][];

            for (var x = 0; x < kernelSize; ++x)
            {
                kernel[x] = new float[kernelSize];

                for (var y = 0; y < kernelSize; ++y)
                {
                    kernel[x][y] = k;
                }
            }

            return kernel;
        }

        static float getSigmaForKernelSize(Int32 kSize)
        {
            return ((float)kSize - 1f) / 3f;
        }

        private static Bitmap Blur(Bitmap image, Int32 blurSize)
        {
            var kernel = GetGaussianKernel1D(blurSize, getSigmaForKernelSize(blurSize));
            var rect = new Rectangle(0, 0, image.Width, image.Height);

            return Blur(Blur(image, rect, kernel), rect, FlipKernel(kernel));
        }

        private static unsafe Bitmap Blur(Bitmap image, Rectangle rectangle, float[][] kernel)
        {
            int blurSizeX = kernel.Length / 2;
            int blurSizeY = kernel[0].Length / 2;
            int width = image.Width;
            int height = image.Height;

            Bitmap blurred = new Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (var graphics = System.Drawing.Graphics.FromImage(blurred))
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // Lock the bitmap's bits
            BitmapData blurredData = blurred.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, blurred.PixelFormat);
            BitmapData origData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, blurred.PixelFormat);

            // Get bits per pixel for current PixelFormat
            int bitsPerPixel = Image.GetPixelFormatSize(blurred.PixelFormat);
            int bitsPerPixel_orig = Image.GetPixelFormatSize(image.PixelFormat);

            // Get pointer to first line
            byte* scan0 = (byte*)blurredData.Scan0.ToPointer();
            byte* scan0_orig = (byte*)origData.Scan0.ToPointer();

            // look at every pixel in the blur rectangle
            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    float avgR = 0, avgG = 0, avgB = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx - blurSizeX, kx = 0; x <= xx + blurSizeX; x++, kx++)
                    {
                        for (int y = yy - blurSizeY, ky = 0; y <= yy + blurSizeY; y++, ky++)
                        {
                            var safeX = x;
                            var safeY = y;
                            
                            if (x < 0)
                                safeX = 0;
                            else if (x >= width)
                                safeX = width - 1;

                            if (safeY < 0)
                                safeY = 0;
                            else if (y >= height)
                                safeY = height - 1;
    
                            // Get pointer to RGB
                            byte* data = scan0_orig + safeY * origData.Stride + safeX * bitsPerPixel_orig / 8;

                            avgB += data[0] * kernel[kx][ky]; // Blue
                            avgG += data[1] * kernel[kx][ky]; // Green
                            avgR += data[2] * kernel[kx][ky]; // Red
                        }
                    }

                    {
                        byte* data = scan0 + yy * blurredData.Stride + xx * bitsPerPixel / 8;
                        data[0] = (byte) avgB;
                        data[1] = (byte) avgG;
                        data[2] = (byte) avgR;
                    }
                }
            }

            // Unlock the bits
            blurred.UnlockBits(blurredData);
            image.UnlockBits(origData);

            return blurred;
        }
    }
}

#endif