using UnityEngine;
using System.Linq;

namespace Transidious
{
    public class NaturalFeature : StaticMapObject
    {
        public enum Type
        {
            Park,
            Lake,
            River,
            Green,
            SportsPitch,
            Allotment,
            Cemetery,
            FootpathArea,
            Beach,
            Forest,
            Parking,
            Footpath,
            Residential,
            Zoo,
            Railway,
        }

        public Type type;
        public Mesh mesh;
        public int Capacity;

        public void UpdateMesh(Map map)
        {
            if (outlinePositions == null || outlinePositions.Length == 0 || outlinePositions[0].Length == 0)
            {
                return;
            }

            var collisionRect = MeshBuilder.GetCollisionRect(outlinePositions);
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
            this.Capacity = GetDefaultCapacity();
        }

        public override int GetCapacity(OccupancyKind kind)
        {
            if ((type != Type.Parking && kind == OccupancyKind.Visitor)
                || (type == Type.Parking && kind == OccupancyKind.ParkingCitizen))
            {
                return Capacity;
            }

            return 0;
        }

        public override Color GetColor()
        {
            return GetColor(type);
        }

        public static Color GetColor(Type type)
        {
            return Colors.GetColor($"feature.{type}");
        }

        public float GetLayer()
        {
            return GetLayer(type);
        }

        public static float GetLayer(Type type)
        {
            float layer = 0f;
            switch (type)
            {
                case NaturalFeature.Type.Park:
                    layer = Map.Layer(MapLayer.Parks, 0);
                    break;
                case NaturalFeature.Type.Green:
                    layer = Map.Layer(MapLayer.Parks, 1);
                    break;
                case NaturalFeature.Type.Allotment:
                case NaturalFeature.Type.Cemetery:
                case NaturalFeature.Type.SportsPitch:
                case NaturalFeature.Type.FootpathArea:
                case NaturalFeature.Type.Parking:
                    layer = Map.Layer(MapLayer.Parks, 2);
                    break;
                case NaturalFeature.Type.Forest:
                case Type.Residential:
                case Type.Zoo:
                    layer = Map.Layer(MapLayer.NatureBackground, 0);
                    break;
                case NaturalFeature.Type.Beach:
                    layer = Map.Layer(MapLayer.NatureBackground, 1);
                    break;
                case NaturalFeature.Type.Lake:
                    layer = Map.Layer(MapLayer.Lakes);
                    break;
                case NaturalFeature.Type.River:
                    layer = Map.Layer(MapLayer.Rivers);
                    break;
                case NaturalFeature.Type.Footpath:
                    layer = Map.Layer(MapLayer.Rivers, 1);
                    break;
            }

            return layer;
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
                return (int)Mathf.Ceil(area / 50f);
            case Type.Forest:
            case Type.Lake:
                return (int)Mathf.Ceil(area / 500f);
            case Type.SportsPitch:
                return (int)Mathf.Ceil(area / 50f);
            case Type.Allotment:
            case Type.Cemetery:
                return (int)Mathf.Ceil(area / 100f);
            case Type.FootpathArea:
            case Type.Footpath:
                return (int)Mathf.Ceil(area / 50f);
            case Type.Parking:
                return (int)Mathf.Ceil(area / 50f);
            }
        }

        public void Delete(bool deleteMesh = false)
        {
            var map = GameController.instance.loadedMap;
            map.naturalFeatures.Remove(this);
            map.DeleteMapObject(this);
        }

        public new Serialization.NaturalFeature ToProtobuf()
        {
            return new Serialization.NaturalFeature
            {
                MapObject = base.ToProtobuf(),
                // Mesh = mesh?.ToProtobuf2D() ?? new Serialization.Mesh2D(),
                Type = (Serialization.NaturalFeature.Types.Type)type,
            };
        }

        public static NaturalFeature Deserialize(Serialization.NaturalFeature feature, Map map)
        {
            var newFeature = map.CreateFeature(
                feature.MapObject.Name, (Type)feature.Type,
                feature.MapObject.OutlinePositions.Select(arr => arr.OutlinePositions.Select(v => v.Deserialize()).ToArray()),
                feature.MapObject.Area, feature.MapObject.Centroid.Deserialize(),
                (int)feature.MapObject.Id);

            newFeature.Deserialize(feature.MapObject);
            return newFeature;
        }

        public override void ActivateModal()
        {
            var modal = MainUI.instance.featureModal;
            modal.SetFeature(this);

            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            modal.modal.Enable();
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();

            if (!Game.MouseDownActive(MapObjectKind.NaturalFeature))
            {
                return;
            }

            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = MainUI.instance.featureModal;
            if (modal.modal.Active && modal.feature == this)
            {
                modal.modal.Disable();
                return;
            }

            ActivateModal();
        }
    }
}
