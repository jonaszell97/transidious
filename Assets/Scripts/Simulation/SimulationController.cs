using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// The car prefab.
        public GameObject carPrefab;

        /// The traffic simulator.
        public TrafficSimulator trafficSim;

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

        void Update()
        {
            if (game.Paused)
            {
                return;
            }

            var minuteOfDay = this.MinuteOfDay;
            bool isNewDay = UpdateGameTime();

            foreach (var citizien in citiziens)
            {
                if (isNewDay)
                {
                    citizien.UpdateAge();
                    citizien.UpdateDailySchedule(minuteOfDay);
                }

                citizien.Update(minuteOfDay);
            }
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
                }
            }
        }

        bool UpdateGameTime()
        {
            float minutesPerFrame = 0.02f * SpeedMultiplier;
            var prevDay = gameTime.DayOfYear;

            gameTime = gameTime.AddMinutes(minutesPerFrame);
            game.gameTimeText.text = gameTime.ToShortDateString()
                + " " + gameTime.ToShortTimeString();

            var newDay = gameTime.DayOfYear;
            return prevDay != newDay;
        }

        public Car CreateCar(Citizien driver, Vector3 pos, Color c, int carModel = -1)
        {
            var obj = Instantiate(carPrefab);
            obj.transform.position = new Vector3(pos.x, pos.y, Map.Layer(MapLayer.Cars));
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
            int tries = 0;

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

        public Building ClosestUnoccupiedBuilding(Building.Type type, Vector3 pos)
        {
            if (!game.loadedMap.buildingsByType.TryGetValue(type, out List<Building> buildings))
            {
                return null;
            }

            var minDistance = float.PositiveInfinity;
            Building closestBuilding = null;

            foreach (var building in buildings)
            {
                var distance = (building.position - pos).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestBuilding = building;
                }
            }

            return closestBuilding;
        }

#if DEBUG
        public void SpawnTestCars(int amount = 100)
        {
            var options = new PathPlanning.PathPlanningOptions();
            for (int i = 0; i < amount; ++i)
            {
                var start = RandomBuilding();
                var goal = RandomBuilding();

                var planner = new PathPlanning.PathPlanner(options);
                var result = planner.FindClosestDrive(game.loadedMap, start.position,
                                                      goal.position);

                trafficSim.SpawnCar(result, new Citizien(this));
            }
        }

        public void SpawnTestCitiziens(int amount = 1000)
        {
            for (int i = 0; i < amount; ++i)
            {
                var citizien = new Citizien(this);
                Debug.Log(citizien.ToString());
                citiziens.Add(citizien);
            }
        }
#endif
        public void OnGUI()
        {
#if DEBUG
            if (GUI.Button(new Rect(25, 25, 100, 30), "Spawn Cars"))
            {
                SpawnTestCars();
            }
            if (GUI.Button(new Rect(250, 25, 100, 30), "Spawn Citiziens"))
            {
                SpawnTestCitiziens();
            }
#endif

            if (GUI.Button(new Rect(150, 25, 100, 30), "Spawn Car"))
            {
                game.input.debugRouteTest = true;
            }
            if (GUI.Button(new Rect(350, 25, 100, 30), "Save"))
            {
                SaveManager.SaveMapData(game.loadedMap);
                Debug.Log("file saved successfully");
            }
        }
    }
}