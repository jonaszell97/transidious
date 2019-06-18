using UnityEngine;

namespace Transidious
{
    public class MapLoader
    {
        public Map map;
        public OSMImportHelper.Area area;

        public MapLoader(Map map, OSMImportHelper.Area area)
        {
            this.map = map;
            this.area = area;
        }

        public void ImportArea()
        {
            map.LoadFromFile(area.ToString());
        }
    }
}