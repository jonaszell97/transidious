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
                var approximateDistance = Distance.Between(citizen.currentPosition, fe._location.VisualCenter);
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
                type = RNG.RandomElement(_leisureEventTypes);
                duration = Mathf.Min(GetRandomDuration(type), minutesUntilNextEvent, GetMaxPossibleDuration(type));
            }

            var location = GetDestination(citizen, type);

            _pathPlanner.Reset();
            var path = _pathPlanner.FindClosestPath(
                SaveManager.loadedMap, 
                citizen.currentPosition,
                location.VisualCenter,
                currentTime);

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

            _pathPlanner.Reset();
            nextFixedEvent.path = _pathPlanner.FindClosestPath(
                SaveManager.loadedMap, 
                citizen.currentPosition,
                nextFixedEvent.location.VisualCenter,
                currentTime);

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
                    if (gym == null || RNG.value <= .5f)
                    {
                        return GetRandomDestination(c, type);
                    }

                    return gym;
                }
                case EventType.GroceryShopping:
                {
                    var gs = c.GetPointOfInterest(Citizen.PointOfInterest.GroceryStore);
                    if (gs == null || RNG.value <= .5f)
                    {
                        return GetRandomDestination(c, type);
                    }

                    return gs;
                }
                case EventType.Relaxation:
                    if (RNG.value <= .75f)
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
                Mathf.Clamp(pos.x + RNG.Next(50f, 150f), map.minX, map.maxX),
                Mathf.Clamp(pos.y + RNG.Next(50f, 150f), map.minY, map.maxY));

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
                    return RNG.Next(8 * 60, 10 * 60);
                case EventType.Leisure:
                    return RNG.Next(30, 300);
                case EventType.Exercise:
                    return RNG.Next(30, 120);
                case EventType.GroceryShopping:
                    return RNG.Next(30, 60);
                case EventType.Relaxation:
                    return RNG.Next(30, 360);
                case EventType.Sleep:
                    // This is only used for naps.
                    return RNG.Next(30, 120);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}