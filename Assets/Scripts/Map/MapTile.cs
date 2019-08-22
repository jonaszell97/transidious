using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class MapTile : MonoBehaviour
    {
        [System.Serializable]
        public struct SerializableMapTile
        {
            public int[] mapObjectIDs;
            public int[] orphanedObjectIDs;
        }

        public Map map;
        public int x, y;
        public HashSet<MapObject> mapObjects;
        public Rect rect;
        RenderingDistance currentRenderingDist;
        public MultiMesh mesh;
        public HashSet<MapObject> orphanedObjects;

        public IEnumerable<StreetSegment> streetSegments
        {
            get
            {
                return mapObjects.OfType<StreetSegment>();
            }
        }

        public IEnumerable<Stop> transitStops
        {
            get
            {
                return mapObjects.OfType<Stop>();
            }
        }

        public void Initialize(Map map, int x, int y)
        {
            this.map = map;
            this.mesh.map = map;
            this.name = "Tile " + x + " " + y;
            this.transform.SetParent(map.transform);
            this.x = x;
            this.y = y;
            this.mapObjects = new HashSet<MapObject>();
            this.rect = new Rect(x * Map.tileSize, y * Map.tileSize,
                                 Map.tileSize, Map.tileSize);

            this.gameObject.SetActive(false);
        }

        public void AddOrphanedObject(MapObject obj)
        {
            if (orphanedObjects == null)
            {
                orphanedObjects = new HashSet<MapObject>();
            }

            orphanedObjects.Add(obj);
        }

        public void Show(RenderingDistance dist)
        {
            if (gameObject.activeSelf && currentRenderingDist == dist)
            {
                return;
            }

            mesh.UpdateScale(dist);
            currentRenderingDist = dist;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            if (orphanedObjects != null)
            {
                foreach (var obj in orphanedObjects)
                {
                    obj.Hide();
                }
            }
        }

        public SerializableMapTile Serialize()
        {
            return new SerializableMapTile
            {
                mapObjectIDs = mapObjects.Select(obj => obj.id).ToArray(),
                orphanedObjectIDs = orphanedObjects?.Select(obj => obj.id).ToArray(),
            };
        }

        public void Deserialize(Map map, SerializableMapTile tile)
        {
            foreach (var id in tile.mapObjectIDs)
            {
                var obj = map.GetMapObject(id);
                if (obj == null)
                {
                    Debug.LogWarning("missing map object with ID " + id);
                    continue;
                }

                this.mapObjects.Add(obj);
            }

            if (tile.orphanedObjectIDs != null)
            {
                this.orphanedObjects = new HashSet<MapObject>();

                foreach (var id in tile.orphanedObjectIDs)
                {
                    var obj = map.GetMapObject(id);
                    if (obj == null)
                    {
                        Debug.LogWarning("missing map object with ID " + id);
                        continue;
                    }

                    this.orphanedObjects.Add(obj);
                }
            }
        }
    }

    public struct TileIterator
    {
        Map map;
        MapObject obj;
        int x, y;

        public TileIterator(Map map, MapObject obj = null)
        {
            this.map = map;
            this.obj = obj;
            this.x = 0;
            this.y = 0;
        }

        MapTile Current
        {
            get
            {
                return map.tiles[x][y];
            }
        }

        bool MoveNext()
        {
            var foundNext = false;
            if (y < map.tilesHeight - 1)
            {
                ++y;
                foundNext = true;
            }
            else if (x < map.tilesWidth - 1)
            {
                ++x;
                y = 0;
                foundNext = true;
            }

            if (foundNext && obj != null)
            {
                if (!Current.mapObjects.Contains(obj))
                {
                    return MoveNext();
                }
            }

            return foundNext;
        }

        public IEnumerator<MapTile> GetEnumerator()
        {
            if (obj == null || Current.mapObjects.Contains(obj))
            {
                yield return Current;
            }

            while (MoveNext())
            {
                yield return Current;
            }

            yield break;
        }
    }
}