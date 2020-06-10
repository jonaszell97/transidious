using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class TransitEditor : MonoBehaviour
    {
        public GameController game;
        
        public void InitOverlappingRoutes()
        {
            var crossedStreets = new HashSet<Tuple<StreetSegment, int>>();
            foreach (var route in game.loadedMap.transitRoutes)
            {
                foreach (var entry in route.GetStreetSegmentOffsetInfo())
                {
                    foreach (var segInfo in entry.Value)
                    {
                        var routesOnSegment = segInfo.segment.GetTransitRoutes(segInfo.lane);
                        routesOnSegment.Add(route);

                        crossedStreets.Add(Tuple.Create(segInfo.segment, segInfo.lane));
                    }
                }
            }

            CheckOverlappingRoutes(crossedStreets);
        }

        public void CheckOverlappingRoutes(HashSet<Tuple<StreetSegment, int>> segments)
        {
            var linesPerPositionMap = new Dictionary<Tuple<StreetSegment, int>, int>();
            var affectedRoutes = new HashSet<Route>();

            foreach (var seg in segments)
            {
                UpdateLinesPerPosition(linesPerPositionMap,
                                       affectedRoutes,
                                       seg.Item1, seg.Item2);
            }

            UpdateRouteMeshes(affectedRoutes, linesPerPositionMap);
        }

        void UpdateLinesPerPosition(Dictionary<Tuple<StreetSegment, int>, int> linesPerPositionMap,
                                    HashSet<Route> affectedRoutes,
                                    StreetSegment seg, int lane)
        {
            var key = Tuple.Create(seg, lane);
            if (linesPerPositionMap.ContainsKey(key))
                return;

            var routes = seg.GetTransitRoutes(lane);
            var lines = new HashSet<Line>();

            foreach (var route in routes)
            {
                lines.Add(route.line);

                if (!affectedRoutes.Add(route))
                {
                    continue;
                }

                foreach (var otherSeg in route.GetStreetSegmentOffsetInfo())
                {
                    UpdateLinesPerPosition(linesPerPositionMap, affectedRoutes,
                                           otherSeg.Key.Item1, otherSeg.Key.Item2);
                }
            }

            if (linesPerPositionMap.ContainsKey(key))
                return;

            linesPerPositionMap.Add(key, lines.Count);
        }

        void UpdateRouteMeshes(HashSet<Route> routes,
                               Dictionary<Tuple<StreetSegment, int>, int> linesPerPositionMap)
        {
            // Go line by line so every line has a consistent offset.
            var lineSet = new HashSet<Line>();
            foreach (var route in routes)
            {
                lineSet.Add(route.line);
            }

            var lines = lineSet.ToList();
            lines.Sort((l1, l2) => string.Compare(l1.name, l2.name, StringComparison.Ordinal));

            var offsetMap = new Dictionary<Tuple<Line, StreetSegment, int>, int>();
            var latestOffsetMap = new Dictionary<Tuple<StreetSegment, int>, int>();

            foreach (var line in lines)
            {
                foreach (var route in line.routes)
                {
                    if (!routes.Contains(route))
                    {
                        continue;
                    }

                    UpdateRouteMesh(route, linesPerPositionMap, offsetMap, latestOffsetMap);

                    // route.beginStop.UpdateAppearance();
                    // route.endStop.UpdateAppearance();
                }
            }
        }

        static int MaxOverlappingLines = 4;

        void GetLineWidthAndOffset(Route route, StreetSegment streetSeg,
                                   int currentLines, int currentLineOffset,
                                   out float halfLineWidth, out float offset)
        {
            currentLines = Mathf.Min(currentLines, MaxOverlappingLines);
            currentLineOffset = currentLineOffset % MaxOverlappingLines;

            if (currentLines == 1)
            {
                offset = 0f;
                halfLineWidth = route.line.LineWidth;

                return;
            }

            float availableSpace;
            if (currentLines < 3)
            {
                availableSpace = route.line.LineWidth * 2f;
            }
            else
            {
                int lanes = streetSeg?.street.lanes ?? 2;
                float width = StreetSegment.GetStreetWidth(
                    streetSeg?.street.type ?? Street.Type.Secondary,
                    lanes, RenderingDistance.Near);

                availableSpace = width * .7f;
            }

            var spacePerLine = availableSpace / currentLines;
            var gap = spacePerLine * 0.1f;
            var lineWidth = spacePerLine - gap;
            halfLineWidth = lineWidth * 0.5f;

            var baseOffset = -(availableSpace * .5f) + halfLineWidth;
            offset = baseOffset + (currentLineOffset * spacePerLine);
        }

        void UpdateRouteMesh(Route route,
                             Dictionary<Tuple<StreetSegment, int>, int> linesPerPositionMap,
                             Dictionary<Tuple<Line, StreetSegment, int>, int> offsetMap,
                             Dictionary<Tuple<StreetSegment, int>, int> latestOffsetMap)
        {
            var positions = route.positions;
            
            var newPositions = Enumerable.Repeat(Vector2.zero, positions.Count).ToList();
            var newWidths = Enumerable.Repeat(0f, positions.Count).ToList();

            var segments = new Dictionary<int, StreetSegment>();
            var numLines = new Dictionary<int, int>();
            var lineOffsets = new Dictionary<int, int>();

            for (var i = 0; i <= route.positions.Count;)
            {
                var pathSegment = route.GetSegmentForPosition(i);
                if (pathSegment == null)
                {
                    while (pathSegment == null)
                    {
                        ++i;
                        if (i >= route.positions.Count)
                        {
                            break;
                        }

                        pathSegment = route.GetSegmentForPosition(i);
                    }

                    continue;
                }

                var lane = pathSegment.lane;
                var streetSeg = pathSegment.segment;
                var startIdx = i;

                var key = Tuple.Create(streetSeg, lane);
                var currentLines = linesPerPositionMap[key];

                ++i;
                while (i < positions.Count)
                {
                    var nextSegment = route.GetSegmentForPosition(i);
                    if (nextSegment?.segment != streetSeg && i != positions.Count)
                    {
                        break;
                    }

                    ++i;
                }

                var offsetKey = Tuple.Create(route.line, streetSeg, lane);
                if (!offsetMap.TryGetValue(offsetKey, out int currentLineOffset))
                {
                    if (latestOffsetMap.TryGetValue(key, out currentLineOffset))
                    {
                        ++latestOffsetMap[key];
                        ++currentLineOffset;
                    }
                    else
                    {
                        latestOffsetMap.Add(key, 0);
                        currentLineOffset = 0;
                    }

                    offsetMap[offsetKey] = currentLineOffset;
                }

                segments.Add(startIdx, streetSeg);
                numLines.Add(startIdx, currentLines);
                lineOffsets.Add(startIdx, currentLineOffset);

                var endIdx = i;
                if (currentLineOffset >= MaxOverlappingLines)
                {
                    for (var j = startIdx; j < endIdx; ++j)
                    {
                        newPositions[j] = Vector3.positiveInfinity;
                    }

                    continue;
                }

                float offset, halfLineWidth;
                GetLineWidthAndOffset(route, streetSeg, currentLines, currentLineOffset,
                                      out halfLineWidth, out offset);

                var length = endIdx - startIdx;
                var range = MeshBuilder.GetOffsetPath(
                    positions.GetRange(startIdx, length), offset);

                for (var j = startIdx; j < endIdx; ++j)
                {
                    var nextPos = range[j - startIdx];
                    newPositions[j] = new Vector3(nextPos.x, nextPos.y, 0f);

                    newWidths[j] = halfLineWidth;
                }
            }

            for (var i = 0; i < route.positions.Count;)
            {
                var pathSegment = route.GetSegmentForPosition(i);
                if (pathSegment != null)
                {
                    ++i;
                    continue;
                }

                var startIdx = i++;
                while (i < positions.Count)
                {
                    var nextPathSegment = route.GetSegmentForPosition(i);
                    if (nextPathSegment != null)
                    {
                        break;
                    }

                    ++i;
                }

                var prevIdx = startIdx;
                while (!segments.ContainsKey(prevIdx))
                {
                    --prevIdx;

                    if (prevIdx < 0)
                    {
                        prevIdx = -1;
                        break;
                    }
                }

                var nextIdx = i - 1;
                while (!segments.ContainsKey(nextIdx))
                {
                    ++nextIdx;

                    if (nextIdx >= positions.Count)
                    {
                        nextIdx = -1;
                        break;
                    }
                }

                Debug.Assert(prevIdx != -1 || nextIdx != -1, "orphaned intersection?");

                if (prevIdx == -1 || nextIdx == -1)
                {
                    StreetSegment segment;
                    int currentLines;
                    int lineOffset;

                    if (prevIdx != -1)
                    {
                        segment = segments[prevIdx];
                        currentLines = numLines[prevIdx];
                        lineOffset = lineOffsets[prevIdx];
                    }
                    else
                    {
                        segment = segments[nextIdx];
                        currentLines = numLines[nextIdx];
                        lineOffset = lineOffsets[nextIdx];
                    }

                    var length = i - startIdx;
                    if (lineOffset >= MaxOverlappingLines)
                    {
                        for (var j = startIdx; j < startIdx + length; ++j)
                        {
                            newPositions[j] = Vector3.positiveInfinity;
                        }

                        continue;
                    }

                    float offset, halfLineWidth;
                    GetLineWidthAndOffset(route, segment, currentLines, lineOffset,
                                          out halfLineWidth, out offset);

                    var range = MeshBuilder.GetOffsetPath(
                        positions.GetRange(startIdx, length), offset);

                    for (var j = startIdx; j < startIdx + length; ++j)
                    {
                        var nextPos = range[j - startIdx];
                        newPositions[j] = new Vector3(nextPos.x, nextPos.y, 0f);

                        newWidths[j] = halfLineWidth;
                    }
                }
                else
                {
                    var prevSegment = segments[prevIdx];
                    var nextSegment = segments[nextIdx];

                    var prevLines = numLines[prevIdx];
                    var nextLines = numLines[nextIdx];

                    var prevLineOffset = lineOffsets[prevIdx];
                    var nextLineOffset = lineOffsets[nextIdx];

                    var z = 0f;
                    var length = i - startIdx;
                    if (prevLineOffset >= MaxOverlappingLines
                    && nextLineOffset >= MaxOverlappingLines)
                    {
                        for (var j = startIdx; j < startIdx + length; ++j)
                        {
                            newPositions[j] = Vector3.positiveInfinity;
                        }

                        continue;
                    }
                    else if (prevLineOffset >= MaxOverlappingLines
                    || nextLineOffset >= MaxOverlappingLines)
                    {
                        z += 1f;
                    }

                    float prevOffset, prevHalfLineWidth;
                    GetLineWidthAndOffset(route, prevSegment, prevLines, prevLineOffset,
                                          out prevHalfLineWidth, out prevOffset);

                    float nextOffset, nextHalfLineWidth;
                    GetLineWidthAndOffset(route, prevSegment, nextLines, nextLineOffset,
                                          out nextHalfLineWidth, out nextOffset);

                    var offsetDiff = nextOffset - prevOffset;
                    var widthDiff = nextHalfLineWidth - prevHalfLineWidth;

                    var offsetStep = offsetDiff / (length - 1);
                    var widthStep = widthDiff / (length - 1);

                    for (var j = 0; j < length; ++j)
                    {
                        newWidths[startIdx + j] = prevHalfLineWidth + j * widthStep;
                    }

                    var offsets = new List<float>();
                    for (var j = 0; j < length; ++j)
                    {
                        offsets.Add(prevOffset + j * offsetStep);
                    }

                    var range = MeshBuilder.GetOffsetPath(
                        positions.GetRange(startIdx, length), offsets);

                    for (var j = startIdx; j < i; ++j)
                    {
                        var nextPos = range[j - startIdx];
                        newPositions[j] = new Vector3(nextPos.x, nextPos.y, z);
                    }
                }
            }

            var collider = route.GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            //var mesh = MeshBuilder.CreateBakedLineMesh(newPositions, newWidths, collider);
            var mesh = MeshBuilder.CreateSmoothLine(
                newPositions, newWidths, 20, 0f, collider);

            route.UpdateMesh(mesh, newPositions, newWidths);
            route.EnableCollision();
        }
    }
}