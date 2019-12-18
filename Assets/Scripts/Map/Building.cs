using UnityEngine;
using System.Collections;
using System.Linq;

namespace Transidious
{
    public class Building : StaticMapObject
    {
        [System.Serializable]
        public struct SerializableBuilding
        {
            public SerializableMapObject mapObject;
            public SerializableMesh2D mesh;
            public int streetID;
            public string number;
            public Type type;
            public SerializableVector2 position;
        }

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

            GroceryStore,
        }

        public Mesh mesh;
        public StreetSegment street;
        public int streetID;
        public string number;

        public Type type;
        public int capacity;
        public int occupants;

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
            this.occupants = 0;
            this.capacity = GetDefaultCapacity(type, area);
            this.number = numberStr;
            this.mesh = mesh;
            this.name = name;
        }

        public void UpdateStreet(Map map)
        {
            var closest = map.GetClosestStreet(this.centroid);
            if (closest != null)
            {
                street = closest.seg;
            }
        }

        public int GetDefaultCapacity()
        {
            return GetDefaultCapacity(type, area);
        }

        public static int GetDefaultCapacity(Type type, float area)
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

                return (int)Mathf.Ceil(floors * (area / 40f));
            case Type.Office:
                // Calculate with an average of 3 floors and 1 person per 10m^2.
                return (int)Mathf.Ceil(3 * (area / 10f));
            case Type.Shop:
            case Type.GroceryStore:
                // Calculate with an average of 1 floors and 1 worker per 100m^2.
                return (int)Mathf.Ceil(1 * (area / 100f));
            case Type.Hospital:
                // Calculate with an average of 5 floors and 1 worker per 20m^2.
                return (int)Mathf.Ceil(5 * (area / 20f));
            case Type.ElementarySchool:
                // Calculate with an average of 2 floors and 1 student per 5m^2.
                return (int)Mathf.Ceil(2 * (area / 50f));
            case Type.HighSchool:
                // Calculate with an average of 3 floors and 1 student per 5m^2.
                return (int)Mathf.Ceil(3 * (area / 40f));
            case Type.University:
                // Calculate with an average of 5 floors and 1 student per 5,^2.
                return (int)Mathf.Ceil(5 * (area / 25f));
            case Type.Stadium:
                // Calculate with an average of 1 floors and 1 visitor per 2m^2.
                return (int)Mathf.Ceil(1 * (area / 2f));
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
#if DEBUG
            if (GameController.instance.ImportingMap)
            {
                return Color.black;
            }
#endif

            switch (mode)
            {
            case MapDisplayMode.Day:
            default:
                return new Color(.87f, .89f, .91f);
            case MapDisplayMode.Night:
                return new Color(.20f, .21f, .22f);
            }
        }

        public void UpdateMesh(Map map)
        {
            if (mesh == null)
            {
                return;
            }

            float layer = 0f;
            switch (type)
            {
            default:
                layer = Map.Layer(MapLayer.Buildings);
                break;
            }

            var minAngle = 0f;
            var surroundingRect = MeshBuilder.GetSmallestSurroundingRect(mesh, ref minAngle);

            //Quaternion q = Quaternion.Euler(0f, 0f, minAngle * Mathf.Rad2Deg);
            //mesh = MeshBuilder.RotateMesh(mesh, centroid, q);

            var collisionRect = MeshBuilder.GetCollisionRect(mesh);
            foreach (var tile in map.GetTilesForObject(this))
            {
                tile.AddMesh("Buildings", mesh, GetColor(), layer);

                if (outlinePositions != null)
                {
                    tile.AddCollider(this, outlinePositions, collisionRect, true);
                }
                else
                {
                    Debug.Log("using simple bounding box for '" + name + "'");
                    tile.AddCollider(this, surroundingRect, collisionRect, true);
                }
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
            var map = GameController.instance.loadedMap;
            foreach (var tile in map.GetTilesForObject(this))
            {
                tile.GetMesh("Buildings").RemoveMesh(mesh);
            }
        }

        public new Serialization.Building ToProtobuf()
        {
            return new Serialization.Building
            {
                MapObject = base.ToProtobuf(),
                Mesh = mesh.ToProtobuf2D() ?? new Serialization.Mesh2D(),
                StreetID = (uint)(street?.id ?? 0),
                Type = (Serialization.Building.Types.Type)type,
                Position = centroid.ToProtobuf(),
            };
        }

        public new SerializableBuilding Serialize()
        {
            return new SerializableBuilding
            {
                mapObject = base.Serialize(),
                mesh = new SerializableMesh2D(mesh),
                streetID = street?.id ?? 0,
                number = number,
                type = type,
                position = new SerializableVector2(centroid),
            };
        }

        public static Building Deserialize(Serialization.Building b, Map map)
        {
            var building = map.CreateBuilding((Type)b.Type, b.Mesh.Deserialize(), b.MapObject.Name, "",
                                              b.MapObject.Area, b.MapObject.Centroid.Deserialize(),
                                              (int)b.MapObject.Id);

            building.streetID = (int)b.StreetID;
            building.Deserialize(b.MapObject);

            return building;
        }

        public static Building Deserialize(Map map, SerializableBuilding b)
        {
            Mesh mesh = b.mesh.GetMesh();

            var building = map.CreateBuilding(b.type, mesh, b.mapObject.name, b.number,
                                              b.mapObject.area, b.mapObject.centroid,
                                              b.mapObject.id);

            building.streetID = b.streetID;
            building.number = b.number;
            building.Deserialize(b.mapObject);

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

        public void ActivateModal()
        {
            var modal = GameController.instance.sim.buildingInfoModal;
            modal.SetBuilding(this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.EnableAt(pos);
        }

        public override void OnMouseDown()
        {
            if (!Game.MouseDownActive(MapObjectKind.Building))
            {
                return;
            }

            base.OnMouseDown();

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            ActivateModal();
        }
    }
}
