using UnityEngine;
using UnityEngine.EventSystems;
using Lean.Touch;

namespace Transidious
{
    public class Draggable : MonoBehaviour, IDragHandler
    {
        public PointerEventData.InputButton button = PointerEventData.InputButton.Left;
        private LeanFinger draggingFinger;

        public delegate void DragListener(Vector2 screenPos);
        public DragListener dragHandler = null;

        void Start()
        {
            
        }

        Rect BoundingRect
        {
            get
            {
                var rectTransform = this.GetComponent<RectTransform>();
                var transformedPos = Camera.main.ScreenToWorldPoint(new Vector2(
                    rectTransform.position.x, rectTransform.position.y - rectTransform.rect.height));
                var baseSize = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
                var transformedSize = Camera.main.ScreenToWorldPoint(
                    new Vector2(rectTransform.rect.width, rectTransform.rect.height));
    
                return new Rect(transformedPos.x, transformedPos.y,
                                transformedSize.x - baseSize.x,
                                transformedSize.y - baseSize.y);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != this.button)
            {
                return;
            }
            if (dragHandler != null)
            {
                dragHandler(eventData.position);
            }
        }

        // protected virtual void OnEnable()
        // {
        //     LeanTouch.OnFingerDown += OnFingerDown;
        //     LeanTouch.OnFingerUp += OnFingerUp;
        // }

        // protected virtual void OnDisable()
        // {
        //     LeanTouch.OnFingerDown -= OnFingerDown;
        //     LeanTouch.OnFingerUp -= OnFingerUp;
        // }

        // protected virtual void LateUpdate()
        // {
        //     if (draggingFinger != null && this.dragHandler != null)
        //     {
        //         this.dragHandler(draggingFinger.ScreenPosition);
        //     }
        // }

        // public void OnFingerDown(LeanFinger finger)
        // {
        //     if (BoundingRect.Contains(finger.GetWorldPosition(0f)))
        //     {
        //         Debug.Log("hit");
        //         draggingFinger = finger;
        //     }
        //     else
        //     {
        //         Debug.Log("no hit");
        //     }
        // }

        // public void OnFingerUp(LeanFinger finger)
        // {
        //     if (finger == draggingFinger)
        //     {
        //         draggingFinger = null;
        //     }
        // }
    }
}