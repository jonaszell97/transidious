using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using Transidious.PathPlanning;
using UnityEngine;
using UnityEngine.Serialization;

using DrivingCar = Transidious.TrafficSimulator.DrivingCar;

namespace Transidious
{
    public class ActivePath : MonoBehaviour
    {
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
                if (_currentStep >= path.path.Steps.Length)
                    return null;

                return path.path.Steps[_currentStep];
            }
        }
        
        /// The next step on the path.
        [CanBeNull]
        public PathStep nextStep
        {
            get
            {
                if (_currentStep + 1 >= path.path.Steps.Length)
                    return null;

                return path.path.Steps[_currentStep + 1];
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
        private PathFollower _pathFollowingHelper;
        public PathFollower PathFollowingHelper => _pathFollowingHelper;

        /// The time to wait until.
        private DateTime? _waitUntil;

        /// The current driving car ref.
        internal DrivingCar _drivingCar;

        /// The transit vehicle this path is currently in.
        public TransitVehicle transitVehicle;

        public bool IsDriving => _drivingCar != null && _pathFollowingHelper != null;

        public Bounds Bounds => spriteRenderer.bounds;

        /// The current step number.
        private int _currentStep = 0;

        /// Progress on the current step from 0.0 - 1.0.
        private float _currentStepProgress = 0f;

        public Velocity CurrentVelocity => _pathFollowingHelper?.Velocity ?? Velocity.zero;

        /// The IDM simulator.
        public IDM idm;

        /// The map tile this path is currently on.
        public MapTile currentTile;
        
        /// Interval for tile updates.
        private static readonly float _tileUpdateInterval = 0.5f;

        /// Time since last tile update.
        private float _timeSinceLastTileUpdate;

        /// Initialize a path for a citizen.
        public void Initialize(PathPlanningResult path, Citizen c, System.Action onDone = null)
        {
            this.path = path;
            this.citizen = c;
            this.trafficSim = GameController.instance.sim.trafficSim;
            this.onDone = onDone;
            this._currentStep = path.path.Steps.Length;

            this.transform.SetLayer(MapLayer.Cars);
            this.transform.SetParent(GameController.instance.sim.transform);

            c.ActivePath = this;
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

            if (transitVehicle != null)
            {
                citizen.CurrentPosition = transitVehicle.transform.position;
            }
            else
            {
                citizen.CurrentPosition = transform.position;
            }

            if (_currentStep >= path.path.Steps.Length)
            {
                return;
            }

            UpdateProgress();

#if DEBUG
            if (trafficSim.displayPathMetrics)
            {
                _metricsTxt.SetText($"v {CurrentVelocity:n2}\nd {(_drivingCar?.DistanceToGoal ?? 0f):n2}");
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
                    _currentStepProgress = _pathFollowingHelper.TotalProgressRelative;
                    break;
                case PathStep.Type.PublicTransit:
                    // TODO
                    break;
            }
        }

        public void ContinuePath()
        {
            var step = path.path.Steps[_currentStep];
            switch (step)
            {
                case WalkStep walkStep:
                    InitWalk(walkStep);
                    break;
                case WaitStep waitStep:
                    InitWait(waitStep);
                    break;
                case DriveStep _:
                case TurnStep _:
                    Debug.Assert(false, "should never happen");
                    break;
                case PartialDriveStep partialDriveStep:
                    InitDrivingSteps(partialDriveStep);
                    break;
                case PublicTransitStep publicTransitStep:
                    InitTransitStep(publicTransitStep);
                    break;
            }
        }

        void PathDone()
        {
            this.onDone?.Invoke();
            citizen.ActivePath = null;

            onDone = null;
            _currentStep = int.MaxValue;

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
            _drivingCar = null;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            if (++_currentStep < path.path.Steps.Length)
            {
                ContinuePath();
            }
            else
            {
                PathDone();
            }
        }

        /// Whether or not we can safely abort this path right now.
        public bool Abortable => !IsDriving || idm.Abortable;

        public void Abort(bool reclaim)
        {
            Debug.Assert(Abortable, "can't abort this path right now!");

            if (_drivingCar != null)
            {
                if (_drivingCar.Turning)
                {
                    trafficSim.ExitIntersection(_drivingCar, _drivingCar.NextIntersection);
                }
                else
                {
                    trafficSim.ExitStreetSegment(_drivingCar.Segment, _drivingCar);
                }
            }

            _waitUntil = null;
            _pathFollowingHelper = null;
            _currentStepProgress = 0f;
            _drivingCar = null;
            _currentStep = int.MaxValue;
            onDone = null;
            transitVehicle = null;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            if (reclaim)
            {
                citizen.ActivePath = null;
                ResourceManager.instance.Reclaim(this);
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
            Debug.Assert(_drivingCar == null);

            // FIXME use actual walking path.
            var path = new PathSegment(step.from, step.to);

            // Initialize walking citizen sprite.
            UpdateSprite("Sprites/citizen", citizen.PreferredColor);
            transform.SetPositionInLayer(step.from);

            // Initialize path following helper.
            _pathFollowingHelper = new PathFollower(
                this.gameObject, new Path(path), citizen.WalkingSpeed, _ => this.CompleteStep());

            if (_currentStepProgress > 0f)
            {
                _pathFollowingHelper.SimulateProgressRelative(_currentStepProgress);
            }
        }

        /**
         * Driving steps
         */

        void InitDrivingSteps(PartialDriveStep firstStep)
        {
            var car = citizen.Car;
            var pathSegments = new List<PathSegment>();
            var steps = path.path.Steps;
            var length = 0f;

            for (var i = _currentStep; i < steps.Length; ++i)
            {
                var step = steps[i];
                if (!IsDrivingStep(step))
                {
                    break;
                }

                var stepPath = trafficSim.StreetPathBuilder.GetStepPath(step);
                length += stepPath.Length;

                pathSegments.Add(stepPath);
            }

            var completePath = new Path(pathSegments, length);
            _pathFollowingHelper = new PathFollower(gameObject, completePath, Velocity.zero, (nextSegment) =>
            {
                var finishedStep = currentStep;
                Debug.Assert(finishedStep != null);

                switch (finishedStep.type)
                {
                    case PathStep.Type.Drive:
                    case PathStep.Type.PartialDrive:
                    {
                        if (finishedStep.type == PathStep.Type.PartialDrive)
                        {
                            var dstParkingLot = ((PartialDriveStep)finishedStep).parkingLot;
                            if (dstParkingLot != null)
                            {
                                dstParkingLot.AddOccupant(OccupancyKind.ParkingCitizen, citizen);
                                car.parkingLot = dstParkingLot;
                            }
                        }

                        trafficSim.ExitStreetSegment(_drivingCar.Segment, _drivingCar);
                        break;
                    }
                    case PathStep.Type.Turn:
                    {
                        idm.UnblockIntersection();
                        trafficSim.ExitIntersection(_drivingCar, ((TurnStep)finishedStep).intersection);
                        break;
                    }
                    default:
                        Debug.LogError("not a drive step!");
                        break;
                }

                if (nextSegment == null)
                {
                    CompleteStep();
                    return;
                }

                ++_currentStep;

                var next = currentStep;
                if (next == null)
                    return;

                switch (next.type)
                {
                    case PathStep.Type.Drive:
                    case PathStep.Type.PartialDrive:
                    {
                        if (car.parkingLot != null)
                        {
                            car.parkingLot.RemoveOccupant(OccupancyKind.ParkingCitizen, citizen);
                            car.parkingLot = null;
                        }

                        DriveSegment driveSegment;
                        float distanceFromStart;

                        if (next is DriveStep driveStep)
                        {
                            driveSegment = driveStep.driveSegment;
                            distanceFromStart = 0;
                        }
                        else
                        {
                            driveSegment = ((PartialDriveStep) next).driveSegment;
                            distanceFromStart = ((PartialDriveStep) next).DistanceFromStart;
                        }

                        trafficSim.EnterStreetSegment(
                            _drivingCar, car, driveSegment.segment, distanceFromStart,
                            trafficSim.GetDefaultLane(driveSegment.segment,
                                driveSegment.backward), this, nextStep);

                        _drivingCar.Backward = driveSegment.backward;
                        idm.Reset(_drivingCar, CurrentVelocity);

                        break;
                    }
                    case PathStep.Type.Turn:
                    {
                        if (!idm.BlockingIntersection)
                        {
                            GameController.instance.EnterPause();
                            GameController.instance.input.MoveTowards(transform.position);
                            GameController.instance.input.SetZoomLevel(InputController.minZoom);
                            citizen.ActivateModal();
                        }
                        Debug.Assert(idm.BlockingIntersection, "entering unblocked intersection!");

                        trafficSim.EnterIntersection(_drivingCar, ((TurnStep)next).intersection);
                        idm.Reset(_drivingCar, CurrentVelocity);

                        break;
                    }
                }
            });

            // Initialize car sprite.
            UpdateSprite($"Sprites/car{(int)car.model}", car.color);

            // Update starting position.
            Debug.Assert(_drivingCar == null);
            transform.SetPositionInLayer(completePath.Start);

            // Inform traffic sim that a car is entering the intersection.
            _drivingCar = trafficSim.EnterStreetSegment(car, firstStep.driveSegment.segment, 
                                                        firstStep.DistanceFromStart,
                                                        trafficSim.GetDefaultLane(firstStep.driveSegment.segment,
                                                            firstStep.driveSegment.backward), this,
                                                        nextStep);

            _drivingCar.Backward = firstStep.driveSegment.backward;

            // (Re-)initialize IDM.
            if (idm == null)
            {
                idm = new IDM();
            }

            idm.Initialize(_drivingCar, Velocity.zero);
        }

        void UpdateDrivingState()
        {
            var sim = trafficSim.sim;
            idm.Update(sim, _pathFollowingHelper);
        }

        /**
         * Transit steps
         */

        void InitTransitStep(PublicTransitStep step)
        {
            var firstStop = step.routes.First().beginStop;
            firstStop.AddWaitingCitizen(this, step);

            citizen.Money -= step.line.TripFare;
            Debug.Assert(citizen.Money >= 0m, "not enough money to pay for trip!");

            GameController.instance.financeController.Earn(step.line.TripFare);
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
                        return -3f;
                    case PathStep.Type.Walk:
                        return -4f;
                    case PathStep.Type.Wait:
                        return -.5f;
                    case PathStep.Type.PublicTransit:
                        return +.5f;
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
            };
        }

        public static ActivePath Deserialize(Serialization.ActivePath path)
        {
            var result = ResourceManager.instance.GetActivePath(true);
            result.Initialize(new PathPlanningResult(path.Path, GameController.instance.loadedMap), 
                GameController.instance.sim.GetCitizen(path.CitizenId));

            if (path.WaitUntil != -1)
            {
                result._waitUntil = new DateTime(path.WaitUntil);
            }

            result._currentStep = path.CurrentStep;
            result._currentStepProgress = path.CurrentStepProgress;

            return result;
        }
    }
}