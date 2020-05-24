using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIProgressBar : MonoBehaviour
    {
        /// The progress bar image.
        public Image Image;

        /// The color gradient to use.
        public Gradient Gradient { get; set; }

        /// The progress text.
        public TMPro.TMP_Text ProgressText;

        /// The current progress (used to avoid updating the sprite when we don't need to).
        private float _CurrentProgress = -1f;

#if DEBUG
        /// Minimum change in progress value to update the progress bar.
        private float UpdateThreshold = .001f;
#else
        /// Minimum change in progress value to update the progress bar.
        private float UpdateThreshold = .01f;
#endif

        /// Update the progress of this progress bar.
        public void SetProgress(float percentage)
        {
            if (Mathf.Abs(_CurrentProgress - percentage) < UpdateThreshold)
            {
                return;
            }
 
            _CurrentProgress = percentage;
            Image.color = Gradient.Evaluate(percentage);

            var rt = transform.parent.GetComponent<RectTransform>();
            var maxWidth = rt.sizeDelta.x;

            var thisRt = GetComponent<RectTransform>();
            thisRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth * percentage);

            if (percentage > .45f)
            {
                ProgressText.color = Math.ContrastColor(Image.color);
            }
            else
            {
                ProgressText.color = Color.white;
            }

#if DEBUG
            ProgressText.text = $"{percentage * 100f:n2} %";
#else
            ProgressText.text = $"{percentage * 100f:n0} %";
#endif
        }
    }
}
