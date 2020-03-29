using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public class PathFollowingObject
    {
        public GameObject subject;
        public SimulationController sim;
        public List<Vector3> pathNodes;
        public Velocity velocity;
        public float threshold;
        public Vector2 direction;
        public float progress;
        Vector2 nextPosition;
        int currentNode;
        Vector2 startPosition;
        Quaternion prevRotation;

        public delegate void CompletionCallback();

        public CompletionCallback completionCallback;

        private float totalProgress;
        private float totalThreshold;

        public float TotalProgress => Mathf.Min(1f, totalProgress / totalThreshold);

        public PathFollowingObject(SimulationController sim, GameObject subject,
                                   List<Vector3> pathNodes, Velocity velocity,
                                   CompletionCallback callback = null)
        {
            this.sim = sim;
            this.subject = subject;
            this.pathNodes = pathNodes;
            this.velocity = velocity;
            this.completionCallback = callback;
            this.currentNode = 1;

            this.totalProgress = 0f;
            this.totalThreshold = 0f;

            for (var i = 0; i < pathNodes.Count - 1; ++i)
            {
                this.totalThreshold += (pathNodes[i + 1] - pathNodes[i]).magnitude;
            }

            MoveNext();

            prevRotation = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
            subject.transform.rotation = prevRotation;
        }

        public void SimulateProgress(float relativeProgress)
        {
            var totalProgress = relativeProgress * totalThreshold;
            while (this.totalProgress < totalProgress)
            {
                if (this.totalProgress + this.threshold >= totalProgress)
                {
                    this.progress = totalProgress - this.totalProgress;
                    break;
                }

                this.totalProgress += threshold;
                MoveNext();
            }

            UpdatePositionAndRotation();
        }

        void MoveNext()
        {
            progress = 0f;
            startPosition = subject.transform.position;
            nextPosition = pathNodes[currentNode++];

            var dir = (nextPosition - startPosition);
            threshold = dir.magnitude;
            direction = dir.normalized;
            prevRotation = subject.transform.rotation;
        }

        void UpdateRotation(float progress)
        {
            if (startPosition.Equals(subject.transform.position))
                return;

            var rot = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
            subject.transform.rotation = Quaternion.Lerp(prevRotation, rot, progress);
        }

        void UpdatePositionAndRotation()
        {
            var diff = threshold - progress;
            if (diff > 0f)
            {
                var progressPercentage = progress / threshold;
                var lerpedPos = Vector2.Lerp(startPosition, nextPosition, progressPercentage);

                subject.transform.position = new Vector3(lerpedPos.x, lerpedPos.y,
                                                     subject.transform.position.z);

                UpdateRotation(progressPercentage);
            }
            else if (currentNode < pathNodes.Count)
            {
                subject.transform.position = new Vector3(nextPosition.x, nextPosition.y,
                                                         subject.transform.position.z);

                UpdateRotation(1);
                MoveNext();
            }
            else if (completionCallback != null)
            {
                completionCallback();
                completionCallback = null;
            }
        }

        public void Update(float delta)
        {
            var newProgress = velocity.RealTimeMPS * delta;

            progress += newProgress;
            totalProgress += newProgress;

            UpdatePositionAndRotation();
        }
    }
}
