using UnityEngine;

namespace Transidious
{
    public class PropertyAnimator : MonoBehaviour
    {
        /// Whether or not this animator is currently animating.
        public bool animating;

        /// Callback type to update the value.
        public delegate void UpdateValueHandler(float progressPercentage);

        /// Callback to update the value.
        private UpdateValueHandler _updateValue;

        /// Passed time since starting the animation.
        private float _timeSinceStart;

        /// The animation duration.
        private float _duration;

        /// Initialize.
        public void Initialize(UpdateValueHandler updateValueHandler)
        {
            _updateValue = updateValueHandler;

            animating = false;
            enabled = false;
        }

        /// Start the animation.
        public void StartAnimation(float duration)
        {
            Debug.Assert(!animating, "already animating!");
            animating = true;
            enabled = true;

            _duration = duration;
            _timeSinceStart = 0f;
            _updateValue(0f);
        }

        /// Stop the animation.
        public void StopAnimation()
        {
            animating = false;
            enabled = false;
            _duration = 0f;
            _timeSinceStart = 0f;
        }

        private void Update()
        {
            _timeSinceStart += Time.deltaTime;

            if (_timeSinceStart >= _duration)
            {
                _updateValue(1f);
                StopAnimation();
            }
            else
            {
                _updateValue(_timeSinceStart / _duration);
            }
        }
    }
}