using UnityEngine;
using UnityEngine.EventSystems;
using Lean.Touch;

namespace Transidious
{
    public class Clickable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public PointerEventData.InputButton button = PointerEventData.InputButton.Left;
        bool isClicked = false;

        public delegate void MouseEventListener(Vector2 screenPos);
        public MouseEventListener mouseDown = null;
        public MouseEventListener mouseUp = null;

        void Start()
        {
            
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != this.button)
            {
                return;
            }
            if (mouseDown != null)
            {
                mouseDown(eventData.position);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != this.button)
            {
                return;
            }
            if (mouseUp != null)
            {
                mouseUp(eventData.position);
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

        // Rect BoundingRect
        // {
        //     get
        //     {
        //         var rectTransform = this.GetComponent<RectTransform>();
        //         var transformedPos = Camera.main.ScreenToWorldPoint(new Vector2(
        //             rectTransform.position.x, rectTransform.position.y - rectTransform.rect.height));
        //         var baseSize = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
        //         var transformedSize = Camera.main.ScreenToWorldPoint(
        //             new Vector2(rectTransform.rect.width, rectTransform.rect.height));
    
        //         return new Rect(transformedPos.x, transformedPos.y,
        //                         transformedSize.x - baseSize.x,
        //                         transformedSize.y - baseSize.y);
        //     }
        // }

        // public void OnFingerDown(LeanFinger finger)
        // {
        //     if (BoundingRect.Contains(finger.GetWorldPosition(0f)))
        //     {
        //         if (this.mouseDown != null)
        //         {
        //             this.mouseDown(finger.ScreenPosition);
        //         }

        //         isClicked = true;
        //     }
        // }

        // public void OnFingerUp(LeanFinger finger)
        // {
        //     if (isClicked)
        //     {
        //         if (this.mouseUp != null)
        //         {
        //             this.mouseUp(finger.ScreenPosition);
        //         }
                
        //         isClicked = false;
        //     }
        // }
    }
}