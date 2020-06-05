using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class ScrollViewContent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private ScrollRect mainScroll;
 
        void Awake()
        {
            mainScroll = transform.GetComponentInParent<ScrollRect>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            mainScroll.OnBeginDrag(eventData);
        }
 
        public void OnDrag(PointerEventData eventData)
        {
            mainScroll.OnDrag(eventData);
        }
 
        public void OnEndDrag(PointerEventData eventData)
        {
            mainScroll.OnEndDrag(eventData);
        }
 
        public void OnScroll(PointerEventData data)
        {
            mainScroll.OnScroll(data);
        }
    }
}