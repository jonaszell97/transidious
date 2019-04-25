using UnityEngine;
using System.Collections;

public class Building
{
    [System.Serializable]
    public struct SerializableBuilding
    {
        public SerializableMesh mesh;
        public int streetID;
        public int number;

        public string name;
        public Type type;
    }

    public enum Type
    {
        Residential,
        ElementarySchool,
        HighSchool,
        University,
        Hospital,
    }

    public Mesh mesh;
    public Street street;
    public int number;

    public string name;
    public Type type;

    public Building(Map map, Type type, Street street, int number, Mesh mesh, string name = "")
    {
        this.type = type;
        this.street = street;
        this.number = number;

        if (name == string.Empty && street != null)
        {
            this.name = street.name + " " + number.ToString();
        }
        else
        {
            this.name = name;
        }

        UpdateMesh(map, mesh);
    }

    Color GetColor()
    {
        switch (type)
        {
            case Type.Residential:
            case Type.ElementarySchool:
            case Type.HighSchool:
            case Type.University:
            case Type.Hospital:
                return new Color(223f/255f, 226f/255f, 231.8f/255f);
            default:
                throw new System.ArgumentException(string.Format("Illegal enum value {0}", type));
        }
    }

    public void UpdateMesh(Map map, Mesh mesh)
    {
        if (mesh == null)
        {
            return;
        }

        this.mesh = mesh;

        float layer = 0f;
        switch (type)
        {
            case Type.Residential:
            case Type.ElementarySchool:
            case Type.HighSchool:
            case Type.University:
            case Type.Hospital:
                layer = Map.Layer(MapLayer.Buildings);
                break;
        }

        map.buildingMesh.AddMesh(GetColor(), mesh, layer);
    }

    public SerializableBuilding Serialize()
    {
        return new SerializableBuilding
        {
            mesh = new SerializableMesh(mesh),
            streetID = street?.id ?? 0,
            number = number,
            name = name,
            type = type
        };
    }
}
