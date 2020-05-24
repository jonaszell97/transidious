using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        private Quaternion nextRotation;
        private float rotationThreshold;

        public delegate void CompletionCallback();

        public CompletionCallback completionCallback;

        internal float totalProgress;
        private float totalThreshold;

        public float TotalProgressRelative => Mathf.Min(1f, totalProgress / totalThreshold);
        public float TotalProgressAbsolute => Mathf.Min(totalThreshold, totalProgress);
        public float ExtraDistanceDriven => Mathf.Max(0f, progress - threshold);
        
        public Distance DistanceFromStart => Distance.FromMeters(totalProgress);
        public Distance DistanceLeft => Distance.FromMeters(Mathf.Max(0f, totalThreshold - totalProgress));

        public PathFollowingObject(SimulationController sim, GameObject subject,
                                   List<Vector3> pathNodes, Velocity velocity,
                                   CompletionCallback callback = null, 
                                   bool setRotation = false)
        {
            this.sim = sim;
            this.subject = subject;
            this.pathNodes = pathNodes;
            this.velocity = velocity;
            this.completionCallback = callback;

            this.totalProgress = 0f;
            this.totalThreshold = 0f;
            this.progress = 0f;

            for (var i = 0; i < pathNodes.Count - 1; ++i)
            {
                this.totalThreshold += (pathNodes[i + 1] - pathNodes[i]).magnitude;
            }

            var tf = subject.transform;

            Vector2 dir;
            if (!((Vector2)tf.position).Equals(pathNodes[0]))
            {
                totalProgress += ((Vector2) tf.position - (Vector2) pathNodes[0]).magnitude;

                this.currentNode = 1;
                startPosition = tf.position;
                nextPosition = pathNodes[0];

                dir = nextPosition - startPosition;
            }
            else
            {
                this.currentNode = 2;
                startPosition = pathNodes[0];
                nextPosition = pathNodes[1];

                dir = nextPosition - startPosition;
            }

            threshold = dir.magnitude;
            direction = dir.normalized;
            prevRotation = subject.transform.rotation;

            if (setRotation)
            {
                prevRotation = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
                subject.transform.rotation = prevRotation;
            }

            if (currentNode < pathNodes.Count)
            {
                var targetDir = (Vector2) pathNodes[currentNode] - nextPosition;
                rotationThreshold = threshold; // targetDir.magnitude;
                nextRotation = Quaternion.FromToRotation(Vector3.up, targetDir);
            }
            else
            {
                nextRotation = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
                rotationThreshold = threshold;
            }
        }

        public void SimulateProgressRelative(float relativeProgress)
        {
            var absoluteProgress = relativeProgress * totalThreshold;
            SimulateProgressAbsolute(absoluteProgress);
        }

        public void SimulateProgressAbsolute(float absoluteProgress)
        {
            while (totalProgress < absoluteProgress)
            {
                if (totalProgress + this.threshold >= absoluteProgress)
                {
                    this.progress = absoluteProgress - this.totalProgress;
                    this.totalProgress += this.progress;
                    break;
                }

                totalProgress += threshold;
                subject.transform.SetPositionInLayer(nextPosition);

                MoveNext();
            }

            UpdatePositionAndRotation();
        }

        void MoveNext()
        {
            progress = 0f;
            startPosition = subject.transform.position;
            nextPosition = pathNodes[currentNode++];

            var dir = nextPosition - startPosition;
            threshold = dir.magnitude;
            direction = dir.normalized;
            prevRotation = subject.transform.rotation;

            if (currentNode < pathNodes.Count)
            {
                var targetDir = (Vector2) pathNodes[currentNode] - nextPosition;
                rotationThreshold = threshold;//targetDir.magnitude;
                nextRotation = Quaternion.FromToRotation(Vector3.up, targetDir);
            }
            else
            {
                nextRotation = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
                rotationThreshold = threshold;
            }
        }

        void UpdateRotation()
        {
            subject.transform.rotation = Quaternion.Lerp(prevRotation, nextRotation, progress / rotationThreshold);
        }

        void UpdatePositionAndRotation()
        {
            var diff = threshold - progress;
            if (diff > 0f)
            {
                var lerpedPos = startPosition + direction * progress;
                subject.transform.position = new Vector3(lerpedPos.x, lerpedPos.y, subject.transform.position.z);
                
                UpdateRotation();
            }
            else if (currentNode < pathNodes.Count)
            {
                if (diff.Equals(0f))
                {
                    UpdateRotation();
                    MoveNext();
                }
                else
                {
                    MoveNext();
                    progress = -diff;

                    UpdateRotation();
                }

            }
            else if (completionCallback != null)
            {
                subject.transform.position = new Vector3(nextPosition.x, nextPosition.y, subject.transform.position.z);
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
