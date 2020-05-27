using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UIProgressBar : MonoBehaviour
    {
        /// The progress bar image.
        public Image image;

        /// The color gradient to use.
        public Gradient Gradient { get; set; }

        /// If true, reverse the color gradient.
        public bool ReverseGradient { get; set; }

        /// The progress text.
        public TMPro.TMP_Text progressText;

        /// The current progress (used to avoid updating the sprite when we don't need to).
        private float _currentProgress = -1f;

#if DEBUG
        /// Minimum change in progress value to update the progress bar.
        private readonly float _updateThreshold = .001f;
#else
        /// Minimum change in progress value to update the progress bar.
        private readonly float _updateThreshold = .01f;
#endif

        /// Update the progress of this progress bar.
        public void SetProgress(float percentage)
        {
            if (Mathf.Abs(_currentProgress - percentage) < _updateThreshold)
            {
                return;
            }
 
            _currentProgress = percentage;
            image.color = Gradient.Evaluate(ReverseGradient ? 1f - percentage : percentage);

            var rt = transform.parent.GetComponent<RectTransform>();
            var maxWidth = rt.sizeDelta.x;

            var thisRt = GetComponent<RectTransform>();
            thisRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth * percentage);

            if (percentage > .45f)
            {
                progressText.color = Math.ContrastColor(image.color);
            }
            else
            {
                progressText.color = Color.white;
            }

#if DEBUG
            progressText.text = $"{percentage * 100f:n2} %";
#else
            ProgressText.text = $"{percentage * 100f:n0} %";
#endif
        }
    }
}
