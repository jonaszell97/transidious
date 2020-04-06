
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class MapTile : MonoBehaviour
    {
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
        public HashSet<ActivePath> activePaths;
        public Rect rect;
        RenderingDistance? currentRenderingDist;

        public Canvas canvas;
        public GameObject meshes;

        [SerializeField] SpriteRenderer backgroundImage;
        [SerializeField] BoxCollider2D boxCollider;

        BoxCollider2D[] boxColliders;
        List<ColliderInfo> colliderInfo;

        static List<LineRenderer> lineRenderers;

        public IEnumerable<StreetSegment> streetSegments => mapObjects.OfType<StreetSegment>();

        public IEnumerable<Stop> transitStops => mapObjects.OfType<Stop>();

        public void Initialize(Map map, int x, int y)
        {
            if (x == -1)
            {
                Debug.Assert(y == -1);
                this.name = "Shared Tile";
                this.rect = new Rect(map.minX, map.minY, map.width, map.height);
                this.boxCollider.enabled = false;
            }
            else
            {
                this.name = "Tile " + x + " " + y;
                this.rect = new Rect(x * map.tileSize, y * map.tileSize,
                                 map.tileSize, map.tileSize);
            }

            this.map = map;
            this.x = x;
            this.y = y;
            this.mapObjects = new HashSet<IMapObject>();
            this.activePaths = new HashSet<ActivePath>();
            this.colliderInfo = new List<ColliderInfo>();

            if (backgroundImage.sprite == null && x != -1)
            {
                UpdateSprite();
            }

            this.checkMouseOver = false;
        }

        public void UpdateSprite()
        {
            var sprite = Resources.Load<Sprite>($"Maps/{map.name}/{x}_{y}");
            if (sprite == null)
            {
                backgroundImage.color = map.GetDefaultBackgroundColor(MapDisplayMode.Day);
                sprite = Resources.Load<Sprite>("Sprites/ui_square");
            }
            else
            {
                backgroundImage.color = Color.white;
            }

            backgroundImage.transform.localScale = Vector3.one;
            backgroundImage.sprite = sprite;
            backgroundImage.transform.position = new Vector3(
                x * map.tileSize + map.tileSize * .5f,
                y * map.tileSize + map.tileSize * .5f,
                Map.Layer(MapLayer.NatureBackground));

            float spriteSize = backgroundImage.bounds.size.x;
            backgroundImage.transform.localScale = new Vector3(
                map.tileSize / spriteSize, map.tileSize / spriteSize, 1f);
        }

        public void Reset()
        {
            this.mapObjects.Clear();
            this.colliderInfo.Clear();
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

        public bool IsPointInTile(Vector2 pt)
        {
            return rect.Contains(pt);
        }

        public void FinalizeTile()
        {
            CreateColliders();
            currentRenderingDist = null;
        }

        void CreateColliders()
        {
            boxCollider.offset = rect.center;
            boxCollider.size = rect.size;
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

            currentRenderingDist = dist;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            switch (dist)
            {
                case RenderingDistance.Near:
                    meshes.SetActive(true);
                    backgroundImage.gameObject.SetActive(false);
                    canvas.gameObject.SetActive(true);
                    boxCollider.enabled = true;

                    break;
                case RenderingDistance.Far:
                    meshes.SetActive(false);
                    backgroundImage.gameObject.SetActive(true);
                    canvas.gameObject.SetActive(false);
                    boxCollider.enabled = false;

                    break;
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        ColliderInfo enteredMapObject = null;
        Vector3? mousePosition = null;

        void OnMouseDown()
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Check if a car was clicked.
            foreach (var car in activePaths)
            {
                var bounds = car.Bounds;
                if (bounds.Contains2D(clickedPos))
                {
                    car.OnMouseDown();
                    return;
                }
            }

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
                line.startWidth = .5f;
                line.endWidth = .5f;
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
            if (!map.Game.MouseOverActive(info.obj.Kind))
            {
                return;
            }

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
                line.material = GameController.instance.GetUnlitMaterial(color);
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

        private bool checkMouseOver;

        void OnMouseOver()
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            var mousePosWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (mousePosition.HasValue && mousePosWorld == mousePosition.Value && !checkMouseOver)
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
                        if (enteredMapObject.obj == info.obj)
                        {
                            if (checkMouseOver)
                            {
                                info.obj.OnMouseOver();
                            }

                            found = true;
                            break;
                        }

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

                    if (info.obj.ShouldCheckMouseOver())
                    {
                        checkMouseOver = true;
                        info.obj.OnMouseOver();
                    }
                    
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
            checkMouseOver = false;
        }

        public Serialization.MapTile ToProtobuf()
        {
            var tile = new Serialization.MapTile
            {
                X = (uint)x,
                Y = (uint)y,
            };

            tile.MapObjectIDs.AddRange(mapObjects.Select(obj => (uint)obj.Id));
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

            if (obj?.UniqueTile != null)
            {
                this.x = obj.UniqueTile.x;
                this.y = obj.UniqueTile.y;
            }
        }

        MapTile Current => map.GetTile(x, y);

        bool MoveNext()
        {
            if (obj?.UniqueTile != null)
            {
                return false;
            }
            
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