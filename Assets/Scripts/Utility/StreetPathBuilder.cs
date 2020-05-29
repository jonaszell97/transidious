using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;
using UnityEngine;

namespace Transidious
{
    public class StreetPathBuilder
    {
        /// Cached street paths.
        private Dictionary<Tuple<StreetSegment, int>, PathSegment> _streetPaths;

        /// Cached intersection paths.
        private Dictionary<Tuple<StreetIntersection, StreetSegment, StreetSegment>, PathSegment> _intersectionPaths;

        /// C'tor.
        public StreetPathBuilder()
        {
            _streetPaths = new Dictionary<Tuple<StreetSegment, int>, PathSegment>();
            _intersectionPaths = new Dictionary<Tuple<StreetIntersection, StreetSegment, StreetSegment>, PathSegment>();
        }

        /// Get the path for a particular lane of a street.
        public PathSegment GetPath(StreetSegment seg, int lane)
        {
            var key = Tuple.Create(seg, lane);
            if (!_streetPaths.ContainsKey(key))
            {
                ComputePaths(seg);
            }

            return _streetPaths[key];
        }

        /// Get the path or an intersection.
        public PathSegment GetIntersectionPath(StreetIntersection intersection,
                                               StreetSegment from, StreetSegment to)
        {
            var key = Tuple.Create(intersection, from, to);
            if (!_intersectionPaths.ContainsKey(key))
            {
                ComputePaths(intersection);
            }

            return _intersectionPaths[key];
        }

        /// Get the path for a path step.
        public PathSegment GetStepPath(PathStep step)
        {
            switch (step)
            {
                case DriveStep driveStep:
                {
                    return GetPath(driveStep.driveSegment.segment,
                        GetDefaultLane(driveStep.driveSegment.segment, driveStep.driveSegment.backward));
                }
                case TurnStep turnStep:
                {
                    return GetIntersectionPath(turnStep.intersection, turnStep.from.segment, turnStep.to.segment);
                }
                case PartialDriveStep partialDriveStep:
                {
                    var driveSegment = partialDriveStep.driveSegment;
                    var lane = GetDefaultLane(driveSegment.segment, driveSegment.backward);
                    var points = GetPath(driveSegment.segment, lane).Points;

                    var partialStart = partialDriveStep.partialStart;
                    var partialEnd = partialDriveStep.partialEnd;
                    
                    var minIdx = 0;
                    Vector2 startPos;

                    if (partialStart)
                    {
                        var startDistance = (partialDriveStep.startPos - points[0]).sqrMagnitude;
                        for (int i = 0; i < points.Length; ++i)
                        {
                            var pt = points[i];
                            var dist = (pt - points[0]).sqrMagnitude;
                            var cmp = dist.CompareTo(startDistance);

                            if (cmp > 0)
                            {
                                minIdx = i;
                                break;
                            }
                            else if (cmp == 0)
                            {
                                minIdx = i + 1;
                                break;
                            }
                        }

                        startPos = partialDriveStep.startPos;
                    }
                    else
                    {
                        startPos = points.First();
                    }

                    var maxIdx = points.Length;
                    Vector2 endPos;

                    if (partialEnd)
                    {
                        var endDistance = (partialDriveStep.endPos - points[0]).sqrMagnitude;
                        for (int i = 0; i < points.Length; ++i)
                        {
                            var pt = points[i];
                            var dist = (pt - points[0]).sqrMagnitude;
                            var cmp = dist.CompareTo(endDistance);

                            if (cmp >= 0)
                            {
                                maxIdx = i;
                                break;
                            }
                        }

                        endPos = partialDriveStep.endPos;
                    }
                    else
                    {
                        endPos = points.Last();
                    }

                    var positions = new List<Vector2>();

                    // This can happen when the the start and end position are the first position on
                    // the street, which is technically still a valid path since an intersection will be
                    // crossed.
                    if (startPos.Equals(endPos))
                    {
                        Vector2 direction;
                        if (startPos.Equals(points.First()))
                        {
                            direction = (points[1] - startPos).normalized;
                        }
                        else
                        {
                            Debug.Assert(startPos.Equals(points.Last()));
                            direction = (endPos - points[points.Length - 2]).normalized;
                        }

                        // FIXME is there a better fix for this? Seems a tad hacky
                        positions.Add(startPos);
                        positions.Add(startPos + direction * .001f);
                    }
                    else
                    {
                        if (partialStart)
                        {
                            positions.Add(startPos);
                        }

                        for (int i = minIdx; i < maxIdx; ++i)
                        {
                            positions.Add(points[i]);
                        }

                        if (partialEnd)
                        {
                            positions.Add(endPos);
                        }
                    }

                    return new PathSegment(positions);
                }
                default:
                    Debug.LogError("not a drive step!");
                    return default;
            }
        }
        
        /// The default lane to use for a segment.
        public int GetDefaultLane(StreetSegment seg, bool backward)
        {
            if (backward && !seg.IsOneWay)
            {
                return seg.LeftmostLane;
            }

            return seg.RightmostLane;
        }

        /// Compute paths for a street segment.
        private void ComputePaths(StreetSegment seg)
        {
            var lanes = seg.street.lanes;
            var halfLanes = lanes / 2;
            var offset = seg.GetStreetWidth(RenderingDistance.Near) / lanes;
            var segPositions = seg.drivablePositions;

            var positions = new List<Vector2>();
            for (int lane = 0; lane < lanes; ++lane)
            {
                var isLeftLane = lane < halfLanes;
                var laneOffset = seg.LanePositionFromMiddle(lane, true);

                var currentOffset = offset * laneOffset;
                if (isLeftLane)
                {
                    currentOffset = -currentOffset;
                }

                var perpendicular = Vector3.zero;
                for (int j = 1; j < segPositions.Count; ++j)
                {
                    Vector3 p0 = segPositions[j - 1];
                    Vector3 p1 = segPositions[j];

                    if (j == 1)
                    {
                        var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, out perpendicular);
                        positions.Add(offsetPoints.Item1);
                        positions.Add(offsetPoints.Item2);
                    }
                    else
                    {
                        var offsetPoints = GetOffsetPoints(p0, p1, currentOffset, perpendicular,
                            out perpendicular);

                        positions.Add(offsetPoints.Item2);
                    }
                }

                if (isLeftLane && !seg.OneWay)
                {
                    positions.Reverse();
                }

                _streetPaths.Add(Tuple.Create(seg, lane), new PathSegment(positions));
                positions.Clear();
            }
        }

        private HashSet<StreetIntersection> _int = new HashSet<StreetIntersection>();

        /// Compute paths for an intersection.
        private void ComputePaths(StreetIntersection intersection)
        {
            foreach (var from in intersection.IncomingStreets)
            {
                var endsHere = from.endIntersection == intersection;
                var fromLane = GetDefaultLane(from, !endsHere);

                foreach (var to in intersection.OutgoingStreets)
                {
                    if (from.name == "Charlottenburger Ufer, Segment #2" &&
                        to.name == "Charlottenburger Ufer, Segment #1")
                    {
                        Debug.Break();
                    }
                    var toLane = GetDefaultLane(to, to.endIntersection == intersection);
                    var uturn = from == to;

                    Vector2 p0_A, p1_A, p0_B, p1_B;

                    var incomingPath = GetPath(from, fromLane).Points;
                    p0_A = incomingPath[incomingPath.Length - 2];
                    p1_A = incomingPath[incomingPath.Length - 1];

                    var outgoingPath = GetPath(to, toLane).Points;
                    p0_B = outgoingPath[0];
                    p1_B = outgoingPath[1];

                    // If the streets are almost parallel, an intersection point 
                    // might not make sense.
                    PathSegment pathSegment;
                    if (uturn)
                    {
                        var controlPt1 = p1_A + (p1_A - p0_A).normalized * 5f;
                        var controlPt2 = p0_B + (p0_B - p1_B).normalized * 5f;

                        pathSegment = new PathSegment(p1_A, controlPt1, controlPt2, p0_B);
                    }
                    else if (!Math.EquivalentAngles(p0_A, p1_A, p0_B, p1_B, 15f))
                    {
                        var intPt = Math.GetIntersectionPoint(
                            p0_A, p1_A, p0_B, p1_B, out bool found);

                        Debug.Assert(found, "streets do not intersect!");
                        pathSegment = new PathSegment(p1_A, intPt, p0_B);
                    }
                    else
                    {
                        pathSegment = new PathSegment(p1_A, p0_B);
                    }

                    var key = Tuple.Create(intersection, from, to);
                    _intersectionPaths.Add(key, pathSegment);
                }
            }
        }

        /// Utility function for path computation. Return the vector between p0 and p1 offset by `currentOffset`.
        private  static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
            float currentOffset, out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            p0 = p0 + (perpendicular * currentOffset);
            p1 = p1 + (perpendicular * currentOffset);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }

        /// Utility function for path computation. Return the vector between p0 and p1 offset by `currentOffset`.
        private static Tuple<Vector3, Vector3> GetOffsetPoints(Vector3 p0, Vector3 p1,
            float currentOffset, Vector3 prevPerpendicular,
            out Vector3 perpendicular)
        {
            var dir = p1 - p0;
            var perpendicular2d = -Vector2.Perpendicular(new Vector2(dir.x, dir.y)).normalized;
            perpendicular = new Vector3(perpendicular2d.x, perpendicular2d.y, 0f);

            var mid = (perpendicular + prevPerpendicular).normalized;
            perpendicular = mid;

            p0 = p0 + (mid * currentOffset);
            p1 = p1 + (mid * currentOffset);

            return new Tuple<Vector3, Vector3>(p0, p1);
        }
    }
}