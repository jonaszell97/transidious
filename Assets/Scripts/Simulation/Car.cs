using UnityEngine;
using System.Collections.Generic;

namespace Transidious
{
    public class Car : MonoBehaviour
    {
        SimulationController sim;
        public Citizien driver;
        public float maxVelocity;
        public float acceleration;
        public float length;
        public PathFollowingObject pathFollow;
        public PathFollowingObject.CompletionCallback callback;
        public TrafficSimulator.DrivingCar drivingCar;
        public bool isFocused;

        public static Car focusedCar;

        public void Initialize(SimulationController sim, Citizien driver, Color c, int carModel = -1)
        {
            var renderer = GetComponent<SpriteRenderer>();
            renderer.color = c;

            if (carModel == -1)
            {
                carModel = UnityEngine.Random.Range(0, sim.game.carSprites.Length - 1);
            }

            renderer.sprite = sim.game.carSprites[carModel];

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
            this.length = renderer.bounds.size.y;

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
            // Debug.Log("velocity: " + velocity);
        }

        /// Total time elapsed while driving.
        public float timeElapsed;

        /// Elapsed time since the last update of the car's velocity.
        public float timeSinceLastUpdate;

        void UpdateVelocity()
        {
            SetVelocity(sim.trafficSim.GetCarVelocity(drivingCar));

            // Must be updated after the velocity calculation.
            timeElapsed += Time.deltaTime * sim.SpeedMultiplier;
            timeSinceLastUpdate = 0f;
        }

        void Update()
        {
            if (isFocused)
            {
                UpdateUIPosition();
            }

            if (!sim.game.Paused && pathFollow != null)
            {
                var elapsedTime = Time.deltaTime * sim.SpeedMultiplier;
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

                pathFollow.Update();

                var prevPos = drivingCar.exactPosition;
                drivingCar.exactPosition = transform.position;

                var prevDist = drivingCar.distanceFromStart;
                drivingCar.distanceFromStart = sim.trafficSim.GetDistanceFromStart(
                    drivingCar.segment,
                    drivingCar.exactPosition,
                    drivingCar.lane);
            }
        }

        void UpdateUIPosition()
        {
            var ui = sim.game.citizienUI;
            var rectTransform = ui.GetComponent<RectTransform>();

            var pos = sim.game.input.WorldToUISpace(sim.game.uiCanvas, this.transform.position);
            rectTransform.position = new Vector3(pos.x, pos.y, ui.transform.position.z);
        }

        void OnMouseDown()
        {
            var game = sim.game;
            var ui = game.citizienUI;
            isFocused = !isFocused;

            if (isFocused)
            {
                if (focusedCar != null)
                {
                    focusedCar.isFocused = false;
                }

                focusedCar = this;
                ui.SetActive(true);

                game.citizienUINameText.text = driver.Name + " (" + driver.age + ")";
                game.citizienUIMoneyText.text = Translator.GetCurrency(driver.money);

                var destination = driver.CurrentDestination;
                if (destination.HasValue)
                {
                    var msg = Citizien.GetDestinationString(destination.Value);
                    game.citizienUIDestinationImg.gameObject.SetActive(true);
                    game.citizienUIDestinationText.gameObject.SetActive(true);
                    game.citizienUIDestinationText.text = msg;
                }
                else
                {
                    game.citizienUIDestinationImg.gameObject.SetActive(false);
                    game.citizienUIDestinationText.gameObject.SetActive(false);
                }

                if (driver.happiness < 50)
                {
                    game.citizienUIHappinessSprite.sprite = game.happinessSprites[0];
                    game.citizienUIHappinessText.text = Translator.Get("ui:happiness_unhappy");
                }
                else if (driver.happiness < 80)
                {
                    game.citizienUIHappinessSprite.sprite = game.happinessSprites[1];
                    game.citizienUIHappinessText.text = Translator.Get("ui:happiness_med_happy");
                }
                else
                {
                    game.citizienUIHappinessSprite.sprite = game.happinessSprites[2];
                    game.citizienUIHappinessText.text = Translator.Get("ui:happiness_happy");
                }

                UpdateUIPosition();
            }
            else
            {
                ui.SetActive(false);
                focusedCar = null;
            }
        }
    }
}