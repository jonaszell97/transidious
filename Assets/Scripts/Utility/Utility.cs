using System.Linq;
using UnityEngine;

namespace Transidious
{
    public static class Utility
    {
        public static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }

        public static Vector2[] Points(this Rect rect)
        {
            return new Vector2[]
            {
                new Vector2(rect.x, rect.y),
                new Vector2(rect.x, rect.y + rect.height),
                new Vector2(rect.x + rect.width, rect.y + rect.height),
                new Vector2(rect.x + rect.width, rect.y),
            };
        }

        public static Texture2D ResizeNonDestructive(this Texture2D tex, int newWidth, int newHeight)
        {
            Debug.Assert(newWidth <= tex.width);
            Debug.Assert(newHeight <= tex.height);

            var newTex = new Texture2D(newWidth, newHeight);
            for (var x = 0; x < newWidth; ++x)
            {
                for (var y = 0; y < newHeight; ++y)
                {
                    newTex.SetPixel(x, y, tex.GetPixel(x, y));
                }
            }

            return newTex;
        }

        public static bool Contains2D(this Bounds bounds, Vector2 pos)
        {
            return (pos.x >= bounds.center.x - bounds.extents.x)
                && (pos.x <= bounds.center.x + bounds.extents.x)
                && (pos.y >= bounds.center.y - bounds.extents.y)
                && (pos.y <= bounds.center.y + bounds.extents.y);
        }

        public static void Dump(object o)
        {
            var s = new System.Text.StringBuilder();
            if (o == null)
            {
                s.Append("<null>");
                Debug.Log(s.ToString());

                return;
            }

            var properties = o.GetType().GetFields();

            s.Append('{');
            s.Append(o.GetType().Name);

            if (properties.Length != 0)
            {
                s.Append(' ');

                for (int i = 0, n = properties.Length; i < n; i++)
                {
                    if (i != 0)
                        s.Append("; ");

                    var property = properties[i];

                    s.Append(property.Name);
                    s.Append(" = ");
                    s.Append(property.GetValue(o));
                }
            }

            s.Append('}');
            Debug.Log(s.ToString());

            return;
        }

        public static bool DrawingEnabled = true;

        public static GameObject DrawCircle(Vector3 center, float radius,
                                            float lineWidth, Color c)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var segments = 360;
            var obj = new GameObject();
            obj.name = "DebugCircle";
            obj.transform.position = new Vector3(center.x, center.y, -300);

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = segments + 1;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.instance.GetUnlitMaterial(c);
            line.loop = true;

            var pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            var points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                var rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, -13f);
            }

            line.SetPositions(points);
            return obj;
        }

        public static GameObject DrawRect(Vector2 bl, Vector2 tl,
                                          Vector2 tr, Vector2 br,
                                          float lineWidth, Color c)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var obj = new GameObject();
            obj.name = "DebugRect";
            obj.transform.position = new Vector3(0, 0, Map.Layer(MapLayer.Foreground, 9));

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = 4;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.instance.GetUnlitMaterial(c);
            line.loop = true;

            var points = new Vector3[] { bl, tl, tr, br };
            line.SetPositions(points);

            return obj;
        }

        public static GameObject DrawLine(Vector3[] points, float lineWidth,
                                          Color c, bool loop = false,
                                          bool arrow = false)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var obj = new GameObject();
            obj.name = "DebugLine";
            obj.transform.position = new Vector3(0, 0, Map.Layer(MapLayer.Foreground, 9));

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = points.Length;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.instance.GetUnlitMaterial(c);
            line.loop = loop;
            line.SetPositions(points);
            
            if (arrow)
            {
                DrawArrow(points[points.Length - 1], points.Last(), lineWidth, c);
            }

            return obj;
        }

        public static GameObject DrawLine(Vector2[] points, float lineWidth,
                                          Color c, float z, bool loop = false,
                                          bool arrow = false)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var obj = new GameObject();
            obj.name = "DebugLine";
            obj.transform.position = new Vector3(0, 0, Map.Layer(MapLayer.Foreground, 9));

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null)
                line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = points.Length;
            line.startColor = c;
            line.endColor = c;
            line.material = GameController.instance.GetUnlitMaterial(c);
            line.loop = loop;
            line.SetPositions(points.Select(v => v.WithZ(z)).ToArray());

            if (arrow)
            {
                DrawArrow(points[points.Length - 1], points.Last(), lineWidth, c);
            }
            
            return obj;
        }

        public static GameObject DrawArrow(Vector2 from, Vector2 to,
                                           float width, Color c)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var direction = from - to;
            var normal = new Vector2(-direction.y, direction.x).normalized * width;
            var halfNormal = normal * .5f;

            // Bottom left
            var bl = from - halfNormal;

            // Top left
            var tl = to - halfNormal;

            // Top right
            var tr = to + halfNormal;

            // Bottom right
            var br = from + halfNormal;

            // Arrow bottom left
            var abl = to - normal;

            // Arrow top middle
            var atm = to - (direction.normalized * width);

            // Arrow bottom right.
            var abr = to + normal;

            var z = Map.Layer(MapLayer.Foreground, 9);
            var mesh = new Mesh
            {
                vertices = new Vector3[]
                {
                    new Vector3(bl.x, bl.y, z),
                    new Vector3(tl.x, tl.y, z),
                    new Vector3(tr.x, tr.y, z),
                    new Vector3(br.x, br.y, z),
                    new Vector3(abl.x, abl.y, z),
                    new Vector3(atm.x, atm.y, z),
                    new Vector3(abr.x, abr.y, z),
                },
                triangles = new int[]
                {
                    // Bl, tl, tr
                    0, 1, 2,

                    // Bl, tr, br
                    0, 2, 3,

                    // Abl, Atm, Abr
                    4, 5, 6,
                },
            };

            var obj = GameObject.Instantiate(GameController.instance.loadedMap.meshPrefab);
            obj.name = "DebugArrow";

            var meshFilter = obj.GetComponent<MeshFilter>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            meshRenderer.material = new Material(GameController.instance.unlitMaterial)
            {
                color = c,
            };

            return obj;
        }

        public static GameObject DrawText(Vector2 pos, string text, float fontSize, Color c)
        {
            if (!DrawingEnabled)
            {
                return null;
            }

            var obj = new GameObject {name = "DebugText"};
            obj.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.Foreground, 9));
            obj.transform.SetParent(GameController.instance.loadedMap.canvas.transform);

            var txt = obj.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = c;

            return obj;
        }

        public static Rect RectTransformToScreenSpace(Camera cam, RectTransform transform)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);

            var minPtScreen = cam.WorldToScreenPoint(corners[0]);
            var maxPtScreen = cam.WorldToScreenPoint(corners[2]);

            return new Rect(minPtScreen.x, minPtScreen.y,
                            maxPtScreen.x - minPtScreen.x,
                            maxPtScreen.y - minPtScreen.y);
        }
    }
}