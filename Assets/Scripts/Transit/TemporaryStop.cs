using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    class TemporaryStop : MapObject
    {
        internal Vector3 position;

        public void Initialize(GameController game, string name, Vector3 position)
        {
            base.inputController = game.input;
            this.name = name;
            this.position = position;
            this.transform.position = new Vector3(position.x, position.y, Map.Layer(MapLayer.TransitStops));
        }
    }

    class TemporaryLine
    {
        internal string name;
        internal List<MapObject> stops;
        internal List<Vector3> completePath;
        internal List<int> paths;
    }
}