using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Transidious.Simulation;

namespace Transidious
{
    public class SimulationController : MonoBehaviour
    {
        public enum SimulationSpeed
        {
            Speed1 = 0,
            Speed2 = 1,
            Speed3 = 2,
            Speed4 = 3,
        }

        /// Reference to the game controller.
        public GameController game;

        /// The current in-game time.
        [SerializeField] DateTime gameTime;

        /// The simulation speed.
        public SimulationSpeed simulationSpeed;

        /// The citizens.
        public Dictionary<uint, Citizen> citizens;
        public List<Citizen> citizenList;
        
        /// List of cars.
        public Dictionary<uint, Car> cars;

        /// The number of citizens.
        public int totalCitizenCount;

        /// The trend of the citizen count.
        public int citizenCountTrend;

        /// The citizen count ui text.
        public TMPro.TMP_Text citizenCountText;

        /// The citizen trend arrow.
        public Image citizenCountTrendImg;

        /// The transform that contains all transit vehicles.
        public Transform transitVehicleContainer;

        /// The transit vehicle prefab.
        public GameObject transitVehiclePrefab;

        /// The traffic simulator.
        public TrafficSimulator trafficSim;

        /// Scratch buffer used for time string building.
        char[] timeStringBuffer;

        public class SimulationSettings
        {
            /// Max number of citizen updates per frame.
            public int maxCitizenUpdates = 150;
        }

        /// The simulation settings.
        public SimulationSettings settings;
        int citizenUpdateCnt = 0;
        TimeSpan timeSinceLastCompleteUpdate = TimeSpan.Zero;
        bool newDay = false;

#if UNITY_EDITOR
        [SerializeField] private string _gameTime = "01/01/2000"; 
#endif

        public delegate void TimedEvent();

        class TimedEventInfo
        {
            internal bool active;
            internal TimedEvent action;
            internal int schedule;
            internal int lastExecution;
        }

        int lastScheduledEvent = 0;
        Dictionary<int, TimedEventInfo> scheduledEvents;
        List<Tuple<DateTime, TimedEvent>> timedEvents;

        public DateTime GameTime
        {
            get => gameTime;

            set
            {
                gameTime = value;
                UpdateGameTimeString();
                game.mainUI.UpdateDate(gameTime);
            }
        }

        public int MinuteOfDay => gameTime.MinuteOfDay();

        void Awake()
        {
#if UNITY_EDITOR
            this.gameTime = DateTime.Parse(_gameTime);
#else
            this.gameTime = new DateTime(2000, 1, 1, 7, 0, 0);
#endif

            this.simulationSpeed = 0;
            this.citizens = new Dictionary<uint, Citizen>();
            this.citizenList = new List<Citizen>();
            this.cars = new Dictionary<uint, Car>();
            this.timeStringBuffer = new char[Translator.MaxTimeStringLength];
            this.scheduledEvents = new Dictionary<int, TimedEventInfo>();
            this.timedEvents = new List<Tuple<DateTime, TimedEvent>>();
            this.settings = new SimulationSettings();

            UpdateGameTimeString();
        }

        void Start()
        {
            CitizenBuilder.Initialize();
            EventManager.current.RegisterEventListener(this);
            UpdateCitizenUI();
        }

        void Update()
        {
            if (!game.Playing)
            {
                return;
            }

            var prevTime = gameTime;

            bool isNewDay = UpdateGameTime();
            if (isNewDay)
            {
                newDay = true;
                game.mainUI.UpdateDate(gameTime);
            }

            var threshold = System.Math.Min(citizenUpdateCnt + settings.maxCitizenUpdates, citizenList.Count);
            timeSinceLastCompleteUpdate += gameTime - prevTime;

            for (; citizenUpdateCnt < threshold; ++citizenUpdateCnt)
            {
                var citizen = citizenList[citizenUpdateCnt];
                citizen.Update(gameTime, newDay, timeSinceLastCompleteUpdate);
            }

            if (citizenUpdateCnt == citizenList.Count)
            {
                citizenUpdateCnt = 0;
                timeSinceLastCompleteUpdate = TimeSpan.Zero;
                newDay = false;
            }

            // if (gameTime.Minute % 10 == 0 && gameTime.Second == 0)
            // {
            //     foreach (var line in SaveManager.loadedMap.transitLines)
            //     {
            //         var maxDiff = 0f;
            //         var sum = 0f;
            //
            //         var first = true;
            //         foreach (var v in line.vehicles)
            //         {
            //             if (first || v.Velocity.MPS.Equals(0f))
            //             {
            //                 first = false;
            //                 sum += 20f;
            //                 continue;
            //             }
            //
            //             var dist = (float)v.DistanceToNext.TotalMinutes;
            //             sum += dist;
            //             maxDiff = Mathf.Max(maxDiff, Mathf.Abs(20f - dist));
            //         }
            //
            //         Debug.Log($"avg distance: {sum / line.vehicles.Count:n2} min, max diff: {maxDiff:n2} min");
            //     }
            // }
        }

        /// <summary>
        ///  Schedule a function to be executed every n seconds of game time.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="n"></param>
        public int ScheduleEvent(TimedEvent e, int n = 1, bool active = true)
        {
            var id = lastScheduledEvent++;
            var info = new TimedEventInfo()
            {
                active = active,
                action = e,
                schedule = n,
            };

            scheduledEvents.Add(id, info);
            return id;
        }

        public void ScheduleEvent(DateTime gameTime, TimedEvent callback)
        {
            Debug.Assert(gameTime > this.gameTime, "scheduled time is in the past");

#if DEBUG
            if ((gameTime.Millisecond % (Time.fixedDeltaTime * 1000)) > .01f)
            {
                Debug.LogWarning($"scheduling event at invalid millisecond {gameTime.Millisecond}");
            }
#endif

            timedEvents.Add(Tuple.Create(gameTime, callback));
            timedEvents.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
        }

        public float FixedUpdateInterval => Time.fixedDeltaTime * 1000 * BaseSpeedMultiplier;

        public DateTime RoundToNextFixedUpdate(DateTime gameTime)
        {
            var interval = FixedUpdateInterval;
            var millis = gameTime.Second * 1000f + gameTime.Millisecond;
            var missingMillis = interval - (millis % FixedUpdateInterval);

            return gameTime.AddMilliseconds(missingMillis);
        }

        public void DeactivateEvent(int id)
        {
            scheduledEvents[id].active = false;
        }

        public void ActivateEvent(int id)
        {
            scheduledEvents[id].active = true;
        }

        public static readonly int BaseSpeedMultiplier = 60;

        public int SpeedMultiplier
        {
            get
            {
                if (game.Paused)
                {
                    return 0;
                }

                switch (simulationSpeed)
                {
                default:
                case SimulationSpeed.Speed1:
                    return 1;
                case SimulationSpeed.Speed2:
                    return 4;
                case SimulationSpeed.Speed3:
                    return 10;
                case SimulationSpeed.Speed4:
                    return 100;
                }
            }
        }

        public void SetSimulationSpeed(SimulationSpeed simSpeed)
        {
            this.simulationSpeed = simSpeed;
            game.mainUI.simSpeedButton.GetComponent<Image>().sprite =
                SpriteManager.instance.simSpeedSprites[Mathf.Min((int)simSpeed, 2)];
        }
        
        void OnLanguageChange()
        {
            if (this.timeStringBuffer.Length != Translator.MaxTimeStringLength)
            {
                this.timeStringBuffer = new char[Translator.MaxTimeStringLength];
            }

            UpdateGameTimeString();
        }

        void FireScheduledEvents(int minute)
        {
            foreach (var info in scheduledEvents)
            {
                if (!info.Value.active)
                {
                    continue;
                }

                if (minute % info.Value.schedule == 0)
                {
                    info.Value.action();
                }
            }
        }

        void FireTimedEvents()
        {
            while (timedEvents.Count > 0)
            {
                var nextEvent = timedEvents.First();
                if (gameTime < nextEvent.Item1)
                {
                    break;
                }

#if DEBUG
                if (nextEvent.Item1 != gameTime)
                {
                    Debug.LogWarning($"scheduled time: {nextEvent.Item1.ToString("yyyyMMddhhmmss fff")} <-> real time {gameTime.ToString("yyyyMMddhhmmss fff")}");
                }
#endif

                nextEvent.Item2();
                timedEvents.RemoveAt(0);
            }
        }

        void UpdateGameTimeString()
        {
            Translator.FormatTime(gameTime, ref timeStringBuffer);
            game.mainUI.gameTimeText.SetCharArray(timeStringBuffer);
        }

        bool UpdateGameTime()
        {
            var prevDay = gameTime.DayOfYear;
            var prevMinute = gameTime.Minute;

            for (var i = 0; i < SpeedMultiplier; ++i)
            {
                gameTime = gameTime.AddSeconds(BaseSpeedMultiplier * Time.deltaTime);
                FireTimedEvents();

                if (gameTime.Minute == prevMinute)
                {
                    continue;
                }

                FireScheduledEvents(gameTime.Minute);
            }

            if (gameTime.Minute == prevMinute)
            {
                return false;
            }

            UpdateGameTimeString();
            UpdateCitizenUI();
            game.mainUI.UpdateDayNightOverlay(gameTime);

            return prevDay != gameTime.DayOfYear;
        }

        public void SetGameTime(DateTime time)
        {
            var prevDay = gameTime.DayOfYear;
            var prevYear = gameTime.Year;

            gameTime = time;

            FireTimedEvents();
            FireScheduledEvents(gameTime.Minute);
            UpdateGameTimeString();
            UpdateCitizenUI();

            var hour = gameTime.Hour;
            if (hour == 7)
            {
                game.displayMode = MapDisplayMode.Day;
                game.input.FireEvent(InputEvent.DisplayModeChange);
            }
            else if (hour == 19)
            {
                game.displayMode = MapDisplayMode.Night;
                game.input.FireEvent(InputEvent.DisplayModeChange);
            }

            if (prevDay != gameTime.DayOfYear || prevYear != gameTime.Year)
            {
                game.mainUI.UpdateDate(gameTime);
            }
        }

        void UpdateCitizenUI()
        {
            this.citizenCountText.text = Translator.GetNumber(totalCitizenCount);
            
            if (citizenCountTrend == 0)
            {
                this.citizenCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            }
            else if (citizenCountTrend < 0)
            {
                this.citizenCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                this.citizenCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        public Car CreateCar(Citizen driver, Vector3 pos, Color? c = null, int carModel = -1)
        {
            var car = new Car(this, driver, c, carModel);
            cars.Add(car.id, car);
            
            return car;
        }

        public Car CreateCar(Serialization.Car c)
        {
            var driver = citizens[c.DriverId];
            var car = new Car(this, driver, c.Color?.Deserialize() ?? RNG.RandomColor,
                              (int)c.CarModel, c.Id, (int)c.BehaviourID);

            if (c.ParkingLotID != 0)
            {
                car.parkingLot = game.loadedMap.GetMapObject((int)c.ParkingLotID);
            }
            
            cars.Add(car.id, car);
            return car;
        }

        public TransitVehicle CreateVehicle(Line line, TransitVehicle next)
        {
            var obj = Instantiate(transitVehiclePrefab, transitVehicleContainer, false);
            var vehicle = obj.GetComponent<TransitVehicle>();
            vehicle.Initialize(line, next);

            return vehicle;
        }

        public void DisableTransitVehicles()
        {
            transitVehicleContainer.gameObject.SetActive(false);
        }

        public void EnableTransitVehicles()
        {
            transitVehicleContainer.gameObject.SetActive(true);
        }

        public Building RandomBuilding()
        {
            return RNG.RandomElement(game.loadedMap.buildings);
        }

        public Building RandomUnoccupiedBuilding(Building.Type type)
        {
            var map = SaveManager.loadedMap;
            var pos = RNG.Vector2(map.minX, map.maxX, map.minY, map.maxY);

            while (!map.IsPointOnMap(pos))
            {
                pos = RNG.Vector2(map.minX, map.maxX, map.minY, map.maxY);
            }
            
            return ClosestUnoccupiedBuilding(type, pos);
        }

        public Building ClosestUnoccupiedBuilding(Building.Type type, Vector2 pos, bool ignoreOccupancy = false)
        {
            if (!game.loadedMap.buildingsByType.TryGetValue(type, out List<Building> buildings))
            {
                return null;
            }

            OccupancyKind occupancyKind;
            switch (type)
            {
                case Building.Type.Residential:
                    occupancyKind = OccupancyKind.Resident;
                    break;
                case Building.Type.Kindergarden:
                case Building.Type.ElementarySchool:
                case Building.Type.HighSchool:
                case Building.Type.University:
                    occupancyKind = OccupancyKind.Student;
                    break;
                default:
                    occupancyKind = OccupancyKind.Worker;
                    break;
            }

            var minDistance = float.PositiveInfinity;
            Building closestBuilding = null;

            foreach (var building in buildings)
            {
                var distance = (building.centroid - pos).sqrMagnitude;
                if (distance < minDistance && (ignoreOccupancy || building.HasCapacity(occupancyKind)))
                {
                    minDistance = distance;
                    closestBuilding = building;
                }
            }

            return closestBuilding;
        }

        public Citizen GetCitizen(uint id)
        {
            return citizens[id];
        }

        public void SpawnRandomCitizens(int amount, List<Citizen> citizens = null)
        {
            for (int i = 0; i < amount; ++i)
            {
                var citizen = CitizenBuilder.Create().Build();
                citizens?.Add(citizen);
            }
        }

        public IEnumerator SpawnRandomCitizensAsync(int amount, float thresholdTime)
        {
            for (int i = 0; i < amount; ++i)
            {
                _ = CitizenBuilder.Create().Build();
                if (FrameTimer.instance.FrameDuration >= thresholdTime)
                {
                    yield return null;
                }
            }
        }

#if DEBUG
        public IEnumerator SpawnTestCars(int amount = 100)
        {
            var options = new PathPlanning.PathPlanningOptions();
            for (int i = 0; i < amount; ++i)
            {
                var start = RandomBuilding();
                var goal = RandomBuilding();

                var planner = new PathPlanning.PathPlanner(options);
                var result = planner.FindClosestDrive(game.loadedMap, start.centroid,
                                                      goal.centroid);

                var citizen = CitizenBuilder.Create().WithCar(true).Build();
                citizen.FollowPath(result);

                if (FrameTimer.instance.FrameDuration >= 8)
                {
                    yield return null;
                }
            }

            yield break;
        }

        public IEnumerator SpawnTestCitizens(int amount = 1000)
        {
            for (int i = 0; i < amount; ++i)
            {
                var citizen = CitizenBuilder.Create().WithCar(true).Build();
                citizen.Car = CreateCar(citizen, Vector3.zero, RNG.RandomColor);

                if (FrameTimer.instance.FrameDuration >= 8)
                {
                    yield return null;
                }
            }

            yield break;
        }
#endif
    }
}