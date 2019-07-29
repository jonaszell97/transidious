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
            // Debug.Log("mouse over " + this.GetType().Name);
            inputController.MouseOverMapObject(this);
        }

        protected virtual void OnMouseExit()
        {
            // Debug.Log("mouse exit " + this.GetType().Name);
            inputController.MouseExitMapObject(this);
        }

        protected virtual void OnMouseDown()
        {
            // Debug.Log("mouse down " + this.GetType().Name);
            inputController.MouseDownMapObject(this);
        }

        protected virtual void OnMouseEnter()
        {
            // Debug.Log("mouse enter " + this.GetType().Name);
            inputController.MouseEnterMapObject(this);
        }
    }
}