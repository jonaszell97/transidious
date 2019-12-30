using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class Citizien
    {
        public class AbstractSchedule
        {
            public struct Event
            {
                public PointOfInterest place;
                public int startTime;
                public int duration;
                public float probability;
                public bool flexible;
            }

            public uint id;
            public Event[] events;

            static AbstractSchedule[] workSchedules;
            static AbstractSchedule[] elementarySchoolSchedules;
            static AbstractSchedule[] highSchoolSchedules;
            static AbstractSchedule[] universitySchedules;

            static uint lastAssignedID = 0;
            static Dictionary<uint, AbstractSchedule> schedules;

            AbstractSchedule()
            {
                this.id = ++lastAssignedID;
                schedules.Add(id, this);
            }

            public static void Initialize()
            {
                if (workSchedules != null)
                {
                    return;
                }

                schedules = new Dictionary<uint, AbstractSchedule>();

                workSchedules = new AbstractSchedule[] {
                    new AbstractSchedule() {
                        events = new Event[] {
                            // Work 8:00 AM to 4:00 PM
                            new Event {
                                place = PointOfInterest.Work,
                                startTime = 480,
                                duration = 480,
                                probability = 1f,
                                flexible = true,
                            },
                            // Groceries 4:00 PM to 4:30 PM
                            new Event {
                                place = PointOfInterest.GroceryStore,
                                startTime = -1,
                                duration = 30,
                                probability = 0.3f,
                                flexible = true,
                            },
                            // Home 4:30 PM to end of day
                            new Event {
                                place = PointOfInterest.Home,
                                startTime = -1,
                                duration = -1,
                                probability = 1f,
                                flexible = true,
                            }
                        }
                    },
                    new AbstractSchedule() {
                        events = new Event[] {
                            // Work 9:00 AM to 5:00 PM
                            new Event {
                                place = PointOfInterest.Work,
                                startTime = 540,
                                duration = 480,
                                probability = 1f,
                                flexible = true,
                            },
                            // Groceries 5:00 PM to 5:30 PM
                            new Event {
                                place = PointOfInterest.GroceryStore,
                                startTime = -1,
                                duration = 30,
                                probability = 0.45f,
                                flexible = true,
                            },
                            // Home 5:30 PM to end of day
                            new Event {
                                place = PointOfInterest.Home,
                                startTime = -1,
                                duration = -1,
                                probability = 1f,
                                flexible = true,
                            }
                        }
                    },
                };

                elementarySchoolSchedules = new AbstractSchedule[] {
                    new AbstractSchedule() {
                        events = new Event[] {
                            // School 8:00 AM to 1:00 PM
                            new Event {
                                place = PointOfInterest.School,
                                startTime = 480,
                                duration = 300,
                                probability = 1f,
                                flexible = true,
                            },
                            // Home to end of day
                            new Event {
                                place = PointOfInterest.Home,
                                startTime = -1,
                                duration = -1,
                                probability = 1f,
                                flexible = true,
                            }
                        }
                    }
                };

                highSchoolSchedules = new AbstractSchedule[] {
                    new AbstractSchedule() {
                        events = new Event[] {
                            // School 8:00 AM to 3:00 PM
                            new Event {
                                place = PointOfInterest.School,
                                startTime = 480,
                                duration = 420,
                                probability = 1f,
                                flexible = true,
                            },
                            // Home to end of day
                            new Event {
                                place = PointOfInterest.Home,
                                startTime = -1,
                                duration = -1,
                                probability = 1f,
                                flexible = true,
                            }
                        }
                    }
                };

                universitySchedules = new AbstractSchedule[] {
                    new AbstractSchedule() {
                        events = new Event[] {
                            // School 10:00 AM to 6:00 PM
                            new Event {
                                place = PointOfInterest.School,
                                startTime = 480,
                                duration = 480,
                                probability = 1f,
                                flexible = true,
                            },
                            // Home to end of day
                            new Event {
                                place = PointOfInterest.Home,
                                startTime = -1,
                                duration = -1,
                                probability = 1f,
                                flexible = true,
                            }
                        }
                    }
                };
            }

            public static AbstractSchedule WorkSchedule
            {
                get
                {
                    Initialize();
                    return Utility.RandomElement(workSchedules);
                }
            }

            public static AbstractSchedule ElementarySchoolSchedule
            {
                get
                {
                    Initialize();
                    return Utility.RandomElement(elementarySchoolSchedules);
                }
            }

            public static AbstractSchedule HighSchoolSchedule
            {
                get
                {
                    Initialize();
                    return Utility.RandomElement(highSchoolSchedules);
                }
            }

            public static AbstractSchedule UniversitySchedule
            {
                get
                {
                    Initialize();
                    return Utility.RandomElement(universitySchedules);
                }
            }

            public static AbstractSchedule Get(uint id)
            {
                return schedules[id];
            }
        }

        public class ScheduledEvent
        {
            public int startsAt;
            public PathPlanning.PathPlanningResult path;
            public ScheduledEvent nextEvent;
            public ScheduledEvent prevEvent;
            public PointOfInterest place;

#if DEBUG
            public override string ToString()
            {
                var s = new System.Text.StringBuilder();
                var curr = this;

                var i = 0;
                while (curr != null)
                {
                    if (i++ != 0)
                        s.Append('\n');

                    var time = new System.DateTime(1, 1, 1, 0, 0, 0);
                    var startTime = time.AddMinutes(curr.startsAt);

                    s.Append("[");
                    s.Append(startTime.ToShortTimeString());
                    s.Append("] leave for ");
                    s.Append(GetDestinationName(curr.place));

                    if (curr.path != null)
                    {
                        s.Append('\n');

                        var arriveTime = startTime.AddMinutes(curr.path.duration * 60);
                        s.Append("[");
                        s.Append(arriveTime.ToShortTimeString());
                        s.Append("] arrive at ");
                        s.Append(GetDestinationName(curr.place));
                    }

                    curr = curr.nextEvent;
                }

                return s.ToString();
            }
#endif
        }

        public struct HappinessInfluence
        {
            /// The key for the description of this influence.
            public string descriptionKey;

            /// The total impact of this item.
            public float influence;

            /// The number of ticks this item is spread over.
            public int ticks;

            /// The relative cap of this item (i.e. it is not applied if the
            /// happiness is below / above this cap).
            public float relativeCap;

            /// The absolute cap this item applies to the happiness value.
            public float absoluteCapLo;
            public float absoluteCapHi;

            public HappinessInfluence(string key, float influence, int ticks,
                                      float relativeCap = -1f,
                                      float absoluteCapLo = 0f,
                                      float absoluteCapHi = 100f)
            {
                this.descriptionKey = key;
                this.influence = influence;
                this.ticks = ticks;

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
            Kindergarden,
            ElementarySchoolStudent,
            HighSchoolStudent,
            UniversityStudent,
            Trainee,
        }

        public enum PointOfInterest
        {
            Home,
            Work,
            School,
            GroceryStore,
        }

        public enum Relationship
        {
            SignificantOther,
        }

        static uint lastAssignedID = 0;

        public SimulationController sim;

        public uint id;
        public string firstName;
        public string lastName;
        public short age;
        public short birthday;
        public bool female;
        public Occupation occupation;
        public decimal money;
        public Car car;
        public bool educated;
        public float happiness;
        public AbstractSchedule[] schedules;
        public Dictionary<Relationship, Citizien> relationships;
        public Dictionary<PointOfInterest, Building> pointsOfInterest;

        public PathPlanning.PathPlanningOptions transitPreferences;
        public Vector2 currentPosition;

        int scheduleIdx = 0;
        public ScheduledEvent dailySchedule;
        public TrafficSimulator.ActivePath activePath = null;

        public List<HappinessInfluence> happinessInfluences;

        public static readonly float UniversityProbability = 0.7f;

        public Citizien(SimulationController sim, Car car = null, uint id = 0)
        {
            this.sim = sim;
            this.sim.totalCitizienCount++;

            if (id == 0)
            {
                this.id = ++lastAssignedID;
            }
            else
            {
                this.id = id;
                lastAssignedID = System.Math.Max(id, lastAssignedID);
            }

            this.sim.citiziens.Add(this.id, this);
            this.sim.citizienList.Add(this);
            this.car = car;

            this.pointsOfInterest = new Dictionary<PointOfInterest, Building>();
            this.relationships = new Dictionary<Relationship, Citizien>();
            this.happinessInfluences = new List<HappinessInfluence>();
        }

        public Citizien(SimulationController sim, Serialization.Citizien c)
        {
            this.sim = sim;
            this.id = c.Id;

            this.sim.totalCitizienCount++;
            this.sim.citiziens.Add(this.id, this);
            this.sim.citizienList.Add(this);

            this.firstName = c.FirstName;
            this.lastName = c.LastName;
            this.age = (short)c.Age;
            this.female = c.Female;
            this.birthday = (short)c.Birthday;
            this.occupation = (Occupation)c.Occupation;
            this.money = (decimal)c.Money;
            this.happiness = (float)c.Happiness;
            this.educated = c.Educated;

            this.pointsOfInterest = new Dictionary<PointOfInterest, Building>();
            this.relationships = new Dictionary<Relationship, Citizien>();
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

            // FIXME serialize this
            transitPreferences = new PathPlanning.PathPlanningOptions();
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

            return new PathPlanning.PathPlanningOptions
            {
                allowCar = allowCar,
                carTimeFactor = carTimeFactor,
                changingPenalty = changingPenalty,
                waitingTimeFactor = waitingTimeFactor,
                walkingTimeFactor = walkingTimeFactor,
                maxWalkingDistance = maxWalkingDistance,
            };
        }

        public void Finalize(Serialization.Citizien c)
        {
            var map = GameController.instance.loadedMap;

            foreach (var rel in c.Relationships)
            {
                this.relationships.Add((Relationship)rel.Kind, sim.citiziens[rel.CitizienId]);
            }

            if (c.ScheduleIdx != -1)
            {
                for  (var i = 0; i < c.ScheduleIdx; ++i)
                {
                    dailySchedule = dailySchedule.nextEvent;
                }
            }

            if (c.ActivePath == null)
            {
                return;
            }

            activePath = TrafficSimulator.ActivePath.Deserialize(map, c.ActivePath);
            sim.trafficSim.FollowPath(this, activePath);
        }

        public void AddHappinessInfluence(string key, float influence, int ticks,
                                          float relativeCap = -1f,
                                          float absoluteCapLo = 0f,
                                          float absoluteCapHi = 100f)
        {
            this.happinessInfluences.Add(new HappinessInfluence(key, influence,
                ticks, relativeCap, absoluteCapLo, absoluteCapHi));
        }

        public void AssignRandomValues(string firstName = null,
                                       string lastName = null,
                                       short? age = null,
                                       short? birthday = null,
                                       bool? female = null,
                                       Citizien.Occupation? occupation = null,
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
                this.happiness = 100;
            }
            else
            {
                this.happiness = happiness.Value;
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
                AddHappinessInfluence("Poorness", -40f, 100_000, -1f, 0f, 70f);
            }

            transitPreferences = CreateRandomPreferences();
            AssignOccupation(occupation);
        }

        public void AssignRandomHome()
        {
            var home = sim.RandomUnoccupiedBuilding(Building.Type.Residential);
            if (home == null)
            {
                Debug.LogError("could not find home!");
                return;
            }

            ++home.occupants;
            this.pointsOfInterest.Add(PointOfInterest.Home, home);
        }

        public void Initialize(uint scheduleID = 0)
        {
            if (scheduleID == 0)
            {
                CreateSchedules();
            }
            else
            {
                CreateSchedules(scheduleID);
            }

            var groceryStore = sim.RandomUnoccupiedBuilding(Building.Type.GroceryStore);
            if (groceryStore != null)
            {
                pointsOfInterest.Add(PointOfInterest.GroceryStore, groceryStore);
            }

            this.currentPosition = Home.centroid;
            UpdateDailySchedule(sim.MinuteOfDay);

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
                this.occupation = Occupation.Kindergarden;
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
            case Occupation.Kindergarden:
            case Occupation.Retired:
            default:
                return;
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

        void CreateSchedules(uint id)
        {
            this.schedules = new AbstractSchedule[7];

            AbstractSchedule.Initialize();

            var schedule = AbstractSchedule.Get(id);
            schedules[0] = schedule;
            schedules[1] = schedule;
            schedules[2] = schedule;
            schedules[3] = schedule;
            schedules[4] = schedule;
        }

        void CreateSchedules()
        {
            this.schedules = new AbstractSchedule[7];
            switch (this.occupation)
            {
            default:
                break;
            case Occupation.Worker:
                {
                    var workSchedule = AbstractSchedule.WorkSchedule;
                    schedules[0] = workSchedule;
                    schedules[1] = workSchedule;
                    schedules[2] = workSchedule;
                    schedules[3] = workSchedule;
                    schedules[4] = workSchedule;
                }
                break;
            case Occupation.ElementarySchoolStudent:
                {
                    var schedule = AbstractSchedule.ElementarySchoolSchedule;
                    schedules[0] = schedule;
                    schedules[1] = schedule;
                    schedules[2] = schedule;
                    schedules[3] = schedule;
                    schedules[4] = schedule;
                }
                break;
            case Occupation.HighSchoolStudent:
                {
                    var schedule = AbstractSchedule.HighSchoolSchedule;
                    schedules[0] = schedule;
                    schedules[1] = schedule;
                    schedules[2] = schedule;
                    schedules[3] = schedule;
                    schedules[4] = schedule;
                }
                break;
            case Occupation.UniversityStudent:
                {
                    var schedule = AbstractSchedule.UniversitySchedule;
                    schedules[0] = schedule;
                    schedules[1] = schedule;
                    schedules[2] = schedule;
                    schedules[3] = schedule;
                    schedules[4] = schedule;
                }
                break;
            }
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
                CreateSchedules();
            }
        }
        
        public decimal GetTaxes()
        {
            switch (occupation)
            {
                case Citizien.Occupation.Worker:
                    return 2m;
                case Citizien.Occupation.UniversityStudent:
                case Citizien.Occupation.Trainee:
                    return 1m;
                case Citizien.Occupation.Retired:
                case Citizien.Occupation.Kindergarden:
                case Citizien.Occupation.ElementarySchoolStudent:
                case Citizien.Occupation.HighSchoolStudent:
                default:
                    return 0m;
            }
        }

        bool ScheduleEvent(ref ScheduledEvent prevEvent,
                           AbstractSchedule.Event e,
                           PathPlanning.PathPlanningResult path,
                           int startTime, int duration,
                           ref int earliestStartTime)
        {
            var nextEvent = new ScheduledEvent
            {
                startsAt = startTime,
                path = path,
                place = e.place,
            };

            if (prevEvent == null)
            {
                prevEvent = nextEvent;
                dailySchedule = nextEvent;
            }
            else
            {
                prevEvent.nextEvent = nextEvent;
                nextEvent.prevEvent = prevEvent;
            }

            prevEvent = nextEvent;

            if (duration == -1)
            {
                return false;
            }

            earliestStartTime = startTime + duration;
            return true;
        }

        public void UpdateDailySchedule(int minuteOfDay)
        {
            var schedule = schedules[sim.GameTime.Day];
            if (schedule == null)
            {
                return;
            }

            ScheduledEvent prevEvent = null;
            var earliestStartTime = minuteOfDay;

            foreach (var e in schedule.events)
            {
                // Check if this event happens today.
                if (!e.probability.Equals(1f) && Random.value > e.probability)
                {
                    continue;
                }

                // FIXME this should not really ever happen
                if (!pointsOfInterest.TryGetValue(e.place, out Building poi))
                {
                    continue;
                }

                var planner = new PathPlanning.PathPlanner(transitPreferences);
                var path = planner.FindClosestPath(sim.game.loadedMap, currentPosition, poi.centroid);
                var pathDuration = (int)Mathf.Ceil(path.duration * 60);

                // Schedule immediately after preceding event.
                if (e.startTime == -1)
                {
                    if (ScheduleEvent(ref prevEvent, e, path, earliestStartTime,
                                      e.duration + pathDuration, ref earliestStartTime))
                    {
                        continue;
                    }

                    break;
                }

                // Check if we can still get to the event location in time.
                if (earliestStartTime + pathDuration <= e.startTime)
                {
                    if (ScheduleEvent(ref prevEvent, e, path, e.startTime - pathDuration,
                                      e.duration + pathDuration,
                                      ref earliestStartTime))
                    {
                        continue;
                    }

                    break;
                }

                // Don't schedule non-flexible events we can't get to in time.
                if (!e.flexible)
                {
                    continue;
                }

                // Schedule immediately.
                if (ScheduleEvent(ref prevEvent, e, path, earliestStartTime,
                                  e.duration + pathDuration, ref earliestStartTime))
                {
                    continue;
                }

                break;
            }
        }

        public void UpdateHappiness(int ticks)
        {
            var totalCapLo = 0f;
            var totalCapHi = 100f;
            var newHappiness = happiness;

            foreach (var item in happinessInfluences)
            {
                if (item.influence < 0f && newHappiness <= item.relativeCap)
                {
                    continue;
                }
                if (item.influence > 0f && newHappiness >= item.relativeCap)
                {
                    continue;
                }

                newHappiness += (item.influence / item.ticks) * ticks;
                totalCapLo = Mathf.Max(totalCapLo, item.absoluteCapLo);
                totalCapHi = Mathf.Min(totalCapHi, item.absoluteCapHi);
            }

            happiness = Mathf.Clamp(newHappiness, totalCapLo, totalCapHi);
        }

        public void Update(int minuteOfDay, int ticks)
        {
            if (happinessInfluences.Count > 0)
            {
                UpdateHappiness(ticks);
            }

            if (dailySchedule == null)
            {
                return;
            }

            if (minuteOfDay < dailySchedule.startsAt)
            {
                return;
            }

            if (activePath != null)
            {
                // Debug.LogWarning("next event started before current path is done!");
                return;
            }

#if DEBUG
            Debug.Log("[" + Name + "] " + GetDestinationString(dailySchedule.place));
#endif

            if (dailySchedule.path != null)
            {
                sim.trafficSim.FollowPath(this, dailySchedule.path);
            }

            dailySchedule = dailySchedule.nextEvent;
            ++scheduleIdx;
        }

        public Building Home
        {
            get
            {
                Debug.Assert(pointsOfInterest.ContainsKey(PointOfInterest.Home), "citizien has no home!");
                return pointsOfInterest[PointOfInterest.Home];
            }
        }

        public string Name
        {
            get
            {
                return firstName + " " + lastName;
            }
        }

        public PointOfInterest? CurrentDestination
        {
            get
            {
                return dailySchedule?.prevEvent?.place ?? null;
            }
        }

#if DEBUG
        public static string GetDestinationString(PointOfInterest poi)
        {
            var msg = "Driving ";
            switch (poi)
            {
            case Citizien.PointOfInterest.Home:
                msg += "home";
                break;
            case Citizien.PointOfInterest.School:
                msg += "to school";
                break;
            case Citizien.PointOfInterest.Work:
                msg += "to work";
                break;
            case Citizien.PointOfInterest.GroceryStore:
                msg += "to the grocery store";
                break;
            }

            return msg;
        }

        public static string GetDestinationName(PointOfInterest poi)
        {
            switch (poi)
            {
            case Citizien.PointOfInterest.Home:
                return "home";
            case Citizien.PointOfInterest.School:
                return "school";
            case Citizien.PointOfInterest.Work:
                return "work";
            case Citizien.PointOfInterest.GroceryStore:
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

        public Serialization.Citizien ToProtobuf()
        {
            var c = new Serialization.Citizien
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Age = (uint)age,
                Birthday = (uint)birthday,
                Female = female,
                Occupation = (Serialization.Citizien.Types.Occupation)occupation,
                Money = (float)money,
                Educated = educated,
                Happiness = (uint)happiness,
                CarID = car?.id ?? 0,

                CurrentPosition = currentPosition.ToProtobuf(),
                ActivePath = activePath?.ToProtobuf(),
                ScheduleIdx = scheduleIdx,
                ScheduleID = schedules[0]?.id ?? 0,
            };

            c.Relationships.AddRange(relationships.Select(r => new Serialization.Citizien.Types.Relationship
            {
                Kind = (Serialization.Citizien.Types.RelationshipKind)r.Key,
                CitizienId = r.Value.id,
            }));

            c.PointsOfInterest.AddRange(pointsOfInterest.Select(r => new Serialization.Citizien.Types.PointOfInterest
            {
                Kind = (Serialization.Citizien.Types.PointOfInterestKind)r.Key,
                BuildingId = (uint)r.Value.id,
            }));

            return c;
        }
    }
}