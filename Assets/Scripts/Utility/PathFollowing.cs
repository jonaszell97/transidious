using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class PointsFollower
    {
        public GameObject subject;
        public List<Vector2> pathNodes;
        public Velocity velocity;
        public float threshold;
        public Vector2 direction;
        public float progress;
        Vector2 nextPosition;
        int currentNode;
        Vector2 startPosition;
        
        public delegate void CompletionCallback();

        public CompletionCallback completionCallback;

        internal float totalProgress;
        private float totalThreshold;

        public float TotalProgressRelative => Mathf.Min(1f, totalProgress / totalThreshold);
        public float TotalProgressAbsolute => Mathf.Min(totalThreshold, totalProgress);

        public Distance DistanceFromStart => Distance.FromMeters(totalProgress);
        public Distance DistanceLeft => Distance.FromMeters(Mathf.Max(0f, totalThreshold - totalProgress));

        public PointsFollower(GameObject subject,
                              List<Vector2> pathNodes, Velocity velocity,
                              CompletionCallback callback = null)
        {
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

            subject.transform.rotation = Quaternion.FromToRotation(Vector3.up, nextPosition - startPosition);
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
        }

        void UpdateRotation()
        {
            var tf = subject.transform;
            var lookaheadPos = startPosition + direction * (progress + 3f);
            tf.rotation = Quaternion.FromToRotation(Vector3.up, lookaheadPos - (Vector2)tf.position);
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
    
    public class PathFollower
    {
        /// Callback type.
        public delegate void StepCompletionCallback(PathSegment? nextSegment);

        /// The subject that should follow the path.
        public readonly GameObject Subject;
        
        /// The path to follow.
        public Path Path;
        
        /// The current velocity.
        public Velocity Velocity;
        
        /// Threshold for completion of the current step.
        public float Threshold;
        
        /// Direction of the current step.
        public Vector2 Direction;
        
        /// Progress of the current step.
        public float Progress;

        /// The current path segment index.
        private int _currentStep;

        /// The callback to call upon completing a step.
        public StepCompletionCallback CompletionCallback;

        /// The total progress along the path.
        public float TotalProgress;

        /// The total completion threshold (i.e. the total length of the path).
        private readonly float _totalThreshold;

        public float TotalProgressRelative => Mathf.Min(1f, TotalProgress / _totalThreshold);
        public float TotalProgressAbsolute => Mathf.Min(_totalThreshold, TotalProgress);

        public Distance DistanceFromStart => Distance.FromMeters(TotalProgress);
        public Distance DistanceLeft => Distance.FromMeters(Mathf.Max(0f, _totalThreshold - TotalProgress));

        public PathFollower(GameObject subject, Path path, Velocity velocity,
                            StepCompletionCallback callback = null)
        {
            this.Subject = subject;
            this.Path = path;
            this.Velocity = velocity;
            this.CompletionCallback = callback;

            this.TotalProgress = 0f;
            this._totalThreshold = path.Length;

            MoveToStep(0);
        }

        public void MoveToStep(int step)
        {
            if (Path.Segments.Count <= step)
            {
                Debug.Break();
            }
            Debug.Assert(Path.Segments.Count > step, "invalid step!");

            var seg = Path.Segments[step];
            _currentStep = step;
            Progress = 0f;
            Threshold = seg.Length;

            Subject.transform.SetPositionInLayer(seg.Points.First());
            Subject.transform.rotation = Quaternion.FromToRotation(Vector3.up, seg.StartDirection);
        }

        public void SimulateProgressRelative(float relativeProgress)
        {
            var absoluteProgress = relativeProgress * _totalThreshold;
            SimulateProgressAbsolute(absoluteProgress);
        }

        public void SimulateProgressAbsolute(float absoluteProgress)
        {
            while (TotalProgress < absoluteProgress)
            {
                if (TotalProgress + this.Threshold >= absoluteProgress)
                {
                    this.Progress = absoluteProgress - this.TotalProgress;
                    this.TotalProgress += this.Progress;
                    break;
                }

                TotalProgress += Threshold;
                MoveNext();
            }

            UpdatePositionAndRotation();
        }

        void MoveNext()
        {
            MoveToStep(_currentStep + 1);
            CompletionCallback?.Invoke(Path.Segments[_currentStep]);
        }

        void UpdateRotation(float percentage)
        {
            var tf = Subject.transform;
            var lookaheadPos = Path.Segments[_currentStep].PointAt(percentage + .01f);
            tf.rotation = Quaternion.FromToRotation(Vector3.up, lookaheadPos - (Vector2)tf.position);
        }

        void UpdatePositionAndRotation()
        {
            var percentage = Progress / Threshold;
            if (percentage < 1f)
            {
                var lerpedPos = Path.Segments[_currentStep].PointAt(percentage);
                Subject.transform.SetPositionInLayer(lerpedPos);
                UpdateRotation(percentage);
            }
            else if (_currentStep + 1 < Path.Segments.Count)
            {
                MoveNext();
            }
            else
            {
                var lerpedPos = Path.Segments[_currentStep].PointAt(1f);
                Subject.transform.SetPositionInLayer(lerpedPos);
                CompletionCallback?.Invoke(null);
            }
        }

        public float Update(float delta)
        {
            var newProgress = Velocity.RealTimeMPS * delta;

            Progress += newProgress;
            TotalProgress += newProgress;

            UpdatePositionAndRotation();
            return newProgress;
        }

        public void UpdatePosition(float distanceDelta)
        {
            Progress += distanceDelta;
            TotalProgress += distanceDelta;

            UpdatePositionAndRotation();
        }
    }
}
