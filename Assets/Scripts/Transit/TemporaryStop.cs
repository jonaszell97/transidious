using UnityEngine;
using System;
using System.Collections.Generic;

namespace Transidious
{
    class TemporaryStop : DynamicMapObject
    {
        internal Vector3 position;

        public void Initialize(GameController game, string name, Vector3 position)
        {
            this.name = name;
            this.position = position;
            this.transform.position = new Vector3(position.x, position.y, Map.Layer(MapLayer.TransitStops));
        }
    }

    class TemporaryLine
    {
        internal string name;
        internal List<DynamicMapObject> stops;
        internal List<Vector3> completePath;
        internal List<int> paths;
        internal List<List<TrafficSimulator.PathSegmentInfo>> streetSegments;
    }
}