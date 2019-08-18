using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class NaturalFeature
    {
        public enum Type
        {
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
            Footpath,
        }

        [System.Serializable]
        public struct SerializedFeature
        {
            public string name;
            public Type type;
            public SerializableMesh2D mesh;
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

        public void UpdateMesh(Mesh mesh, MultiMesh multiMesh)
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
            case NaturalFeature.Type.Footpath:
                layer = Map.Layer(MapLayer.Rivers, 1);
                break;
            }

            multiMesh.AddMesh(GetColor(), mesh, layer);
            /*transform.position = new Vector3(transform.position.x,
                                             transform.position.y,
                                             layer);*/
        }

        public void Initialize(string name, Type type,
                               Mesh mesh = null, MultiMesh multiMesh = null)
        {
            this.name = name;
            this.type = type;

            if (mesh != null)
            {
                UpdateMesh(mesh, multiMesh);
            }
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
            case Type.Footpath:
                return new Color(232f / 255f, 220f / 255f, 192f / 255f);
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
                mesh = new SerializableMesh2D(mesh),
            };
        }
    }
}
