using UnityEngine;

namespace Transidious
{
    public class BoxColliderScaler : MonoBehaviour
    {
        BoxCollider2D boxCollider;
        RectTransform rectTransform;

        void Awake()
        {
            this.runInEditMode = true;

            this.boxCollider = this.gameObject.GetComponent<BoxCollider2D>();
            this.rectTransform = this.gameObject.GetComponent<RectTransform>();

            Vector3 min = rectTransform.TransformPoint(rectTransform.rect.min);
            Vector3 max = rectTransform.TransformPoint(rectTransform.rect.max);
            
            float width = max.x - min.x;
            float height = max.y - min.y;

            this.boxCollider.size = new Vector2(width, height);
            this.boxCollider.offset = new Vector2(width * .5f, -height * .5f);
        }
    }
}