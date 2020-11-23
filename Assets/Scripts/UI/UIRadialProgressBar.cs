using TMPro;
using Transidious;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class UIRadialProgressBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// The progress bar image.
        public Image radialBarImg;

        /// The progress text.
        public TMP_Text progressText;

        /// The time left until we should fade the progress bar.
        public float fadeTime = 0f;

        /// The default time interval after which to fade the progress bar (in seconds).
        private static readonly float DefaultFadeTime = 3f;

        /// Whether or not the progress bar should be faded.
        public bool shouldFade;
        
        /// Whether or not the progress bar is hovered
        public bool hovered;

        /// Animator for this progress bar.
        private PropertyAnimator _propertyAnimator;
        private PropertyAnimator PropertyAnimator
        {
            get
            {
                if (_propertyAnimator == null)
                    _propertyAnimator = gameObject.AddComponent<PropertyAnimator>();

                return _propertyAnimator;
            }
        }

        public void SetProgress(float progressPercentage, bool animate = true, bool unfade = true)
        {
            Debug.Assert(progressPercentage >= 0f && progressPercentage <= 1f);

            gameObject.SetActive(true);

            if (animate)
            {
                var animator = PropertyAnimator;
                if (animator.animating)
                {
                    animator.StopAnimation();
                }

                var fillAmount = radialBarImg.fillAmount;
                var diff = progressPercentage - fillAmount;
                animator.Initialize(percentage =>
                {
                    var newVal = fillAmount + percentage * diff;
                    radialBarImg.fillAmount = newVal;
                    progressText.text = $"{newVal * 100f:n0}%";
                });

                animator.StartAnimation(1f * progressPercentage);
            }
            else
            {
                radialBarImg.fillAmount = progressPercentage;
                progressText.text = $"{progressPercentage * 100f:n0}%";
            }

            if (!unfade)
            {
                return;
            }

            SetAlpha(1f, false);
            fadeTime = DefaultFadeTime;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetAlpha(float a, bool animate = true)
        {
            var c = radialBarImg.color;
            if (c.a.Equals(a))
                return;

            if (animate)
            {
                var animator = PropertyAnimator;
                if (animator.animating)
                {
                    animator.StopAnimation();
                }

                var diff = a - c.a;
                animator.Initialize((float percentage) =>
                {
                    radialBarImg.color = new Color(c.r, c.g, c.b, c.a + percentage * diff);
                    // progressText.color = new Color(1f, 1f, 1f, c.a + percentage * diff);
                });

                animator.StartAnimation(.5f);
            }
            else
            {
                radialBarImg.color = new Color(c.r, c.g, c.b, a);
                // progressText.color = new Color(1f, 1f, 1f, a);
            }
        }

        public void Fade()
        {
            SetAlpha(0.5f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetAlpha(1f);
            fadeTime = DefaultFadeTime;
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            fadeTime = DefaultFadeTime;
            hovered = false;
        }
    }
}