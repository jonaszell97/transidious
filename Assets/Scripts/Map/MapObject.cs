using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    // Used instead of dynamic dispatch for performance's sake.
    [System.Flags] public enum MapObjectKind
    {
        None                 = 0x0,
        Street               = 0x1,
        StreetIntersection   = 0x2,
        StreetSegment        = 0x4,
        Building             = 0x8,
        NaturalFeature       = 0x10,

        Line                 = 0x20,
        Route                = 0x40,
        Stop                 = 0x80,
        TemporaryStop        = 0x100,

        All                  = ~0x0,
    }

    public enum OccupancyKind
    {
        /// Someone who lives in the building.
        Resident,

        /// Someone who works in the building / feature.
        Worker,

        /// Someone who is a student at the building.
        Student,

        /// Someone who is shopping at the building.
        Customer,

        /// Someone who is visiting the building / feature.
        Visitor,

        /// Someone who is parking at the lot.
        ParkingCitizen,
        
        /// Sentinel value to mark the last occupancy kind.
        _Last,
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

        Vector2 Centroid { get; }
        Vector2 VisualCenter { get; }
        Transform transform { get; }
        Vector2[][] outlinePositions { get; }

        int GetCapacity(OccupancyKind kind);
        bool HasCapacity(OccupancyKind kind);
        int GetOccupancyCount(OccupancyKind kind);
        SortedSet<Citizen> GetOccupants(OccupancyKind kind);
        bool AddOccupant(OccupancyKind kind, Citizen c, bool force = false);
        void RemoveOccupant(OccupancyKind kind, Citizen c);

        void Hide();
        void Show(RenderingDistance renderingDistance);
        void Destroy();

        void ActivateModal();
        
        void OnMouseOver();
        void OnMouseEnter();
        void OnMouseExit();
        void OnMouseDown();

        bool ShouldCheckMouseOver();
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
        public Vector2 VisualCenter { get; set; }

        public int Id
        {
            get => id;
            set => id = value;
        }

        public bool Active => uniqueTile?.gameObject.activeSelf ?? true;

        public MapObjectKind Kind => kind;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public MapTile UniqueTile
        {
            get => uniqueTile;
            set => uniqueTile = value;
        }

        public Transform transform => null;

        public GameController Game => GameController.instance;

        public Vector2 Centroid => centroid;

        public virtual int GetCapacity(OccupancyKind kind)
        {
            return 0;
        }

        public bool HasCapacity(OccupancyKind kind)
        {
            return GetCapacity(kind) > GetOccupancyCount(kind);
        }

        public int GetOccupancyCount(OccupancyKind kind)
        {
            return Game.loadedMap.GetOccupancyCount(id, kind);
        }

        public SortedSet<Citizen> GetOccupants(OccupancyKind kind)
        {
            return Game.loadedMap.GetOccupants(id, kind);
        }

        public bool AddOccupant(OccupancyKind kind, Citizen c, bool force = false)
        {
            var map = Game.loadedMap;
            var capacity = GetCapacity(kind);

            if (!force && (capacity == 0 || map.GetOccupancyCount(id, kind) >= capacity))
            {
                return false;
            }

            Game.loadedMap.AddOccupant(id, kind, c);
            return true;
        }

        public void RemoveOccupant(OccupancyKind kind, Citizen c)
        {
            Game.loadedMap.RemoveOccupant(id, kind, c);
        }

        protected void Initialize(MapObjectKind kind, int id,
                                  float area = 0f,
                                  Vector2? centroid = null)
        {
            this.id = id;
            this.kind = kind;
            this.area = area;
            this.centroid = centroid.HasValue ? centroid.Value : Vector2.zero;
            this.VisualCenter = this.centroid;
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
                UniqueTileX = uniqueTile?.x ?? -1,
                UniqueTileY = uniqueTile?.y ?? -1,
                Area = area,
                Centroid = centroid.ToProtobuf(),
                VisualCenter = VisualCenter.ToProtobuf(),
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
            this.VisualCenter = obj.VisualCenter.Deserialize();

            if (obj.UniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.GetTile(obj.UniqueTileX, obj.UniqueTileY);
                this.uniqueTile = tile;
            }

            if (obj.OutlinePositions != null)
            {
                this.outlinePositions = obj.OutlinePositions.Select(arr => arr.OutlinePositions.Select(v => v.Deserialize()).ToArray()).ToArray();
            }
        }

        public virtual void ActivateModal()
        {
        }

        public virtual void OnMouseOver()
        {
            Game.input.MouseOverMapObject(this);
        }

        public virtual void OnMouseExit()
        {
            Game.input.MouseExitMapObject(this);
        }

        public virtual void OnMouseDown()
        {
            Game.input.MouseDownMapObject(this);
        }

        public virtual void OnMouseEnter()
        {
            Game.input.MouseEnterMapObject(this);
        }

        public virtual bool ShouldCheckMouseOver()
        {
            return false;
        }

        public virtual void Destroy() { }
    }

    public class DynamicMapObject : MonoBehaviour, IMapObject
    {
        public MapObjectKind kind;
        public int id;
        public MapTile uniqueTile;
        public bool highlighted = false;
        public new Renderer renderer;
        public new Collider2D collider;
        public Vector2 centroid;
        public Vector2 VisualCenter { get; set; }
        public Vector2[][] outlinePositions { get; }

        protected void Initialize(MapObjectKind kind, int id, Vector2 centroid)
        {
            this.id = id;
            this.kind = kind;
            this.centroid = centroid;
            this.renderer = GetComponent<Renderer>();
            this.collider = GetComponent<Collider2D>();
            this.VisualCenter = this.centroid;
        }

        public int Id
        {
            get => id;
            set => id = value;
        }

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public MapObjectKind Kind => kind;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public MapTile UniqueTile
        {
            get => uniqueTile;
            set => uniqueTile = value;
        }

        public GameController Game => GameController.instance;

        public InputController inputController => GameController.instance.input;

        public Vector2 Centroid => centroid;

        public virtual int GetCapacity(OccupancyKind kind)
        {
            return 0;
        }
        
        public bool HasCapacity(OccupancyKind kind)
        {
            return GetCapacity(kind) > GetOccupancyCount(kind);
        }

        public int GetOccupancyCount(OccupancyKind kind)
        {
            return Game.loadedMap.GetOccupancyCount(id, kind);
        }

        public SortedSet<Citizen> GetOccupants(OccupancyKind kind)
        {
            return Game.loadedMap.GetOccupants(id, kind);
        }

        public bool AddOccupant(OccupancyKind kind, Citizen c, bool force = false)
        {
            var map = Game.loadedMap;
            var capacity = GetCapacity(kind);

            if (!force && (capacity == 0 || map.GetOccupancyCount(id, kind) >= capacity))
            {
                return false;
            }

            Game.loadedMap.AddOccupant(id, kind, c);
            return true;
        }

        public void RemoveOccupant(OccupancyKind kind, Citizen c)
        {
            Game.loadedMap.RemoveOccupant(id, kind, c);
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

        public virtual void OnMouseOver()
        {
            inputController.MouseOverMapObject(this);
        }

        public virtual void OnMouseExit()
        {
            inputController.MouseExitMapObject(this);
        }

        public virtual void OnMouseDown()
        {
            inputController.MouseDownMapObject(this);
        }

        public virtual void OnMouseEnter()
        {
            inputController.MouseEnterMapObject(this);
        }

        public virtual bool ShouldCheckMouseOver()
        {
            return false;
        }

        public Serialization.MapObject ToProtobuf()
        {
            var result = new Serialization.MapObject
            {
                Id = (uint)id,
                Name = name,
                UniqueTileX = uniqueTile?.x ?? -1,
                UniqueTileY = uniqueTile?.y ?? -1,
                VisualCenter = VisualCenter.ToProtobuf(),
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
            this.VisualCenter = obj.VisualCenter.Deserialize();

            if (obj.UniqueTileX != -1)
            {
                var tile = GameController.instance.loadedMap.GetTile(obj.UniqueTileX, obj.UniqueTileY);
                this.uniqueTile = tile;
            }
        }

        public virtual void ActivateModal()
        {
        }

        public virtual void Destroy()
        {
            Destroy(this.gameObject);
        }
    }
}