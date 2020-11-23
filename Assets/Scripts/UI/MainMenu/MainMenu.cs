using System;
using System.Collections.Generic;
using Transidious;
using UnityEngine;
using Math = Transidious.Math;

namespace UI.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        /// The background image.
        public RectTransform BackgroundImage;

        /// Bounds of the background image.
        public Rect BackgroundBounds;

        /// Current movement vector of the background image.
        private Tuple<Vector2, Vector2, Vector2> _backgroundMovementCurve;

        /// Current movement speed of the background image.
        private float _backgroundMovementSpeed;

        /// Current progress along the path of the background image.
        private float _backgroundMovementProgress;
        
        private Vector2 EightCurvePoint(float t)
        {
            // t ∈ (−1/2𝜋, 3/2𝜋) ↦ (cos t, sin t * cos t) ∈ ℝ2
            return new Vector2(Mathf.Cos(t), Mathf.Sin(t) * Mathf.Cos(t));
        }

        void UpdateBackgroundPosition()
        {
            _backgroundMovementProgress += Time.deltaTime * _backgroundMovementSpeed;

            var t = (_backgroundMovementProgress % (Math.TwoPI)) - Math.HalfPI;
            var curvePoint = EightCurvePoint(t);
            var pt = new Vector2(BackgroundBounds.center.x + curvePoint.x * BackgroundBounds.x,
                                 BackgroundBounds.center.y + curvePoint.y * BackgroundBounds.y);

            BackgroundImage.anchoredPosition = pt;
        }

        private void Awake()
        {
            RNG.Reseed(-1);

            BackgroundBounds = new Rect(-285, -335, 635, 860);
            _backgroundMovementProgress = 0f;
            _backgroundMovementSpeed = .05f;
        }

        private void Update()
        {
            UpdateBackgroundPosition();
        }
    }
}