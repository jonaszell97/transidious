using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public class Car : MonoBehaviour
    {
        SimulationController sim;
        int carModel;
        public Citizien driver;
        public float maxVelocity;
        public float acceleration;
        public float length;
        public PathFollowingObject pathFollow;
        public PathFollowingObject.CompletionCallback callback;
        public TrafficSimulator.DrivingCar drivingCar;

        public bool isFocused
        {
            get
            {
                return sim.citizienModal.citizien == driver;
            }
        }

        public bool IsDriving
        {
            get
            {
                return pathFollow != null;
            }
        }

        public Color color
        {
            get
            {
                var renderer = GetComponent<SpriteRenderer>();
                return renderer.color;
            }
            set
            {
                var renderer = GetComponent<SpriteRenderer>();
                renderer.color = value;
            }
        }

        public void Initialize(SimulationController sim, Citizien driver, Color c, int carModel = -1)
        {
            var renderer = GetComponent<SpriteRenderer>();
            renderer.color = c;

            if (carModel == -1)
            {
                carModel = UnityEngine.Random.Range(0, SpriteManager.instance.carSprites.Length - 1);
            }

            this.carModel = carModel;
            renderer.sprite = SpriteManager.instance.carSprites[carModel];

            switch (carModel)
            {
            default:
            case 0:
                this.maxVelocity = 33.333f * Map.Meters; // 120 km/h
                this.acceleration = 2.5f * Map.Meters;
                break;
            case 1:
                this.maxVelocity = 19.444f * Map.Meters; // 70 km/h
                this.acceleration = 2.5f * Map.Meters;
                break;
            case 2:
                this.maxVelocity = 27.777f * Map.Meters; // 100 km/h
                this.acceleration = 2.5f * Map.Meters;
                break;
            case 3:
                this.maxVelocity = 27.777f * Map.Meters; // 100 km/h
                this.acceleration = 2.5f * Map.Meters;
                break;
            case 4:
                this.maxVelocity = 22.222f * Map.Meters; // 80 km/h
                this.acceleration = 2.5f * Map.Meters;
                break;
            }

            this.sim = sim;
            this.driver = driver;
            driver.car = this;
            this.length = renderer.bounds.size.y;
            this.transform.SetLayer(MapLayer.Cars, 1);

            var collider = GetComponent<BoxCollider2D>();
            collider.size = renderer.bounds.size;
        }

        void PathDone(PathFollowingObject obj)
        {
            var oldPathFollow = pathFollow;
            this.gameObject.SetActive(false);

            if (callback != null)
            {
                callback(obj);
            }
            if (pathFollow == oldPathFollow)
            {
                pathFollow = null;
            }
        }

        public void FollowPath(List<Vector3> path,
                               float startingVelocity, bool isFinalStep = false,
                               PathFollowingObject.CompletionCallback callback = null)
        {
            this.gameObject.SetActive(true);
            this.callback = callback;
            this.pathFollow = new PathFollowingObject(sim, this.gameObject, path,
                                                      startingVelocity, length, isFinalStep,
                                                      PathDone);

            // Make sure to update the velocity right away.
            timeSinceLastUpdate = TrafficSimulator.VelocityUpdateInterval;
            timeElapsed = 0f;
        }

        public void SetVelocity(float velocity)
        {
            if (pathFollow == null)
                return;

            pathFollow.velocity = velocity;
        }

        /// Total time elapsed while driving.
        public float timeElapsed;

        /// Elapsed time since the last update of the car's velocity.
        public float timeSinceLastUpdate;

        void UpdateVelocity()
        {
            SetVelocity(sim.trafficSim.GetCarVelocity(drivingCar));

            // Must be updated after the velocity calculation.
            timeElapsed += Time.fixedDeltaTime * sim.SpeedMultiplier;
            timeSinceLastUpdate = 0f;
        }

        void FixedUpdate()
        {
            if (!sim.game.Paused && pathFollow != null)
            {
                var elapsedTime = Time.fixedDeltaTime * sim.SpeedMultiplier;
                timeSinceLastUpdate += elapsedTime;

                if (timeSinceLastUpdate < TrafficSimulator.VelocityUpdateInterval)
                {
                    timeElapsed += elapsedTime;
                }
                else
                {
                    UpdateVelocity();
                }

                if (drivingCar.waitingForTrafficLight != null)
                {
                    if (drivingCar.waitingForTrafficLight.MustStop)
                    {
                        return;
                    }

                    drivingCar.waitingForTrafficLight = null;
                }

                pathFollow.FixedUpdate();

                drivingCar.exactPosition = transform.position;
                drivingCar.distanceFromStart = sim.trafficSim.GetDistanceFromStart(
                    drivingCar.segment,
                    drivingCar.exactPosition,
                    drivingCar.lane);
            }

            if (isFocused)
            {
                UpdateUIPosition();
            }
        }

        public void Highlight()
        {
            GetComponent<SpriteRenderer>().sprite = SpriteManager.instance.carSpritesOutlined[carModel];
        }

        public void Unhighlight()
        {
            GetComponent<SpriteRenderer>().sprite = SpriteManager.instance.carSprites[carModel];
        }

        void UpdateUIPosition()
        {
            var modal = GameController.instance.sim.citizienModal;
            modal.modal.PositionAt(transform.position);
        }

        void OnMouseEnter()
        {
            this.Highlight();
        }

        void OnMouseExit()
        {
            this.Unhighlight();
        }

        void OnMouseDown()
        {
            if (GameController.instance.input.IsPointerOverUIElement())
            {
                return;
            }

            var modal = GameController.instance.sim.citizienModal;
            modal.SetCitizien(this.driver);

            modal.modal.PositionAt(transform.position);
            modal.modal.Enable();
        }
    }
}