using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Transidious
{
    public class TransformAnimator : MonoBehaviour
    {
        [System.Flags]
        public enum TransformType
        {
            None            = 0x0,
            Position        = 0x1,
            AnchoredPosition   = 0x2,
            Rotation        = 0x4,
            Scale           = 0x8,
            SizeDelta       = 0x10,
        }

        public enum AnimationType
        {
            Loop,
            Circular,
        }

        public enum ExecutionMode
        {
            Manual,
            Automatic,
        }

        /// <summary>
        ///  The animation type.
        /// </summary>
        public AnimationType animationType;

        /// <summary>
        ///  The types of animation to perform.
        /// </summary>
        public TransformType type;

        /// <summary>
        ///  The execution mode of the animation.
        /// </summary>
        public ExecutionMode executionMode;

        /// <summary>
        ///  The desired duration of the animation.
        /// </summary>
        public float duration;

        /// <summary>
        /// The target position / local position.
        /// </summary>
        public Vector3 targetPosition;

        /// <summary>
        /// The target rotation.
        /// </summary>
        public Quaternion targetRotation;

        /// <summary>
        ///  The target scale.
        /// </summary>
        public Vector3 targetScale;

        /// <summary>
        /// The original position / local position.
        /// </summary>
        public Vector3 originalPosition;

        /// <summary>
        /// The original rotation.
        /// </summary>
        public Quaternion originalRotation;

        /// <summary>
        ///  The original scale.
        /// </summary>
        public Vector3 originalScale;

        /// <summary>
        /// Callback function to call once the animation is finished.
        /// </summary>
        public UnityAction onFinish;

        /// <summary>
        ///  Whether or not the animation is currently active.
        /// </summary>
        bool active;

        /// <summary>
        /// Multiplier for the move towards function.
        /// </summary>
        float[] movementMultipliers;

        public void SetTargetPosition(Vector3 targetPosition, Vector3? originalPosition = null)
        {
            this.type |= TransformType.Position;
            this.type &= ~TransformType.AnchoredPosition;
            this.targetPosition = targetPosition;
            this.originalPosition = originalPosition ?? transform.position;
            this.movementMultipliers[0] = (this.targetPosition - this.originalPosition).magnitude;
        }

        public void SetTargetAnchoredPosition(Vector2 targetPosition, Vector2? originalPosition = null)
        {
            this.type |= TransformType.AnchoredPosition;
            this.type &= ~TransformType.Position;
            this.targetPosition = targetPosition;
            this.originalPosition = originalPosition ?? rectTransform.anchoredPosition;
            this.movementMultipliers[0] = (this.targetPosition - this.originalPosition).magnitude;
        }

        public void SetTargetRotation(Quaternion targetRotation, Quaternion? originalRotation= null)
        {
            this.type |= TransformType.Rotation;
            this.targetRotation = targetRotation;
            this.originalRotation = originalRotation ?? transform.rotation;
            //this.movementMultipliers[1] = (this.targetRotation - this.originalRotation).magnitude;
        }

        public void SetTargetScale(Vector3 targetScale, Vector3? originalScale = null)
        {
            this.type |= TransformType.Scale;
            this.type &= ~TransformType.SizeDelta;
            this.targetScale = targetScale;
            this.originalScale = originalScale ?? transform.localScale;
            this.movementMultipliers[2] = (this.targetScale - this.originalScale).magnitude;
        }

        public void SetTargetSizeDelta(Vector2 targetDelta, Vector2? originalDelta = null)
        {
            this.type |= TransformType.SizeDelta;
            this.type &= ~TransformType.Scale;
            this.targetScale = targetDelta;
            this.originalScale = originalDelta ?? rectTransform.sizeDelta;
            this.movementMultipliers[2] = (this.targetScale - this.originalScale).magnitude;
        }

        public void SetAnimationType(AnimationType animationType, ExecutionMode mode)
        {
            this.animationType = animationType;
            this.executionMode = mode;
        }

        public void StartAnimation(float duration)
        {
            this.active = true;
            gameObject.SetActive(true);

            if (!duration.Equals(this.duration))
            {
                movementMultipliers[0] = movementMultipliers[0] * this.duration / duration;
                movementMultipliers[1] = movementMultipliers[1] * this.duration / duration;
                movementMultipliers[2] = movementMultipliers[2] * this.duration / duration;

                this.duration = duration;
            }
        }

        RectTransform rectTransform => (RectTransform)transform;

        void Swap<T>(ref T t1, ref T t2)
        {
            var tmp = t1;
            t1 = t2;
            t2 = tmp;
        }

        public void Invert()
        {
            Swap(ref originalPosition, ref targetPosition);
            Swap(ref originalRotation, ref targetRotation);
            Swap(ref originalScale, ref targetScale);
        }

        public void Reset()
        {
            if (type.HasFlag(TransformType.Position))
            {
                transform.position = originalPosition;
            }

            if (type.HasFlag(TransformType.AnchoredPosition))
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            if (type.HasFlag(TransformType.Rotation))
            {
                transform.rotation = originalRotation;
            }

            if (type.HasFlag(TransformType.Scale))
            {
                transform.localScale = originalScale;
            }
            else if (type.HasFlag(TransformType.SizeDelta))
            {
                rectTransform.sizeDelta = originalScale;
            }

            onFinish = null;
        }

        public void Initialize()
        {
            this.duration = 1f;
            this.movementMultipliers = new [] { 1f, 1f, 1f };
            this.executionMode = ExecutionMode.Manual;
        }

        void Update()
        {
            if (!active)
            {
                return;
            }

            bool allDone = true;

            if (type.HasFlag(TransformType.Position))
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * movementMultipliers[0]);
                allDone &= transform.position.Equals(targetPosition);
            }

            if (type.HasFlag(TransformType.AnchoredPosition))
            {
                var rectTransform = this.rectTransform;
                rectTransform.anchoredPosition = Vector3.MoveTowards(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * movementMultipliers[0]);
                allDone &= rectTransform.anchoredPosition.Equals(targetPosition);
            }

            if (type.HasFlag(TransformType.Rotation))
            {
                transform.rotation = Quaternion.Lerp(originalRotation, targetRotation, Time.deltaTime * movementMultipliers[1]);
                allDone &= transform.rotation.Equals(targetRotation);
            }

            if (type.HasFlag(TransformType.Scale))
            {
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * movementMultipliers[2]);
                allDone &= transform.localScale.Equals(targetScale);
            }

            if (type.HasFlag(TransformType.SizeDelta))
            {
                var rectTransform = this.rectTransform;
                rectTransform.sizeDelta = Vector3.MoveTowards(rectTransform.sizeDelta, targetScale, Time.deltaTime * movementMultipliers[2]);
                allDone &= rectTransform.sizeDelta.Equals(targetScale);
            }

            if (allDone)
            {
                onFinish?.Invoke();

                switch (animationType)
                {
                    case AnimationType.Loop:
                        if (executionMode == ExecutionMode.Manual)
                        {
                            active = false;
                        }

                        Reset();
                        break;
                    case AnimationType.Circular:
                        if (executionMode == ExecutionMode.Manual)
                        {
                            active = false;
                        }

                        Invert();
                        break;
                }
            }
        }
    }
}