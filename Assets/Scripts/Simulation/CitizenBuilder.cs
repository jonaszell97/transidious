using System;
using Transidious.PathPlanning;
using UnityEngine;

using Occupation = Transidious.Citizen.Occupation;
using PointOfInterest = Transidious.Citizen.PointOfInterest;
using Relationship = Transidious.Citizen.Relationship;

namespace Transidious
{
    public class CitizenBuilder
    {
        /// Reference to the simulation controller.
        private static SimulationController _sim;

        /// Probability of a citizen going to university vs becoming a trainee.
        private static readonly float UniversityProbability = 0.7f;

        /// The citizen's first name.
        private string _firstName;

        /// The citizen's last name.
        private string _lastName;

        /// The citizen's age in years.
        private short? _age;

        /// The citizen's birthday in range [0..365].
        private short? _birthday;

        /// True iff this citizen is a woman.
        private bool? _female;

        /// The citizen's occupation.
        private Citizen.Occupation? _occupation;

        /// The citizen's current money.
        private decimal? _money;

        /// The citizen's current happiness [0..100].
        private float? _happiness;

        /// The citizen's current energy level [0..100].
        private float? _energy;

        /// The amount of work this citizen has to perform [0..100].
        private float? _remainingWork;

        /// Whether or not this citizen has a car.
        private bool? _hasCar;

        /// Initialize static fields.
        public static void Initialize()
        {
            _sim = GameController.instance.sim;
        }

        /// Create a new citizen builder.
        public static CitizenBuilder Create()
        {
            return new CitizenBuilder();
        }

        /// Set the citizen's first name.
        public CitizenBuilder WithFirstName(string firstName)
        {
            _firstName = firstName;
            return this;
        }
        
        /// Set the citizen's last name.
        public CitizenBuilder WithLastName(string lastName)
        {
            _lastName = lastName;
            return this;
        }

        /// Set the citizen's age.
        public CitizenBuilder WithAge(short age)
        {
            _age = age;
            return this;
        }
        
        /// Set the citizen's occupation.
        public CitizenBuilder WithOccupation(Citizen.Occupation occupation)
        {
            _occupation = occupation;
            return this;
        }
        
        /// Set the citizen's birthday.
        public CitizenBuilder WithBirthday(short birthday)
        {
            _birthday = birthday;
            return this;
        }

        /// Set the citizen's gender.
        public CitizenBuilder WithGender(bool female)
        {
            _female = female;
            return this;
        }
        
        /// Set the citizen's money.
        public CitizenBuilder WithMoney(decimal money)
        {
            _money = money;
            return this;
        }
        
        /// Set the citizen's happiness.
        public CitizenBuilder WithHappiness(float happiness)
        {
            _happiness = happiness;
            return this;
        }
        
        /// Set the citizen's energy.
        public CitizenBuilder WithEnergy(float energy)
        {
            _energy = energy;
            return this;
        }

        /// Set the citizen's remaining work.
        public CitizenBuilder WithRemainingWork(float remainingWork)
        {
            _remainingWork = remainingWork;
            return this;
        }

        /// Set whether or not the citizen has a car.
        public CitizenBuilder WithCar(bool hasCar)
        {
            _hasCar = hasCar;
            return this;
        }

        /// Finalize the citizen.
        public Citizen Build()
        {
            var c = new Citizen(_sim);
            if (!_female.HasValue || !_age.HasValue)
            {
                var genderAndAge = RandomNameGenerator.GenderAndAge;
                c.Female = _female ?? genderAndAge.Item1;
                c.Age = _age ?? (short) genderAndAge.Item2;
            }
            else
            {
                c.Female = _female.Value;
                c.Age = _age.Value;
            }

            c.LastName = _lastName ?? RandomNameGenerator.LastName;
            c.FirstName = _firstName ?? (c.Female ? RandomNameGenerator.FemaleFirstName : RandomNameGenerator.MaleFirstName);
            c.Birthday = _birthday ??  (short)RNG.Next((float) 0, 365);

            if (_occupation.HasValue)
            {
                c.occupation = _occupation.Value;
            }
            else
            {
                AssignOccupation(c);
            }

            AssignHome(c);
            AssignGroceryStore(c);
            AssignWorkplace(c);
            AssignIcon(c);

            if (_money.HasValue)
            {
                c.Money = _money.Value;
            }
            else
            {
                AssignMoney(c);
            }

            if (!_hasCar.HasValue)
            {
                _hasCar = c.Age >= 16 && RNG.Next(0, 10) <= 8;
            }

            if (_hasCar.Value)
            {
                c.Car = _sim.CreateCar(c, c.CurrentPosition, c.PreferredColor);
            }

            c.Happiness = _happiness ?? RNG.Next(70f, 100f);
            if (!_remainingWork.HasValue || !_energy.HasValue)
            {
                var currentHour = _sim.GameTime.Hour;
                if (c.occupation != Citizen.Occupation.Retired && c.occupation != Citizen.Occupation.Unemployed)
                {
                    if (currentHour < 8)
                    {
                        c.RemainingWork = 100f;
                        c.Energy = 100f - currentHour * (100f / 16f);
                    }
                    else if (currentHour < 16)
                    {
                        c.RemainingWork = (100f / 8f) * (16 - currentHour);
                        c.Energy = 100f - ((currentHour - 8) * (30f / 8f));
                    }
                    else
                    {
                        c.RemainingWork = 0f;
                        c.Energy = 100f - (30f / 8f);
                    }
                }
                else
                {
                    c.RemainingWork = 0f;
                    c.Energy = 100f;
                }
            }
            else
            {
                c.RemainingWork = _remainingWork.Value;
                c.Energy = _energy.Value;
            }

            CreateRandomPreferences(c);
            c.Schedule = new Simulation.Schedule(c, null);

            c.Initialize();
            return c;
        }

        /// Threshold ages.
        public static int ElementarySchoolThresholdAge => 7;
        public static int HighSchoolThresholdAge => 11;
        public static int UniversityThresholdAge => 18;
        public static int WorkerThresholdAge => 25;
        public static int RetirementThresholdAge => 67;

        /// Assign a random occupation.
        public static void AssignOccupation(Citizen c)
        {
            var age = c.Age;
            if (age < ElementarySchoolThresholdAge)
            {
                c.occupation = Occupation.Kindergardener;
            }
            else if (age < HighSchoolThresholdAge)
            {
                c.occupation = Occupation.ElementarySchoolStudent;
            }
            else if (age < UniversityThresholdAge)
            {
                c.occupation = Occupation.HighSchoolStudent;
            }
            else if (age < WorkerThresholdAge)
            {
                c.occupation = RNG.value < UniversityProbability ? Occupation.UniversityStudent : Occupation.Trainee;
            }
            else if (age < RetirementThresholdAge)
            {
                c.occupation = Occupation.Worker;
            }
            else
            {
                c.occupation = Occupation.Retired;
            }
        }

        /// The available occupation icons.
        private static readonly string[] OccupationIcons = {
            "athlete", "businessman", "callcenter", "detective", "doctor", "engineer", "farmer", "hunter", "judge",
            "pastor", "police", "scientist", "scientist2", "teacher",
        };

        /// Assign a random icon.
        static void AssignIcon(Citizen c)
        {
            string iconName;
            switch (c.occupation)
            {
                case Occupation.ElementarySchoolStudent:
                case Occupation.HighSchoolStudent:
                    iconName = c.Female ? "pupil_female" : "pupil_male";
                    break;
                case Occupation.UniversityStudent:
                    iconName = "student";
                    break;
                case Occupation.Retired:
                    iconName = c.Female ? "retiree_female" : "retiree_male";
                    break;
                case Occupation.Unemployed:
                    iconName = "generic2";
                    break;
                default:
                    iconName = RNG.RandomElement(OccupationIcons);
                    break;
            }

            c.Icon = SpriteManager.GetSprite($"Sprites/occupation_{iconName}");
        }

        /// Assign a random amount of starting money.
        private static void AssignMoney(Citizen c)
        {
            switch (c.occupation)
            {
                case Occupation.Worker:
                    c.Money = RNG.NextDecimal(1000, 10000);
                    break;
                case Occupation.Retired:
                    c.Money = RNG.NextDecimal(50, 2000);
                    break;
                case Occupation.Kindergardener:
                case Occupation.ElementarySchoolStudent:
                    c.Money = RNG.NextDecimal(0, 100);
                    break;
                case Occupation.HighSchoolStudent:
                    c.Money = RNG.NextDecimal(20, 100);
                    break;
                case Occupation.UniversityStudent:
                    c.Money = RNG.NextDecimal(100, 500);
                    break;
                case Occupation.Trainee:
                    c.Money = RNG.NextDecimal(1000, 5000);
                    break;
                case Occupation.Unemployed:
                    c.Money = RNG.NextDecimal(0, 50);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// Available workplaces.
        public static readonly Tuple<float, Building.Type>[] Workplaces = {
            Tuple.Create(.15f, Building.Type.Shop),
            Tuple.Create(.35f, Building.Type.Office),
            Tuple.Create(.25f, Building.Type.Industrial),
            Tuple.Create(.05f, Building.Type.Hospital),
            Tuple.Create(.05f, Building.Type.University),
            Tuple.Create(.01f, Building.Type.Kindergarden),
            Tuple.Create(.02f, Building.Type.ElementarySchool),
            Tuple.Create(.02f, Building.Type.HighSchool),
            Tuple.Create(.01f, Building.Type.Airport),
            Tuple.Create(.01f, Building.Type.Church),
            Tuple.Create(.01f, Building.Type.Hotel),
            Tuple.Create(.01f, Building.Type.Stadium),
            Tuple.Create(.01f, Building.Type.Sight),
        };

        /// Assign a random workplace.
        public static void AssignWorkplace(Citizen c)
        {
            Building.Type buildingType;
            PointOfInterest poiType;

            switch (c.occupation)
            {
                case Occupation.Retired:
                default:
                    return;
                case Occupation.Kindergardener:
                    buildingType = Building.Type.Kindergarden;
                    poiType = PointOfInterest.School;
                    break;
                case Occupation.ElementarySchoolStudent:
                    buildingType = Building.Type.ElementarySchool;
                    poiType = PointOfInterest.School;
                    break;
                case Occupation.HighSchoolStudent:
                    buildingType = Building.Type.HighSchool;
                    poiType = PointOfInterest.School;
                    break;
                case Occupation.UniversityStudent:
                    buildingType = Building.Type.University;
                    poiType = PointOfInterest.School;
                    break;
                case Occupation.Trainee:
                case Occupation.Worker:
                {
                    buildingType = Building.Type.Office;
                
                    var rnd = RNG.value;
                    var sum = 0f;

                    foreach (var el in Workplaces)
                    {
                        sum += el.Item1;
                        if (rnd <= sum)
                        {
                            buildingType = el.Item2;
                            break;
                        }
                    }

                    poiType = PointOfInterest.Work;
                    break;
                }
            }

            var place = _sim.ClosestUnoccupiedBuilding(buildingType, c.Home.centroid);
            if (place == null)
            {
                Debug.LogWarning($"could not find unoccupied {buildingType} building!");
                place = _sim.ClosestUnoccupiedBuilding(buildingType, c.Home.centroid, true);

                if (place == null)
                {
                    AssignWorkplace(c);
                    return;
                }
            }

            place.AddOccupant(OccupancyKind.Worker, c);
            c.PointsOfInterest.Add(poiType, place);
        }

        /// Assign a random home.
        private void AssignHome(Citizen c)
        {
            var home = _sim.RandomUnoccupiedBuilding(Building.Type.Residential);
            if (home == null)
            {
                Debug.LogError("could not find home!");
                return;
            }

            home.AddOccupant(OccupancyKind.Resident, c);
            c.PointsOfInterest.Add(Citizen.PointOfInterest.Home, home);

            c.CurrentPosition = home.Centroid;
        }

        /// Assign a random grocery store.
        private void AssignGroceryStore(Citizen c)
        {
            var groceryStore = _sim.ClosestUnoccupiedBuilding(Building.Type.GroceryStore, c.Home.Centroid);
            if (groceryStore != null)
            {
                c.PointsOfInterest.Add(PointOfInterest.GroceryStore, groceryStore);
            }
        }

        /// Create random transit preferences for a citizen.
        private void CreateRandomPreferences(Citizen c)
        {
            bool allowCar;
            float carTimeFactor;
            float changingPenalty;
            float waitingTimeFactor;
            float walkingTimeFactor;
            float maxWalkingDistance;

            if (c.Age < 10)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = RNG.Next(10f, 15f);
                waitingTimeFactor = RNG.Next(1.5f, 2f);
                walkingTimeFactor = RNG.Next(2f, 2.5f);
                maxWalkingDistance = 100f;
            }
            else if (c.Age < 18)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = RNG.Next(5f, 15f);
                waitingTimeFactor = RNG.Next(1f, 2f);
                walkingTimeFactor = RNG.Next(1f, 2.5f);
                maxWalkingDistance = 200f;
            }
            else if (c.Age < 40)
            {
                allowCar = RNG.value <= .6f;
                carTimeFactor = RNG.Next(.8f, 3f);
                changingPenalty = RNG.Next(3f, 15f);
                waitingTimeFactor = RNG.Next(2f, 3f);
                walkingTimeFactor = RNG.Next(.8f, 2f);
                maxWalkingDistance = 150f;
            }
            else if (c.Age < 65)
            {
                allowCar = RNG.value <= .75f;
                carTimeFactor = RNG.Next(.6f, 2.5f);
                changingPenalty = RNG.Next(5f, 15f);
                waitingTimeFactor = RNG.Next(1f, 2f);
                walkingTimeFactor = RNG.Next(2.5f, 3f);
                maxWalkingDistance = 100f;
            }
            else
            {
                allowCar = RNG.value <= .3f;
                carTimeFactor = RNG.Next(2f, 4f);
                changingPenalty = RNG.Next(2f, 5f);
                waitingTimeFactor = RNG.Next(1f, 1.5f);
                walkingTimeFactor = RNG.Next(3f, 8f);
                maxWalkingDistance = 50f;
            }

            if (allowCar && c.Car == null)
            {
                c.Car = _sim.CreateCar(c, c.CurrentPosition);
            }

            c.TransitPreferences = new PathPlanningOptions
            {
                citizen = c,
                allowCar = allowCar,
                carTimeFactor = carTimeFactor,
                changingPenalty = changingPenalty,
                waitingTimeFactor = waitingTimeFactor,
                walkingTimeFactor = walkingTimeFactor,
                maxWalkingDistance = maxWalkingDistance,
            };
        }
    }
}