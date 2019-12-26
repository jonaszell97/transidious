using UnityEngine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace Transidious
{
    public class MapExporter
    {
        public struct MeshInfo
        {
            internal int layer;
            internal UnityEngine.Color color;
            internal Vector2[][] outlines;
            internal Vector2[][] holes;

            public MeshInfo(PSLG pslg, int layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;

                this.outlines = pslg.Outlines;
                this.holes = pslg.Holes.ToArray();
            }

            public MeshInfo(Vector2[] poly, int layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;

                this.outlines = new Vector2[][] { poly };
                this.holes = null;
            }

            public MeshInfo(Vector3[] poly, int layer, UnityEngine.Color color)
            {
                this.layer = layer;
                this.color = color;

                this.outlines = new Vector2[][] { poly.Select(v => (Vector2)v).ToArray() };
                this.holes = null;
            }
        }

        public Map map;
        public readonly int resolution;
        public Dictionary<int, MeshInfo> meshInfo;

        public MapExporter(Map map, int resolution = 4096)
        {
            this.map = map;
            this.resolution = resolution;
            this.meshInfo = new Dictionary<int, MeshInfo>();
        }

        public void RegisterMesh(IMapObject obj, PSLG pslg,
                                 int layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj.Id, new MeshInfo(pslg, layer, color));
        }

        public void RegisterMesh(IMapObject obj, Vector2[] poly,
                                 int layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj.Id, new MeshInfo(poly, layer, color));
        }

        public void RegisterMesh(IMapObject obj, Vector3[] poly,
                                 int layer, UnityEngine.Color color)
        {
            this.meshInfo.Add(obj.Id, new MeshInfo(poly, layer, color));
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

                            if (DrawTile(map.tiles[x][y], graphics))
                            {
                                drawing.Save($"Assets/Resources/Maps/{fileName}/{x}_{y}.png");
                            }
                        }
                    }
                }
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
                    drawing.Save($"Assets/Resources/Maps/{fileName}/minimap.png");
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
            var path = new GraphicsPath();
            foreach (var outline in info.outlines)
            {
                path.AddPolygon(outline.Select(v => GetCoordinate(tile, v)).ToArray());
            }

            var region = new Region(path);
            if (info.holes != null)
            {
                using (var tmpPath = new GraphicsPath())
                {
                    foreach (var hole in info.holes)
                    {
                        tmpPath.AddPolygon(hole.Select(v => GetCoordinate(tile, v)).ToArray());
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

        void DrawGlobalMesh(System.Drawing.Graphics g, UnityEngine.Color c,
                            MeshInfo info, int resolution, float padding = 0f)
        {
            var path = new GraphicsPath();
            foreach (var outline in info.outlines)
            {
                path.AddPolygon(outline.Select(v =>
                    GetGlobalCoordinate(map, v, resolution, padding)).ToArray());
            }

            var region = new Region(path);
            if (info.holes != null)
            {
                using (var tmpPath = new GraphicsPath())
                {
                    foreach (var hole in info.holes)
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

        bool DrawTile(MapTile tile, System.Drawing.Graphics g)
        {
            var drewSomething = false;

            foreach (var obj in tile.mapObjects)
            {
                if (!meshInfo.TryGetValue(obj.Id, out MeshInfo info))
                {
                    continue;
                }

                drewSomething = true;
                DrawMesh(g, info.color, info, tile);
            }

            if (tile.orphanedObjects != null)
            {
                foreach (var obj in tile.orphanedObjects)
                {
                    if (!meshInfo.TryGetValue(obj.Id, out MeshInfo info))
                    {
                        continue;
                    }

                    drewSomething = true;
                    DrawMesh(g, info.color, info, tile);
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
            g.FillPolygon(new SolidBrush(backgroundColor),
                          boundary.Select(v => GetGlobalCoordinate(map, v, resolution,
                                                                   padding)).ToArray());

            foreach (var info in meshInfo)
            {
                DrawGlobalMesh(g, info.Value.color, info.Value, resolution, padding);
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
            foreach (var info in meshInfo)
            {
                DrawGlobalMesh(g, info.Value.color, info.Value, resolution);
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
    }
}
