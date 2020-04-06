using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;
using Transidious.Simulation;
using Random = UnityEngine.Random;

namespace Transidious
{
    public class Citizen
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
        public string firstName { get; private set; }
        
        /// The citizen's last name.
        public string lastName { get; private set; }
        
        /// The citizen's age in years.
        public short age { get; private set; }

        /// The citizen's birthday in range [0..365].
        public short birthday { get; private set; }
        
        /// True iff this citizen is a woman.
        public bool female { get; private set; }
        
        /// The citizen's occupation.
        public Occupation occupation { get; private set; }
        
        /// The citizen's current money.
        public decimal money { get; private set; }

        /// The citizen's car (can be null).
        public Car car;

        /// True iff this citizen has an education.
        public bool educated { get; private set; }

        /// The citizen's current happiness [0..100].
        public float happiness { get; private set; }

        /// The citizen's current energy level [0..100].
        public float energy { get; private set; }

        /// The amount of work this citizen has to perform [0..100].
        public float remainingWork { get; private set; }

        /// The color to use for rendering this citizen.
        public readonly Color preferredColor;

        /// Map of the citizen's relationships.
        public readonly Dictionary<Relationship, Citizen> relationships;
        
        /// Map of the citizen's points of interest.
        public readonly Dictionary<PointOfInterest, IMapObject> pointsOfInterest;

        /// Options to use for path planning.
        public PathPlanningOptions transitPreferences;

        /// Current position on the map.
        public Vector2 currentPosition;

        /// The citizen's schedule.
        public Simulation.Schedule schedule;
        
        /// The citizen's next scheduled event.
        public Simulation.Schedule.EventInfo currentEvent;

        /// The path that the citizen is currently following (can be null).
        public ActivePath activePath;

        /// Regular influences on the citizen's happiness.
        public readonly Dictionary<string, HappinessInfluence> happinessInfluences;

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
            this.car = car;
            this.preferredColor = Utility.RandomColor;

            this.pointsOfInterest = new Dictionary<PointOfInterest, IMapObject>();
            this.relationships = new Dictionary<Relationship, Citizen>();
            this.happinessInfluences = new Dictionary<string, HappinessInfluence>();
        }

        public Citizen(SimulationController sim, Serialization.Citizen c)
        {
            this.sim = sim;
            this.id = c.Id;

            this.sim.totalCitizenCount++;
            this.sim.citizens.Add(this.id, this);
            this.sim.citizenList.Add(this);

            this.firstName = c.FirstName;
            this.lastName = c.LastName;
            this.age = (short)c.Age;
            this.female = c.Female;
            this.birthday = (short)c.Birthday;
            this.occupation = (Occupation)c.Occupation;
            this.money = (decimal)c.Money;
            this.happiness = (float)c.Happiness;
            this.educated = c.Educated;

            this.pointsOfInterest = new Dictionary<PointOfInterest, IMapObject>();
            this.relationships = new Dictionary<Relationship, Citizen>();
            this.happinessInfluences = new Dictionary<string, HappinessInfluence>();
            this.currentPosition = c.CurrentPosition.Deserialize();

            var map = GameController.instance.loadedMap;
            foreach (var poi in c.PointsOfInterest)
            {
                var kind = (PointOfInterest)poi.Kind;
                var building = map.GetMapObject<Building>((int)poi.BuildingId);
                this.pointsOfInterest.Add(kind, building);

                switch (kind)
                {
                    case PointOfInterest.Home:
                    case PointOfInterest.Work:
                    case PointOfInterest.School:
                        ++building.occupants;
                        break;
                    default:
                        break;
                }
            }

            foreach (var inf in c.HappinessInfluences)
            {
                happinessInfluences.Add(inf.DescriptionKey, new HappinessInfluence
                {
                    absoluteCapHi = inf.AbsoluteCapHi,
                    absoluteCapLo = inf.AbsoluteCapLo,
                    influence = inf.Influence,
                    relativeCap = inf.RelativeCap,
                    duration = TimeSpan.FromTicks(inf.Ticks),
                });
            }

            this.preferredColor = c.PreferredColor.Deserialize();
            transitPreferences = PathPlanningOptions.Deserialize(c.TransitPreferences);
        }

        public PathPlanning.PathPlanningOptions CreateRandomPreferences()
        {
            bool allowCar;
            float carTimeFactor;
            float changingPenalty;
            float waitingTimeFactor;
            float walkingTimeFactor;
            float maxWalkingDistance;

            if (age < 10)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = Random.Range(10f, 15f);
                waitingTimeFactor = Random.Range(1.5f, 2f);
                walkingTimeFactor = Random.Range(2f, 2.5f);
                maxWalkingDistance = 100f;
            }
            else if (age < 18)
            {
                allowCar = false;
                carTimeFactor = 1f;
                changingPenalty = Random.Range(5f, 15f);
                waitingTimeFactor = Random.Range(1f, 2f);
                walkingTimeFactor = Random.Range(1f, 2.5f);
                maxWalkingDistance = 200f;
            }
            else if (age < 40)
            {
                allowCar = Random.value <= .6f;
                carTimeFactor = Random.Range(.8f, 3f);
                changingPenalty = Random.Range(3f, 15f);
                waitingTimeFactor = Random.Range(2f, 3f);
                walkingTimeFactor = Random.Range(.8f, 2f);
                maxWalkingDistance = 150f;
            }
            else if (age < 65)
            {
                allowCar = Random.value <= .75f;
                carTimeFactor = Random.Range(.6f, 2.5f);
                changingPenalty = Random.Range(5f, 15f);
                waitingTimeFactor = Random.Range(1f, 2f);
                walkingTimeFactor = Random.Range(2.5f, 3f);
                maxWalkingDistance = 100f;
            }
            else
            {
                allowCar = Random.value <= .3f;
                carTimeFactor = Random.Range(2f, 4f);
                changingPenalty = Random.Range(2f, 5f);
                waitingTimeFactor = Random.Range(1f, 1.5f);
                walkingTimeFactor = Random.Range(3f, 8f);
                maxWalkingDistance = 50f;
            }

            if (allowCar && car == null)
            {
                this.car = sim.CreateCar(this, currentPosition);
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
                this.relationships.Add((Relationship)rel.Kind, sim.citizens[rel.CitizenId]);
            }
        }

        public void AddHappinessInfluence(string key, float influence,
                                          TimeSpan? duration = null,
                                          float relativeCap = -1f,
                                          float absoluteCapLo = 0f,
                                          float absoluteCapHi = 100f)
        {
            this.happinessInfluences.Add(key, new HappinessInfluence(influence,
                duration, relativeCap, absoluteCapLo, absoluteCapHi));
        }

        public void SetHappiness(float newHappiness)
        {
            happiness = Mathf.Clamp(newHappiness, 0f, 100f);

            if (_lastHappinessAnimation < 0f)
            {
                _lastHappinessAnimation = happiness;
                return;
            }

            var diff = happiness - _lastHappinessAnimation;
            if (Mathf.Abs(diff) >= 1f)
            {
                DisplayHappinessChangeAnimation(diff);
                _lastHappinessAnimation = happiness;
            }
        }

        public void SetEnergy(float newEnergy)
        {
            this.energy = Mathf.Clamp(newEnergy, 0f, 100f);

            var tired = happinessInfluences.ContainsKey("Tired");
            if (energy < 0f && !tired)
            {
                // .5% per hour
                AddHappinessInfluence("Tired", -(.5f / (24f * 60f)), null, 
                                      -1f, 0f, 70f);
            }
            else if (energy > 0f && tired)
            {
                happinessInfluences.Remove("Tired");
            }
        }

        public void SetRemainingWork(float newRemainingWork)
        {
            if (occupation == Occupation.Unemployed || occupation == Occupation.Retired)
                return;

            this.remainingWork = Mathf.Clamp(newRemainingWork, 0f, 100f);
        }

        void DisplayHappinessChangeAnimation(float diff)
        {
            var sprite = ResourceManager.instance.GetTemporarySprite();
            if (sprite == null)
            {
                return;
            }

            Debug.Log("starting animation!");

            var sr = sprite.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteManager.GetSprite("Sprites/arrow");
            sr.color = Colors.GetColor(diff < 0f ? "ui.happinessLow" : "ui.happinessHigh");
            sr.transform.localScale = new Vector3(.3f, .3f, 1f);

            var pos = currentPosition.WithZ(Map.Layer(MapLayer.Foreground));
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
                this.lastName = RandomNameGenerator.LastName;
            }
            else
            {
                this.lastName = lastName;
            }

            var genderAndAge = RandomNameGenerator.GenderAndAge;

            if (!female.HasValue)
            {
                this.female = genderAndAge.Item1;
            }
            else
            {
                this.female = female.Value;
            }
            
            if (!age.HasValue)
            {
                this.age = (short)genderAndAge.Item2;
            }
            else
            {
                this.age = age.Value;
            }
            
            if (firstName == null)
            {
                this.firstName = this.female ? RandomNameGenerator.FemaleFirstName
                                         : RandomNameGenerator.MaleFirstName;
            }
            else
            {
                this.firstName = firstName;
            }
            
            if (!birthday.HasValue)
            {
                this.birthday = (short)Random.Range(0, 365);
            }
            else
            {
                this.birthday = birthday.Value;
            }

            if (!happiness.HasValue)
            {
                this.happiness = Random.Range(70f, 100f);
            }
            else
            {
                this.happiness = happiness.Value;
            }

            var currentHour = sim.GameTime.Hour;
            if (occupation != Occupation.Retired && occupation != Occupation.Unemployed)
            {
                if (currentHour < 8)
                {
                    remainingWork = 100f;
                    energy = 100f - currentHour * (100f / 16f);
                }
                else if (currentHour < 16)
                {
                    remainingWork = (100f / 8f) * (16 - currentHour);
                    energy = 100f - ((currentHour - 8) * (30f / 8f));
                }
                else
                {
                    remainingWork = 0f;
                    energy = 100f - (30f / 8f);
                }
            }
            else
            {
                remainingWork = 0f;
                energy = 100f;
            }

            if (!money.HasValue)
            {
                this.money = (decimal)Random.Range(0f, 1000f);
            }
            else
            {
                this.money = money.Value;
            }

            if (this.money < 200m)
            {
                // 1% per day
                AddHappinessInfluence("Poorness", -(1f / (24f * 60f * 60f)), null, 
                                      -1f, 0f, 70f);
            }

            transitPreferences = CreateRandomPreferences();
            AssignOccupation(occupation);

            this.schedule = new Simulation.Schedule(this, null);
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

            home.AddInhabitant(this);
            this.pointsOfInterest.Add(PointOfInterest.Home, home);
            
            currentPosition = home.Centroid;
        }

        public void Initialize(uint scheduleID = 0)
        {
            var groceryStore = sim.ClosestUnoccupiedBuilding(Building.Type.GroceryStore, Home.Centroid);
            if (groceryStore != null)
            {
                pointsOfInterest.Add(PointOfInterest.GroceryStore, groceryStore);
            }

            this.currentPosition = Home.centroid;
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
            else if (age < 7)
            {
                this.occupation = Occupation.Kindergardener;
            }
            else if (age < 11)
            {
                this.occupation = Occupation.ElementarySchoolStudent;
            }
            else if (age < 18)
            {
                this.occupation = Occupation.HighSchoolStudent;
            }
            else if (age < 25)
            {
                if (UnityEngine.Random.value < UniversityProbability)
                {
                    this.occupation = Occupation.UniversityStudent;
                }
                else
                {
                    this.occupation = Occupation.Trainee;
                }
            }
            else if (age < 67)
            {
                this.occupation = Occupation.Worker;
            }
            else
            {
                this.occupation = Occupation.Retired;
            }

            AssignWorkplace();
        }

        void AssignWorkplace()
        {
            Building.Type buildingType;
            PointOfInterest poiType;

            switch (occupation)
            {
            case Occupation.Kindergardener:
            case Occupation.Retired:
            default:
                return;
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
                buildingType = educated ? Building.Type.Office : Building.Type.Shop;
                poiType = PointOfInterest.Work;
                break;
            }

            var place = sim.ClosestUnoccupiedBuilding(buildingType, Home.centroid);
            if (place != null)
            {
                ++place.occupants;
                pointsOfInterest.Add(poiType, place);
            }
        }

        public IMapObject GetPointOfInterest(params Citizen.PointOfInterest[] options)
        {
            foreach (var poi in pointsOfInterest)
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
            foreach (var poi in pointsOfInterest)
            {
                if (options.Contains(poi.Key))
                {
                    possibilities.Add(poi.Value);
                }
            }

            return Utility.RandomElement(possibilities);
        }

        public void UpdateAge()
        {
            if (sim.GameTime.DayOfYear != birthday)
            {
                return;
            }

            ++age;

            if (IsThresholdAge(age))
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
            if (currentEvent.location != null)
            {
                --currentEvent.location.Visitors;
            }

            currentEvent = schedule.GetNextEvent(currentTime, newDay);
            Debug.Log($"[{Name}] {currentEvent.DebugDescription}");

            if (currentEvent.path != null)
            {
                var activePath = FollowPath(currentEvent.path);
                if (currentEvent.location != null)
                {
                    activePath.onDone = () =>
                    {
                        ++currentEvent.location.Visitors;

                        if (sim.citizenModal.citizen == this)
                        {
                            sim.citizenModal.UpdateAll();
                        }
                    };
                }
            }
        }

        public void UpdateHappiness(TimeSpan timeSinceLastUpdate, float bonus)
        {
            var totalCapLo = 0f;
            var totalCapHi = 100f;
            var newHappiness = happiness;
            var passedSeconds = (float) timeSinceLastUpdate.TotalSeconds;

            foreach (var (key, item) in happinessInfluences)
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

            if (activePath == null)
            {
                happinessBonus = currentEvent.HappinessBonusPerHour;
            }

            if (happinessInfluences.Count > 0)
            {
                UpdateHappiness(timeSinceLastUpdate, happinessBonus);
            }
            else if (happinessBonus > 0f)
            {
                SetHappiness(happiness + passedHours * happinessBonus);
            }

            float energyBonus;
            float remainingWorkBonus;

            if (activePath == null)
            {
                energyBonus = currentEvent.EnergyBonusPerHour;
                remainingWorkBonus = currentEvent.RemainingWorkBonusPerHour;
            }
            else
            {
                energyBonus = activePath.EnergyBonusPerHour;
                remainingWorkBonus = activePath.RemainingWorkBonusPerHour;
            }

            SetEnergy(energy + energyBonus * passedHours);
            SetRemainingWork(remainingWork + remainingWorkBonus * passedHours);

            if (currentTime >= currentEvent.endTime)
            {
                if (activePath != null)
                {
                    // We completely missed the previous event, apply a penalty.
                    SetHappiness(happiness - currentEvent.PenaltyForMissing);
                }

                UpdateDailySchedule(currentTime, newDay);
            }
            else if (sim.citizenModal.citizen == this)
            {
                sim.citizenModal.UpdateFrequentChanges();
            }
        }

        public ActivePath FollowPath(PathPlanningResult path, System.Action callback = null)
        {
            if (activePath == null)
            {
                activePath = ResourceManager.instance.GetActivePath();
                if (activePath == null)
                {
                    return null;
                }
            }

            activePath.Initialize(path, this, callback);
            activePath.gameObject.SetActive(true);
            activePath.StartPath();

            return activePath;
        }

        public Building Home
        {
            get
            {
                Debug.Assert(pointsOfInterest.ContainsKey(PointOfInterest.Home), "citizen has no home!");
                return pointsOfInterest[PointOfInterest.Home] as Building;
            }
        }

        public string Name => $"{firstName} {lastName}";

        public IMapObject CurrentDestination => currentEvent.location;

        public Velocity WalkingSpeed
        {
            get
            {
                if (age < 10)
                {
                    return Velocity.FromRealTimeKPH(5f);
                }
                
                if (age < 30)
                {
                    return Velocity.FromRealTimeKPH(8f);
                }
                
                if (age < 60f)
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
            var s = firstName + " " + lastName + (female ? " (f)" : " (m)") + ", " + age;

            foreach (var poi in pointsOfInterest)
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
            var modal = GameController.instance.sim.citizenModal;
            modal.SetCitizen(this);

            modal.modal.PositionAt(currentPosition);
            modal.modal.Enable();
        }

        public Serialization.Citizen ToProtobuf()
        {
            var c = new Serialization.Citizen
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Age = (uint)age,
                Birthday = (uint)birthday,
                Female = female,
                Occupation = (Serialization.Citizen.Types.Occupation)occupation,
                Money = (float)money,
                Educated = educated,
                Happiness = (uint)happiness,
                CarID = car?.id ?? 0,

                CurrentPosition = currentPosition.ToProtobuf(),
                
                PreferredColor = preferredColor.ToProtobuf(),
                TransitPreferences = transitPreferences.ToProtobuf(),
            };

            c.Relationships.AddRange(relationships.Select(r => new Serialization.Citizen.Types.Relationship
            {
                Kind = (Serialization.Citizen.Types.RelationshipKind)r.Key,
                CitizenId = r.Value.id,
            }));

            c.PointsOfInterest.AddRange(pointsOfInterest.Select(r => new Serialization.Citizen.Types.PointOfInterest
            {
                Kind = (Serialization.Citizen.Types.PointOfInterestKind)r.Key,
                BuildingId = (uint)r.Value.Id,
            }));

            c.HappinessInfluences.AddRange(happinessInfluences.Select(inf => new Serialization.Citizen.Types.HappinessInfluence
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
    }
}