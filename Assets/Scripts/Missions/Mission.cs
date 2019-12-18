using UnityEngine;
using System;
using System.Collections.Generic;

namespace Transidious
{
    [Serializable]
    public class Mission
    {
        [Serializable]
        public struct CitizienData
        {
            public string firstName;
            public string lastName;
            public short? age;
            public short? birthday;
            public bool? female;
            public string occupation;
            public decimal? money;
            public Car car;
            public bool? educated;
            public byte? happiness;
        }

        [Serializable]
        public struct MissionStep
        {
            string message;
            Objective[] objectives;
        }

        /// <summary>
        /// The name of the mission.
        /// </summary>
        public string name;

        /// <summary>
        /// The area that should be loaded for the mission.
        /// </summary>
        public string area;

        /// <summary>
        /// The starting game time in the format MM/DD/YYYY HH:MM.
        /// </summary>
        public string gameTime;

        /// <summary>
        /// The amount of money the player starts with.
        /// </summary>
        public float startingMoney;

        /// <summary>
        /// The starting population.
        /// </summary>
        public int startingPopulation;

        /// <summary>
        /// Specific citiziens that should always be included in the mission.
        /// </summary>
        public CitizienData[] citiziens;

        /// <summary>
        /// The objectives required to complete the mission.
        /// </summary>
        public MissionStep[] steps;

        /// <summary>
        /// Load a mission via the file name.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Mission FromFile(string path)
        {
            var file = Resources.Load("Missions/" + path) as TextAsset;
            return JsonUtility.FromJson<Mission>(file.text);
        }

        /// <summary>
        /// Load the mission into the game.
        /// </summary>
        public void Load()
        {
            var game = GameController.instance;

            OSMImportHelper.Area area;
            Enum.TryParse(this.area, out area);

            if (!string.IsNullOrEmpty(gameTime))
            {
                game.sim.GameTime = DateTime.Parse(gameTime);
            }

            game.StartCoroutine(game.LoadMap(area));
            game.financeController.Money = (decimal)startingMoney;

            game.onLoad.AddListener(() =>
            {
                var numCitiziens = startingPopulation - (citiziens?.Length ?? 0);
                game.sim.SpawnRandomCitiziens(numCitiziens);

                if (citiziens != null)
                {
                    foreach (var c in citiziens)
                    {
                        Citizien.Occupation? occupation = null;
                        if (Enum.TryParse(c.occupation, out Citizien.Occupation o))
                        {
                            occupation = o;
                        }

                        game.sim.CreateCitizien(c.firstName, c.lastName, c.age, c.birthday, c.female,
                                                occupation, c.money, c.educated, c.happiness);
                    }
                }
            });
        }
    }
}