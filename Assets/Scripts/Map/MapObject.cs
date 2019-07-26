using UnityEngine;

namespace Transidious
{
    public class MapObject : MonoBehaviour
    {
        public InputController inputController;
        public bool highlighted = false;

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