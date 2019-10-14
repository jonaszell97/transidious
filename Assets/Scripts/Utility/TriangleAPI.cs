using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TriangleNet.IO;
using TriangleNet.Geometry;
using TriangleNet.Meshing;

namespace Transidious
{
    public class PSLG
    {
        public List<Vector3> vertices;
        public List<int[]> segments;
        public List<PSLG> holes;
        public List<int> boundaryMarkersForPolygons;
        public float z;
        List<Tuple<Vector2, Vector2>> edges;
        bool simplified = true;

        public PSLG(float z = 0)
        {
            vertices = new List<Vector3>();
            segments = new List<int[]>();
            holes = new List<PSLG>();
            boundaryMarkersForPolygons = new List<int>();
            this.z = z;
        }

        public PSLG(List<Vector3> vertices) : this()
        {
            this.AddVertexLoop(vertices);
        }

        public bool Empty
        {
            get
            {
                return vertices.Count == 0;
            }
        }

        public IEnumerable<Vector2[]> VertexLoops
        {
            get
            {
                return Outlines.Take(boundaryMarkersForPolygons.Count);
            }
        }

        public IEnumerable<Vector2[]> Holes
        {
            get
            {
                return Outlines.Skip(boundaryMarkersForPolygons.Count);
            }
        }

        public Vector2[][] Outlines
        {
            get
            {
                var outlines = new Vector2[boundaryMarkersForPolygons.Count + holes.Count][];
                var i = 0;

                for (; i < boundaryMarkersForPolygons.Count; ++i)
                {
                    var start = boundaryMarkersForPolygons[i];
                    var end = i < boundaryMarkersForPolygons.Count - 1 ? boundaryMarkersForPolygons[i + 1] : vertices.Count;
                    var verts = vertices.GetRange(start, end - start);

                    if (!verts.First().Equals(verts.Last()))
                    {
                        var tmp = verts.ToList();
                        tmp.Add(tmp.First());

                        outlines[i] = tmp.Select(v => (Vector2)v).ToArray();
                    }
                    else
                    {
                        outlines[i] = verts.Select(v => (Vector2)v).ToArray();
                    }
                }

                foreach (var hole in holes)
                {
                    var verts = hole.vertices.Select(v => (Vector2)v);
                    outlines[i++] = verts.ToArray();
                }

                return outlines;
            }
        }

        public List<Tuple<Vector2, Vector2>> Edges
        {
            get
            {
                if (edges != null)
                {
                    return edges;
                }

                edges = new List<Tuple<Vector2, Vector2>>();

                for (var i = 1; i < vertices.Count; ++i)
                {
                    edges.Add(Tuple.Create<Vector2, Vector2>(vertices[i - 1], vertices[i]));
                }

                if (vertices.Count > 0 && !vertices.First().Equals(vertices.Last()))
                {
                    edges.Add(Tuple.Create<Vector2, Vector2>(vertices.Last(), vertices.First()));
                }

                return edges;
            }
        }

        public float Area
        {
            get
            {
                var baseArea = Math.GetAreaOfPolygon(vertices);
                var area = baseArea;

                foreach (var hole in holes)
                {
                    area -= Math.GetAreaOfPolygon(hole.vertices);
                }

                // Can happen with nested holes etc, just ignore it, this isn't a perfect science.
                if (area <= 0f)
                {
                    return baseArea;
                }

                return area;
            }
        }

        public Vector2 Centroid
        {
            get
            {
                return Math.GetCentroid(vertices);
            }
        }

#if DEBUG
        public void Draw(GameObject obj, Color outlineColor, Color holeColor)
        {
            for (var i = 0; i < boundaryMarkersForPolygons.Count - 1; ++i)
            {
                var start = boundaryMarkersForPolygons[i];
                var end = boundaryMarkersForPolygons[i + 1];
                var verts = vertices.GetRange(start, end - start).ToArray();

                obj.DrawLine(verts, 1f, outlineColor, true);
            }

            foreach (var hole in holes)
            {
                hole.Draw(obj, holeColor, outlineColor);
            }
        }
#endif

        public bool IsValidFloat(float val)
        {
            if (float.IsNaN(val))
            {
                return false;
            }
            if (float.IsInfinity(val))
            {
                return false;
            }

            return true;
        }

        public bool Validate()
        {
            this.Simplify();

            foreach (var vert in vertices)
            {
                if (!IsValidFloat(vert.x))
                {
                    return false;
                }
                if (!IsValidFloat(vert.y))
                {
                    return false;
                }
                if (!IsValidFloat(vert.z))
                {
                    return false;
                }
            }

            foreach (var hole in holes)
            {
                if (!hole.Validate())
                {
                    return false;
                }
            }

            return true;
        }

        bool IsSimple(IReadOnlyList<Vector3> vertices)
        {
            return vertices.Distinct().Count() == vertices.Count;
        }

        public void AddVertexLoop(List<Vector3> vertices)
        {
            if (vertices.Count < 3)
            {
                return;
            }

            simplified &= IsSimple(vertices);

            this.vertices.AddRange(vertices);
            int segmentOffset = segments.Count;
            boundaryMarkersForPolygons.Add(segments.Count);
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                segments.Add(new int[] { i + segmentOffset, i + 1 + segmentOffset });
            }
            segments.Add(new int[] { vertices.Count - 1 + segmentOffset, segmentOffset });
        }

        public void AddOrderedVertices(Vector3[] vertices)
        {
            if (vertices.Length < 3)
            {
                return;
            }

            AddVertexLoop(new List<Vector3>(vertices));
        }

        public void AddHole(List<Vector3> vertices)
        {
            if (vertices.Count < 3)
                return;

            simplified &= IsSimple(vertices);

            PSLG hole = new PSLG();
            hole.AddVertexLoop(vertices);
            holes.Add(hole);
        }

        public void AddHole(Vector3[] vertices)
        {
            if (vertices.Length < 3)
                return;

            AddHole(new List<Vector3>(vertices));
        }

        static readonly float SimplificationDistance = 0.001f;

        void Simplify()
        {
            if (simplified)
            {
                return;
            }

            var encounteredVerts = new HashSet<Vector3>();
            for (var i = 0; i < vertices.Count; ++i)
            {
                if (encounteredVerts.Add(vertices[i]))
                {
                    continue;
                }

                // Move backward along the edge by a small amount.
                var edge = (vertices[i - 1] - vertices[i]);
                if (edge.magnitude <= SimplificationDistance)
                {
                    vertices[i] += edge * .5f;
                }
                else
                {
                    vertices[i] += edge.normalized * SimplificationDistance;
                }

                encounteredVerts.Add(vertices[i]);
            }

            foreach (var hole in holes)
            {
                hole.Simplify();
            }

            simplified = true;
        }

        public int GetNumberOfSegments()
        {
            int offset = vertices.Count;
            foreach (PSLG hole in holes)
            {
                offset += hole.segments.Count;
            }

            return offset;
        }

        public bool IsPointInPolygon(Vector3 point)
        {
            int j = segments.Count - 1;
            bool oddNodes = false;

            for (int i = 0; i < segments.Count; i++)
            {
                if ((vertices[i].y < point.y && vertices[j].y >= point.y
                || vertices[j].y < point.y && vertices[i].y >= point.y)
                && (vertices[i].x <= point.x || vertices[j].x <= point.x))
                {
                    oddNodes ^= (vertices[i].x + (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) * (vertices[j].x - vertices[i].x) < point.x);
                }
                j = i;
            }

            return oddNodes;
        }

        public Vector3 GetPointInPolygon()
        {
            float topMost = vertices[0].y;
            float bottomMost = vertices[0].y;
            float leftMost = vertices[0].x;
            float rightMost = vertices[0].x;

            foreach (Vector3 vertex in vertices)
            {
                if (vertex.y > topMost)
                    topMost = vertex.y;
                if (vertex.y < bottomMost)
                    bottomMost = vertex.y;
                if (vertex.x < leftMost)
                    leftMost = vertex.x;
                if (vertex.x > rightMost)
                    rightMost = vertex.x;
            }

            Vector3 point;

            int whileCount = 0;
            do
            {
                point = new Vector3(UnityEngine.Random.Range(leftMost, rightMost),
                                    UnityEngine.Random.Range(bottomMost, topMost));

                if (whileCount++ > 10000)
                {
                    string polygonstring = "";
                    foreach (Vector3 vertex in vertices)
                    {
                        polygonstring += vertex + ", ";
                    }

                    throw new Exception("Stuck in while loop. Vertices: " + polygonstring);
                }
            }
            while (!IsPointInPolygon(point));

            return point;
        }

        public Vector3 GetPointInHole(PSLG hole)
        {
            // 10 Get point in hole
            // 20 Is the point in a polygon that the hole is not in
            // 30 if so goto 10 else return
            List<PSLG> polygons = new List<PSLG>();
            for (int i = 0; i < boundaryMarkersForPolygons.Count; i++)
            {
                int startIndex = boundaryMarkersForPolygons[i];
                int endIndex = vertices.Count - 1;
                if (i < boundaryMarkersForPolygons.Count - 1)
                    endIndex = boundaryMarkersForPolygons[i + 1] - 1;
                polygons.Add(new PSLG(vertices.GetRange(startIndex, endIndex - startIndex + 1)));
            }

            int whileCount = 0;

            Vector3 point;
            bool isPointGood;
            do
            {
                isPointGood = true;
                point = hole.GetPointInPolygon();
                foreach (PSLG polygon in polygons)
                {
                    string polygonVertices = "";
                    foreach (Vector3 vertex in polygon.vertices)
                        polygonVertices += vertex + ",";

                    if (polygon.IsPointInPolygon(hole.vertices[0]))
                    {
                        // This polygon surrounds the hole, which is OK
                    }
                    else if (hole.IsPointInPolygon(polygon.vertices[0]))
                    {
                        // This polygon is within the hole

                        if (polygon.IsPointInPolygon(point))
                        {
                            // The point is within a polygon that is inside the hole, which is NOT OK
                            isPointGood = false;
                        }
                        else
                        {
                            // But the point was not within this polygon
                        }
                    }
                    else
                    {
                        // This polygon is far away from the hole
                    }

                }

                if (whileCount++ > 10000)
                {
                    string holestring = "";
                    foreach (Vector3 vertex in hole.vertices)
                    {
                        holestring += vertex + ", ";
                    }

                    throw new Exception("Stuck in while loop. Vertices: " + holestring);
                }
            }
            while (!isPointGood);

            return point;
        }
    }

    public class Polygon2D
    {
        public int[] triangles;
        public Vector3[] vertices;

        public Polygon2D(int[] triangle, Vector3[] vertices)
        {
            this.triangles = triangle;
            this.vertices = vertices;
        }
    }

    public static class TriangleAPI
    {
#if UNITY_EDITOR_OSX
        [DllImport("UnityTriangle", CallingConvention = CallingConvention.Cdecl)]
#elif UNITY_EDITOR_WIN
        [DllImport("triangle")]
#else
        [DllImport("unitytriangle", EntryPoint = "Triangulate", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int Triangulate(StringBuilder bin, StringBuilder args, StringBuilder file);


        // Use this for initialization
        public static Polygon2D Triangulate(PSLG pslg)
        {
            if (pslg.vertices.Count == 0)
            {
                Debug.LogWarning("No vertices passed to triangle. hole count: "
                    + pslg.holes.Count + ", vert count: " + pslg.vertices.Count);

                return null;
            }
            else
            {
                try
                {
                    if (!pslg.Validate())
                    {
                        Debug.LogError("Invalid points in PSLG!");
                        return null;
                    }

                    // Write poly file
                    var polyFilePath = WritePolyFile(pslg);

                    // Execute Triangle
                    var exitCode = ExecuteTriangle(polyFilePath);
                    if (exitCode != 0)
                    {
                        Debug.LogWarning("'triangle' failed with exit code " + exitCode);
                        return null;
                    }

                    // Read output
                    Vector3[] vertices = ReadVerticesFile(polyFilePath, pslg.z);
                    int[] triangles = ReadTrianglesFile(polyFilePath);

                    return new Polygon2D(triangles, vertices);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.GetType().AssemblyQualifiedName + " " + e.Message);
                    return null;
                }
            }
        }

        static int GetIndex(Vertex v, List<Vector3> vertices, Dictionary<Vector2, int> triMap)
        {
            Vector3 vert = new Vector3((float)v.x, (float)v.y, 0f);
            if (triMap.TryGetValue(vert, out int index))
            {
                return index;
            }

            index = vertices.Count;
            vertices.Add(vert);
            triMap.Add(vert, index);

            return index;
        }

        static Dictionary<Vector2, int> triMap;
        static List<int> triangles;
        static List<Vector3> vertices;

        static Mesh GetMesh(IMesh mesh)
        {
            if (triMap == null)
            {
                triMap = new Dictionary<Vector2, int>();
                triangles = new List<int>();
                vertices = new List<Vector3>();
            }
            else
            {
                triMap.Clear();
                triangles.Clear();
                vertices.Clear();
            }

            foreach (var tri in mesh.Triangles)
            {
                triangles.Add(GetIndex(tri.GetVertex(2), vertices, triMap));
                triangles.Add(GetIndex(tri.GetVertex(1), vertices, triMap));
                triangles.Add(GetIndex(tri.GetVertex(0), vertices, triMap));
            }

            var result = new Mesh()
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
            };

            //MeshBuilder.FixWindingOrder(result);
            return result;
        }

        public static Mesh CreateMeshNew(PSLG pslg)
        {
            if (pslg.vertices.Count == 0)
            {
                Debug.LogWarning("No vertices passed to triangle. hole count: "
                    + pslg.holes.Count + ", vert count: " + pslg.vertices.Count);

                return null;
            }
            else
            {
                try
                {
                    if (!pslg.Validate())
                    {
                        Debug.LogError("Invalid points in PSLG!");
                        return null;
                    }

                    var polygon = new Polygon();

                    var i = 0;
                    foreach (var verts in pslg.Outlines)
                    {
                        polygon.Add(new Contour(verts.Select(v => new Vertex(v.x, v.y))), i >= pslg.boundaryMarkersForPolygons.Count);
                        ++i;
                    }

                    //var polyFile = WritePolyFile(pslg);
                    //var polygon = FileProcessor.Read(polyFile);

                    var options = new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };
                    var quality = new TriangleNet.Meshing.QualityOptions() { MinimumAngle = 0 };

                    // Triangulate the polygon
                    var mesh = (TriangleNet.Mesh)polygon.Triangulate(options, quality);
                    if (mesh.Vertices.Count > 10000)
                    {
                        Debug.LogWarning("mesh has too many vertices (" + mesh.Vertices.Count + ")");
                        return null;
                    }
                    
                    return GetMesh(mesh);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.GetType().AssemblyQualifiedName + " " + e.Message);
                    return null;
                }
            }
        }

        static Mesh TriangulateSimple(PSLG pslg)
        {
            var mesh = MeshBuilder.PointsToMeshFast(pslg.vertices.ToArray());
            MeshBuilder.FixWindingOrder(mesh);

            return mesh;
        }

        public static Mesh CreateMesh(PSLG pslg, bool fast = false)
        {
            Mesh mesh;
            if (fast)
            {
                mesh = CreateMeshNew(pslg);
            }
            else
            {
                mesh = CreateMeshOld(pslg);
            }

            if (mesh == null)
            {
                Debug.LogWarning("creating simple mesh without holes");
                return TriangulateSimple(pslg);
            }

            return mesh;
        }

        public static Mesh CreateMeshOld(PSLG pslg)
        {
            if (pslg == null)
            {
                return null;
            }

            var polygon = Triangulate(pslg);
            if (polygon == null)
            {
                Debug.LogWarning("creating simple mesh without holes");
                return TriangulateSimple(pslg);
            }

            var mesh = new Mesh
            {
                vertices = polygon.vertices,
                triangles = polygon.triangles
            };

            MeshBuilder.FixWindingOrder(mesh);
            return mesh;
        }

        static string WritePolyFile(PSLG pslg)
        {
            var polyFilePath = System.IO.Path.GetTempFileName().Replace(".tmp", ".poly");
            if (File.Exists(polyFilePath))
            {
                File.Delete(polyFilePath);
            }

            using (StreamWriter sw = File.CreateText(polyFilePath))
            {
                sw.WriteLine("# polygon.poly");
                sw.WriteLine("# generated by Unity Triangle API");
                sw.WriteLine("#");
                // Vertices
                sw.WriteLine(pslg.GetNumberOfSegments() + " 2 0 1");
                sw.WriteLine("# The polyhedrons.");
                int boundaryMarker = 2;
                int i;
                for (i = 0; i < pslg.vertices.Count; i++)
                {
                    if (i != 0 && pslg.boundaryMarkersForPolygons.Contains(i))
                    {
                        boundaryMarker++;
                    }
                    sw.WriteLine(i + 1 + "\t" + pslg.vertices[i].x + "\t" + pslg.vertices[i].y + "\t" + boundaryMarker);
                }
                int offset = i;
                for (i = 0; i < pslg.holes.Count; i++)
                {
                    sw.WriteLine("# Hole #" + (i + 1));
                    int j;
                    for (j = 0; j < pslg.holes[i].vertices.Count; j++)
                    {
                        sw.WriteLine((offset + j + 1) + "\t" + pslg.holes[i].vertices[j].x + "\t" + pslg.holes[i].vertices[j].y + "\t" + (boundaryMarker + i + 1));
                    }
                    offset += j;
                }

                // Line segments
                sw.WriteLine();
                sw.WriteLine("# Line segments.");
                sw.WriteLine(pslg.GetNumberOfSegments() + " 1");
                sw.WriteLine("# The polyhedrons.");
                boundaryMarker = 2;
                for (i = 0; i < pslg.segments.Count; i++)
                {
                    if (i != 0 && pslg.boundaryMarkersForPolygons.Contains(i))
                    {
                        boundaryMarker++;
                    }
                    sw.WriteLine(i + 1 + "\t" + (pslg.segments[i][0] + 1) + "\t" + (pslg.segments[i][1] + 1) + "\t" + boundaryMarker);
                }
                offset = i;
                for (i = 0; i < pslg.holes.Count; i++)
                {
                    sw.WriteLine("# Hole #" + (i + 1));
                    int j;
                    for (j = 0; j < pslg.holes[i].segments.Count; j++)
                    {
                        sw.WriteLine((offset + j + 1) + "\t" + (offset + 1 + pslg.holes[i].segments[j][0]) + "\t" + (offset + 1 + pslg.holes[i].segments[j][1]) + "  " + (boundaryMarker + i + 1));
                    }
                    offset += j;
                }

                // Holes
                sw.WriteLine();
                sw.WriteLine("# Holes.");
                sw.WriteLine(pslg.holes.Count);
                for (i = 0; i < pslg.holes.Count; i++)
                {
                    Vector3 point = pslg.GetPointInHole(pslg.holes[i]);
                    sw.WriteLine((i + 1) + "\t" + point.x + "\t" + point.y + "\t # Hole #" + (i + 1));
                }

                sw.Close();
            }

            return polyFilePath;
        }

        static int ExecuteTriangle(string polyFilePath)
        {
            using (var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = @"C:\Users\Jonny\triangle\triangle.exe",
                    Arguments = "-pPq0 " + polyFilePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            })
            {
                process.Start();
                if (!process.WaitForExit(10000))
                {
                    return 124;
                }

                return process.ExitCode;
            }
        }

        [DllImport("triangle")]
        static extern void RegisterDebugCallback(debugCallback cb);

        delegate void debugCallback(IntPtr request, int color, int size);
        enum Color { red, green, blue, black, white, yellow, orange };

        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            //Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);

            //Add Specified Color
            debug_string =
                String.Format("{0}{1}{2}{3}{4}",
                "<color=",
                ((Color)color).ToString(),
                ">",
                debug_string,
                "</color>"
                );

            UnityEngine.Debug.Log(debug_string);
        }

        static bool registeredCallback = false;

        static int ExecuteTriangleInProcess(string polyFilePath)
        {
            if (!registeredCallback)
            {
                RegisterDebugCallback(OnDebugCallback);
                registeredCallback = true;
            }

            if (polyFilePath == null)
            {
                throw new InvalidOperationException("no poly file path!");
            }

            Debug.Log("calling triangle");
            return Triangulate(new StringBuilder("/usr/local/bin/triangle"),
                               new StringBuilder("-pPq0VVV"),
                               new StringBuilder(polyFilePath));
        }

        static Vector3[] ReadVerticesFile(string polyFilePath, float z = 0)
        {
            Vector3[] vertices = null;
            string outputVerticesFile = polyFilePath.Replace(".poly", ".1.node");
            StreamReader sr = File.OpenText(outputVerticesFile);

            string line = sr.ReadLine();
            int n = line.IndexOf("  ");
            int nVerts = int.Parse(line.Substring(0, n));
            vertices = new Vector3[nVerts];

            while ((line = sr.ReadLine()) != null)
            {
                int index = -1;
                float x = 0f;
                float y = 0f;
                int c = 0;
                if (!line.Contains("#"))
                {
                    string[] stringBits = line.Split(' ');

                    foreach (string s in stringBits)
                    {
                        if (s != "" && s != " ")
                        {
                            if (c == 0)
                                index = int.Parse(s);
                            else if (c == 1)
                                x = float.Parse(s);
                            else if (c == 2)
                                y = float.Parse(s);

                            c++;
                        }
                    }
                }

                if (index != -1)
                {
                    vertices[index - 1] = new Vector3(x, y, z);
                }
            }

            sr.Close();
            return vertices;
        }

        private static int[] ReadTrianglesFile(string polyFilePath)
        {
            List<int> triList = null;
            string outputTrianglesFile = polyFilePath.Replace(".poly", ".1.ele");

            using (StreamReader sr = File.OpenText(outputTrianglesFile))
            {
                string line = sr.ReadLine();
                int n = line.IndexOf("  ");
                int nTriangles = int.Parse(line.Substring(0, n));
                //int[] triangles = new int[nTriangles * 3];
                triList = new List<int>(nTriangles * 3);

                while ((line = sr.ReadLine()) != null)
                {
                    int index = -1;
                    int c = 0;
                    int[] tri = new int[3];
                    if (!line.Contains("#"))
                    {
                        string[] stringBits = line.Split(' ');

                        foreach (string s in stringBits)
                        {
                            if (s != "" && s != " ")
                            {
                                if (c == 0)
                                    index = int.Parse(s);
                                else if (c == 1)
                                    tri[0] = int.Parse(s) - 1;
                                else if (c == 2)
                                    tri[1] = int.Parse(s) - 1;
                                else if (c == 3)
                                    tri[2] = int.Parse(s) - 1;

                                c++;
                            }
                        }
                    }

                    if (index != -1)
                    {
                        triList.AddRange(tri);
                    }
                }

                sr.Close();
            }

            return triList.ToArray();
        }
    }
}
