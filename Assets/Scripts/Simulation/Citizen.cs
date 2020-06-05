using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;

namespace Transidious
{
    public class Citizen : IComparable
    {
        public struct HappinessInfluence
        {
            /// The impact of this item per second.
            public float influence;

            /// The amount of time this influence lasts.
            public TimeSpan? duration;

            /// The relative cap of this item (i.e. it is not applied if the
            /// happiness is below / above this cap).
            public float relativeCap;

            /// The absolute cap this item applies to the happiness value.
            public float absoluteCapLo;
            public float absoluteCapHi;

            public HappinessInfluence(float influence, TimeSpan? duration,
                                      float relativeCap = -1f,
                                      float absoluteCapLo = 0f,
                                      float absoluteCapHi = 100f)
            {
                this.influence = influence;
                this.duration = duration;

                if (relativeCap < 0f || relativeCap > 100f)
                {
                    relativeCap = influence < 0f ? 0f : 100f;
                }

                this.relativeCap = relativeCap;
                this.absoluteCapLo = absoluteCapLo;
                this.absoluteCapHi = absoluteCapHi;
            }
        }

        public enum Occupation
        {
            Worker,
            Retired,
            Kindergardener,
            ElementarySchoolStudent,
            HighSchoolStudent,
            UniversityStudent,
            Trainee,
            Unemployed,
        }

        public enum PointOfInterest
        {
            Home,
            Work,
            School,
            GroceryStore,
            Gym,
        }

        public enum Relationship
        {
            SignificantOther,
        }

        /// The last assigned citizen ID.
        private static uint _lastAssignedId = 0;

        /// Reference to the simulation controller.
        public readonly SimulationController sim;
        
        /// The unique ID of this citizen.
        public readonly uint id;
        
        /// The citizen's first name.
        public string FirstName { get; set; }
        
        /// The citizen's last name.
        public string LastName { get; set; }
        
        /// The citizen's age in years.
        public short Age { get; set; }

        /// The citizen's birthday in range [0..365].
        public short Birthday { get; set; }
        
        /// True iff this citizen is a woman.
        public bool Female { get; set; }
        
        /// The citizen's occupation.
        public Occupation occupation { get; set; }
        
        /// The citizen's current money.
        public decimal Money { get; set; }

        /// The citizen's car (can be null).
        public Car Car;

        /// The citizen's icon.
        public Sprite Icon;

        /// True iff this citizen has an education.
        public bool Educated { get; set; }

        /// The citizen's current happiness [0..100].
        public float Happiness { get; set; }

        /// The citizen's current energy level [0..100].
        public float Energy { get; set; }

        /// The amount of work this citizen has to perform [0..100].
        public float RemainingWork { get; set; }

        /// The color to use for rendering this citizen.
        public readonly Color PreferredColor;

        /// Map of the citizen's relationships.
        public readonly Dictionary<Relationship, Citizen> Relationships;
        
        /// Map of the citizen's points of interest.
        public readonly Dictionary<PointOfInterest, IMapObject> PointsOfInterest;

        /// Options to use for path planning.
        public PathPlanningOptions TransitPreferences;

        /// Current position on the map.
        public Vector2 CurrentPosition;

        /// The citizen's schedule.
        public Simulation.Schedule Schedule;
        
        /// The citizen's next scheduled event.
        public Simulation.Schedule.EventInfo CurrentEvent;

        /// The path that the citizen is currently following (can be null).
        public ActivePath ActivePath;

        /// Regular influences on the citizen's happiness.
        public readonly Dictionary<string, HappinessInfluence> HappinessInfluences;

        /// The last happiness level where an animation was displayed.
        private float _lastHappinessAnimation = -1f; 

        /// Probability of a citizen going to university vs becoming a trainee.
        public static readonly float UniversityProbability = 0.7f;

        public Citizen(SimulationController sim, Car car = null, uint id = 0)
        {
            this.sim = sim;
            this.sim.totalCitizenCount++;

            if (id == 0)
            {
                this.id = ++_lastAssignedId;
            }
            else
            {
                this.id = id;
                _lastAssignedId = System.Math.Max(id, _lastAssignedId);
            }

            this.sim.citizens.Add(this.id, this);
            this.sim.citizenList.Add(this);
            this.Car = car;
            this.PreferredColor = RNG.RandomColor;

            this.PointsOfInterest = new Dictionary<PointOfInterest, IMapObject>();
            this.Relationships = new Dictionary<Relationship, Citizen>();
            this.HappinessInfluences = new Dictionary<string, HappinessInfluence>();
        }

        public Citizen(SimulationController sim, Serialization.Citizen c)
        {
            this.sim = sim;
            this.id = c.Id;

            this.sim.totalCitizenCount++;
            this.sim.citizens.Add(this.id, this);
            this.sim.citizenList.Add(this);

            this.FirstName = c.FirstName;
            this.LastName = c.LastName;
            this.Age = (short)c.Age;
            this.Female = c.Female;
            this.Birthday = (short)c.Birthday;
            this.occupation = (Occupation)c.Occupation;
            this.Money = (decimal)c.Money;
            this.Happiness = (float)c.Happiness;
            this.Educated = c.Educated;

            this.PointsOfInterest = new Dictionary<PointOfInterest, IMapObject>();
            this.Relationships = new Dictionary<Relationship, Citizen>();
            this.HappinessInfluences = new Dictionary<string, HappinessInfluence>();
            this.CurrentPosition = c.CurrentPosition.Deserialize();

            var map = GameController.instance.loadedMap;
            foreach (var poi in c.PointsOfInterest)
            {
                var kind = (PointOfInterest)poi.Kind;
                var building = map.GetMapObject<Building>((int)poi.BuildingId);
                this.PointsOfInterest.Add(kind, building);
            }

            foreach (var inf in c.HappinessInfluences)
            {
                HappinessInfluences.Add(inf.DescriptionKey, new HappinessInfluence
                {
                    absoluteCapHi = inf.AbsoluteCapHi,
                    absoluteCapLo = inf.AbsoluteCapLo,
                    influence = inf.Influence,
                    relativeCap = inf.RelativeCap,
                    duration = TimeSpan.FromTicks(inf.Ticks),
                });
            }

            this.PreferredColor = c.PreferredColor.Deserialize();
            TransitPreferences = PathPlanningOptions.Deserialize(c.TransitPreferences);
        }

        public PathPlanning.PathPlanningOptions CreateRandomPreferences()
        {
            bool allowCar;
            float carTimeFactor;
            float changingPenalty;
            float waitingTimeFactor;
            float walkingTimeFactor;
            float maxWalkingDistance;

            if (Age < 10)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = RNG.Next(10f, 15f);
                waitingTimeFactor = RNG.Next(1.5f, 2f);
                walkingTimeFactor = RNG.Next(2f, 2.5f);
                maxWalkingDistance = 100f;
            }
            else if (Age < 18)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = RNG.Next(5f, 15f);
                waitingTimeFactor = RNG.Next(1f, 2f);
                walkingTimeFactor = RNG.Next(1f, 2.5f);
                maxWalkingDistance = 200f;
            }
            else if (Age < 40)
            {
                allowCar = RNG.value <= .6f;
                carTimeFactor = RNG.Next(.8f, 3f);
                changingPenalty = RNG.Next(3f, 15f);
                waitingTimeFactor = RNG.Next(2f, 3f);
                walkingTimeFactor = RNG.Next(.8f, 2f);
                maxWalkingDistance = 150f;
            }
            else if (Age < 65)
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

            if (allowCar && Car == null)
            {
                this.Car = sim.CreateCar(this, CurrentPosition);
            }

            return new PathPlanningOptions
            {
                citizen = this,
                allowCar = allowCar,
                carTimeFactor = carTimeFactor,
                changingPenalty = changingPenalty,
                waitingTimeFactor = waitingTimeFactor,
                walkingTimeFactor = walkingTimeFactor,
                maxWalkingDistance = maxWalkingDistance,
            };
        }

        public void Finalize(Serialization.Citizen c)
        {
            var map = GameController.instance.loadedMap;

            foreach (var rel in c.Relationships)
            {
                this.Relationships.Add((Relationship)rel.Kind, sim.citizens[rel.CitizenId]);
            }
        }

        public void AddHappinessInfluence(string key, float influence,
                                          TimeSpan? duration = null,
                                          float relativeCap = -1f,
                                          float absoluteCapLo = 0f,
                                          float absoluteCapHi = 100f)
        {
            this.HappinessInfluences.Add(key, new HappinessInfluence(influence,
                duration, relativeCap, absoluteCapLo, absoluteCapHi));
        }

        public void SetHappiness(float newHappiness)
        {
            Happiness = Mathf.Clamp(newHappiness, 0f, 100f);

            if (_lastHappinessAnimation < 0f)
            {
                _lastHappinessAnimation = Happiness;
                return;
            }

            var diff = Happiness - _lastHappinessAnimation;
            if (Mathf.Abs(diff) >= 1f)
            {
                DisplayHappinessChangeAnimation(diff);
                _lastHappinessAnimation = Happiness;
            }
        }

        public void SetEnergy(float newEnergy)
        {
            this.Energy = Mathf.Clamp(newEnergy, 0f, 100f);

            var tired = HappinessInfluences.ContainsKey("Tired");
            if (Energy < 0f && !tired)
            {
                // .5% per hour
                AddHappinessInfluence("Tired", -(.5f / (24f * 60f)), null, 
                                      -1f, 0f, 70f);
            }
            else if (Energy > 0f && tired)
            {
                HappinessInfluences.Remove("Tired");
            }
        }

        public void SetRemainingWork(float newRemainingWork)
        {
            if (occupation == Occupation.Unemployed || occupation == Occupation.Retired)
                return;

            this.RemainingWork = Mathf.Clamp(newRemainingWork, 0f, 100f);
        }

        void DisplayHappinessChangeAnimation(float diff)
        {
            var sprite = ResourceManager.instance.GetTemporarySprite();
            if (sprite == null)
            {
                return;
            }

            var sr = sprite.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteManager.GetSprite("Sprites/arrow");
            sr.color = Colors.GetColor(diff < 0f ? "ui.happinessLow" : "ui.happinessHigh");
            sr.transform.localScale = new Vector3(.3f, .3f, 1f);

            var pos = CurrentPosition.WithZ(Map.Layer(MapLayer.Foreground));
            var anim = sr.gameObject.GetComponent<TransformAnimator>();
            anim.Initialize();
            anim.SetAnimationType(TransformAnimator.AnimationType.Loop, TransformAnimator.ExecutionMode.Manual);
            anim.SetTargetPosition(pos + (Vector3.up * 3f), pos);
            anim.onFinish = () => ResourceManager.instance.Reclaim(sr);
            
            anim.StartAnimation(.7f);
        }

        public void AssignRandomValues(string firstName = null,
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
            if (lastName == null)
            {
                this.LastName = RandomNameGenerator.LastName;
            }
            else
            {
                this.LastName = lastName;
            }

            var genderAndAge = RandomNameGenerator.GenderAndAge;
            if (!female.HasValue)
            {
                this.Female = genderAndAge.Item1;
            }
            else
            {
                this.Female = female.Value;
            }
            
            if (!age.HasValue)
            {
                this.Age = (short)genderAndAge.Item2;
            }
            else
            {
                this.Age = age.Value;
            }
            
            if (firstName == null)
            {
                this.FirstName = this.Female ? RandomNameGenerator.FemaleFirstName
                                         : RandomNameGenerator.MaleFirstName;
            }
            else
            {
                this.FirstName = firstName;
            }
            
            if (!birthday.HasValue)
            {
                this.Birthday = (short)RNG.Next((float) 0, 365);
            }
            else
            {
                this.Birthday = birthday.Value;
            }

            if (!happiness.HasValue)
            {
                this.Happiness = RNG.Next(70f, 100f);
            }
            else
            {
                this.Happiness = happiness.Value;
            }

            if (!money.HasValue)
            {
                this.Money = (decimal)RNG.Next(0f, 1000f);
            }
            else
            {
                this.Money = money.Value;
            }

            if (this.Money < 200m)
            {
                // 1% per day
                AddHappinessInfluence("Poorness", -(1f / (24f * 60f * 60f)), null, 
                                      -1f, 0f, 70f);
            }

            TransitPreferences = CreateRandomPreferences();
            AssignOccupation(occupation);

            var currentHour = sim.GameTime.Hour;
            if (this.occupation != Occupation.Retired && this.occupation != Occupation.Unemployed)
            {
                if (currentHour < 8)
                {
                    RemainingWork = 100f;
                    Energy = 100f - currentHour * (100f / 16f);
                }
                else if (currentHour < 16)
                {
                    RemainingWork = (100f / 8f) * (16 - currentHour);
                    Energy = 100f - ((currentHour - 8) * (30f / 8f));
                }
                else
                {
                    RemainingWork = 0f;
                    Energy = 100f - (30f / 8f);
                }
            }
            else
            {
                RemainingWork = 0f;
                Energy = 100f;
            }
            
            this.Schedule = new Simulation.Schedule(this, null);
            // if (this.occupation == Occupation.Kindergardener || this.occupation == Occupation.Retired)
            // {
            //     this.schedule = new Simulation.Schedule(this, new Simulation.Schedule.FixedEvent[] {});
            // }
            // else
            // {
            //     this.schedule = new Simulation.Schedule(this, new[]
            //     {
            //         new Simulation.Schedule.FixedEvent
            //         {
            //             startingTime = 8 * 60,
            //             duration = 8 * 60,
            //             mustBePerformedFully = true,
            //             type = Simulation.Schedule.EventType.Work,
            //             weekdays = Weekday.Weekdays,
            //         },
            //     });
            // }
        }

        public void AssignRandomHome()
        {
            var home = sim.RandomUnoccupiedBuilding(Building.Type.Residential);
            if (home == null)
            {
                Debug.LogError("could not find home!");
                return;
            }

            home.AddOccupant(OccupancyKind.Resident, this);
            this.PointsOfInterest.Add(PointOfInterest.Home, home);
            
            CurrentPosition = home.Centroid;
        }

        public void Initialize()
        {
            this.CurrentPosition = Home.centroid;
            UpdateDailySchedule(sim.GameTime, false);

            sim.game.financeController.taxes.amount += GetTaxes();
        }

        bool IsThresholdAge(int age)
        {
            switch (age)
            {
            case 7:
            case 11:
            case 18:
            case 25:
            case 67:
                return true;
            default:
                return false;
            }
        }

        void AssignOccupation(Occupation? occupation = null)
        {
            if (occupation.HasValue)
            {
                this.occupation = occupation.Value;
            }
            else if (Age < 7)
            {
                this.occupation = Occupation.Kindergardener;
            }
            else if (Age < 11)
            {
                this.occupation = Occupation.ElementarySchoolStudent;
            }
            else if (Age < 18)
            {
                this.occupation = Occupation.HighSchoolStudent;
            }
            else if (Age < 25)
            {
                if (RNG.value < UniversityProbability)
                {
                    this.occupation = Occupation.UniversityStudent;
                }
                else
                {
                    this.occupation = Occupation.Trainee;
                }
            }
            else if (Age < 67)
            {
                this.occupation = Occupation.Worker;
            }
            else
            {
                this.occupation = Occupation.Retired;
            }

            AssignWorkplace();
            AssignIcon();
        }

        private static Tuple<float, Building.Type>[] _workplaces = new[]
        {
            Tuple.Create(.15f, Building.Type.Shop),
            Tuple.Create(.40f, Building.Type.Office),
            Tuple.Create(.60f, Building.Type.Industrial),
            Tuple.Create(.65f, Building.Type.Hospital),
            Tuple.Create(.70f, Building.Type.Airport),
            Tuple.Create(.75f, Building.Type.Church),
            Tuple.Create(.80f, Building.Type.Hotel),
            Tuple.Create(.85f, Building.Type.Stadium),
            Tuple.Create(.90f, Building.Type.Sight),
            Tuple.Create(.95f, Building.Type.University),
            Tuple.Create(1.0f, Building.Type.HighSchool),
        };
        
        void AssignWorkplace()
        {
            Building.Type buildingType;
            PointOfInterest poiType;

            switch (occupation)
            {
            case Occupation.Retired:
            default:
                return;
            case Occupation.Kindergardener:
            case Occupation.ElementarySchoolStudent:
                // FIXME
                buildingType = Building.Type.HighSchool;
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
                foreach (var el in _workplaces)
                {
                    if (rnd <= el.Item1)
                    {
                        buildingType = el.Item2;
                        break;
                    }
                }

                poiType = PointOfInterest.Work;
                break;
            }
            }

            var place = sim.ClosestUnoccupiedBuilding(buildingType, Home.centroid);
            if (place == null)
            {
                Debug.LogWarning($"could not find unoccupied {buildingType} building!");
                place = sim.ClosestUnoccupiedBuilding(buildingType, Home.centroid, true);

                if (place == null)
                {
                    AssignWorkplace();
                    return;
                }
            }

            place.AddOccupant(OccupancyKind.Worker, this);
            PointsOfInterest.Add(poiType, place);
        }

        private static readonly string[] _occupationIcons = new[]
        {
            "athlete", "businessman", "callcenter", "detective", "doctor", "engineer", "farmer", "hunter", "judge",
            "pastor", "police", "scientist", "scientist2", "teacher",
        };

        void AssignIcon()
        {
            string iconName;
            switch (occupation)
            {
                case Occupation.ElementarySchoolStudent:
                case Occupation.HighSchoolStudent:
                    iconName = Female ? "pupil_female" : "pupil_male";
                    break;
                case Occupation.UniversityStudent:
                    iconName = "student";
                    break;
                case Occupation.Retired:
                    iconName = Female ? "retiree_female" : "retiree_male";
                    break;
                case Occupation.Unemployed:
                    iconName = "generic2";
                    break;
                default:
                    iconName = RNG.RandomElement(_occupationIcons);
                    break;
            }

            this.Icon = SpriteManager.GetSprite($"Sprites/occupation_{iconName}");
        }

        public IMapObject GetPointOfInterest(params Citizen.PointOfInterest[] options)
        {
            foreach (var poi in PointsOfInterest)
            {
                if (options.Contains(poi.Key))
                {
                    return poi.Value;
                }
            }

            return null;
        }

        public IMapObject GetRandomPointOfInterest(params Citizen.PointOfInterest[] options)
        {
            var possibilities = new List<IMapObject>();
            foreach (var poi in PointsOfInterest)
            {
                if (options.Contains(poi.Key))
                {
                    possibilities.Add(poi.Value);
                }
            }

            return RNG.RandomElement(possibilities);
        }

        public void UpdateAge()
        {
            if (sim.GameTime.DayOfYear != Birthday)
            {
                return;
            }

            ++Age;

            if (IsThresholdAge(Age))
            {
                AssignOccupation();
                UpdateDailySchedule(sim.GameTime, true);
            }
        }
        
        public decimal GetTaxes()
        {
            switch (occupation)
            {
                case Citizen.Occupation.Worker:
                    return 2m;
                case Citizen.Occupation.UniversityStudent:
                case Citizen.Occupation.Trainee:
                    return 1m;
                case Citizen.Occupation.Retired:
                case Citizen.Occupation.Kindergardener:
                case Citizen.Occupation.ElementarySchoolStudent:
                case Citizen.Occupation.HighSchoolStudent:
                default:
                    return 0m;
            }
        }

        public void UpdateDailySchedule(DateTime currentTime, bool newDay)
        {
            CurrentEvent.location?.RemoveOccupant(OccupancyKind.Visitor, this);
            CurrentEvent = Schedule.GetNextEvent(currentTime, newDay);

            if (CurrentEvent.path != null)
            {
                ActivePath = FollowPath(CurrentEvent.path);
                if (CurrentEvent.location != null)
                {
                    ActivePath.onDone = () =>
                    {
                        CurrentEvent.location.AddOccupant(OccupancyKind.Visitor, this);

                        var modal = MainUI.instance.citizenModal;
                        if (modal.citizen == this)
                        {
                            modal.UpdateAll();
                        }
                    };
                }
            }
        }

        public void UpdateHappiness(TimeSpan timeSinceLastUpdate, float bonus)
        {
            var totalCapLo = 0f;
            var totalCapHi = 100f;
            var newHappiness = Happiness;
            var passedSeconds = (float) timeSinceLastUpdate.TotalSeconds;

            foreach (var (key, item) in HappinessInfluences)
            {
                if (item.influence < 0f && newHappiness <= item.relativeCap)
                {
                    continue;
                }
                if (item.influence > 0f && newHappiness >= item.relativeCap)
                {
                    continue;
                }

                newHappiness += item.influence * passedSeconds;
                totalCapLo = Mathf.Max(totalCapLo, item.absoluteCapLo);
                totalCapHi = Mathf.Min(totalCapHi, item.absoluteCapHi);
            }

            newHappiness += (passedSeconds / 60f) * bonus;
            SetHappiness(Mathf.Clamp(newHappiness, totalCapLo, totalCapHi));
        }

        public void Update(DateTime currentTime, bool newDay, TimeSpan timeSinceLastUpdate)
        {
            if (newDay)
            {
                UpdateAge();
            }

            var happinessBonus = 0f;
            var passedHours = (float) timeSinceLastUpdate.TotalHours;

            if (ActivePath == null)
            {
                happinessBonus = CurrentEvent.HappinessBonusPerHour;
            }

            if (HappinessInfluences.Count > 0)
            {
                UpdateHappiness(timeSinceLastUpdate, happinessBonus);
            }
            else if (happinessBonus > 0f)
            {
                SetHappiness(Happiness + passedHours * happinessBonus);
            }

            float energyBonus;
            float remainingWorkBonus;

            if (ActivePath == null)
            {
                energyBonus = CurrentEvent.EnergyBonusPerHour;
                remainingWorkBonus = CurrentEvent.RemainingWorkBonusPerHour;
            }
            else
            {
                energyBonus = ActivePath.EnergyBonusPerHour;
                remainingWorkBonus = ActivePath.RemainingWorkBonusPerHour;
            }

            SetEnergy(Energy + energyBonus * passedHours);
            SetRemainingWork(RemainingWork + remainingWorkBonus * passedHours);

            if (currentTime >= CurrentEvent.endTime)
            {
                if (ActivePath != null)
                {
                    // Check if we can safely abort the current path.
                    if (!ActivePath.Abortable)
                    {
                        return;
                    }

                    // We completely missed the previous event, apply a penalty.
                    SetHappiness(Happiness - CurrentEvent.PenaltyForMissing);
                    ActivePath.Abort(false);
                }

                UpdateDailySchedule(currentTime, newDay);
            }
            else if (MainUI.instance.citizenModal.citizen == this)
            {
                MainUI.instance.citizenModal.UpdateFrequentChanges();
            }
        }

        public ActivePath FollowPath(PathPlanningResult path, System.Action callback = null)
        {
            if (ActivePath == null)
            {
                ActivePath = ResourceManager.instance.GetActivePath();
                if (ActivePath == null)
                {
                    return null;
                }
            }

            ActivePath.Initialize(path, this, callback);
            ActivePath.gameObject.SetActive(true);
            ActivePath.StartPath();

            return ActivePath;
        }

        public Building Home
        {
            get
            {
                Debug.Assert(PointsOfInterest.ContainsKey(PointOfInterest.Home), "citizen has no home!");
                return PointsOfInterest[PointOfInterest.Home] as Building;
            }
        }

        public string Name => $"{FirstName} {LastName}";

        public IMapObject CurrentDestination => CurrentEvent.location;

        public Velocity WalkingSpeed
        {
            get
            {
                if (Age < 10)
                {
                    return Velocity.FromRealTimeKPH(5f);
                }
                
                if (Age < 30)
                {
                    return Velocity.FromRealTimeKPH(8f);
                }
                
                if (Age < 60f)
                {
                    return Velocity.FromRealTimeKPH(7f);
                }

                return Velocity.FromRealTimeKPH(3f);
            }
        }

#if DEBUG
        public static string GetDestinationString(PointOfInterest poi)
        {
            var msg = "Driving ";
            switch (poi)
            {
            case Citizen.PointOfInterest.Home:
                msg += "home";
                break;
            case Citizen.PointOfInterest.School:
                msg += "to school";
                break;
            case Citizen.PointOfInterest.Work:
                msg += "to work";
                break;
            case Citizen.PointOfInterest.GroceryStore:
                msg += "to the grocery store";
                break;
            }

            return msg;
        }

        public static string GetDestinationName(PointOfInterest poi)
        {
            switch (poi)
            {
            case Citizen.PointOfInterest.Home:
                return "home";
            case Citizen.PointOfInterest.School:
                return "school";
            case Citizen.PointOfInterest.Work:
                return "work";
            case Citizen.PointOfInterest.GroceryStore:
                return "the grocery store";
            default:
                return "";
            }
        }

        public override string ToString()
        {
            var s = FirstName + " " + LastName + (Female ? " (f)" : " (m)") + ", " + Age;

            foreach (var poi in PointsOfInterest)
            {
                s += "\n   ";
                s += poi.Key.ToString();
                s += ": ";
                s += poi.Value.ToString();
            }

            return s;
        }
#endif

        public void ActivateModal()
        {
            var modal = MainUI.instance.citizenModal;
            modal.SetCitizen(this);
            modal.modal.Enable();
        }

        public Serialization.Citizen ToProtobuf()
        {
            var c = new Serialization.Citizen
            {
                Id = id,
                FirstName = FirstName,
                LastName = LastName,
                Age = (uint)Age,
                Birthday = (uint)Birthday,
                Female = Female,
                Occupation = (Serialization.Citizen.Types.Occupation)occupation,
                Money = (float)Money,
                Educated = Educated,
                Happiness = (uint)Happiness,
                CarID = Car?.id ?? 0,

                CurrentPosition = CurrentPosition.ToProtobuf(),
                
                PreferredColor = PreferredColor.ToProtobuf(),
                TransitPreferences = TransitPreferences.ToProtobuf(),
            };

            c.Relationships.AddRange(Relationships.Select(r => new Serialization.Citizen.Types.Relationship
            {
                Kind = (Serialization.Citizen.Types.RelationshipKind)r.Key,
                CitizenId = r.Value.id,
            }));

            c.PointsOfInterest.AddRange(PointsOfInterest.Select(r => new Serialization.Citizen.Types.PointOfInterest
            {
                Kind = (Serialization.Citizen.Types.PointOfInterestKind)r.Key,
                BuildingId = (uint)r.Value.Id,
            }));

            c.HappinessInfluences.AddRange(HappinessInfluences.Select(inf => new Serialization.Citizen.Types.HappinessInfluence
            {
                AbsoluteCapHi = inf.Value.absoluteCapHi,
                AbsoluteCapLo = inf.Value.absoluteCapLo,
                DescriptionKey = inf.Key,
                Influence = inf.Value.influence,
                RelativeCap = inf.Value.relativeCap,
                Ticks = (int)(inf.Value.duration?.Ticks ?? 0),
            }));

            return c;
        }

        public int CompareTo(object obj)
        {
            if (obj is Citizen c)
            {
                return id.CompareTo(c.id);
            }

            return 0;
        }
    }
}