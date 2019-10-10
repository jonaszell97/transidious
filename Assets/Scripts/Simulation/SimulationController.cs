using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Transidious
{
    public class SimulationController : MonoBehaviour
    {
        /// Reference to the game controller.
        public GameController game;

        /// The current in-game time.
        public DateTime gameTime;

        /// The simulation speed.
        public int simulationSpeed;

        /// The citiziens.
        public List<Citizien> citiziens;

        /// The number of citiziens.
        public int totalCitizienCount;

        /// The trend of the citizien count.
        public int citizienCountTrend;

        /// The citizien count ui text.
        public TMPro.TMP_Text citizienCountText;

        /// The citizien trend arrow.
        public Image citizienCountTrendImg;

        /// The car prefab.
        public GameObject carPrefab;

        /// The traffic simulator.
        public TrafficSimulator trafficSim;

        /// The building modal.
        public UIBuildingInfoModal buildingInfoModal;

        /// The natural feature modal.
        public UIFeatureInfoModal featureModal;

        /// The citizien modal.
        public UICitizienInfoModal citizienModal;

        /// Scratch buffer used for time string building.
        char[] timeStringBuffer;

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

        public int MinuteOfDay
        {
            get
            {
                return gameTime.Hour * 60 + gameTime.Minute;
            }
        }

        void Awake()
        {
            this.gameTime = new DateTime(2000, 1, 1, 7, 0, 0);
            this.simulationSpeed = 0;
            this.citiziens = new List<Citizien>();
        }

        void Start()
        {
            this.timeStringBuffer = new char[Translator.MaxTimeStringLength];
            this.scheduledEvents = new Dictionary<int, TimedEventInfo>();

            EventManager.current.RegisterEventListener(this);
            UpdateCitizienUI();
        }

        void Update()
        {
            if (game.Paused)
            {
                return;
            }

            var minuteOfDay = this.MinuteOfDay;
            bool isNewDay = UpdateGameTime();

            if (isNewDay)
            {
                game.mainUI.UpdateDate(gameTime);
            }

            foreach (var citizien in citiziens)
            {
                if (isNewDay)
                {
                    citizien.UpdateAge();
                    citizien.UpdateDailySchedule(minuteOfDay);
                }

                citizien.Update(minuteOfDay);
            }

#if DEBUG
            if (measuring)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (firstMeasurePos == null)
                    {
                        firstMeasurePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        return;
                    }
                    else
                    {
                        Vector2 otherPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Debug.Log("distance: " + (firstMeasurePos.Value - otherPos).magnitude);

                        measuring = false;
                        firstMeasurePos = null;
                    }
                }
            }
#endif
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

        public void DeactivateEvent(int id)
        {
            scheduledEvents[id].active = false;
        }

        public void ActivateEvent(int id)
        {
            scheduledEvents[id].active = true;
        }

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
                    return 1;
                case 1:
                    return 4;
                case 2:
                    return 10;

#if DEBUG
                case 3:
                    return 100;
#endif
                }
            }
        }

        void OnLanguageChange()
        {
            if (this.timeStringBuffer.Length != Translator.MaxTimeStringLength)
            {
                this.timeStringBuffer = new char[Translator.MaxTimeStringLength];
            }

            UpdateGameTimeString();
        }

        void FireTimedEvents(int minute)
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

        void UpdateGameTimeString()
        {
            Translator.FormatTime(gameTime, ref timeStringBuffer);
            game.mainUI.gameTimeText.SetCharArray(timeStringBuffer);
        }

        bool UpdateGameTime()
        {
            var minutesPerFrame = 0.02f * SpeedMultiplier;
            var prevDay = gameTime.DayOfYear;
            var prevMinute = gameTime.Minute;

            gameTime = gameTime.AddMinutes(minutesPerFrame);
            if (gameTime.Minute == prevMinute)
            {
                return false;
            }

            FireTimedEvents(gameTime.Minute);
            UpdateGameTimeString();
            UpdateCitizienUI();

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

            var newDay = gameTime.DayOfYear;
            return prevDay != newDay;
        }

        void UpdateCitizienUI()
        {
            this.citizienCountText.text = Translator.GetNumber(totalCitizienCount);
            
            if (citizienCountTrend == 0)
            {
                this.citizienCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            }
            else if (citizienCountTrend < 0)
            {
                this.citizienCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                this.citizienCountTrendImg.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        public Car CreateCar(Citizien driver, Vector3 pos, Color c, int carModel = -1)
        {
            var obj = Instantiate(carPrefab);
            obj.transform.position = new Vector3(pos.x, pos.y);
            obj.transform.SetParent(this.transform);

            var car = obj.GetComponent<Car>();
            car.Initialize(this, driver, c, carModel);

            return car;
        }

        public void DestroyCar(Car c)
        {
            Destroy(c.gameObject);
        }

        public Building RandomBuilding()
        {
            var idx = UnityEngine.Random.Range(0, game.loadedMap.buildings.Count);
            return game.loadedMap.buildings[idx];
        }

        public Building RandomUnoccupiedBuilding(Building.Type type)
        {
            if (!game.loadedMap.buildingsByType.TryGetValue(type, out List<Building> buildings))
            {
                return null;
            }

            var idx = UnityEngine.Random.Range(0, buildings.Count);
            var building = buildings[idx];
            var tries = 0;

            while (building.capacity - building.occupants == 0)
            {
                idx = UnityEngine.Random.Range(0, buildings.Count);
                building = buildings[idx];

                // FIXME this might be a slight hack
                if (tries++ == 100)
                {
                    ++building.capacity;
                    break;
                }
            }

            return building;
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
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestBuilding = building;
                }
            }

            return closestBuilding;
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

                trafficSim.SpawnCar(result, new Citizien(this));

                if (FrameTimer.instance.FrameDuration >= 8)
                {
                    yield return null;
                }
            }

            yield break;
        }

        public IEnumerator SpawnTestCitiziens(int amount = 1000)
        {
            for (int i = 0; i < amount; ++i)
            {
                var citizien = new Citizien(this);
                citizien.car = CreateCar(citizien, Vector3.zero, Utility.RandomColor);
                citiziens.Add(citizien);

                if (FrameTimer.instance.FrameDuration >= 8)
                {
                    yield return null;
                }
            }

            yield break;
        }

        bool measuring = false;
        Vector2? firstMeasurePos;

        public void OnGUI()
        {
            if (GUI.Button(new Rect(25, 25, 100, 30), "Spawn Cars"))
            {
                StartCoroutine(SpawnTestCars());
            }
            if (GUI.Button(new Rect(250, 25, 100, 30), "Spawn Citiziens"))
            {
                StartCoroutine(SpawnTestCitiziens());
            }
            if (GUI.Button(new Rect(150, 25, 100, 30), "Spawn Car"))
            {
                game.input.debugRouteTest = true;
            }
            if (GUI.Button(new Rect(350, 25, 100, 30), "Save"))
            {
                SaveManager.SaveMapData(game.loadedMap);
                Debug.Log("file saved successfully");
            }
            if (GUI.Button(new Rect(450, 25, 100, 30), "Reset"))
            {
                game.loadedMap.Reset();
            }
            if (GUI.Button(new Rect(550, 25, 100, 30), "SwitchLang"))
            {
                if (Translator.current.CurrentLanguageID == "en_US")
                {
                    GameController.instance.SetLanguage("de_DE");
                }
                else
                {
                    GameController.instance.SetLanguage("en_US");
                }
            }
            if (GUI.Button(new Rect(650, 25, 100, 30), "Measure"))
            {
                if (!measuring)
                {
                    measuring = true;
                }
            }
            if (GUI.Button(new Rect(25, 60, 100, 30), "MOPSGESCHWINDIGKEIT"))
            {
                this.simulationSpeed = 3;
            }
        }
#endif
    }
}