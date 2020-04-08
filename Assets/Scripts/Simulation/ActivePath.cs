using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using Transidious.PathPlanning;
using UnityEngine;
using UnityEngine.Serialization;

namespace Transidious
{
    public class ActivePath : MonoBehaviour
    {
        /// Helper class to store the current driving state.
        class DrivingState
        {
            /// Reference to the TrafficSimulator's DrivingCar for this path.
            internal TrafficSimulator.DrivingCar drivingCar;

            /// Time since the last velocity update.
            internal float timeSinceLastUpdate;
        }

        /// Reference to the traffic simulator.
        public TrafficSimulator trafficSim;

        /// The path planning result that this path executes.
        public PathPlanningResult path;

        /// The citizen that is following the path.
        public Citizen citizen;

        /// The completion callback.
        public System.Action onDone;

        /// The current step on the path.
        [CanBeNull]
        public PathStep currentStep
        {
            get
            {
                if (_currentStep >= path.steps.Count)
                    return null;

                return path.steps[_currentStep];
            }
        }
        
        /// The next step on the path.
        [CanBeNull]
        public PathStep nextStep
        {
            get
            {
                if (_currentStep + 1 >= path.steps.Count)
                    return null;

                return path.steps[_currentStep + 1];
            }
        }
        
        /// The renderer for the car / citizen sprites.
        private SpriteRenderer _spriteRenderer;

        [FormerlySerializedAs("_boxCollider2D")] public BoxCollider2D boxCollider2D;

        private SpriteRenderer spriteRenderer
        {
            get
            {
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
                    boxCollider2D.enabled = false;
                }

                return _spriteRenderer;
            }
        }

        /// The path following helper.
        private PathFollowingObject _pathFollowingHelper;
        public PathFollowingObject PathFollowingHelper => _pathFollowingHelper;

        public Vector2 CurrentDirection => _pathFollowingHelper?.direction ?? Vector2.zero;

        /// The time to wait until.
        private DateTime? _waitUntil;

        /// The current driving car ref.
        private DrivingState _drivingState;

        /// The transit vehicle this path is currently in.
        public TransitVehicle transitVehicle;

        public bool IsDriving => _drivingState != null && _pathFollowingHelper != null;
        public bool IsWalking => currentStep.type == PathStep.Type.Walk;

        public Bounds Bounds => spriteRenderer.bounds;

        /// The current step number.
        private int _currentStep = 0;

        /// Progress on the current step from 0.0 - 1.0.
        private float _currentStepProgress = 0f;

        /// The current velocity (for steps where it matters).
        private Velocity _currentVelocity;

        public Velocity CurrentVelocity => _currentVelocity;

        /// The map tile this path is currently on.
        public MapTile currentTile;
        
        /// Interval for tile updates.
        private static readonly float _tileUpdateInterval = 0.5f;

        /// Time since last tile update.
        private float _timeSinceLastTileUpdate = 0f;
        
        /// Initialize a path for a citizen.
        public void Initialize(PathPlanningResult path, Citizen c, System.Action onDone = null)
        {
            this.path = path;
            this.citizen = c;
            this.trafficSim = GameController.instance.sim.trafficSim;
            this.onDone = onDone;
            this._currentStep = path.steps.Count;

            this.transform.SetLayer(MapLayer.Cars);
            this.transform.SetParent(GameController.instance.sim.transform);

            c.activePath = this;
        }

#if DEBUG
        private Text _metricsTxt;
#endif

        public void StartPath()
        {
            _currentStep = 0;
            ContinuePath();
            UpdateTile();

#if DEBUG
            if (trafficSim.displayPathMetrics)
            {
                if (_metricsTxt == null)
                {
                    _metricsTxt = GameController.instance.loadedMap.CreateText(Vector3.zero, "", Color.black, 2f);
                    _metricsTxt.textMesh.alignment = TextAlignmentOptions.Center;
                }

                _metricsTxt.enabled = true;
            }
#endif
        }

        private void Update()
        {
            if (GameController.instance.Paused)
            {
                return;
            }

            _timeSinceLastTileUpdate += Time.deltaTime;

            if (_timeSinceLastTileUpdate >= _tileUpdateInterval)
            {
                UpdateTile();
            }

            // Check if we're still waiting for something.
            if (_waitUntil.HasValue)
            {
                if (trafficSim.sim.GameTime >= _waitUntil.Value)
                {
                    CompleteStep();
                }
                else
                {
                    return;
                }
            }

            // Update velocity if currently driving.
            if (IsDriving)
            {
                UpdateDrivingState();
            }
            else
            {
                _pathFollowingHelper?.Update(Time.deltaTime * trafficSim.sim.SpeedMultiplier);
            }

            citizen.currentPosition = transform.position;

            if (_currentStep >= path.steps.Count)
            {
                return;
            }

            UpdateProgress();

#if DEBUG
            if (trafficSim.displayPathMetrics)
            {
                _metricsTxt.SetText($"v {_currentVelocity:n2}\nd {(_drivingState?.drivingCar.DistanceToGoal ?? 0f):n2}");
                var tf = transform.position;
                _metricsTxt.textMesh.transform.position = new Vector3(tf.x, tf.y + 5f, tf.z);
            }
#endif
        }

        void UpdateTile()
        {
            var tile = SaveManager.loadedMap.GetTile(transform.position);
            if (tile != null)
            {
                currentTile?.activePaths.Remove(this);
                tile.activePaths.Add(this);
                currentTile = tile;
            }

            _timeSinceLastTileUpdate = 0f;
        }
        
        void UpdateProgress()
        {
            switch (currentStep.type)
            {
                case PathStep.Type.Drive:
                case PathStep.Type.PartialDrive:
                case PathStep.Type.Walk:
                case PathStep.Type.Turn:
                    _currentStepProgress = _pathFollowingHelper.TotalProgress;
                    break;
                case PathStep.Type.PublicTransit:
                    // TODO
                    break;
            }
        }

        public void ContinuePath()
        {
            var step = path.steps[_currentStep];
            switch (step.type)
            {
                case PathStep.Type.Walk:
                    InitWalk(step as WalkStep);
                    break;
                case PathStep.Type.Wait:
                    InitWait(step as WaitStep);
                    break;
                case PathStep.Type.Drive:
                case PathStep.Type.PartialDrive:
                {
                    trafficSim.GetStepPath(step, out StreetSegment segment, 
                        out bool backward, out bool _,
                        out int lane, out List<Vector3> positions,
                        out bool _, out bool _,
                        out Vector2 _);

                    IMapObject dstParkingLot = null;
                    if (step is PartialDriveStep ps)
                    {
                        dstParkingLot = ps.parkingLot;
                    }

                    InitDrive(positions, segment, backward, lane, dstParkingLot);
                    break;
                }
                case PathStep.Type.Turn:
                    InitTurnStep(step as TurnStep);
                    break;
                case PathStep.Type.PublicTransit:
                    InitTransitStep(step as PublicTransitStep);
                    break;
            }
        }

        void PathDone()
        {
            this.onDone?.Invoke();
            citizen.activePath = null;
            ResourceManager.instance.Reclaim(this);

#if DEBUG
            if (trafficSim.displayPathMetrics)
            {
                _metricsTxt.enabled = false;
            }
#endif
        }

        bool IsDrivingStep(PathStep step)
        {
            if (step == null)
                return false;

            switch (step.type)
            {
                case PathStep.Type.Turn:
                case PathStep.Type.Drive:
                case PathStep.Type.PartialDrive:
                    return true;
                default:
                    return false;
            }
        }

        public void CompleteStep()
        {
            _waitUntil = null;
            _pathFollowingHelper = null;
            _currentStepProgress = 0f;
            transitVehicle = null;

            if (!IsDrivingStep(currentStep) || !IsDrivingStep(nextStep))
            {
                _drivingState = null;
                _currentVelocity = Velocity.zero;
                transform.localScale = Vector3.one;

                if (_spriteRenderer != null)
                {
                    _spriteRenderer.enabled = false;
                    // _boxCollider2D.enabled = false;
                }
            }

            if (++_currentStep < path.steps.Count)
            {
                ContinuePath();
            }
            else
            {
                PathDone();
            }
        }

        void UpdateSprite(string spriteName, Color c)
        {
            var sr = spriteRenderer;
            sr.color = c;
            sr.sprite = SpriteManager.GetSprite(spriteName);
            sr.enabled = true;

            boxCollider2D.size = sr.bounds.size;
            // _boxCollider2D.enabled = true;
        }

        /**
         * Other steps
         */

        void InitWait(WaitStep step)
        {
            // FIXME
            // _waitUntil = trafficSim.sim.GameTime.Add(step.waitingTime);
            CompleteStep();
        }

        /**
         * Walking steps
         */

        void InitWalk(WalkStep step)
        {
            // FIXME use actual walking path.
            var positions = new List<Vector3> {step.from, step.to};

            // Initialize walking citizen sprite.
            UpdateSprite("Sprites/citizen", citizen.preferredColor);
            transform.SetPositionInLayer(positions.First());

            // Initialize path following helper.
            _currentVelocity = citizen.WalkingSpeed;
            _pathFollowingHelper = new PathFollowingObject(
                trafficSim.sim, this.gameObject, positions, _currentVelocity, this.CompleteStep);

            if (_currentStepProgress > 0f)
            {
                _pathFollowingHelper.SimulateProgress(_currentStepProgress);
            }
        }

        /**
         * Driving steps
         */

        void InitDrive(List<Vector3> positions, StreetSegment segment, bool backward, int lane,
                       IMapObject dstParkingLot = null)
        {
            var car = citizen.car;
            Debug.Assert(car != null, "citizen has no car!");

            // Leave the parking lot (if necessary).
            if (_drivingState == null && car.parkingLot != null)
            {
                car.parkingLot.RemoveResident(citizen);
            }

            // Initialize car sprite.
            UpdateSprite($"Sprites/car{car.model}", car.color);

            var tf = transform;
            tf.localScale = new Vector3(.6f, .6f, 1f);

            // Inform traffic sim that a car is entering the intersection.
            var drivingCar = trafficSim.EnterStreetSegment(car, segment, positions.First(), lane, this);
            drivingCar.backward = backward;

            _drivingState = new DrivingState
            {
                drivingCar = drivingCar,
                timeSinceLastUpdate = 0f,
            };

            // Initialize path following helper.
            _pathFollowingHelper = new PathFollowingObject(
                trafficSim.sim, this.gameObject, positions, _currentVelocity,
                () =>
                {
                    if (dstParkingLot != null)
                    {
                        dstParkingLot.AddResident(citizen);
                        car.parkingLot = dstParkingLot;
                    }

                    trafficSim.ExitStreetSegment(segment, drivingCar);
                    CompleteStep();
                }, (currentStep as PartialDriveStep)?.partialStart ?? false);

            if (_currentStepProgress > 0f)
            {
                _pathFollowingHelper.SimulateProgress(_currentStepProgress);
            }
        }

        void InitTurnStep(TurnStep step)
        {
            // First, generate a smooth path crossing the intersection.
            var intersectionPath = trafficSim.GetIntersectionPath(step, _drivingState.drivingCar.lane);
            var carOnIntersection = trafficSim.EnterIntersection(
                _drivingState.drivingCar, step.intersection, step.to.segment);

            // Initialize path following helper.
            _pathFollowingHelper = new PathFollowingObject(
                trafficSim.sim, this.gameObject, intersectionPath, _currentVelocity, 
                () =>
                {
                    trafficSim.ExitIntersection(carOnIntersection, step.intersection);
                    CompleteStep();
                });

            if (_currentStepProgress > 0f)
            {
                _pathFollowingHelper.SimulateProgress(_currentStepProgress);
            }
        }


        void UpdateDrivingState()
        {
            var drivingCar = _drivingState.drivingCar;
            var sim = trafficSim.sim;

            var elapsedTime = Time.deltaTime * sim.SpeedMultiplier;
            _drivingState.timeSinceLastUpdate += elapsedTime;

            if (_drivingState.timeSinceLastUpdate >= TrafficSimulator.VelocityUpdateInterval)
            {
                _pathFollowingHelper.velocity = sim.trafficSim.GetCarVelocity(drivingCar, 1f);

                // Must be updated after the velocity calculation.
                _drivingState.timeSinceLastUpdate = 0f;
                _currentVelocity = _pathFollowingHelper.velocity;
            }

            if (drivingCar.waitingForTrafficLight != null)
            {
                if (drivingCar.waitingForTrafficLight.MustStop)
                {
                    return;
                }

                drivingCar.waitingForTrafficLight = null;
            }

            _pathFollowingHelper.Update(elapsedTime);

            drivingCar.distanceFromStart = sim.trafficSim.GetDistanceFromStart(
                drivingCar.segment,
                drivingCar.CurrentPosition,
                drivingCar.lane);
        }

        /**
         * Transit steps
         */
        
        void InitTransitStep(PublicTransitStep step)
        {
            var firstStop = step.routes.First().beginStop;
            firstStop.AddWaitingCitizen(this, step);
        }
        
        /**
         * Simulation
         */

        /// The work bonus per hour of the trip.
        public float RemainingWorkBonusPerHour => +(100f / 16f);

        /// The energy bonus per hour of the trip.
        public float EnergyBonusPerHour
        {
            get
            {
                if (currentStep == null)
                {
                    return 0f;
                }

                switch (currentStep.type)
                {
                    default:
                    case PathStep.Type.Drive:
                    case PathStep.Type.PartialDrive:
                    case PathStep.Type.Turn:
                        return -7f;
                    case PathStep.Type.Walk:
                        return -5f;
                    case PathStep.Type.Wait:
                        return -1f;
                    case PathStep.Type.PublicTransit:
                        return +1f;
                }
            }
        }

        /**
         * UI Callbacks
         */

        public void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            citizen.ActivateModal();
        }

        public Serialization.ActivePath Serialize()
        {
            return new Serialization.ActivePath
            {
                Path = path.ToProtobuf(),
                CitizenId = citizen.id,
                CurrentStep = _currentStep,
                WaitUntil = _waitUntil?.Ticks ?? -1,
                CurrentStepProgress = _currentStepProgress,
                CurrentVelocity = _currentVelocity.MPS,
            };
        }

        public static ActivePath Deserialize(Serialization.ActivePath path)
        {
            var result = ResourceManager.instance.GetActivePath(true);
            result.Initialize(
                PathPlanningResult.Deserialize(GameController.instance.loadedMap, path.Path), 
                GameController.instance.sim.GetCitizen(path.CitizenId));
            
            if (path.WaitUntil != -1)
            {
                result._waitUntil = new DateTime(path.WaitUntil);
            }
            
            result._currentStep = path.CurrentStep;
            result._currentStepProgress = path.CurrentStepProgress;
            result._currentVelocity = Velocity.FromMPS(path.CurrentVelocity);

            return result;
        }
    }
}