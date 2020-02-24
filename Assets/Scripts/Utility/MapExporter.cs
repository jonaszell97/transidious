using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Transidious
{
    public class MapExporter
    {
        public struct MeshInfo
        {
            internal float layer;
            internal UnityEngine.Color color;
            internal PSLG pslg;

            public IEnumerable<Vector2[]> Outlines
            {
                get
                {
                    return pslg.VertexLoops;
                }
            }

            public IEnumerable<Vector2[]> Holes
            {
                get
                {
                    return pslg.Holes;
                }
            }

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

        public Map map;
        public readonly int resolution;
        public Dictionary<IMapObject, MeshInfo> meshInfo;
        Statistics stats;

        List<Tuple<IMapObject, MeshInfo>> _SortedMeshes;
        List<Tuple<IMapObject, MeshInfo>> SortedMeshes
        {
            get
            {
                if (_SortedMeshes == null)
                {
                    _SortedMeshes = meshInfo.Select(k => Tuple.Create(k.Key, k.Value)).ToList();
                    _SortedMeshes.Sort((e1, e2) => e2.Item2.layer.CompareTo(e1.Item2.layer));
                }

                return _SortedMeshes;
            }
        }

        public MapExporter(Map map, int resolution = 4096)
        {
            this.map = map;
            this.resolution = resolution;
            this.meshInfo = new Dictionary<IMapObject, MeshInfo>();
            this.stats = new Statistics();
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

        public void ExportMap(string fileName)
        {
            // FIXME
            var prevStatus = GameController.instance.status;
            GameController.instance.status = GameController.GameStatus.Disabled;

            System.IO.Directory.CreateDirectory($"Assets/Resources/Maps/{fileName}");

            using (var drawing = new Bitmap(resolution, resolution))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(drawing))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = CompositingMode.SourceCopy;

                    for (var x = 0; x < map.tilesWidth; ++x)
                    {
                        for (var y = 0; y < map.tilesHeight; ++y)
                        {
                            var backgroundColor = ToDrawingColor(map.GetDefaultBackgroundColor(MapDisplayMode.Day));
                            graphics.FillRectangle(
                                new SolidBrush(backgroundColor),
                                new Rectangle(0, 0, resolution - 1, resolution - 1));

                            if (DrawTile(map.GetTile(x, y), graphics, false))
                            {
                                var assetName = $"Assets/Resources/Maps/{fileName}/{x}_{y}.png";
                                drawing.Save(assetName);
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
                        new Rectangle(0, 0, resolution - 1, resolution - 1));

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
                        new Rectangle(0, 0, resolution - 1, resolution - 1));

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
            var baseX = tile.x * Map.tileSize;
            var baseY = tile.y * Map.tileSize;

            return new PointF(
                ((pos.x - baseX) / Map.tileSize) * resolution,
                resolution - ((pos.y - baseY) / Map.tileSize) * resolution
            );
        }

        static PointF GetGlobalCoordinate(Map map, Vector2 pos, int resolution,
                                          float padding = 0f)
        {
            var baseX = map.minX - padding;
            var baseY = map.minY - padding;
            var width = map.width + 2 * padding;
            var height = map.height + 2 * padding;

            return new PointF(
                ((pos.x - baseX) / width) * resolution,
                resolution - ((pos.y - baseY) / height) * resolution
            );
        }

        void DrawMesh(System.Drawing.Graphics g, UnityEngine.Color c, Mesh mesh,
                      MapTile tile)
        {
            if (mesh == null)
            {
                return;
            }

            var c_real = ToDrawingColor(c);

            var verts = mesh.vertices;
            var tris = mesh.triangles;

            var tri = new PointF[3];
            for (var i = 0; i < tris.Length; i += 3)
            {
                tri[0] = GetCoordinate(tile, verts[tris[i + 0]]);
                tri[1] = GetCoordinate(tile, verts[tris[i + 1]]);
                tri[2] = GetCoordinate(tile, verts[tris[i + 2]]);

                g.FillPolygon(new SolidBrush(c_real), tri);
            }
        }

        void DrawPoly(System.Drawing.Graphics g, UnityEngine.Color c,
                      Vector2[] poly, MapTile tile)
        {
            g.FillPolygon(new SolidBrush(ToDrawingColor(c)),
                poly.Select(v => GetCoordinate(tile, v)).ToArray());
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

        bool DrawTile(MapTile tile, System.Drawing.Graphics g, bool global)
        {
            var objectsToDraw = new HashSet<IMapObject>();
            foreach (var obj in tile.mapObjects)
            {
                if (!meshInfo.ContainsKey(obj))
                {
                    continue;
                }

                objectsToDraw.Add(obj);
            }

            if (tile.orphanedObjects != null)
            {
                foreach (var obj in tile.orphanedObjects)
                {
                    if (!meshInfo.TryGetValue(obj, out MeshInfo info))
                    {
                        continue;
                    }

                    objectsToDraw.Add(obj);
                }
            }

            if (objectsToDraw.Count == 0)
            {
                return false;
            }

            foreach (var entry in SortedMeshes)
            {
                if (!objectsToDraw.Contains(entry.Item1))
                {
                    continue;
                }

                if (global)
                {
                    DrawGlobalMesh(g, entry.Item2.color, entry.Item2, resolution);
                }
                else
                {
                    DrawMesh(g, entry.Item2.color, entry.Item2, tile);
                }
            }

            return true;
        }

        void DrawMinimap(System.Drawing.Graphics g, int resolution)
        {
            var boundary = map.boundaryPositions;
            var brush = Brushes.Black;
            var width = resolution / 150f;
            var padding = width * .5f;

            var backgroundColor = ToDrawingColor(map.GetDefaultBackgroundColor(MapDisplayMode.Day));
            g.FillPolygon(new SolidBrush(backgroundColor),
                          boundary.Select(v => GetGlobalCoordinate(map, v, resolution,
                                                                   padding)).ToArray());

            foreach (var info in SortedMeshes)
            {
                DrawGlobalMesh(g, info.Item2.color, info.Item2, resolution, padding);
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

            /*foreach (var street in map.streets)
            {
                switch (street.type)
                {
                    case Street.Type.Highway:
                    case Street.Type.Primary:
                    case Street.Type.Secondary:
                    case Street.Type.River:
                        break;
                    default:
                        continue;
                }

                var rel = Mathf.Min(map.width, map.height);
                var streetDrawCalls = new List<Action>();

                foreach (var seg in street.segments)
                {
                    var width = seg.GetStreetWidth(RenderingDistance.Near);
                    var borderWidth = width + seg.GetBorderWidth(RenderingDistance.Near);

                    var relativeWidth = (width / rel) * resolution;
                    var relativeBorderWidth = (borderWidth / rel) * resolution;

                    var color = seg.GetStreetColor(RenderingDistance.Near);
                    var borderColor = seg.GetBorderColor(RenderingDistance.Near);

                    var points = seg.positions.Select(v => (Vector2)v).ToArray();

                    DrawGobalLine(g, points, borderColor, relativeBorderWidth, resolution);
                    streetDrawCalls.Add(() =>
                    {
                        DrawGobalLine(g, points, color, relativeWidth, resolution);
                    });
                }

                foreach (var call in streetDrawCalls)
                {
                    call();
                }
            }*/
        }

        Mesh CreateMesh(MeshInfo info, bool fast)
        {
            var pslg = info.pslg;
            return TriangleAPI.CreateMesh(pslg, fast || pslg.Simple);
        }

        public void CreatePrefabMeshes(bool fast)
        {
            foreach (var info in meshInfo)
            {
                if (info.Key is StreetSegment seg)
                {
                    var meshes = seg.CreateMeshes();
                    var streetColor = seg.GetStreetColor(RenderingDistance.Near);
                    var outlineColor = seg.GetBorderColor(RenderingDistance.Near);
                    var streetLayer = Map.Layer(MapLayer.Streets);
                    var outlineLayer = Map.Layer(MapLayer.StreetOutlines);

                    var dist = seg.GetMaxVisibleRenderingDistance();
                    info.Key.UniqueTile.AddMesh("Streets", meshes.Item1, streetColor, streetLayer, dist);
                    info.Key.UniqueTile.AddMesh("Streets", meshes.Item2, outlineColor, outlineLayer, dist);

                    stats.AddMesh("Streets", meshes.Item1);
                    stats.AddMesh("StreetOutlines", meshes.Item2);

                    continue;
                }

                var mesh = CreateMesh(info.Value, fast);

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

                info.Key.UniqueTile.AddMesh(group, mesh, color, layer, RenderingDistance.Near);
                stats.AddMesh(group, mesh);
            }
        }

        void SaveMesh(GameObject obj, string fileName)
        {
            var meshFilter = obj.GetComponent<MeshFilter>();
            AssetDatabase.CreateAsset(meshFilter.mesh, fileName);
        }

        public void ExportMapPrefab()
        {
            string meshPath = $"Assets/Resources/Meshes/{map.name}";
            if (!AssetDatabase.IsValidFolder(meshPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Meshes", map.name);
            }

            // Save multi meshes.
            foreach (var tile in map.AllTiles)
            {
                foreach (var mesh in tile.meshes)
                {
                    var i = 0;
                    foreach (var obj in mesh.Value.renderingObjects)
                    {
                        SaveMesh(obj, $"{meshPath}/{tile.x}_{tile.y}_{mesh.Key}_{i}.asset");
                        ++i;
                    }
                }
            }

            // Save shared mesh.
            foreach (var mesh in map.sharedTile.meshes)
            {
                var i = 0;
                foreach (var obj in mesh.Value.renderingObjects)
                {
                    SaveMesh(obj, $"{meshPath}/SharedTile_{mesh.Key}_{i}.asset");
                    ++i;
                }
            }

            // Save boundary outline.
            SaveMesh(map.boundaryOutlineObj, $"{meshPath}/BoundaryOutline.asset");

            // Save boundary mask.
            SaveMesh(map.boundaryMaskObj, $"{meshPath}/BoundaryMask.asset");

            // Save boundary background.
            SaveMesh(map.boundaryBackgroundObj, $"{meshPath}/BoundaryBackground.asset");

            PrefabUtility.SaveAsPrefabAsset(
                map.gameObject, $"Assets/Resources/Prefabs/Maps/{map.name}.prefab");
        }

        public void PrintStats()
        {
            var statsStr = new System.Text.StringBuilder();

            statsStr.Append("Vertex Count:");
            foreach (var entry in stats.totalVerts)
            {
                statsStr.Append("\n");
                statsStr.Append($"   {entry.Key}: {entry.Value.Item1} verts, {entry.Value.Item2} tris");
            }

            statsStr.Append("\n");
            Debug.Log(statsStr.ToString());
        }
    }
}
