using System;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIHighlightOverlay : MonoBehaviour
    {
        /// The four overlays (left, top, right, bottom).
        [SerializeField] private RectTransform[] overlays;

        /// The left overlay.
        private RectTransform Left => overlays[0];

        /// The top overlay.
        private RectTransform Top => overlays[1];

        /// The right overlay.
        private RectTransform Right => overlays[2];

        /// The bottom overlay.
        private RectTransform Bottom => overlays[3];

        /// The circle overlay.
        private RectTransform Circle => overlays[4];

        /// The arrow.
        private RectTransform Arrow => overlays[5];

        /// The current canvas scale.
        private float _canvasScale;

        /// This object's rect transform.
        private RectTransform _rectTransform;

        /// The current screen size.
        private Rect _screenSize;

        /// Initialize fields.
        public void Initialize(bool isResolutionChange = false)
        {
            _canvasScale = transform.parent.GetComponent<Canvas>().scaleFactor;
            _rectTransform = GetComponent<RectTransform>();
            _screenSize = Math.RectTransformToScreenSpace(_rectTransform, 1f / _canvasScale);

            if (!isResolutionChange)
            {
                GameController.instance.input.RegisterEventListener(InputEvent.ResolutionChange, _ => Initialize(true));
            }

            var animator = Arrow.GetComponent<TransformAnimator>();
            animator.Initialize();
            animator.SetAnimationType(TransformAnimator.AnimationType.Circular, 
                                      TransformAnimator.ExecutionMode.Automatic);
        }

        /// Highlight the given component.
        public void Highlight(GameObject obj, float padding = 0f, bool circle = false, bool arrow = false)
        {
            Highlight(obj.GetComponent<RectTransform>(), padding, circle, arrow);   
        }

        /// Highlight the area of the rect transform.
        public void Highlight(RectTransform rt, float padding = 0f, bool circle = false, bool arrow = false)
        {
            Highlight(Math.RectTransformToScreenSpace(rt, 1f / _canvasScale), padding, circle, arrow);
        }

        /// Highlight the given screen rectangle.
        public void Highlight(Rect screenRect, float padding = 0f, bool circle = false, bool arrow = false)
        {
            if (padding > 0f)
            {
                screenRect = new Rect(screenRect.x - padding, screenRect.y - padding,
                                      screenRect.width + 2 * padding,
                                      screenRect.height + 2 * padding);
            }

            Left.sizeDelta = new Vector2(screenRect.xMin, Left.sizeDelta.y);
            Right.sizeDelta = new Vector2(_screenSize.width - screenRect.xMax, Right.sizeDelta.y);

            Top.sizeDelta = new Vector2(screenRect.width, _screenSize.height - screenRect.yMax);
            Bottom.sizeDelta = new Vector2(screenRect.width, screenRect.yMin);

            Top.anchoredPosition = new Vector2(screenRect.center.x, Top.anchoredPosition.y);
            Bottom.anchoredPosition = new Vector2(screenRect.center.x, Bottom.anchoredPosition.y);

            if (circle)
            {
                var circleSize = Mathf.Max(screenRect.width, screenRect.height);
                Circle.sizeDelta = new Vector2(circleSize, circleSize);
                Circle.anchoredPosition = screenRect.center;

                Circle.gameObject.SetActive(true);
            }
            else
            {
                Circle.gameObject.SetActive(false);
            }

            if (arrow)
            {
                var startPos = new Vector2(screenRect.center.x, screenRect.center.y + screenRect.height * .5f + 5f);
                var endPos = new Vector2(startPos.x, startPos.y + 5f);

                var animator = Arrow.GetComponent<TransformAnimator>();
                animator.SetTargetAnchoredPosition(endPos, startPos);
                animator.Reset();
                animator.StartAnimation(.7f);
                
                Arrow.anchoredPosition = startPos;
                Arrow.gameObject.SetActive(true);
            }
            else
            {
                Arrow.gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        /// Hide the overlay again.
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}