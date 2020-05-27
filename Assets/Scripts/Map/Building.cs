using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class Building : StaticMapObject
    {
        public enum Type
        {
            Residential,
            Shop,
            Office,
            ElementarySchool,
            HighSchool,
            University,
            Hospital,
            Stadium,
            Airport,
            GroceryStore,
            Leisure,
            Industrial,
            Church,
            Sight,
            Hotel,
            Other,
        }

        public Mesh mesh;
        public StreetSegment street;
        public int streetID;
        public string number;

        public Type type;
        public Rect collisionRect;
        public byte[] Capacities;

        public void Initialize(Map map, Type type,
                               StreetSegment street, string numberStr,
                               Mesh mesh, float area, string name = "",
                               Vector2? centroid = null, int id = -1)
        {
#if DEBUG
            if (name.Length == 0)
            {
                name = "#" + id.ToString();
            }
#endif

            base.Initialize(MapObjectKind.Building, id, area, centroid);

            this.type = type;
            this.street = street;
            this.number = numberStr;
            this.number = numberStr;
            this.mesh = mesh;
            this.name = name;

            InitializeCapacities();
        }

        public override int GetCapacity(OccupancyKind kind)
        {
            return Capacities[(int) kind];
        }

        public void UpdateStreet(Map map)
        {
            var closest = map.GetClosestStreet(this.centroid);
            if (closest != null)
            {
                street = closest.street;
            }
        }

        public int GetDefaultCapacity()
        {
            return GetDefaultCapacity(type, area);
        }

        public void InitializeCapacities()
        {
            Capacities = new byte[(int)OccupancyKind.ParkingCitizen + 1];

            switch (type)
            {
                case Type.Residential:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Resident] = capacity;
                    Capacities[(int) OccupancyKind.Visitor] = capacity;

                    break;
                }
                case Type.Shop:
                case Type.GroceryStore:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Worker] = (byte) (capacity / 2);
                    Capacities[(int) OccupancyKind.Customer] = capacity;

                    break;
                }
                case Type.Office:
                case Type.Industrial:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Worker] = capacity;

                    break;
                }
                case Type.ElementarySchool:
                case Type.HighSchool:
                case Type.University:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Student] = capacity;

                    break;
                }
                case Type.Hospital:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Worker] = (byte) (capacity / 2);
                    Capacities[(int) OccupancyKind.Visitor] = capacity;

                    break;
                }
                case Type.Stadium:
                case Type.Airport:
                case Type.Hotel:
                case Type.Church:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Worker] = (byte) (capacity / 3);
                    Capacities[(int) OccupancyKind.Customer] = capacity;

                    break;
                }
                case Type.Leisure:
                case Type.Sight:
                {
                    var capacity = (byte) GetDefaultCapacity();
                    Capacities[(int) OccupancyKind.Visitor] = capacity;

                    break;
                }
                case Type.Other:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int GetDefaultCapacity(Type type, float area)
        {
            switch (type)
            {
            case Type.Residential:
                // Calculate with an average of 2 floors and 1 person per 40m^2.
                int floors;
                if (area < 200)
                {
                    floors = 1;
                }
                else
                {
                    floors = 3;
                }

                return (int)Mathf.Ceil(floors * (area / 400f));
            case Type.Office:
                // Calculate with an average of 3 floors and 1 person per 10m^2.
                return (int)Mathf.Ceil(3 * (area / 200f));
            case Type.Shop:
            case Type.GroceryStore:
                // Calculate with an average of 1 floors and 1 worker per 100m^2.
                return (int)Mathf.Ceil(1 * (area / 500f));
            case Type.Hospital:
                // Calculate with an average of 5 floors and 1 worker per 20m^2.
                return (int)Mathf.Ceil(5 * (area / 200f));
            case Type.ElementarySchool:
                // Calculate with an average of 2 floors and 1 student per 5m^2.
                return (int)Mathf.Ceil(2 * (area / 5000f));
            case Type.HighSchool:
                // Calculate with an average of 3 floors and 1 student per 5m^2.
                return (int)Mathf.Ceil(3 * (area / 400f));
            case Type.University:
                // Calculate with an average of 5 floors and 1 student per 5,^2.
                return (int)Mathf.Ceil(5 * (area / 250f));
            case Type.Stadium:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 200f));
            case Type.Airport:
                // Calculate with an average of 1 floors and 1 visitor per m^2.
                return (int)Mathf.Ceil(area);
            case Type.Church:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 200f));
            case Type.Leisure:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 200f));
            case Type.Sight:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 500f));
            case Type.Industrial:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 700f));
            case Type.Hotel:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 700f));
            default:
                return 0;
            }
        }

        public override Color GetColor()
        {
            return GetColor(type, Game.displayMode);
        }

        public static Color GetColor(Type type, MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (mode)
            {
            default:
                return Colors.GetColor("building.defaultDay");
            case MapDisplayMode.Night:
                return Colors.GetColor("building.defaultNight");
            }
        }

        public float GetLayer()
        {
            return GetLayer(type);
        }

        public static float GetLayer(Type type)
        {
            return Map.Layer(MapLayer.Buildings);
        }

        public void UpdateMesh(Map map)
        {
            if (outlinePositions == null || outlinePositions.Length == 0 || outlinePositions[0].Length == 0)
            {
                return;
            }

            collisionRect = MeshBuilder.GetCollisionRect(outlinePositions);
            
            if (uniqueTile != null)
            {
                uniqueTile.AddCollider(this, outlinePositions, collisionRect, true);
                return;
            }

            foreach (var tile in map.GetTilesForObject(this))
            {
                tile.AddCollider(this, outlinePositions, collisionRect, true);
            }
        }

        public void UpdateColor(MapDisplayMode mode)
        {
        }

        public void Delete(bool deleteMesh = false)
        {
            var map = GameController.instance.loadedMap;
            map.buildings.Remove(this);
            map.DeleteMapObject(this);
        }

        public void DeleteMesh()
        {
            // var map = GameController.instance.loadedMap;
            // foreach (var tile in map.GetTilesForObject(this))
            // {
            //     tile.GetMesh("Buildings").RemoveMesh(mesh);
            // }
        }

        public new Serialization.Building ToProtobuf()
        {
            return new Serialization.Building
            {
                MapObject = base.ToProtobuf(),
                // Mesh = mesh.ToProtobuf2D() ?? new Serialization.Mesh2D(),
                StreetID = (uint)(street?.id ?? 0),
                Type = (Serialization.Building.Types.Type)type,
                Position = centroid.ToProtobuf(),
            };
        }

        public static Building Deserialize(Serialization.Building b, Map map)
        {
            var building = map.CreateBuilding(
                (Type)b.Type,
                b.MapObject.OutlinePositions.Select(arr => arr.OutlinePositions.Select(v => v.Deserialize()).ToArray()),
                b.MapObject.Name, "",
                b.MapObject.Area, b.MapObject.Centroid.Deserialize(),
                (int)b.MapObject.Id);

            building.streetID = (int)b.StreetID;
            building.Deserialize(b.MapObject);

            return building;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(number))
            {
                return name + " " + number;
            }

            return name;
        }

        public override void ActivateModal()
        {
            var modal = MainUI.instance.buildingModal;
            modal.SetBuilding(this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.Enable();
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            if (!Game.MouseDownActive(MapObjectKind.Building))
            {
                return;
            }

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = MainUI.instance.buildingModal;
            if (modal.modal.Active && modal.building == this)
            {
                modal.modal.Disable();
                return;
            }

            ActivateModal();
        }
    }
}
