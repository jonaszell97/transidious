using System.Linq;
using UnityEngine;

namespace Transidious
{
    // Used instead of dynamic dispatch for performance's sake.
    public enum MapObjectKind
    {
        Street,
        StreetIntersection,
        StreetSegment,
        Building,
        NaturalFeature,

        Line,
        Route,
        Stop,
    }

    [System.Serializable]
    public struct SerializableMapObject
    {
        public int id;
        public string name;
        public int uniqueTileX, uniqueTileY;
        public SerializableVector2[][] outlinePositions;
        public float area;
        public SerializableVector2 centroid;
    }

    public interface IMapObject
    {
        int Id { get; set; }
        MapObjectKind Kind { get; }

        string Name { get; set; }
        MapTile UniqueTile { get; set; }
        bool Active { get; }

        Transform transform { get; }
        Vector2[][] outlinePositions { get; }

        void Hide();
        void Show(RenderingDistance renderingDistance);
    }

    public static class MapObjectExtensions
    {

    }

    public class StaticMapObject : IMapObject
    {
        public MapObjectKind kind;
        public int id;
        public string name;
        public MapTile uniqueTile;
        public Vector2[][] outlinePositions { get; set; }
        public float area;
        public Vector2 centroid;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public bool Active
        {
            get
            {
                return uniqueTile?.gameObject.activeSelf ?? true;
            }
        }

        public MapObjectKind Kind
        {
            get
            {
                return kind;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public MapTile UniqueTile
        {
            get
            {
                return uniqueTile;
            }
            set
            {
                uniqueTile = value;
            }
        }

        public Transform transform
        {
            get
            {
                return null;
            }
        }

        public GameController Game
        {
            get
            {
                return GameController.instance;
            }
        }

        protected void Initialize(MapObjectKind kind, int id,
                                  float area = 0f,
                                  Vector2? centroid = null)
        {
            this.id = id;
            this.kind = kind;
            this.area = area;
            this.centroid = centroid.HasValue ? centroid.Value : Vector2.zero;
        }

        public void Hide() { }

        public void Show(RenderingDistance renderingDistance) { }

        public virtual Color GetColor()
        {
            return Color.clear;
        }

        public Serialization.MapObject ToProtobuf()
        {
            var result = new Serialization.MapObject
            {
                Id = (uint)id,
                Name = name,
                UniqueTileX = uniqueTile != null ? uniqueTile.x : -1,
                UniqueTileY = uniqueTile != null ? uniqueTile.y : -1,
                Area = area,
                Centroid = centroid.ToProtobuf(),
            };

            if (outlinePositions != null)
            {
                foreach (var outline in outlinePositions)
                {
                    var vec = new Serialization.MapObject.Types.Outline();
                    foreach (var vert in outline)
                    {
                        vec.OutlinePositions.Add(vert.ToProtobuf());
                    }

                    result.OutlinePositions.Add(vec);
                }
            }

            return result;
        }

        public void Deserialize(Serialization.MapObject obj)
        {
            this.id = (int)obj.Id;
            this.name = obj.Name;
            this.area = obj.Area;
            this.centroid = obj.Centroid.Deserialize();

            if (obj.UniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.tiles[obj.UniqueTileX][obj.UniqueTileY];
                this.uniqueTile = tile;
            }

            if (obj.OutlinePositions != null)
            {
                this.outlinePositions = obj.OutlinePositions.Select(arr => arr.OutlinePositions.Select(v => v.Deserialize()).ToArray()).ToArray();
            }
        }

        public SerializableMapObject Serialize()
        {
            return new SerializableMapObject
            {
                id = this.id,
                name = this.name,
                uniqueTileX = uniqueTile != null ? uniqueTile.x : -1,
                uniqueTileY = uniqueTile != null ? uniqueTile.y : -1,
                outlinePositions = outlinePositions?.Select(
                    arr => arr.Select(v => new SerializableVector2(v)).ToArray()).ToArray(),
                area = area,
                centroid = new SerializableVector2(centroid),
            };
        }

        public void Deserialize(SerializableMapObject obj)
        {
            this.id = obj.id;
            this.name = obj.name;
            this.area = obj.area;
            this.centroid = obj.centroid;

            if (obj.uniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.tiles
                    [obj.uniqueTileX][obj.uniqueTileY];

                this.uniqueTile = tile;
            }

            if (obj.outlinePositions != null)
            {
                outlinePositions = obj.outlinePositions.Select(
                    arr => arr.Select(v => (Vector2)v).ToArray()).ToArray();
            }
        }

        public virtual void OnMouseDown() { }

        public virtual void OnMouseEnter() { }

        public virtual void OnMouseExit() { }
    }

    public class DynamicMapObject : MonoBehaviour, IMapObject
    {
        public MapObjectKind kind;
        public int id;
        public MapTile uniqueTile;
        public bool highlighted = false;
        public new Renderer renderer;
        public new Collider2D collider;
        public Vector2[][] outlinePositions { get; }

        protected void Initialize(MapObjectKind kind, int id)
        {
            this.id = id;
            this.kind = kind;
            this.renderer = GetComponent<Renderer>();
            this.collider = GetComponent<Collider2D>();
        }

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public bool Active
        {
            get
            {
                return gameObject.activeSelf;
            }

            set
            {
                gameObject.SetActive(value);
            }
        }

        public MapObjectKind Kind
        {
            get
            {
                return kind;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public MapTile UniqueTile
        {
            get
            {
                return uniqueTile;
            }
            set
            {
                uniqueTile = value;
            }
        }

        public GameController Game
        {
            get
            {
                return GameController.instance;
            }
        }

        public InputController inputController
        {
            get
            {
                return GameController.instance.input;
            }
        }

        public void Hide()
        {
            switch (kind)
            {
            default:
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
                if (collider != null)
                {
                    collider.enabled = false;
                }

                break;
            }
        }

        public void Show(RenderingDistance dist)
        {
            switch (kind)
            {
            default:
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
                if (collider != null)
                {
                    collider.enabled = true;
                }

                break;
            }
        }

        public void DisableCollision()
        {
            var collider = this.gameObject.GetComponent<Collider2D>();
            if (collider)
            {
                collider.enabled = false;
            }
        }

        public void EnableCollision()
        {
            var collider = this.gameObject.GetComponent<Collider2D>();
            if (collider)
            {
                collider.enabled = true;
            }
        }

        public void ToggleCollision()
        {
            var collider = this.gameObject.GetComponent<Collider2D>();
            if (collider)
            {
                collider.enabled = !collider.enabled;
            }
        }

        protected virtual void OnMouseOver()
        {
            inputController.MouseOverMapObject(this);
        }

        protected virtual void OnMouseExit()
        {
            inputController.MouseExitMapObject(this);
        }

        protected virtual void OnMouseDown()
        {
            inputController.MouseDownMapObject(this);
        }

        protected virtual void OnMouseEnter()
        {
            inputController.MouseEnterMapObject(this);
        }

        public SerializableMapObject Serialize()
        {
            return new SerializableMapObject
            {
                id = this.id,
                name = this.name,
                uniqueTileX = uniqueTile != null ? uniqueTile.x : -1,
                uniqueTileY = uniqueTile != null ? uniqueTile.y : -1,
            };
        }

        public void Deserialize(Serialization.MapObject obj)
        {
            this.id = (int)obj.Id;
            this.name = obj.Name;

            if (obj.UniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.tiles[obj.UniqueTileX][obj.UniqueTileY];
                this.uniqueTile = tile;
            }
        }

        public void Deserialize(SerializableMapObject obj)
        {
            this.id = obj.id;
            this.name = obj.name;

            if (obj.uniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.tiles
                    [obj.uniqueTileX][obj.uniqueTileY];

                this.uniqueTile = tile;
                this.transform.SetParent(tile.transform);
            }
        }
    }
}