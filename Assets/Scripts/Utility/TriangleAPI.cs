﻿using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Transidious
{
    public class PSLG
    {
        public List<Vector3> vertices;
        public List<int[]> segments;
        public List<PSLG> holes;
        public List<int> boundaryMarkersForPolygons;
        public float z;

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

        public void AddVertexLoop(List<Vector3> vertices)
        {
            if (vertices.Count < 3)
            {
                return;
            }

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
                    Debug.LogError("Stuck in while loop. Vertices: " + polygonstring);
                    break;
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

                    Debug.LogError("Stuck in while loop. Vertices: " + holestring);
                    break;
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
        [DllImport("UnityTriangle", CallingConvention = CallingConvention.Cdecl)]
        static extern int Triangulate(StringBuilder bin, StringBuilder args, StringBuilder file);

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

        public static Mesh CreateMesh(PSLG pslg)
        {
            var polygon = Triangulate(pslg);
            if (polygon == null)
            {
                return new Mesh();
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
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = "/usr/local/bin/triangle",
                    Arguments = "-pPq0 " + polyFilePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }

        static int ExecuteTriangleInProcess(string polyFilePath)
        {
            if (polyFilePath == null)
            {
                throw new InvalidOperationException("no poly file path!");
            }

            Debug.Log("calling triangle");
            return Triangulate(new StringBuilder("/usr/local/bin/triangle"),
                               new StringBuilder("-pPq0"),
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
