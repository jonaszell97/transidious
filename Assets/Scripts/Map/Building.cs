using UnityEngine;
using System.Collections;
using System.Linq;

namespace Transidious
{
    public class Building : MonoBehaviour
    {
        [System.Serializable]
        public struct SerializableBuilding
        {
            public SerializableMesh mesh;
            public int streetID;
            public string number;
            public string name;
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

            GroceryStore,
        }

        public StreetSegment street;
        public int streetID;
        public string number;

        public Type type;
        public int capacity;
        public int occupants;

        public Vector3 position;

        public void Initialize(Map map, Type type, StreetSegment street, string numberStr,
                               Mesh mesh, string name = "", Vector3? position = null)
        {
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

            if (string.IsNullOrEmpty(name) && street != null)
            {
                this.name = street.street.name;
            }
            else
            {
                this.name = name;
            }

            UpdateMesh(map, mesh);
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

            this.position = new Vector3(xSum / vertices.Length, ySum / vertices.Length, 0);
        }

        Color GetColor()
        {
            switch (type)
            {
                default:
                    return new Color(223f / 255f, 226f / 255f, 231.8f / 255f);
            }
        }

        public void UpdateMesh(Map map, Mesh mesh)
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

            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();

            meshFilter.mesh = mesh;
            meshRenderer.material = GameController.GetUnlitMaterial(GetColor());
            meshRenderer.transform.position = new Vector3(meshRenderer.transform.position.x,
                                                          meshRenderer.transform.position.y,
                                                          layer);
        }

        public SerializableBuilding Serialize()
        {
            return new SerializableBuilding
            {
                mesh = new SerializableMesh(GetComponent<MeshFilter>().mesh),
                streetID = street?.id ?? 0,
                number = number,
                name = name,
                type = type,
                position = new SerializableVector2(position),
            };
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
