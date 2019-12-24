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

        public PathFollowingObject(SimulationController sim, GameObject obj,
                                   List<Vector3> pathNodes, float velocity, float length,
                                   bool isFinalStep = false,
                                   PathFollowingObject.CompletionCallback callback = null)
        {
            this.sim = sim;
            this.obj = obj;
            this.pathNodes = pathNodes;
            this.velocity = velocity;
            this.completionCallback = callback;
            this.length = length;
            this.isFinalStep = isFinalStep;
            this.currentNode = 1;

            MoveNext();

            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up,
                                                               nextPosition - startPosition);
            prevRotation = obj.transform.rotation;
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

            _expectedProgress += threshold;
        }

        void UpdateRotation(float progress)
        {
            if (startPosition.Equals(obj.transform.position))
                return;

            var rot = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
            obj.transform.rotation = Quaternion.Lerp(prevRotation, rot, progress);
        }

        public float _expectedProgress = 0f;
        public float _totalProgress = 0f;

        public void FixedUpdate()
        {
            progress += Time.fixedDeltaTime * velocity * sim.SpeedMultiplier;

            var diff = threshold - progress;
            // Kind of a hack, but who am I to judge
            if (diff > 0f && !(isFinalStep && diff < 1f && velocity.Equals(0f)))
            {
                var progressPercentage = this.progress / threshold;
                var lerpedPos = Vector2.Lerp(startPosition, nextPosition, progressPercentage);

                obj.transform.position = new Vector3(lerpedPos.x, lerpedPos.y,
                                                     obj.transform.position.z);

                UpdateRotation(progressPercentage);
            }
            else if (currentNode < pathNodes.Count)
            {
                _totalProgress += progress;
                obj.transform.position = new Vector3(nextPosition.x, nextPosition.y,
                                                     obj.transform.position.z);

                UpdateRotation(1);
                MoveNext();
            }
            else if (completionCallback != null)
            {
                _totalProgress += progress;

                completionCallback(this);
                completionCallback = null;
            }
        }
    }
}
