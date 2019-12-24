using UnityEngine;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace Transidious
{
    public class MapExporter
    {
        public static void ExportMap(Map map, string fileName, int resolution = 3096)
        {
            // FIXME
            var prevStatus = GameController.instance.status;
            GameController.instance.status = GameController.GameStatus.Disabled;

            for (var x = 0; x < map.tilesWidth; ++x)
            {
                for (var y = 0; y < map.tilesHeight; ++y)
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

                            DrawTile(map, map.tiles[x][y], graphics, resolution);
                            drawing.Save($"Assets/Resources/Maps/{fileName}/{x}_{y}.png");
                        }
                    }
                }
            }

            // FIXME
            GameController.instance.status = prevStatus;
        }

        public static System.Drawing.Color ToDrawingColor(UnityEngine.Color c)
        {
            return System.Drawing.Color.FromArgb(
                (int)(c.a * 255f), (int)(c.r * 255f),
                (int)(c.g * 255f), (int)(c.b * 255f));
        }

        public static PointF GetCoordinate(MapTile tile, Vector3 pos, int resolution)
        {
            return GetCoordinate(tile, (Vector2)pos, resolution);
        }

        public static PointF GetCoordinate(MapTile tile, Vector2 pos, int resolution)
        {
            var baseX = tile.x * Map.tileSize;
            var baseY = tile.y * Map.tileSize;

            return new PointF(
                ((pos.x - baseX) / Map.tileSize) * resolution,
                resolution - ((pos.y - baseY) / Map.tileSize) * resolution
            );
        }

        public static void DrawMesh(System.Drawing.Graphics g,
                                    UnityEngine.Color c, Mesh mesh,
                                    MapTile tile, int resolution)
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
                tri[0] = GetCoordinate(tile, verts[tris[i + 0]], resolution);
                tri[1] = GetCoordinate(tile, verts[tris[i + 1]], resolution);
                tri[2] = GetCoordinate(tile, verts[tris[i + 2]], resolution);

                g.FillPolygon(new SolidBrush(c_real), tri);
            }
        }

        static void DrawTile(Map map, MapTile tile,
                             System.Drawing.Graphics g, int resolution)
        {
            foreach (var feature in tile.mapObjects.OfType<NaturalFeature>())
            {
                DrawMesh(g, feature.GetColor(), feature.mesh, tile, resolution);
            }

            foreach (var building in tile.mapObjects.OfType<Building>())
            {
                DrawMesh(g, building.GetColor(), building.mesh, tile, resolution);
            }
        }
    }
}
