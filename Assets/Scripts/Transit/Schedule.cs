using System;
using UnityEngine;

namespace Transidious
{
    [Flags]
    public enum Weekday
    {
        None = 0x0,
        All = ~None,
        Weekends = Saturday | Sunday,
        Weekdays = ~Weekends,

        Sunday = 0x1,
        Monday = 0x2,
        Tuesday = 0x4,
        Wednesday = 0x8,
        Thursday = 0x10,
        Friday = 0x20,
        Saturday = 0x40,
    }

    public interface ISchedule
    {
        DateTime GetNextDeparture(DateTime currentTime);
        string ToString();
        Serialization.Schedule Serialize();
    }

    public class ContinuousSchedule : ISchedule
    {
        public DateTime firstDeparture;
        public TimeSpan interval;

        public ContinuousSchedule(DateTime firstDeparture, TimeSpan interval)
        {
            this.firstDeparture = firstDeparture;
            this.interval = interval;
        }

        public DateTime GetNextDeparture(DateTime currentTime)
        {
            if (currentTime <= firstDeparture)
            {
                return firstDeparture;
            }

            var diff = currentTime - firstDeparture;
            var nextDeparture = diff.RoundToInterval(interval);

            return firstDeparture.Add(nextDeparture);
        }

        public override string ToString()
        {
            return $"Continuous (starting at {Translator.GetDate(firstDeparture, Translator.DateFormat.DateTimeLong)} , every {interval.TotalSeconds}s)";
        }

        public Serialization.Schedule Serialize()
        {
            return new Serialization.Schedule
            {
                FirstDeparture = (ulong)firstDeparture.Ticks,
                Interval = (float)interval.TotalSeconds,
            };
        }

        public static ContinuousSchedule Deserialize(Serialization.Schedule sched)
        {
            return new ContinuousSchedule(new DateTime((long)sched.FirstDeparture),
                TimeSpan.FromSeconds(sched.Interval));
        }
    }

    public class Schedule : ISchedule
    {
        /// <summary>
        /// The time interval (in hours from 0-24) during which the day schedule is used.
        /// </summary>
        public Tuple<int, int> dayHours;

        /// <summary>
        /// The time interval (in hours from 0-24) during which the night schedule is used.
        /// </summary>
        public Tuple<int, int> nightHours;

        /// <summary>
        /// The time (in hours from 0-24) during which the line operates.
        /// </summary>
        public Tuple<int, int> operatingHours
        {
            get
            {
                return Tuple.Create(dayHours.Item1, nightHours.Item2);
            }
        }

        /// <summary>
        /// The days on which the line operates.
        /// </summary>
        public Weekday operatingDays;

        /// <summary>
        /// The interval (in minutes) in which trains depart during the day.
        /// </summary>
        public int dayInterval;

        /// <summary>
        /// The interval (in minutes) in which trains depart during the night.
        /// </summary>
        public int nightInterval;

        /// <summary>
        /// The default schedules for each transit system.
        /// </summary>
        static Schedule[] defaultSchedules;

        /// <summary>
        /// Returns the default schedule for a transit system.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Schedule GetDefaultSchedule(TransitType type)
        {
            if (defaultSchedules == null)
            {
                defaultSchedules = new Schedule[]
                {
                    // Bus
                    new Schedule
                    {
                        dayHours = Tuple.Create(4, 22),
                        nightHours = Tuple.Create(22, 1),
                        operatingDays = Weekday.All,
                        dayInterval = 20,
                        nightInterval = 30,
                    },

                    // Tram
                    new Schedule
                    {
                        dayHours = Tuple.Create(4, 22),
                        nightHours = Tuple.Create(22, 1),
                        operatingDays = Weekday.All,
                        dayInterval = 20,
                        nightInterval = 30,
                    },

                    // Subway
                    new Schedule
                    {
                        dayHours = Tuple.Create(4, 22),
                        nightHours = Tuple.Create(22, 1),
                        operatingDays = Weekday.All,
                        dayInterval = 5,
                        nightInterval = 15,
                    },

                    // Rail
                    new Schedule
                    {
                        dayHours = Tuple.Create(4, 22),
                        nightHours = Tuple.Create(22, 24),
                        operatingDays = Weekday.All,
                        dayInterval = 30,
                        nightInterval = 60,
                    },

                    // Ferry
                    new Schedule
                    {
                        dayHours = Tuple.Create(4, 22),
                        nightHours = Tuple.Create(22, 22),
                        operatingDays = Weekday.All,
                        dayInterval = 20,
                        nightInterval = 30,
                    },
                };
            }

            return defaultSchedules[(int)type];
        }

        public OffsetSchedule OffsetBy(float minutes)
        {
            return new OffsetSchedule(this, minutes);
        }

        public DelayedSchedule DelayedBy(DateTime firstDeparture, float delay)
        {
            return new DelayedSchedule(this, firstDeparture, delay);
        }

        public enum ActiveSchedule
        {
            None,
            Day,
            Night,
        }

        public ActiveSchedule GetScheduleAtTime(DateTime time)
        {
            var day = time.DayOfWeek;
            if (!operatingDays.HasFlag((Weekday)(1 << (int)day)))
            {
                return ActiveSchedule.None;
            }

            var hour = time.Hour;

            // Check if date is outside of operating hours.
            if (!IsBetween(hour, operatingHours))
            {
                return ActiveSchedule.None;
            }

            if (IsBetween(hour, nightHours))
            {
                return ActiveSchedule.Night;
            }

            return ActiveSchedule.Day;
        }

        DateTime GetNextDate(DateTime currentDate, int hour, DayOfWeek? day = null)
        {
            var newDate = currentDate.AddHours(System.Math.Abs(hour - currentDate.Hour) % 24);
            newDate = newDate.AddMinutes(-newDate.Minute);

            if (day == null)
            {
                return newDate;
            }

            var dateDiff = System.Math.Abs(day.Value - newDate.DayOfWeek) % 7;
            return newDate.AddDays(dateDiff);
        }

        bool IsBetween(int hour, Tuple<int, int> range)
        {
            if (range.Item2 > range.Item1)
            {
                return range.Item2 > hour && range.Item1 <= hour;
            }

            return !(range.Item1 > hour && range.Item2 <= hour);
        }

        public DateTime GetNextDeparture(DateTime currentTime)
        {
            var day = currentTime.DayOfWeek;
            if (!operatingDays.HasFlag((Weekday)(1 << (int)day)))
            {
                // Find next operating day.
                while (!operatingDays.HasFlag((Weekday)(1 << (int)day)))
                {
                    // Find next operating day.
                    day = (DayOfWeek)(((int)day + 1) % 7);
                }

                return GetNextDate(currentTime, operatingHours.Item1, day);
            }

            var hour = currentTime.Hour;

            // Check if date is outside of operating hours.
            if (!IsBetween(hour, operatingHours))
            {
                return GetNextDate(currentTime, operatingHours.Item1);
            }

            // Check if night schedule is used.
            DateTime nextDep;
            if (IsBetween(hour, nightHours))
            {
                var dayDuration = System.Math.Abs(dayHours.Item2 - dayHours.Item1) * 60;
                var dayTrips = (int)System.Math.Ceiling((float)dayDuration / dayInterval);
                var dayEndMinute = dayHours.Item1 * 60 + dayTrips * dayInterval;

                var nextDepartureMins = dayEndMinute % nightInterval;
                nextDep = currentTime.AddMinutes(nextDepartureMins);
            }
            else
            {
                var nextDepartureMins = ((currentTime.Minute + currentTime.Hour * 60) - dayHours.Item1 * 60) % dayInterval;
                nextDep = currentTime.AddMinutes(dayInterval - nextDepartureMins);
            }

            if (!IsBetween(nextDep.Hour, operatingHours))
            {
                return GetNextDate(currentTime, operatingHours.Item1);
            }

            return nextDep;
        }

        public DateTime[] NextDepartures(DateTime currentTime, int amount)
        {
            var result = new DateTime[amount];
            for (var i = 0; i < amount; ++i)
            {
                var departure = GetNextDeparture(currentTime);
                result[i] = departure;
                currentTime = departure.AddMinutes(1);
            }

            return result;
        }

        public Serialization.Schedule Serialize()
        {
            return null;
        }
    }

    public class OffsetSchedule : ISchedule
    {
        Schedule baseSchedule;
        float offsetMinutes;

        public OffsetSchedule(Schedule baseSchedule, float offsetMinutes)
        {
            this.baseSchedule = baseSchedule;
            this.offsetMinutes = offsetMinutes;
        }

        public DateTime GetNextDeparture(DateTime currentTime)
        {
            return baseSchedule.GetNextDeparture(currentTime).AddMinutes(offsetMinutes);
        }

        public Serialization.Schedule Serialize()
        {
            return null;
        }
    }

    public class DelayedSchedule : ISchedule
    {
        Schedule baseSchedule;
        DateTime firstDeparture;
        float delay;

        public DelayedSchedule(Schedule baseSchedule, DateTime firstDeparture, float delay)
        {
            this.baseSchedule = baseSchedule;
            this.firstDeparture = firstDeparture.AddMinutes(delay);
            this.delay = delay;
        }

        public DateTime GetNextDeparture(DateTime currentTime)
        {
            if (firstDeparture > currentTime)
            {
                return firstDeparture;
            }

            return baseSchedule.GetNextDeparture(currentTime.AddMinutes(-delay)).AddMinutes(delay);
        }

        public Serialization.Schedule Serialize()
        {
            return null;
        }
    }
}