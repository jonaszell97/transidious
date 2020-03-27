using System;
using UnityEngine;

namespace Transidious
{
    public struct GameTimeSpan
    {
        /// The internal time span.
        internal TimeSpan _ts;

        public static GameTimeSpan zero => new GameTimeSpan();

        /// Create a time span as the difference between two game time dates.
        public static GameTimeSpan Difference(DateTime t1, DateTime t2)
        {
            return new GameTimeSpan
            {
                _ts = t1 - t2,
            };
        }
        
        /// Create from real time seconds.
        public static GameTimeSpan FromRealTimeSeconds(float seconds)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromSeconds(seconds * 60),
            };
        }

        /// Create from game time seconds.
        public static GameTimeSpan FromGameTimeSeconds(float seconds)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromSeconds(seconds),
            };
        }

        /// Create from real time minutes.
        public static GameTimeSpan FromRealTimeMinutes(float minutes)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromMinutes(minutes * 60),
            };
        }

        /// Create from real time seconds.
        public static GameTimeSpan FromGameTimeMinutes(float minutes)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromMinutes(minutes),
            };
        }
        
        /// Create from real time minutes.
        public static GameTimeSpan FromRealTimeMilliseconds(float millis)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromMilliseconds(millis * 60),
            };
        }

        /// Create from real time seconds.
        public static GameTimeSpan FromGameTimeMilliseconds(float millis)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromMilliseconds(millis),
            };
        }

        public double TotalSeconds => _ts.TotalSeconds;
        public double TotalMinutes => _ts.TotalMinutes;
        public double TotalMilliseconds => _ts.TotalMilliseconds;

        public static GameTimeSpan operator+(GameTimeSpan lhs, GameTimeSpan rhs)
        {
            return new GameTimeSpan
            {
                _ts = lhs._ts + rhs._ts,
            };
        }

        public static GameTimeSpan operator-(GameTimeSpan lhs, GameTimeSpan rhs)
        {
            return new GameTimeSpan
            {
                _ts = lhs._ts - rhs._ts,
            };
        }

        public static GameTimeSpan operator*(GameTimeSpan lhs, float rhs)
        {
            return new GameTimeSpan
            {
                _ts = TimeSpan.FromSeconds(lhs.TotalSeconds * rhs),
            };
        }
    }

    public static class TimeExtensions
    {
        public static DateTime Add(this DateTime d, GameTimeSpan ts)
        {
            return d.Add(ts._ts);
        }

        public static DateTime Subtract(this DateTime d, GameTimeSpan ts)
        {
            return d.Subtract(ts._ts);
        }
    }

    public struct Distance
    {
        /// The internal distance (in meters).
        private float _m;
        
        public static Distance zero => new Distance {_m = 0};

        /// Distance between two points on the map.
        public static Distance Between(Vector2 p1, Vector2 p2)
        {
            return new Distance {_m = (p2 - p1).magnitude};
        }

        /// Create a distance from meters.
        public static Distance FromMeters(float m)
        {
            return new Distance {_m = m};
        }

        /// Create a distance from km.
        public static Distance FromKilometers(float km)
        {
            return new Distance {_m = km * 1000f};
        }

        /// Return the distance in meters.
        public float Meters => _m;

        /// Return the velocity in kph.
        public float Kilometers => _m / 1000f;

        /// The time it takes to travel distance d at velocity v (in game).
        public static GameTimeSpan operator /(Distance d, Velocity v)
        {
            return GameTimeSpan.FromRealTimeSeconds(d.Meters / v.MPS);
        }
    }

    public struct Velocity
    {
        /// The internal velocity (in meters per seconds).
        private float _mps;

        public static Velocity zero => new Velocity {_mps = 0};

        /// Create a velocity from real-time mps.
        public static Velocity FromMPS(float mps)
        {
            return new Velocity {_mps = mps};
        }

        /// Create a velocity from kph.
        public static Velocity FromKPH(float kph)
        {
            return new Velocity {_mps = kph * Math.Kph2Mps};
        }

        /// Return the velocity in mps.
        public float MPS => _mps;

        /// Return the velocity in kph.
        public float KPH => _mps * Math.Mps2Kph;
    }
}