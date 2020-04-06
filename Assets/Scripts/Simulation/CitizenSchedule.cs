using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.PathPlanning;
using UnityEngine;

namespace Transidious.Simulation
{
    public class Schedule
    {
        public enum EventType
        {
            /// The citizen is working or at school.
            Work,

            /// The citizen is performing a leisurely activity.
            Leisure,

            /// The citizen is exercising.
            Exercise,

            /// The citizen is shopping groceries.
            GroceryShopping,

            /// The citizen is relaxing.
            Relaxation,

            /// The citizen is sleeping.
            Sleep,
        }

        public class FixedEvent
        {
            /// The event type.
            public EventType type;

            /// The starting time (in minutes of day) of the event.
            public short startingTime;

            /// The duration (in minutes) of the event.
            public short duration;

            /// The earliest end time (in minutes of day) of the event.
            public int endTime => startingTime + duration;

            /// Whether or not the full duration of the event has to be performed, regardless of the starting time.
            public bool mustBePerformedFully;

            /// The weekdays on which this event takes place.
            public Weekday weekdays;
        }

        class FixedEventData
        {
            /// The fixed event.
            internal FixedEvent _fixedEvent;

            /// The computed location of the event.
            internal IMapObject _location;

            /// Whether or not the event has been performed today.
            internal bool _done;
        }

        public struct EventInfo
        {
            /// The time of departure.
            public DateTime departure;

            /// The time at which the next event should be scheduled.
            public DateTime endTime;

            /// The path to follow.
            public PathPlanningResult path;

            /// The type of event.
            public EventType type;

            /// The location where this event takes place.
            public IMapObject location;
    
            /// A textual description of the event for use in the UI.
            public string GetDescription => Translator.Get($"ui:event:{type}");
        
            /// The happiness bonus for every hour of the event that is performed.
            public float HappinessBonusPerHour
            {
                get
                {
                    switch (type)
                    {
                        default:
                        case EventType.Work:
                        case EventType.GroceryShopping:
                            return 0f;
                        case EventType.Exercise:
                            return 1f;
                        case EventType.Leisure:
                            return 2f;
                        case EventType.Relaxation:
                            return .5f;
                        case EventType.Sleep:
                            return .1f;
                    }
                }
            }

            /// The happiness penalty for missing an hour of the event.
            public float HappinessPenaltyPerHourMissed
            {
                get
                {
                    switch (type)
                    {
                        case EventType.Sleep:
                            return .1f;
                        case EventType.Work:
                            return 1f;
                        case EventType.GroceryShopping:
                            return 5f;
                        default:
                            return 0f;
                    }
                }
            }

            /// The happiness penalty for missing the entire event.
            public float PenaltyForMissing =>
                HappinessPenaltyPerHourMissed
                    * (float)(endTime - departure + (path?.duration ?? TimeSpan.Zero)).TotalHours;

            /// The work bonus per hour of the event.
            public float RemainingWorkBonusPerHour => type == EventType.Work
                ? -(100f / 8f)
                : +(100f / 16f);

            /// The energy bonus per hour of the event.
            public float EnergyBonusPerHour => GetEnergyBonusPerHour(type);

            /// The energy bonus per hour of an event type.
            public static float GetEnergyBonusPerHour(EventType type)
            {
                switch (type)
                {
                    default:
                    case EventType.Work:
                        return -(30f / 8f);
                    case EventType.GroceryShopping:
                        return -(20f / 8f);
                    case EventType.Exercise:
                        return -(100f / 8f);
                    case EventType.Leisure:
                        return -(20f / 8f);
                    case EventType.Relaxation:
                        return +(40f / 8f);
                    case EventType.Sleep:
                        return +(100f / 8f);
                }
            }

            /// A textual description of this event.
            public string DebugDescription =>
                $"[{departure.ToShortTimeString()} - {endTime.ToShortTimeString()}] {type} at {location?.Name ?? "current location"}";
        }

        /// Utility array for random event generation.
        private static readonly EventType[] _leisureEventTypes = new[]
            {EventType.Sleep, EventType.Relaxation, EventType.Exercise, EventType.Leisure};

        /// Seed PRNG for reproducibility.
        private static System.Random _random = new System.Random();

        /// The citizen this schedule is for.
        public readonly Citizen citizen;

        /// Fixed events along with their starting time and duration.
        private readonly FixedEventData[] _fixedEvents;

        /// The path planner.
        private readonly PathPlanner _pathPlanner;

        /// The next fixed event.
        private Tuple<EventInfo, FixedEventData> _nextFixedEvent;

        /// Create a new schedule.
        public Schedule(Citizen citizen, FixedEvent[] fixedEvents)
        {
            this.citizen = citizen;

            if (fixedEvents != null && fixedEvents.Length > 0)
            {
                _fixedEvents = fixedEvents.Select(fe => new FixedEventData
                {
                    _fixedEvent = fe,
                }).ToArray();

                Array.Sort(
                    _fixedEvents, (fe1, fe2) =>
                        fe1._fixedEvent.startingTime.CompareTo(fe2._fixedEvent.startingTime));
            }

            this._pathPlanner = new PathPlanner(citizen.transitPreferences);
        }

        /// Move on to the next day to reset the fixed events that have already happened.
        void Reset()
        {
            if (_fixedEvents == null)
                return;
            
            foreach (var t in _fixedEvents)
            {
                t._done = false;
                t._location = null;
            }
        }

        Velocity ApproximateVelocity
        {
            get
            {
                if (citizen.car != null)
                {
                    return Velocity.FromRealTimeKPH(20);
                }
                
                // FIXME transit ticket?

                return citizen.WalkingSpeed;
            }
        }

        /// Return the next scheduled fixed event along with the time of departure and the end time.
        Tuple<EventInfo, FixedEventData> GetNextFixedEvent(DateTime currentTime, int addedDays = 0)
        {
            if (_fixedEvents == null)
            {
                return null;
            }

            var wd = currentTime.GetWeekday().AddDays(addedDays);
            foreach (var fe in _fixedEvents)
            {
                // Check if the event was already done today.
                if (fe._done && addedDays == 0)
                {
                    continue;
                }

                // Check if the event takes place on the current weekday.
                if (!fe._fixedEvent.weekdays.HasFlag(wd))
                {
                    continue;
                }

                // Determine a location for the event.
                if (fe._location == null)
                {
                    fe._location = GetDestination(citizen, fe._fixedEvent.type);
                }

                // Approximate how long it will take to get to the event.
                var approximateDistance = Distance.Between(citizen.currentPosition, fe._location.Centroid);
                var approximateTravelTime = approximateDistance / ApproximateVelocity;

                // Approximate how much time we have left until we have to leave for the event.
                var startingTime = currentTime.Date.AddDays(addedDays).AddMinutes(fe._fixedEvent.startingTime);
                var departure = startingTime.Subtract(approximateTravelTime);

                DateTime endTime;
                if (departure >= currentTime)
                {
                    endTime = startingTime.AddMinutes(fe._fixedEvent.duration);
                }
                else if (fe._fixedEvent.mustBePerformedFully)
                {
                    departure = currentTime;
                    endTime = currentTime.AddMinutes(fe._fixedEvent.duration);
                }
                else
                {
                    departure = currentTime;
                    endTime = startingTime.AddMinutes(fe._fixedEvent.duration);
                }

                return Tuple.Create(new EventInfo
                {
                    departure = departure,
                    endTime = endTime,
                    type = fe._fixedEvent.type,
                    location = fe._location,
                }, fe);
            }

            return GetNextFixedEvent(currentTime, addedDays + 1);
        }

        int GetMaxPossibleDuration(EventType type)
        {
            var bonus = EventInfo.GetEnergyBonusPerHour(type);
            if (bonus >= 0f)
            {
                return Int32.MaxValue;
            }

            return (int)((citizen.energy / -bonus) * 60f);
        }

        /// Schedule a new leisurely event.
        EventInfo GetLeisureEvent(DateTime currentTime, int minutesUntilNextEvent)
        {
            if (minutesUntilNextEvent < 30)
            {
                return new EventInfo
                {
                    departure = currentTime,
                    endTime = currentTime.AddMinutes(minutesUntilNextEvent),
                    type = EventType.Relaxation,
                    location = null,
                    path = null,
                };
            }

            EventType type;
            int duration;

            // If the energy level is below 25%, go to sleep.
            if (citizen.energy < .25f)
            {
                type = EventType.Sleep;
                duration = System.Math.Min(9*60, minutesUntilNextEvent);
            }
            else if (citizen.remainingWork >= 75f)
            {
                type = EventType.Work;
                duration = System.Math.Min((int)Mathf.Ceil(citizen.remainingWork / (100f / 8f) * 60f),
                                           minutesUntilNextEvent);
            }
            else
            {
                type = Utility.RandomElement(_leisureEventTypes);
                duration = Mathf.Min(GetRandomDuration(type), minutesUntilNextEvent, GetMaxPossibleDuration(type));
            }

            var location = GetDestination(citizen, type);

            _pathPlanner.Reset(currentTime);
            var path = _pathPlanner.FindClosestPath(
                SaveManager.loadedMap, 
                citizen.currentPosition,
                location.Centroid);

            return new EventInfo
            {
                departure = currentTime,
                endTime = currentTime.Add(path.duration).AddMinutes(duration),
                type = type,
                location = location,
                path = path,
            };
        }

        /// Suggest the next event based on the time of day.
        public EventInfo GetNextEvent(DateTime currentTime, bool newDay)
        {
            if (newDay)
            {
                Reset();
            }

            if (_fixedEvents == null)
            {
                // Isn't life grand?
                return GetLeisureEvent(currentTime, Int32.MaxValue);
            }

            if (_nextFixedEvent == null)
            {
                _nextFixedEvent = GetNextFixedEvent(currentTime);
                Debug.Assert(_nextFixedEvent != null);
            }

            var nextFixedEvent = _nextFixedEvent.Item1;

            var timeLeft = (int)(nextFixedEvent.departure - currentTime).TotalMinutes;
            if (timeLeft > 0)
            {
                return GetLeisureEvent(currentTime, timeLeft);
            }

            _nextFixedEvent.Item2._done = true;
            _nextFixedEvent = null;

            _pathPlanner.Reset(currentTime);
            nextFixedEvent.path = _pathPlanner.FindClosestPath(
                SaveManager.loadedMap, 
                citizen.currentPosition,
                nextFixedEvent.location.Centroid);

            return nextFixedEvent;
        }

        /// Return a possible location for this event for a specific citizen.
        private static IMapObject GetDestination(Citizen c, EventType type)
        {
            switch (type)
            {
                case EventType.Work:
                    return c.GetPointOfInterest(Citizen.PointOfInterest.Work, Citizen.PointOfInterest.School);
                case EventType.Leisure:
                    return GetRandomDestination(c, type);
                case EventType.Exercise:
                {
                    var gym = c.GetPointOfInterest(Citizen.PointOfInterest.Gym);
                    if (gym == null || _random.NextFloat() <= .5f)
                    {
                        return GetRandomDestination(c, type);
                    }

                    return gym;
                }
                case EventType.GroceryShopping:
                {
                    var gs = c.GetPointOfInterest(Citizen.PointOfInterest.GroceryStore);
                    if (gs == null || _random.NextFloat() <= .5f)
                    {
                        return GetRandomDestination(c, type);
                    }

                    return gs;
                }
                case EventType.Relaxation:
                    if (_random.NextFloat() <= .75f)
                    {
                        return GetRandomDestination(c, type);
                    }

                    return c.Home;
                case EventType.Sleep:
                    return c.Home;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// Return a randomized location for this event for a specific citizen.
        private static IMapObject GetRandomDestination(Citizen c, EventType EventType)
        {
            var map = SaveManager.loadedMap;
            
            bool found;
            IMapObject obj;

            // Move this around a bit so we don't always end up at the same places.
            var pos = c.currentPosition;
            pos = new Vector2(
                Mathf.Clamp(pos.x + _random.NextFloat(50f, 150f), map.minX, map.maxX),
                Mathf.Clamp(pos.y + _random.NextFloat(50f, 150f), map.minY, map.maxY));

            switch (EventType)
            {
                case EventType.Leisure:
                    found = SaveManager.loadedMap.FindClosest(out obj, pos, o =>
                    {
                        switch (o)
                        {
                            case Building b:
                                return b.type == Building.Type.Church
                                       || b.type == Building.Type.Leisure
                                       || b.type == Building.Type.Sight
                                       || b.type == Building.Type.Stadium
                                       || b.type == Building.Type.Shop;
                            case NaturalFeature f:
                                return f.type == NaturalFeature.Type.Beach
                                       || f.type == NaturalFeature.Type.Park
                                       || f.type == NaturalFeature.Type.Zoo
                                       || f.type == NaturalFeature.Type.Green;
                            default:
                                return false;
                        }
                    });

                    break;
                case EventType.Exercise:
                    found = SaveManager.loadedMap.FindClosest(out obj, pos, o =>
                    {
                        switch (o)
                        {
                            case Building b:
                                return b.type == Building.Type.Leisure;
                            case NaturalFeature f:
                                return f.type == NaturalFeature.Type.Park
                                       || f.type == NaturalFeature.Type.Green;
                            default:
                                return false;
                        }
                    });

                    break;
                case EventType.GroceryShopping:
                    found = SaveManager.loadedMap.FindClosest(out obj, pos, o =>
                    {
                        switch (o)
                        {
                            case Building b:
                                return b.type == Building.Type.GroceryStore;
                            default:
                                return false;
                        }
                    });

                    break;
                case EventType.Relaxation:
                    found = SaveManager.loadedMap.FindClosest(out obj, pos, o =>
                    {
                        switch (o)
                        {
                            case Building b:
                                return b.type == Building.Type.Church;
                            case NaturalFeature f:
                                return f.type == NaturalFeature.Type.Beach
                                       || f.type == NaturalFeature.Type.Park
                                       || f.type == NaturalFeature.Type.Green;
                            default:
                                return false;
                        }
                    });

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!found)
            {
                Debug.LogError("no suitable location found!");
                return c.Home;
            }

            return obj;
        }

        /// Return a random duration in minutes for this type of event.
        private static int GetRandomDuration(EventType type)
        {
            switch (type)
            {
                case EventType.Work:
                    return _random.Next(8 * 60, 10 * 60);
                case EventType.Leisure:
                    return _random.Next(30, 300);
                case EventType.Exercise:
                    return _random.Next(30, 120);
                case EventType.GroceryShopping:
                    return _random.Next(30, 60);
                case EventType.Relaxation:
                    return _random.Next(30, 360);
                case EventType.Sleep:
                    // This is only used for naps.
                    return _random.Next(30, 120);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

#if DEBUG
        public static void Reseed(int seed)
        {
            _random = new System.Random(seed);
        }
#endif
    }

    /*
    public class ScheduleEvent
    {
        public enum Kind
        {
            /// The citizen is working or at school.
            Work,

            /// The citizen is performing a leisurely activity.
            Leisure,

            /// The citizen is exercising.
            Exercise,

            /// The citizen is shopping groceries.
            GroceryShopping,

            /// The citizen is relaxing.
            Relaxation,

            /// The citizen is sleeping.
            Sleep,
        }

        /// The unique ID of this schedule event.
        public readonly int ID;

        /// The event kind.
        public readonly Kind kind;

        /// The earliest possible start time of the event.
        public readonly short earliestStart;

        /// The latest possible start time of the event.
        public readonly short latestStart;

        /// The preferred duration of the event.
        public readonly short preferredDuration;

        /// The location where this event takes place.
        public readonly Citizen.PointOfInterest? location;

        /// The next event(s) that can follow this one, along with their probabilities.
        public readonly Tuple<float, ScheduleEvent>[] nextEvents;

        /// Memberwise C'tor.
        public ScheduleEvent(int id, Kind kind, short earliestStart, short latestStart, short preferredDuration,
                             Citizen.PointOfInterest? location, Tuple<float, ScheduleEvent>[] nextEvents)
        {
            this.ID = id;
            this.kind = kind;
            this.earliestStart = earliestStart;
            this.latestStart = latestStart;
            this.preferredDuration = preferredDuration;
            this.location = location;
            this.nextEvents = nextEvents;
        }

        /// Get the event after this one.
        public ScheduleEvent Next
        {
            get
            {
                Debug.Assert(nextEvents.Length > 0, "no next event!");

                var rnd = _random.Next(0f, 1f);
                foreach (var e in nextEvents)
                {
                    if (rnd >= e.Item1)
                    {
                        return e.Item2;
                    }
                }

                Debug.Assert(false, "invalid event probabilities");
                return null;
            }
        }

        /// A textual description of the event for use in the UI.
        public string Description => Translator.Get($"ui:event:{kind}");

        /// The happiness bonus for every hour of the event that is performed.
        public float HappinessBonusPerMinute
        {
            get
            {
                switch (kind)
                {
                    default:
                    case Kind.Work:
                    case Kind.GroceryShopping:
                        return 0f;
                    case Kind.Exercise:
                        return 1f;
                    case Kind.Leisure:
                        return 2f;
                    case Kind.Relaxation:
                        return .5f;
                    case Kind.Sleep:
                        return .1f;
                }
            }
        }

        /// The happiness penalty for missing an hour of the event.
        public float HappinessPenaltyPerMinuteMissed
        {
            get
            {
                switch (kind)
                {
                    case Kind.Sleep:
                        return .01f;
                    case Kind.Work:
                        return 1f;
                    case Kind.GroceryShopping:
                        return 5f;
                    default:
                        return 0f;
                }
            }
        }

        /// Return a possible location for this event for a specific citizen.
        public IMapObject GetDestination(Citizen c)
        {
            switch (kind)
            {
                case Kind.Work:
                    return c.GetRandomPointOfInterest(Citizen.PointOfInterest.Work, Citizen.PointOfInterest.School);
                case Kind.Leisure:
                    return c.GetPointOfInterest(Citizen.PointOfInterest.Park);
                case Kind.Exercise:
                    return c.GetRandomPointOfInterest(Citizen.PointOfInterest.Gym, Citizen.PointOfInterest.Park);
                case Kind.GroceryShopping:
                    return c.GetPointOfInterest(Citizen.PointOfInterest.GroceryStore);
                case Kind.Relaxation:
                    return c.GetRandomPointOfInterest(Citizen.PointOfInterest.Home, Citizen.PointOfInterest.Park);
                case Kind.Sleep:
                    return c.Home;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class ScheduleManager
    {
        public class ScheduleBuilder
        {
            /// Reference to the schedule manager.
            internal ScheduleManager _manager;

            /// The unique ID of this schedule event.
            internal int _id;

            /// The event kind.
            internal ScheduleEvent.Kind _kind;

            /// The earliest possible start time of the event.
            internal short _earliestStart;

            /// The latest possible start time of the event.
            internal short _latestStart;

            /// The preferred duration of the event.
            internal short _preferredDuration;

            /// The location where this event takes place.
            internal Citizen.PointOfInterest? _location;

            /// The previous event.
            internal ScheduleBuilder[] _prevEvents;

            /// The next event(s) that can follow this one, along with their probabilities.
            internal List<Tuple<ScheduleBuilder, float>> _nextEvents;

            /// Public C'tor.
            public ScheduleBuilder(ScheduleManager manager, bool initial = false)
            {
                _manager = manager;
                _id = initial ? 0 : ++manager._lastAssignedID;
                _nextEvents = new List<Tuple<ScheduleBuilder, float>>();
            }

            /// Add a follow up event.
            public ScheduleBuilder AddFollowup(Tuple<ScheduleEvent.Kind, int, int, int> e)
            {
                var followup = new ScheduleBuilder(_manager)
                {
                    _kind = e.Item1,
                    _earliestStart = (short)e.Item2,
                    _latestStart = (short)e.Item3,
                    _preferredDuration = (short)e.Item4,
                    _prevEvents = new []{ this },
                };

                _nextEvents.Add(Tuple.Create(followup, 1f));
                return followup;
            }

            /// Add a selection of multiple followup events with custom probabilities.
            public MultiScheduleBuilder Branch(
                params Tuple<float, ScheduleEvent.Kind, int, int, int>[] events)
            {
                var convergingEvents = new ScheduleBuilder[events.Length];
                for (var i = 0; i < events.Length; ++i)
                {
                    var e = events[i];
                    var followup = new ScheduleBuilder(_manager)
                    {
                        _kind = e.Item2,
                        _earliestStart = (short)e.Item3,
                        _latestStart = (short)e.Item4,
                        _preferredDuration = (short)e.Item5,
                        _prevEvents = new []{ this },
                    };

                    _nextEvents.Add(Tuple.Create(followup, e.Item1));
                    convergingEvents[i] = followup;
                }

                return new MultiScheduleBuilder
                {
                    _builders = convergingEvents,
                };
            }

            /// Add a selection of multiple followup events with equal probabilities.
            public MultiScheduleBuilder Branch(
                params Tuple<ScheduleEvent.Kind, int, int, int>[] events)
            {
                var prob = 1f / events.Length;
                return Branch(events.Select(t => Tuple.Create(
                    prob, t.Item1, t.Item2, t.Item3, t.Item4)).ToArray());
            }

            private static ScheduleEvent GetOrCreateEvent(ScheduleBuilder builder)
            {
                if (builder._id == 0)
                {
                    Debug.Assert(builder._nextEvents.Count == 1);
                    return GetOrCreateEvent(builder._nextEvents.First().Item1);
                }

                if (builder._manager._events.TryGetValue(builder._id, out ScheduleEvent se))
                {
                    return se;
                }

                var nextEvents = new Tuple<float, ScheduleEvent>[builder._nextEvents.Count];
                se = new ScheduleEvent(builder._id, builder._kind, builder._earliestStart, builder._latestStart,
                    builder._preferredDuration, builder._location, nextEvents);

                builder._manager._events[se.ID] = se;

                var i = 0;
                var prob = 0f;

                foreach (var next in builder._nextEvents)
                {
                    prob += next.Item2;
                    se.nextEvents[i++] = Tuple.Create(prob, GetOrCreateEvent(next.Item1));
                }

                return se;
            }

            /// Finalize the schedule.
            public ScheduleEvent Build(bool loop = true)
            {
                var firstEvent = this;
                while (firstEvent._prevEvents != null)
                {
                    firstEvent = firstEvent._prevEvents[0];
                }

                if (loop)
                {
                    _nextEvents = new List<Tuple<ScheduleBuilder, float>>
                    {
                        Tuple.Create(firstEvent, 1f),
                    };
                }

                return GetOrCreateEvent(firstEvent);
            }
        }

        public class MultiScheduleBuilder
        {
            internal ScheduleBuilder[] _builders;
            
            /// Add a follow up event.
            public ScheduleBuilder AddFollowup(Tuple<ScheduleEvent.Kind, int, int, int> e)
            {
                var followup = new ScheduleBuilder(_builders[0]._manager)
                {
                    _kind = e.Item1,
                    _earliestStart = (short)e.Item2,
                    _latestStart = (short)e.Item3,
                    _preferredDuration = (short)e.Item4,
                    _prevEvents = _builders,
                };

                foreach (var b in _builders)
                {
                    b._nextEvents.Add(Tuple.Create(followup, 1f));
                }

                return followup;
            }

            /// Add a selection of multiple followup events with custom probabilities.
            public MultiScheduleBuilder Branch(
                params Tuple<float, ScheduleEvent.Kind, int, int, int>[] events)
            {
                var builders = new List<ScheduleBuilder>();
                foreach (var e in events)
                {
                    var followup = new ScheduleBuilder(_builders[0]._manager)
                    {
                        _kind = e.Item2,
                        _earliestStart = (short)e.Item3,
                        _latestStart = (short)e.Item4,
                        _preferredDuration = (short)e.Item5,
                        _prevEvents = _builders,
                    };

                    builders.Add(followup);
                }

                foreach (var b in _builders)
                {
                    for (var i = 0; i < events.Length; ++i)
                    {
                        b._nextEvents.Add(Tuple.Create(builders[i], events[i].Item1));
                    }
                }

                return new MultiScheduleBuilder
                {
                    _builders = builders.ToArray(),
                };
            }

            /// Add a selection of multiple followup events with equal probabilities.
            public MultiScheduleBuilder Branch(
                params Tuple<ScheduleEvent.Kind, int, int, int>[] events)
            {
                var prob = 1f / events.Length;
                return Branch(events.Select(t => Tuple.Create(
                    prob, t.Item1, t.Item2, t.Item3, t.Item4)).ToArray());
            }

            /// Finalize the schedule.
            public ScheduleEvent Build(bool loop = true)
            {
                return _builders[0].Build(loop);
            }
        }

        /// The last assigned schedule ID.
        private int _lastAssignedID = 0;

        /// Map of events by their ID.
        private Dictionary<int, ScheduleEvent> _events;

        /// Map of generated schedules.
        public Dictionary<Tuple<Citizen.Occupation, System.DayOfWeek>, ScheduleEvent[]> schedules;

        /// C'tor.
        public ScheduleManager()
        {
            _events = new Dictionary<int, ScheduleEvent>();
            schedules = new Dictionary<Tuple<Citizen.Occupation, System.DayOfWeek>, ScheduleEvent[]>();
            
            InitializeRandomSchedules();
        }

        /// Create a new schedule builder.
        public ScheduleBuilder CreateSchedule()
        {
            return new ScheduleBuilder(this, true);
        }

        public ScheduleEvent GetSchedule(Citizen.Occupation occupation, System.DayOfWeek weekday)
        {
            return Utility.RandomElement(schedules[Tuple.Create(occupation, weekday)]);
        }

        void InitializeRandomSchedules()
        {
            var sevenToThree = InitializeWorkerSchedule(420, 900);
            var eightToFour = InitializeWorkerSchedule(480, 960);
            var nineToFive = InitializeWorkerSchedule(540, 1020);
            var weekend = InitializeWeekendSchedule();

            foreach (var wd in new[] { System.DayOfWeek.Monday, System.DayOfWeek.Tuesday, System.DayOfWeek.Wednesday, System.DayOfWeek.Thursday, System.DayOfWeek.Friday })
            {
                schedules[Tuple.Create(Citizen.Occupation.Worker, wd)] = new[]
                {
                    sevenToThree,
                    eightToFour,
                    nineToFive,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.Trainee, wd)] = new[]
                {
                    sevenToThree,
                    eightToFour,
                    nineToFive,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.Kindergardener, wd)] = new[]
                {
                    eightToFour,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.ElementarySchoolStudent, wd)] = new[]
                {
                    eightToFour,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.HighSchoolStudent, wd)] = new[]
                {
                    sevenToThree,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.UniversityStudent, wd)] = new[]
                {
                    sevenToThree,
                    eightToFour,
                    nineToFive,
                };
                
                schedules[Tuple.Create(Citizen.Occupation.Retired, wd)] = new[]
                {
                    weekend,
                };
            }
            
            foreach (var wd in new[] { System.DayOfWeek.Saturday, System.DayOfWeek.Sunday })
            {
                foreach (var occupation in Enum.GetValues(typeof(Citizen.Occupation)))
                {
                    schedules[Tuple.Create((Citizen.Occupation)occupation, wd)] = new[]
                    {
                        weekend,
                    };
                }
            }
        }

        ScheduleEvent InitializeWorkerSchedule(int from, int to)
        {
            return CreateSchedule()
                .AddFollowup(
                    // 8:00 AM - 4:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Work, from, from, 480)
                )
                .Branch(
                    // 4:00 PM - 5:00 PM
                    Tuple.Create(0.3f, ScheduleEvent.Kind.Exercise, from + 480, from + 480 + 30, 60),
                    // 4:00 PM - 5:30 PM
                    Tuple.Create(0.4f, ScheduleEvent.Kind.Leisure, from + 480, from + 480 + 60, 90),
                    // 4:00 PM - 4:30 PM
                    Tuple.Create(0.3f, ScheduleEvent.Kind.GroceryShopping, from + 480, from + 480 + 10, 30)
                )
                .AddFollowup(
                    // 4:30 PM - 10:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Relaxation, from + 480 + 30, 1290, 1320 - (from + 480 + 30))
                )
                .AddFollowup(
                    // 10:00 PM - 07:00 AM
                    Tuple.Create(ScheduleEvent.Kind.Sleep, 10*60, 12*60, 9*60)
                )
                .Build();
        }

        ScheduleEvent InitializeWeekendSchedule()
        {
            return CreateSchedule()
                .AddFollowup(
                    // 8:00 AM - 10:00 AM
                    Tuple.Create(ScheduleEvent.Kind.Relaxation, 8*60, 8*60, 2*60)
                )
                .Branch(
                    // 10:00 AM - 3:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Relaxation, 10*60, 14*60, 5*60),
                    
                    // 10:00 AM - 3:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Exercise, 10*60, 14*60, 60),
                    
                    // 10:00 AM - 3:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Leisure, 10*60, 14*60, 5*60)
                )
                .Branch(
                    // 03:00 PM - 10:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Relaxation, 15*60, 19*60, 5*60),

                    // 03:00 PM - 10:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Exercise, 15*60, 19*60, 60),

                    // 03:00 PM - 10:00 PM
                    Tuple.Create(ScheduleEvent.Kind.Leisure, 15*60, 19*60, 5*60)
                )
                .AddFollowup(
                    // 10:00 PM - 07:00 AM
                    Tuple.Create(ScheduleEvent.Kind.Sleep, 10*60, 12*60, 9*60)
                )
                .Build();
        }
    }
    */
}