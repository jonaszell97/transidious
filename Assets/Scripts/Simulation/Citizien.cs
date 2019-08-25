using UnityEngine;
using System.Collections.Generic;

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

            public Event[] events;

            static AbstractSchedule[] workSchedules;
            static AbstractSchedule[] elementarySchoolSchedules;
            static AbstractSchedule[] highSchoolSchedules;
            static AbstractSchedule[] universitySchedules;

            public static AbstractSchedule WorkSchedule
            {
                get
                {
                    if (workSchedules == null)
                    {
                        workSchedules = new AbstractSchedule[] {
                            new AbstractSchedule {
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
                            new AbstractSchedule {
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
                    }

                    return Utility.RandomElement(workSchedules);
                }
            }

            public static AbstractSchedule ElementarySchoolSchedule
            {
                get
                {
                    if (elementarySchoolSchedules == null)
                    {
                        elementarySchoolSchedules = new AbstractSchedule[] {
                            new AbstractSchedule {
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
                    }

                    return Utility.RandomElement(elementarySchoolSchedules);
                }
            }

            public static AbstractSchedule HighSchoolSchedule
            {
                get
                {
                    if (highSchoolSchedules == null)
                    {
                        highSchoolSchedules = new AbstractSchedule[] {
                            new AbstractSchedule {
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
                    }

                    return Utility.RandomElement(highSchoolSchedules);
                }
            }

            public static AbstractSchedule UniversitySchedule
            {
                get
                {
                    if (universitySchedules == null)
                    {
                        universitySchedules = new AbstractSchedule[] {
                            new AbstractSchedule {
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

                    return Utility.RandomElement(universitySchedules);
                }
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

        public SimulationController sim;
        public string firstName;
        public string lastName;
        public short age;
        public short birthday;
        public bool female;
        public Occupation occupation;
        public float money;
        public Car car;
        public bool educated;
        public byte happiness;
        public AbstractSchedule[] schedules;
        public Dictionary<Relationship, Citizien> relationships;
        public Dictionary<PointOfInterest, Building> pointsOfInterest;

        public Vector3 currentPosition;

        public ScheduledEvent dailySchedule;

        public static readonly float UniversityProbability = 0.7f;

        public Citizien(SimulationController sim)
        {
            this.sim = sim;
            this.lastName = RandomNameGenerator.LastName;

            var genderAndAge = RandomNameGenerator.GenderAndAge;
            this.female = genderAndAge.Item1;
            this.firstName = this.female ? RandomNameGenerator.FemaleFirstName
                                         : RandomNameGenerator.MaleFirstName;
            this.birthday = (short)Random.Range(0, 365);
            this.age = (short)genderAndAge.Item2;
            this.happiness = 100;

            this.car = sim.CreateCar(this, Vector3.zero, Utility.RandomColor);
            this.car.gameObject.SetActive(false);
            this.money = Random.Range(0f, 1000000f);

            this.pointsOfInterest = new Dictionary<PointOfInterest, Building>();
            this.relationships = new Dictionary<Relationship, Citizien>();

            var home = sim.RandomUnoccupiedBuilding(Building.Type.Residential);
            ++home.occupants;

            this.pointsOfInterest.Add(PointOfInterest.Home, home);

            AssignOccupation();
            CreateSchedules();

            var groceryStore = sim.RandomUnoccupiedBuilding(Building.Type.GroceryStore);
            if (groceryStore != null)
            {
                pointsOfInterest.Add(PointOfInterest.GroceryStore, groceryStore);
            }

            this.currentPosition = home.centroid;
            UpdateDailySchedule(sim.MinuteOfDay);

            Debug.Log(dailySchedule?.ToString());
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

        void AssignOccupation()
        {
            if (age < 7)
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
            if (sim.gameTime.DayOfYear != birthday)
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

        bool ScheduleEvent(ref ScheduledEvent prevEvent,
                           AbstractSchedule.Event e,
                           PathPlanning.PathPlanningResult path,
                           int startTime, int duration,
                           ref int earliestStartTime)
        {
            var nextEvent = new ScheduledEvent
            {
                startsAt = startTime,
                path = (path?.steps?.Count ?? 0) < 2 ? null : path,
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
            var schedule = schedules[sim.gameTime.Day];
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

                var pathOptions = new PathPlanning.PathPlanningOptions();
                var planner = new PathPlanning.PathPlanner(pathOptions);
                var path = planner.FindClosestDrive(sim.game.loadedMap, currentPosition, poi.centroid);
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

                // Don't schedule unflexible events we can't get to in time.
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

        public void Update(int minuteOfDay)
        {
            if (dailySchedule == null)
            {
                return;
            }
            if (minuteOfDay < dailySchedule.startsAt)
            {
                return;
            }

#if DEBUG
            Debug.Log("[" + Name + "] " + GetDestinationString(dailySchedule.place));
#endif

            if (dailySchedule.path != null)
            {
                sim.trafficSim.Drive(car, dailySchedule.path);
            }

            dailySchedule = dailySchedule.nextEvent;
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
    }
}