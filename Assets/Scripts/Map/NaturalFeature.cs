using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NaturalFeature
{
    public enum Type {
        Park,
        Lake,
        Green,
        SportsPitch,
        Allotment,
        Cemetery,
        FootpathArea,
        Beach,
        Forest,
        Parking,
    }

    [System.Serializable]
    public struct SerializedFeature
    {
        public string name;
        public Type type;
        public SerializableMesh mesh;
    }

    /// <summary>
    ///  The name of this feature.
    /// </summary>
    public string name;

    /// <summary>
    ///  The feature type.
    /// </summary>
    public Type type;

    public Mesh mesh;

    public void UpdateMesh(Map map, Mesh mesh)
    {
        this.mesh = mesh;
        /*meshFilter.mesh = mesh;
        meshRenderer.material.shader = map.input.defaultShader;
        meshRenderer.material.color = GetColor();*/

        float layer = 0f;
        switch (type)
        {
            case NaturalFeature.Type.Park:
                layer = Map.Layer(MapLayer.Parks, 0);
                break;
            case NaturalFeature.Type.Allotment:
            case NaturalFeature.Type.Cemetery:
            case NaturalFeature.Type.SportsPitch:
            case NaturalFeature.Type.FootpathArea:
            case NaturalFeature.Type.Parking:
                layer = Map.Layer(MapLayer.Parks, 1);
                break;
            case NaturalFeature.Type.Forest:
                layer = Map.Layer(MapLayer.NatureBackground, 0);
                break;
            case NaturalFeature.Type.Beach:
            case NaturalFeature.Type.Green:
                layer = Map.Layer(MapLayer.NatureBackground, 1);
                break;
            case NaturalFeature.Type.Lake:
                layer = Map.Layer(MapLayer.Lakes);
                break;
        }

        map.natureMesh.AddMesh(GetColor(), mesh, layer);
        /*transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         layer);*/
    }

    public void Initialize(Map map, string name, Type type, Mesh mesh)
    {
        this.name = name;
        this.type = type;

        if (mesh != null)
            UpdateMesh(map, mesh);
    }

    Color GetColor()
    {
        switch (type)
        {
            case Type.Lake:
                return new Color(160f / 255f, 218f / 255f, 242f / 255f);
            case Type.Green:
                return new Color(174.9f / 255f, 224.1f / 255f, 159.2f / 255f);
            case Type.SportsPitch:
                return new Color(170.4f / 255f, 224.4f / 255f, 202.6f / 255f);
            case Type.Cemetery:
                return new Color(201f / 255f, 225.3f / 255f, 191.3f / 255f);
            case Type.Park:
                return new Color(200.1f / 255f, 250.4f / 255f, 204.2f / 255f);
            case Type.Allotment:
                return new Color(201f / 255f, 225.3f / 255f, 191.3f / 255f);
            case Type.FootpathArea:
                return new Color(221.1f / 255f, 220.6f / 255f, 231.8f / 255f);
            case Type.Beach:
                return new Color(248.9f / 255f, 234.5f / 255f, 181.6f / 255f);
            case Type.Forest:
                return new Color(174.9f / 255f, 224.1f / 255f, 159.2f / 255f);
            case Type.Parking:
                return new Color(.8f, .8f, .8f);
            default:
                return new Color();
        }
    }

    public SerializedFeature Serialize()
    {
        return new SerializedFeature
        {
            name = name,
            type = type,
            mesh = new SerializableMesh(mesh)
        };
    }
}
