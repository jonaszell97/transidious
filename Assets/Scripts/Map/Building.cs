using UnityEngine;
using System.Collections;
using System.Linq;

namespace Transidious
{
    public class Building : MapObject
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

        public Vector3 position;

        public void Initialize(Map map, Type type, StreetSegment street, string numberStr,
                               Mesh mesh, string name = "", Vector3? position = null,
                               int id = -1)
        {
            base.Initialize(Kind.Building, id);

            this.type = type;

            if (position.HasValue)
            {
                this.position = position.Value;
            }
            else if (mesh != null)
            {
                UpdatePosition(mesh.vertices);
            }

            this.street = street;
            this.number = numberStr;
            this.occupants = 0;
            this.capacity = GetDefaultCapacity(type);
            this.number = numberStr;
            this.mesh = mesh;

            if (string.IsNullOrEmpty(name) && street != null)
            {
                this.name = street.street.name;
            }
            else
            {
                this.name = name;
            }
        }

        public void UpdateStreet(Map map)
        {
            var closest = map.GetClosestStreet(this.position);
            if (closest != null)
            {
                street = closest.seg;
            }
        }

        int GetDefaultCapacity(Type type)
        {
            switch (type)
            {
            case Type.Residential:
                return 100;
            case Type.Office:
                return 30;
            case Type.Shop:
            case Type.GroceryStore:
                return 5;
            case Type.ElementarySchool:
                return 250;
            case Type.HighSchool:
                return 1000;
            case Type.University:
                return 1000;
            default:
                return 0;
            }
        }

        void UpdatePosition(Vector3[] vertices)
        {
            float xSum = 0f;
            float ySum = 0f;

            foreach (var vert in vertices)
            {
                xSum += vert.x;
                ySum += vert.y;
            }

            this.position = new Vector3(xSum / vertices.Length, ySum / vertices.Length,
                                        Map.Layer(MapLayer.Buildings));
        }

        public Color GetColor()
        {
            return GetColor(type);
        }

        public static Color GetColor(Type type, MapDisplayMode mode = MapDisplayMode.Day)
        {
            switch (type)
            {
            default:
                return DefaultColor(mode);
            }
        }

        public static Color DefaultColor(MapDisplayMode mode)
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
            float layer = 0f;
            switch (type)
            {
            default:
                layer = Map.Layer(MapLayer.Buildings);
                break;
            }

            foreach (var tile in map.GetTilesForObject(this))
            {
                tile.mesh.AddMesh(GetColor(), mesh, layer);
            }
        }

        public void UpdateColor(MapDisplayMode mode)
        {
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
                position = new SerializableVector2(position),
            };
        }

        public static Building Deserialize(Map map, SerializableBuilding b)
        {
            Mesh mesh = b.mesh.GetMesh();
            var building = map.CreateBuilding(b.type, mesh, b.mapObject.name, b.number,
                                              b.position.ToVector(), b.mapObject.id);

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
    }
}
