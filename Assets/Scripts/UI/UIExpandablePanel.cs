using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious
{
    public class UIExpandablePanel : MonoBehaviour
    {
        /// <summary>
        /// The game object whose size is used for reference while the panel is hidden.
        /// </summary>
        public RectTransform reference;

        /// <summary>
        ///  The game object containing the content of the panel.
        /// </summary>
        public GameObject content;

        /// <summary>
        ///  The game object that should be used as a trigger.
        /// </summary>
        public GameObject trigger;

        /// <summary>
        ///  Duration of the hide/show animations (in s).
        /// </summary>
        public float animationDuration;

        /// Event listener that is executed before showing the panel.
        public UnityEvent onShow;

        /// Event listener that is executed after hiding the panel.
        public UnityEvent onHide;

        /// <summary>
        /// The final scale of the panel when expanded.
        /// </summary>
        [SerializeField] Vector2 expandedScale;

        /// <summary>
        /// The final scale of the panel when contracted.
        /// </summary>
        [SerializeField] Vector2 contractedScale;

        /// <summary>
        ///  True if the panel is currently expanded.
        /// </summary>
        [SerializeField] bool expanded;

        /// <summary>
        ///  True if the panel is currently expanding.
        /// </summary>
        [SerializeField] bool expanding;

        /// <summary>
        ///  True if the panel is currently contracting.
        /// </summary>
        [SerializeField] bool contracting;

        /// <summary>
        ///  The multiplication factor for y-axis animation.
        /// </summary>
        float animationYFactor;

        /// <summary>
        ///  The active expandable panel.
        /// </summary>
        public static UIExpandablePanel activePanel;

        /// <summary>
        ///  Animation speed along the x axis.
        /// </summary>
        float animationSpeedX;
        
        /// <summary>
        ///  Animation speed along the y axis.
        /// </summary>
        float animationSpeedY;

        void Awake()
        {
            onShow = new UnityEvent();
            onHide = new UnityEvent();
            gameObject.SetActive(true);
        }

        void Start()
        {
            var button = this.trigger.GetComponent<Button>();
            if (button == null)
            {
                button = this.trigger.AddComponent<Button>();
            }

            button.onClick.AddListener(() => this.Toggle());

            var rectTransform = this.GetComponent<RectTransform>();
            this.expandedScale = rectTransform.localScale;

            rectTransform.localScale = new Vector2(reference.sizeDelta.x / rectTransform.sizeDelta.x, 0f);
            this.contractedScale = rectTransform.localScale;

            var xdiff = this.expandedScale.x - this.contractedScale.x;
            animationSpeedX = xdiff / animationDuration * 2f;

            var ydiff = this.expandedScale.y - this.contractedScale.y;
            animationSpeedY = ydiff / animationDuration * 2f;

            content.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (expanding || contracting)
            {
                return;
            }

            if (expanded)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (expanded)
            {
                return;
            }

            activePanel?.HideNoAnim();
            activePanel = this;

            expanding = true;
            contracting = false;
            firstStepDone = false;
            onShow.Invoke();
        }

        public void Hide()
        {
            if (!expanded)
            {
                return;
            }

            expanding = false;
            contracting = true;
            firstStepDone = false;

            content.gameObject.SetActive(false);
            onHide.Invoke();
        }

        public void ShowNoAnim()
        {
            if (expanded)
            {
                return;
            }

            activePanel?.HideNoAnim();
            activePanel = this;

            expanding = false;
            contracting = false;
            expanded = true;

            transform.localScale = expandedScale;
            content.gameObject.SetActive(true);

            onShow.Invoke();
        }

        public void HideNoAnim()
        {
            if (!expanded)
            {
                return;
            }

            expanding = false;
            contracting = false;
            expanded = false;

            transform.localScale = contractedScale;
            content.gameObject.SetActive(false);

            onHide.Invoke();
        }

        bool firstStepDone = false;

        void Update()
        {
            if (!expanding && !contracting)
            {
                return;
            }

            var rectTransform = GetComponent<RectTransform>();
            Vector3 target;

            float speed;

            if (expanding)
            {
                if (!firstStepDone)
                {
                    target = new Vector3(rectTransform.localScale.x, expandedScale.y, 1f);
                    speed = animationSpeedY;
                }
                else
                {
                    target = new Vector3(expandedScale.x, rectTransform.localScale.y, 1f);
                    speed = animationSpeedX;
                }
            }
            else
            {
                if (!firstStepDone)
                {
                    target = new Vector3(contractedScale.x, rectTransform.localScale.y, 1f);
                    speed = animationSpeedX;
                }
                else
                {
                    target = new Vector3(rectTransform.localScale.x, contractedScale.y, 1f);
                    speed = animationSpeedY;
                }
            }

            rectTransform.localScale = Vector3.MoveTowards(rectTransform.localScale, target, Time.deltaTime * speed);

            if (rectTransform.localScale.Equals(target))
            {
                if (!firstStepDone)
                {
                    firstStepDone = true;
                }
                else if (expanding)
                {
                    content.gameObject.SetActive(true);
                    expanding = false;
                    expanded = true;
                }
                else
                {
                    contracting = false;
                    expanded = false;
                    onHide.Invoke();
                }
            }
        }
    }
}
