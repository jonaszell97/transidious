using UnityEngine;

namespace Transidious
{
    public class MapObject : MonoBehaviour
    {
        public InputController inputController;
        public bool highlighted = false;

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
            // Debug.Log("mouse enter " + this.GetType().Name);
            inputController.MouseEnterMapObject(this);
        }
    }
}