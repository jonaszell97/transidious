using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public class PathFollowingObject
    {
        public SimulationController sim;
        public List<Vector3> pathNodes;
        public GameObject obj;
        public float velocity;
        public float threshold;
        public Vector2 direction;
        float length;
        bool isFinalStep;
        public float progress;
        Vector2 nextPosition;
        int currentNode;
        Vector2 startPosition;
        Quaternion prevRotation;

        public delegate void CompletionCallback(PathFollowingObject obj);

        public CompletionCallback completionCallback;
        public TrafficSimulator.ActivePath activePath = null;

        public float totalProgress;
        public float totalThreshold;

        public PathFollowingObject(SimulationController sim, GameObject obj,
                                   List<Vector3> pathNodes, float velocity, float length,
                                   bool isFinalStep = false,
                                   PathFollowingObject.CompletionCallback callback = null,
                                   TrafficSimulator.ActivePath activePath = null)
        {
            this.sim = sim;
            this.obj = obj;
            this.pathNodes = pathNodes;
            this.velocity = velocity;
            this.completionCallback = callback;
            this.length = length;
            this.isFinalStep = isFinalStep;
            this.currentNode = 1;
            this.activePath = activePath;

            this.totalProgress = 0f;
            this.totalThreshold = 0f;

            for (var i = 0; i < pathNodes.Count - 1; ++i)
            {
                this.totalThreshold += (pathNodes[i + 1] - pathNodes[i]).magnitude;
            }

            MoveNext();

            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up,
                                                               nextPosition - startPosition);
            prevRotation = obj.transform.rotation;
        }

        public void SimulateProgress(float progress)
        {
            // Progress is percentual
            progress *= totalThreshold;

            while (this.totalProgress < progress)
            {
                if (this.totalProgress + threshold >= progress)
                {
                    this.progress = (progress - totalProgress);
                    break;
                }
                else
                {
                    totalProgress += threshold;
                    MoveNext();
                }
            }

            UpdatePositionAndRotation();
        }

        void MoveNext()
        {
            progress = 0f;
            startPosition = obj.transform.position;
            nextPosition = pathNodes[currentNode++];

            var dir = (nextPosition - startPosition);
            threshold = dir.magnitude;
            direction = dir.normalized;
            prevRotation = obj.transform.rotation;
        }

        void UpdateRotation(float progress)
        {
            if (startPosition.Equals(obj.transform.position))
                return;

            var rot = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
            obj.transform.rotation = Quaternion.Lerp(prevRotation, rot, progress);
        }

        void UpdatePositionAndRotation()
        {
            var diff = threshold - progress;
            // Kind of a hack, but who am I to judge
            if (diff > 0f && !(isFinalStep && diff < 1f && velocity.Equals(0f)))
            {
                var progressPercentage = progress / threshold;
                var lerpedPos = Vector2.Lerp(startPosition, nextPosition, progressPercentage);

                obj.transform.position = new Vector3(lerpedPos.x, lerpedPos.y,
                                                     obj.transform.position.z);

                UpdateRotation(progressPercentage);
            }
            else if (currentNode < pathNodes.Count)
            {
                obj.transform.position = new Vector3(nextPosition.x, nextPosition.y,
                                                     obj.transform.position.z);

                UpdateRotation(1);
                MoveNext();
            }
            else if (completionCallback != null)
            {
                completionCallback(this);
                completionCallback = null;
            }
        }

        public void FixedUpdate()
        {
            var newProgress = Time.fixedDeltaTime * velocity * sim.SpeedMultiplier;
            progress += newProgress;
            totalProgress += newProgress;

            if (activePath != null)
            {
                activePath.progress = Mathf.Min(1f, totalProgress / totalThreshold);
                activePath.currentVelocity = velocity;
            }

            UpdatePositionAndRotation();
        }
    }
}
