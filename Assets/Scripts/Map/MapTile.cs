using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    public class MapTile : MonoBehaviour
    {
        [System.Serializable]
        public struct SerializableMapTile
        {
            public int x, y;
            public int[] mapObjectIDs;
            public int[] orphanedObjectIDs;
        }

        class ColliderInfo
        {
            internal Rect boundingBox;
            internal Vector2[][] poly;
            internal StaticMapObject obj;
            internal bool shouldHighlight;
        }

        public Map map;
        public int x, y;
        public HashSet<IMapObject> mapObjects;
        public Rect rect;
        RenderingDistance? currentRenderingDist;
        public HashSet<IMapObject> orphanedObjects;

        Dictionary<string, MultiMesh> meshes;

        BoxCollider2D[] boxColliders;
        List<ColliderInfo> colliderInfo;

        static List<LineRenderer> lineRenderers;

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
            this.meshes = new Dictionary<string, MultiMesh>();
            this.name = "Tile " + x + " " + y;
            this.transform.SetParent(map.transform);
            this.x = x;
            this.y = y;
            this.mapObjects = new HashSet<IMapObject>();
            this.colliderInfo = new List<ColliderInfo>();
            this.rect = new Rect(x * Map.tileSize, y * Map.tileSize,
                                 Map.tileSize, Map.tileSize);

            this.gameObject.SetActive(false);
        }

        public void Reset()
        {
            this.mapObjects.Clear();
            this.colliderInfo.Clear();
        }

        public void AddOrphanedObject(IMapObject obj)
        {
            if (orphanedObjects == null)
            {
                orphanedObjects = new HashSet<IMapObject>();
            }

            orphanedObjects.Add(obj);
        }

        public void AddCollider(StaticMapObject obj,
                                Vector2[] points,
                                Rect boundingBox,
                                bool shouldHighlight = false)
        {
            AddCollider(obj, new Vector2[][] { points }, boundingBox, shouldHighlight);
        }

            public void AddCollider(StaticMapObject obj,
                                Vector2[][] points,
                                Rect boundingBox,
                                bool shouldHighlight = false)
        {
            // var pathCount = polygonCollider.pathCount;
            // polygonCollider.pathCount++;
            // polygonCollider.SetPath(pathCount, boundingBox.Points());
            // polygonCollider.SetPath(pathCount, points);

            colliderInfo.Add(new ColliderInfo
            {
                boundingBox = boundingBox,
                poly = points,
                obj = obj,
                shouldHighlight = shouldHighlight,
            });
        }

        public void AddCollider(StaticMapObject obj,
                                Rect rect,
                                bool shouldHighlight = false)
        {
            AddCollider(obj, new Vector2[][]
            {
                new Vector2[]
                {
                    new Vector2(rect.x, rect.y),                            // bl
                    new Vector2(rect.x, rect.y + rect.height),              // tl
                    new Vector2(rect.x + rect.width, rect.y + rect.height), // tr
                    new Vector2(rect.x + rect.width, rect.y),               // br
                }
            }, rect, shouldHighlight);
        }

        public void AddCollider(StaticMapObject obj,
                                Bounds bounds,
                                bool shouldHighlight = false)
        {
            var points = new Vector2[]
            {
                new Vector2(bounds.min.x, bounds.min.y), // bl
                new Vector2(bounds.min.x, bounds.max.y), // tl
                new Vector2(bounds.max.x, bounds.max.y), // tr
                new Vector2(bounds.max.x, bounds.min.y), // br
            };

            AddCollider(obj, new Vector2[][] { points },
                        new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y),
                        shouldHighlight);
        }

        public MultiMesh GetMesh(string category)
        {
            if (meshes.TryGetValue(category, out MultiMesh mesh))
            {
                return mesh;
            }

            mesh = MultiMesh.Create(map, category, this.transform);
            meshes.Add(category, mesh);

            return mesh;
        }

        public void AddMesh(string category, Mesh mesh, Color c, float z = 0f, RenderingDistance dist = RenderingDistance.Near)
        {
            var multiMesh = GetMesh(category);
            multiMesh.AddMesh(c, mesh, dist, z);
        }

        public void FinalizeTile()
        {
            CreateColliders();
            currentRenderingDist = null;

            foreach (var mesh in meshes)
            {
                mesh.Value.CreateMeshes();
            }
        }

        public void CreateColliders()
        {
            var collider = this.gameObject.AddComponent<BoxCollider2D>();
            collider.offset = rect.center;
            collider.size = rect.size;
        }

        public void CreateClusteredColliders()
        {
            var centroids = new HashSet<Vector2>();
            var centroidMap = new Dictionary<Vector2, ColliderInfo>();

            foreach (var info in colliderInfo)
            {
#if DEBUG
                if (centroidMap.ContainsKey(info.obj.centroid))
                {
                    Debug.LogWarning("same centroid: " + centroidMap[info.obj.centroid].obj.name
                        + ", " + info.obj.name);
                    continue;
                }
#endif

                centroids.Add(info.obj.centroid);
                centroidMap.Add(info.obj.centroid, info);
            }

#if DEBUG
            List<List<Vector2>> clusters = null;
            this.RunTimer("Clustering " + centroids.Count + " points", () =>
            {
                clusters = Math.Cluster_DBSCAN(centroids.ToArray(), 100f, 5);
            });

            Debug.Log(clusters.Count + " clusters, "
                + clusters.Where(c => c.Count == 1).Count() + " noise clusters, "
                + clusters.Where(c => c.Count == 0).Count() + " empty clusters");
#else
            var clusters = Math.Cluster_DBSCAN(centroids.ToArray(), 100f, 5);
#endif

            this.boxColliders = new BoxCollider2D[clusters.Count];

            var i = 0;
            var points = new List<Vector2>();

            foreach (var cluster in clusters)
            {
                if (cluster.Count == 0)
                {
                    continue;
                }

                foreach (var centroid in cluster)
                {
                    var info = centroidMap[centroid];

                    foreach (var pts in info.poly)
                    {
                        points.AddRange(pts);
                    }
                }

                var boundingBox = Math.GetBoundingRect(points);
                var collider = this.gameObject.AddComponent<BoxCollider2D>();
                collider.offset = boundingBox.center;
                collider.size = boundingBox.size;

                boxColliders[i++] = collider;
                points.Clear();
            }
        }

        public void Show(RenderingDistance dist)
        {
            if (gameObject.activeSelf && currentRenderingDist.HasValue && currentRenderingDist.Value == dist)
            {
                return;
            }

            foreach (var mesh in meshes)
            {
                mesh.Value.UpdateScale(dist);
            }

            currentRenderingDist = dist;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            //if (orphanedObjects != null)
            //{
            //    foreach (var obj in orphanedObjects)
            //    {
            //        obj.Hide();
            //    }
            //}
        }

        ColliderInfo enteredMapObject = null;
        Vector3? mousePosition = null;

        void OnMouseDown()
        {
            enteredMapObject?.obj.OnMouseDown();
        }

        LineRenderer GetLineRenderer(int i)
        {
            if (lineRenderers == null)
            {
                lineRenderers = new List<LineRenderer>();
            }

            while (i >= lineRenderers.Count)
            {
                var obj = Instantiate(GameController.instance.lineRendererPrefab);
                var line = obj.GetComponent<LineRenderer>();
                line.transform.SetParent(map.transform);
                line.startWidth = 1f;
                line.endWidth = 1f;
                line.loop = true;

                lineRenderers.Add(line);
            }

            return lineRenderers[i];
        }

        Color GetHighlightColor(StaticMapObject obj)
        {
            return MeshBuilder.IncreaseOrDecreaseBrightness(obj.GetColor(), .3f);
        }

        void Highlight(ColliderInfo info)
        {
            var pointArr = info.obj.outlinePositions;
            if (pointArr == null)
            {
                return;
            }

            var i = 0;
            var layer = Map.Layer(MapLayer.Foreground);
            var color = GetHighlightColor(info.obj);

            foreach (var points in pointArr)
            {
                var line = GetLineRenderer(i++);
                line.material = GameController.GetUnlitMaterial(color);
                line.positionCount = points.Length;
                line.SetPositions(points.Select(v => new Vector3(v.x, v.y, layer)).ToArray());
                line.gameObject.SetActive(true);
            }
        }

        void Unhighlight(ColliderInfo info)
        {
            if (lineRenderers != null)
            {
                foreach (var line in lineRenderers)
                {
                    line.gameObject.SetActive(false);
                }
            }
        }

        void OnMouseOver()
        {
            var mousePosWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (mousePosition.HasValue && mousePosWorld == mousePosition.Value)
            {
                return;
            }

            var found = false;
            foreach (var info in colliderInfo)
            {
                if (info.boundingBox.Contains(mousePosWorld)
                && Math.IsPointInPolygon(mousePosWorld, info.poly))
                {
                    if (enteredMapObject != null)
                    {
                        enteredMapObject.obj.OnMouseExit();
                        Unhighlight(enteredMapObject);
                    }

                    if (info.shouldHighlight)
                    {
                        Highlight(info);
                    }

                    found = true;
                    enteredMapObject = info;
                    mousePosition = mousePosWorld;
                    info.obj.OnMouseEnter();

                    break;
                }
            }

            if (!found)
            {
                this.OnMouseExit();
            }
        }

        void OnMouseExit()
        {
            if (enteredMapObject == null)
            {
                return;
            }

            enteredMapObject.obj.OnMouseExit();
            Unhighlight(enteredMapObject);
            enteredMapObject = null;
            mousePosition = null;
        }

        public Serialization.MapTile ToProtobuf()
        {
            var tile = new Serialization.MapTile
            {
                X = (uint)x,
                Y = (uint)y,
            };

            tile.MapObjectIDs.AddRange(mapObjects.Select(obj => (uint)obj.Id));

            if (orphanedObjects != null)
                tile.OrphanedObjectIDs.AddRange(orphanedObjects.Select(obj => (uint)obj.Id));

            return tile;
        }

        public void Deserialize(Serialization.MapTile tile)
        {
            foreach (var id in tile.MapObjectIDs)
            {
                var obj = map.GetMapObject((int)id);
                if (obj == null)
                {
                    Debug.LogWarning("missing map object with ID " + id);
                    continue;
                }

                this.mapObjects.Add(obj);
            }

            if (tile.OrphanedObjectIDs != null)
            {
                this.orphanedObjects = new HashSet<IMapObject>();

                foreach (var id in tile.OrphanedObjectIDs)
                {
                    var obj = map.GetMapObject((int)id);
                    if (obj == null)
                    {
                        Debug.LogWarning("missing map object with ID " + id);
                        continue;
                    }

                    this.orphanedObjects.Add(obj);
                }
            }
        }

        public SerializableMapTile Serialize()
        {
            return new SerializableMapTile
            {
                x = x,
                y = y,
                mapObjectIDs = mapObjects.Select(obj => obj.Id).ToArray(),
                orphanedObjectIDs = orphanedObjects?.Select(obj => obj.Id).ToArray(),
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
                this.orphanedObjects = new HashSet<IMapObject>();

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
        IMapObject obj;
        bool activeOnly;
        int x, y;

        public TileIterator(Map map, IMapObject obj = null, bool activeOnly = false)
        {
            this.map = map;
            this.obj = obj;
            this.x = 0;
            this.y = 0;
            this.activeOnly = activeOnly;
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
            if (foundNext && activeOnly)
            {
                if (!Current.gameObject.activeSelf)
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