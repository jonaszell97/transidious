using UnityEngine;

namespace Transidious
{
    public class MapObject : MonoBehaviour
    {
        [System.Serializable]
        public struct SerializableMapObject
        {
            public int id;
            public string name;
            public int uniqueTileX, uniqueTileY;
        }

        // Used instead of dynamic dispatch for performance's sake.
        public enum Kind
        {
            Street,
            StreetIntersection,
            StreetSegment,
            Building,
            NaturalFeature,

            Line,
            Route,
            Stop,
        }

        public Kind kind;
        public int id;
        public MapTile uniqueTile;
        public InputController inputController;
        public bool highlighted = false;
        public new Renderer renderer;
        public new Collider2D collider;

        protected void Initialize(Kind kind, int id)
        {
            this.id = id;
            this.kind = kind;
            this.inputController = GameController.instance.input;
            this.renderer = GetComponent<Renderer>();
            this.collider = GetComponent<Collider2D>();
        }

        public bool ShouldRender(RenderingDistance dist)
        {
            switch (kind)
            {
            case Kind.Street:
            default:
                return false;
            case Kind.StreetIntersection:
                return dist <= RenderingDistance.Far;
            case Kind.StreetSegment:
                return (this as StreetSegment).GetStreetWidth(dist) > 0f;
            case Kind.Building:
            case Kind.NaturalFeature:
                return dist == RenderingDistance.Near;
            case Kind.Line:
            case Kind.Route:
            case Kind.Stop:
                return true;
            }
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
            case Kind.StreetSegment:
                {
                    var seg = this as StreetSegment;
                    seg.streetMeshObj?.SetActive(false);
                    seg.outlineMeshObj?.SetActive(false);

                    break;
                }
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
            case Kind.StreetSegment:
                {
                    var seg = this as StreetSegment;
                    seg.outlineMeshObj?.SetActive(true);

                    if (dist <= RenderingDistance.Far)
                    {
                        seg.streetMeshObj?.SetActive(true);
                    }

                    break;
                }
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

        protected virtual void OnMouseOver()
        {
            inputController.MouseOverMapObject(this);
        }

        protected virtual void OnMouseExit()
        {
            inputController.MouseExitMapObject(this);
        }

        protected virtual void OnMouseDown()
        {
            inputController.MouseDownMapObject(this);
        }

        protected virtual void OnMouseEnter()
        {
            inputController.MouseEnterMapObject(this);
        }

        public SerializableMapObject Serialize()
        {
            return new SerializableMapObject
            {
                id = id,
                name = name,
                uniqueTileX = uniqueTile != null ? uniqueTile.x : -1,
                uniqueTileY = uniqueTile != null ? uniqueTile.y : -1,
            };
        }

        public void Deserialize(SerializableMapObject obj)
        {
            this.id = obj.id;
            this.name = obj.name;

            if (obj.uniqueTileX != -1)
            {
                this.uniqueTile = GameController.instance.loadedMap.tiles
                    [obj.uniqueTileX][obj.uniqueTileY];

                this.transform.SetParent(this.uniqueTile.transform);
            }
        }
    }
}