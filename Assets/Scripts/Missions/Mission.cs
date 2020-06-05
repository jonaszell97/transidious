using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Transidious
{
    [Serializable]
    public class Mission
    {
        [Serializable]
        public struct CitizenData
        {
            public string firstName;
            public string lastName;
            public short? age;
            public short? birthday;
            public bool? female;
            public string occupation;
            public decimal? money;
            public bool? educated;
            public byte? happiness;
        }

        [Serializable]
        public struct MissionStep
        {
            public string message;
            public Objective[] objectives;
        }

        /// The name of the mission.
        public string name;

        /// The area that should be loaded for the mission.
        public string area;

        /// The starting game time in the format MM/DD/YYYY HH:MM.
        public string gameTime;

        /// The amount of money the player starts with.
        public float startingMoney;

        /// The starting population.
        public int startingPopulation;

        /// Specific citizens that should always be included in the mission.
        public CitizenData[] citizens;

        /// The objectives required to complete the mission.
        public MissionStep[] steps;

        /// Load a mission via the file name.
        public static Mission FromFile(string path)
        {
            var file = Resources.Load("Missions/" + path) as TextAsset;
            return JsonUtility.FromJson<Mission>(file.text);
        }

        /// Load the map for the mission.
        public Map LoadMap()
        {
            var game = GameController.instance;
            if (!string.IsNullOrEmpty(gameTime))
            {
                game.sim.GameTime = DateTime.Parse(gameTime);
            }

            game.financeController.Money = (decimal)startingMoney;
            
            return SaveManager.CreateMap(GameController.instance, area);
        }

        /// Load the mission into the game.
        public IEnumerator Load()
        {
            yield return SpawnCitizens();
        }

        IEnumerator SpawnCitizens()
        {
            var game = GameController.instance;
            
            var numCitizens = startingPopulation - (citizens?.Length ?? 0);
            yield return game.sim.SpawnRandomCitizensAsync(numCitizens, 100f);

            if (citizens == null) 
                yield break;

            foreach (var c in citizens)
            {
                Citizen.Occupation? occupation = null;
                if (Enum.TryParse(c.occupation, out Citizen.Occupation o))
                {
                    occupation = o;
                }

                var builder = CitizenBuilder.Create()
                    .WithFirstName(c.firstName)
                    .WithLastName(c.lastName);

                if (c.age.HasValue)
                    builder = builder.WithAge(c.age.Value);
                
                if (c.birthday.HasValue)
                    builder = builder.WithBirthday(c.birthday.Value);
                
                if (c.female.HasValue)
                    builder = builder.WithGender(c.female.Value);
                
                if (occupation.HasValue)
                    builder = builder.WithOccupation(occupation.Value);
                
                if (c.money.HasValue)
                    builder = builder.WithMoney(c.money.Value);
                
                if (c.happiness.HasValue)
                    builder = builder.WithHappiness(c.happiness.Value);

                _ = builder.Build();

                if (FrameTimer.instance.FrameDuration >= 100f)
                {
                    yield return null;
                }
            }
        }
    }
}