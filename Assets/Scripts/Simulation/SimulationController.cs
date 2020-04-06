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

        /// The transit vehicle prefab.
        public GameObject transitVehiclePrefab;

        /// The traffic simulator.
        public TrafficSimulator trafficSim;

        /// The building modal.
        public UIBuildingInfoModal buildingInfoModal;

        /// The natural feature modal.
        public UIFeatureInfoModal featureModal;

        /// The citizen modal.
        public UICitizenInfoModal citizenModal;

        /// The transit vehicle modal.
        public UITransitVehicleModal transitVehicleModal;

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

        public readonly int BaseSpeedMultiplier = 60;

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

        public static TimeSpan RealTimeToGameTime(TimeSpan ts)
        {
            return TimeSpan.FromSeconds(ts.TotalSeconds * 60);
        }

        public static TimeSpan GameTimeToRealTime(TimeSpan ts)
        {
            return TimeSpan.FromSeconds(ts.TotalSeconds / 60);
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
                gameTime = gameTime.AddSeconds(BaseSpeedMultiplier * Time.fixedDeltaTime);
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

            if (gameTime.Hour == 7 && gameTime.Minute == 0)
            {
                game.displayMode = MapDisplayMode.Day;
                game.input.FireEvent(InputEvent.DisplayModeChange);
            }
            else if (gameTime.Hour == 19 && gameTime.Minute == 0)
            {
                game.displayMode = MapDisplayMode.Night;
                game.input.FireEvent(InputEvent.DisplayModeChange);
            }

            var newDay = gameTime.DayOfYear;
            return prevDay != newDay;
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
            var car = new Car(this, driver, c.Color?.Deserialize() ?? Utility.RandomColor,
                              (int)c.CarModel, c.Id);

            if (c.ParkingLotID != 0)
            {
                car.parkingLot = game.loadedMap.GetMapObject((int)c.ParkingLotID);
            }
            
            cars.Add(car.id, car);
            return car;
        }

        public TransitVehicle CreateVehicle(Line line)
        {
            var obj = Instantiate(transitVehiclePrefab); ;
            obj.transform.SetParent(this.transform, false);

            if (line.stops.Count > 0)
            {
                var pos = line.stops.First().transform.position;
                obj.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.TransitStops, 1));
            }

            var vehicle = obj.GetComponent<TransitVehicle>();
            vehicle.Initialize(line);

            return vehicle;
        }

        public TransitVehicle CreateVehicle(Line line, Stop stop)
        {
            var obj = Instantiate(transitVehiclePrefab); ;
            obj.transform.SetParent(this.transform, false);

            var pos = stop.transform.position;
            obj.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.TransitStops, 1));

            var vehicle = obj.GetComponent<TransitVehicle>();
            vehicle.Initialize(line);
            vehicle.gameObject.SetActive(false);

            return vehicle;
        }

        public Building RandomBuilding()
        {
            var idx = UnityEngine.Random.Range(0, game.loadedMap.buildings.Count);
            return game.loadedMap.buildings[idx];
        }

        public Building RandomUnoccupiedBuilding(Building.Type type)
        {
            var map = SaveManager.loadedMap;
            var pos = new Vector2(UnityEngine.Random.Range(map.minX, map.maxX), 
                                  UnityEngine.Random.Range(map.minY, map.maxY));

            while (!map.IsPointOnMap(pos))
            {
                pos = new Vector2(UnityEngine.Random.Range(map.minX, map.maxX), 
                    UnityEngine.Random.Range(map.minY, map.maxY));
            }
            
            return ClosestUnoccupiedBuilding(type, pos);
        }

        public Building ClosestUnoccupiedBuilding(Building.Type type, Vector2 pos)
        {
            if (!game.loadedMap.buildingsByType.TryGetValue(type, out List<Building> buildings))
            {
                return null;
            }

            var minDistance = float.PositiveInfinity;
            Building closestBuilding = null;

            foreach (var building in buildings)
            {
                var distance = (building.centroid - pos).sqrMagnitude;
                if (distance < minDistance && building.occupants < building.capacity)
                {
                    minDistance = distance;
                    closestBuilding = building;
                }
            }

            return closestBuilding;
        }

        public Citizen CreateCitizen(bool init, bool hasCar = true)
        {
            var c = new Citizen(this);
            if (hasCar)
            {
                c.car = CreateCar(c, Vector3.zero);
            }

            c.AssignRandomHome();
            c.AssignRandomValues();

            if (init)
            {
                c.Initialize();
            }

            return c;
        }

        public Citizen CreateCitizen(string firstName = null,
                                     string lastName = null,
                                     short? age = null,
                                     short? birthday = null,
                                     bool? female = null,
                                     Citizen.Occupation? occupation = null,
                                     decimal? money = null,
                                     bool? educated = null,
                                     float? happiness = null,
                                     Car car = null)
        {
            var c = new Citizen(this, car);
            c.AssignRandomHome();
            c.AssignRandomValues(firstName, lastName, age, birthday, female, occupation,
                                 money, educated, happiness, car);

            c.Initialize();

            return c;
        }

        public Citizen CreateCitizen(Serialization.Citizen c)
        {
            var result = new Citizen(this, c);
            result.Initialize(c.ScheduleID);

            return result;
        }

        public Citizen GetCitizen(uint id)
        {
            return citizens[id];
        }

        public void SpawnRandomCitizens(int amount, List<Citizen> citizens = null)
        {
            for (int i = 0; i < amount; ++i)
            {
                var citizen = CreateCitizen(false);
                if (citizen.age >= 18 && UnityEngine.Random.value >= .1f)
                {
                    citizen.car = CreateCar(citizen, Vector3.zero, Utility.RandomColor);
                }

                citizen.Initialize();
                citizens?.Add(citizen);
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

                var citizen = CreateCitizen();
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
                var citizen = CreateCitizen();
                citizen.car = CreateCar(citizen, Vector3.zero, Utility.RandomColor);

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