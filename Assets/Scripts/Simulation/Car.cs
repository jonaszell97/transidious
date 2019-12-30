using UnityEngine;
using System;
using System.Collections.Generic;

namespace Transidious
{
    public class Car : MonoBehaviour
    {
        static Tuple<float, Color>[] colorDistribution =
        {
            new Tuple<float, Color>(0.24f, Color.white),
            new Tuple<float, Color>(0.16f, new Color(.7f, .7f, .7f)),    // silver
            new Tuple<float, Color>(0.19f, Color.black),
            new Tuple<float, Color>(0.15f, Color.gray),
            new Tuple<float, Color>(0.10f, new Color(.57f, 0f, 0f)),     // red
            new Tuple<float, Color>(0.05f, new Color(.82f, .71f, .55f)), // tan
            new Tuple<float, Color>(0.03f, new Color(.082f, .305f, 0f)), // green
        };

        static uint lastAssignedID = 0;

        SimulationController sim;
        int carModel;

        public uint id;
        public Citizien driver;
        public float maxVelocity;
        public float acceleration;
        public float length;
        public PathFollowingObject pathFollow;
        public PathFollowingObject.CompletionCallback callback;
        public TrafficSimulator.DrivingCar drivingCar;

        new SpriteRenderer renderer;

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
                return renderer.color;
            }
            set
            {
                renderer.color = value;
            }
        }

        public Color RandomCarColor
        {
            get
            {
                var rnd = UnityEngine.Random.Range(0f, 1f);

                var i = 0;
                var sum = colorDistribution[i].Item1;

                while (rnd > sum)
                {
                    if (++i == colorDistribution.Length)
                    {
                        return Utility.RandomColor;
                    }

                    sum += colorDistribution[i].Item1;
                }

                return colorDistribution[i].Item2;
            }
        }

        public Bounds Bounds
        {
            get
            {
                return renderer.bounds;
            }
        }

        public void Initialize(SimulationController sim, Citizien driver,
                               Color? c = null, int carModel = -1, uint id = 0)
        {
            this.renderer = GetComponent<SpriteRenderer>();
            if (c != null)
            {
                renderer.color = c.Value;
            }
            else
            {
                renderer.color = RandomCarColor;
            }

            if (carModel == -1)
            {
                carModel = UnityEngine.Random.Range(0, SpriteManager.instance.carSprites.Length - 1);
            }

            this.carModel = carModel;
            renderer.sprite = SpriteManager.instance.carSpritesOutlined[carModel];

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

            if (id == 0)
            {
                this.id = ++lastAssignedID;
            }
            else
            {
                this.id = id;
                lastAssignedID = System.Math.Max(id, lastAssignedID);
            }

            this.sim = sim;
            this.driver = driver;
            driver.car = this;
            this.length = renderer.bounds.size.y;
            this.transform.SetLayer(MapLayer.Cars, 1);
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
                                                      PathDone, driver?.activePath);

            // Make sure to update the velocity right away.
            timeSinceLastUpdate = TrafficSimulator.VelocityUpdateInterval;
            // timeElapsed = 0f;
        }

        public void SetVelocity(float velocity)
        {
            if (pathFollow == null)
                return;

            pathFollow.velocity = velocity;
        }

        /// Total time elapsed while driving.
        // public float timeElapsed;

        /// Elapsed time since the last update of the car's velocity.
        public float timeSinceLastUpdate;

        void UpdateVelocity()
        {
            SetVelocity(sim.trafficSim.GetCarVelocity(drivingCar));

            // Must be updated after the velocity calculation.
            // timeElapsed += Time.fixedDeltaTime * sim.SpeedMultiplier;
            timeSinceLastUpdate = 0f;
        }

        void FixedUpdate()
        {
            if (!sim.game.Paused && pathFollow != null)
            {
                var elapsedTime = Time.fixedDeltaTime * sim.SpeedMultiplier;
                timeSinceLastUpdate += elapsedTime;

                if (timeSinceLastUpdate >= TrafficSimulator.VelocityUpdateInterval)
                {
                    UpdateVelocity();
                }
                //else
                //{
                //    timeElapsed += elapsedTime;
                //}

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

                driver.currentPosition = drivingCar.exactPosition;

                if (isFocused)
                {
                    UpdateUIPosition();
                }
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

        public void OnMouseDown()
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

        public Serialization.Car ToProtobuf()
        {
            return new Serialization.Car
            {
                Id = id,
                CarModel = (uint)carModel,
                DriverId = driver.id,
                Color = color.ToProtobuf(),
                Position = ((Vector2)transform.position).ToProtobuf(),
            };
        }
    }
}