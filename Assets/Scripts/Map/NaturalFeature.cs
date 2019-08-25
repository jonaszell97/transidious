using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class NaturalFeature : StaticMapObject
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
            public SerializableMapObject mapObject;
            public Type type;
            public SerializableMesh2D mesh;
        }

        public Type type;
        public Mesh mesh;
        public int visitors;
        public int capacity;

        public void UpdateMesh(Map map)
        {
            if (mesh == null)
            {
                return;
            }

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

            foreach (var tile in map.GetTilesForObject(this))
            {
                tile.mesh.AddMesh(GetColor(), mesh, layer);

                if (outlinePositions != null)
                {
                    tile.AddCollider(this, outlinePositions[0],
                                     MeshBuilder.GetCollisionRect(mesh), true);
                }
                else
                {
                    Debug.Log("using simple bounding box for '" + name + "'");
                    tile.AddCollider(this, MeshBuilder.GetSmallestSurroundingRect(mesh),
                                     MeshBuilder.GetCollisionRect(mesh), true);
                }
            }
        }

        public void Initialize(Map map, string name, Type type,
                               Mesh mesh = null, float area = 0f,
                               Vector2? centroid = null, int id = -1)
        {

#if DEBUG
            if (name.Length == 0)
            {
                name = "#" + id.ToString();
            }
#endif

            base.Initialize(MapObjectKind.NaturalFeature, id, area, centroid);

            this.name = name;
            this.type = type;
            this.mesh = mesh;

            this.visitors = 0;
            this.capacity = GetDefaultCapacity();
        }

        public override Color GetColor()
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

        public int GetDefaultCapacity()
        {
            return GetDefaultCapacity(type, area);
        }

        public static int GetDefaultCapacity(Type type, float area)
        {
            switch (type)
            {
            case Type.Park:
            case Type.Green:
            case Type.Beach:
            default:
                return (int)Mathf.Ceil(area / 10f);
            case Type.Forest:
            case Type.Lake:
                return (int)Mathf.Ceil(area / 50f);
            case Type.SportsPitch:
                return (int)Mathf.Ceil(area / 5f);
            case Type.Allotment:
            case Type.Cemetery:
                return (int)Mathf.Ceil(area / 25f);
            case Type.FootpathArea:
            case Type.Footpath:
                return (int)Mathf.Ceil(area / 5f);
            case Type.Parking:
                return (int)Mathf.Ceil(area / 5f);
            }
        }

        public new SerializedFeature Serialize()
        {
            return new SerializedFeature
            {
                mapObject = base.Serialize(),
                type = type,
                mesh = new SerializableMesh2D(mesh),
            };
        }

        public static NaturalFeature Deserialize(Map map, SerializedFeature feature)
        {
            var newFeature = map.CreateFeature(
                feature.mapObject.name, feature.type, feature.mesh,
                feature.mapObject.area, feature.mapObject.centroid,
                feature.mapObject.id);

            newFeature.Deserialize(feature.mapObject);
            return newFeature;
        }

        public override void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = GameController.instance.sim.featureModal;
            modal.SetFeature(this);

            modal.modal.PositionAt(centroid);
            modal.modal.Enable();
        }
    }
}
