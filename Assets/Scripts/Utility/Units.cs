using System;
using UnityEngine;

namespace Transidious
{
    public enum CardinalDirection
    {
        North, South, West, East
    }

    public static class CardinalDirectionExtensions
    {
        public static bool IsHorizontal(this CardinalDirection dir)
        {
            return dir == CardinalDirection.East || dir == CardinalDirection.West;
        }
        
        public static bool IsParallelTo(this CardinalDirection dir, CardinalDirection other)
        {
            switch (dir)
            {
                case CardinalDirection.North:
                case CardinalDirection.South:
                    return other == CardinalDirection.North || other == CardinalDirection.South;
                
                case CardinalDirection.West:
                case CardinalDirection.East:
                    return other == CardinalDirection.West || other == CardinalDirection.East;
                default:
                    throw new System.ArgumentException($"Illegal enum value {dir}");
            }
        }
        
        public static CardinalDirection Opposite(this CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.South;
                case CardinalDirection.South: return CardinalDirection.North;
                case CardinalDirection.East: return CardinalDirection.West;
                case CardinalDirection.West: return CardinalDirection.East;
                default:
                    throw new System.ArgumentException($"Illegal enum value {dir}");
            }
        }

        public static CardinalDirection RotatedRight(this CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.East;
                case CardinalDirection.South: return CardinalDirection.West;
                case CardinalDirection.East: return CardinalDirection.South;
                case CardinalDirection.West: return CardinalDirection.North;
                default:
                    throw new System.ArgumentException($"Illegal enum value {dir}");
            }
        }

        public static CardinalDirection RotatedLeft(this CardinalDirection dir)
        {
            switch (dir)
            {
                case CardinalDirection.North: return CardinalDirection.West;
                case CardinalDirection.South: return CardinalDirection.East;
                case CardinalDirection.East: return CardinalDirection.North;
                case CardinalDirection.West: return CardinalDirection.South;
                default:
                    throw new System.ArgumentException($"Illegal enum value {dir}");
            }
        }
    }

    public static class TimeExtensions
    {
        public static TimeSpan Multiply(this TimeSpan lhs, float rhs)
        {
            return TimeSpan.FromSeconds(lhs.TotalSeconds * rhs);
        }

        public static TimeSpan RoundToInterval(this TimeSpan ts, TimeSpan interval)
        {
            return TimeSpan.FromSeconds(Mathf.Ceil((float)(ts.TotalSeconds / interval.TotalSeconds)) * interval.TotalSeconds);
        }
    }

    public struct Distance
    {
        /// The internal distance (in meters).
        private float _m;
        
        /// Whether or not this distance is zero.
        public bool IsZero => _m.Equals(0f);

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
        public static TimeSpan operator /(Distance d, Velocity v)
        {
            return TimeSpan.FromSeconds(d.Meters / v.MPS);
        }
        
        /// The time it takes to travel distance d at velocity v (in game).
        public static Velocity operator /(Distance d, TimeSpan ts)
        {
            return Velocity.FromMPS(d.Meters / (float)ts.TotalSeconds);
        }

        /// Add two distances.
        public static Distance operator +(Distance d1, Distance d2)
        {
            return FromMeters(d1.Meters + d2.Meters);
        }
        
        /// Subtract two distances.
        public static Distance operator -(Distance d1, Distance d2)
        {
            return FromMeters(d1.Meters - d2.Meters);
        }
    }

    public struct Velocity
    {
        /// The internal velocity (in meters per seconds).
        private float _mps;

        public static Velocity zero => new Velocity {_mps = 0};

        /// Whether or not this velocity is zero.
        public bool IsZero => _mps.Equals(0f);

        /// Create a velocity from real-time mps.
        public static Velocity FromRealTimeMPS(float mps)
        {
            return new Velocity {_mps = mps / SimulationController.BaseSpeedMultiplier};
        }

        /// Create a velocity from game-time mps.
        public static Velocity FromMPS(float mps)
        {
            return new Velocity {_mps = mps};
        }

        /// Create a velocity from real-time kph.
        public static Velocity FromRealTimeKPH(float kph)
        {
            return FromRealTimeMPS(kph * Math.Kph2Mps);
        }

        /// Create a velocity from game-time kph.
        public static Velocity FromKPH(float kph)
        {
            return FromMPS(kph * Math.Kph2Mps);
        }

        /// Return the velocity in mps.
        public float MPS => _mps;

        /// Return the real-time velocity in mps.
        public float RealTimeMPS => _mps * SimulationController.BaseSpeedMultiplier;

        /// Return the velocity in kph.
        public float KPH => _mps * Math.Mps2Kph;
        
        /// Return the real-time velocity in kph.
        public float RealTimeKPH => RealTimeMPS * Math.Mps2Kph;
        
        /// Calculate the distance driven at velocity v over interval t.
        public static Distance operator *(Velocity v, TimeSpan t)
        {
            return Distance.FromMeters(v.MPS * (float)t.TotalSeconds);
        }
        
        /// Add two velocities.
        public static Velocity operator +(Velocity v1, Velocity v2)
        {
            return Velocity.FromMPS(v1.MPS + v2.MPS);
        }
        
        /// Subtract two velocities.
        public static Velocity operator -(Velocity v1, Velocity v2)
        {
            return Velocity.FromMPS(v1.MPS - v2.MPS);
        }
        
        /// Multiply the velocity.
        public static Velocity operator *(Velocity v, float f)
        {
            return Velocity.FromMPS(v.MPS * f);
        }
        
        /// Divide the velocity.
        public static Velocity operator /(Velocity v, float f)
        {
            return Velocity.FromMPS(v.MPS / f);
        }
    }
    
    public struct Acceleration
    {
        /// The internal acceleration (in meters per second^2).
        private float _mps2;
        
        /// Whether or not this acceleration is zero.
        public bool IsZero => _mps2.Equals(0f);

        public static Acceleration zero => new Acceleration {_mps2 = 0};

        /// Create a velocity from real-time mps^2.
        public static Acceleration FromRealTimeMPS2(float mps2)
        {
            return new Acceleration {_mps2 = mps2 / SimulationController.BaseSpeedMultiplier};
        }

        /// Create a velocity from game-time mps^2.
        public static Acceleration FromMPS2(float mps2)
        {
            return new Acceleration {_mps2 = mps2};
        }

        /// Return the velocity in mps.
        public float MPS2 => _mps2;

        /// Return the real-time velocity in mps.
        public float RealTimeMPS2 => _mps2 * SimulationController.BaseSpeedMultiplier;

        /// Calculate the distance driven at velocity v over interval t.
        public static Velocity operator *(Acceleration a, TimeSpan t)
        {
            return Velocity.FromMPS(a.MPS2 * (float)t.TotalSeconds);
        }
        
        /// Add two accelerations.
        public static Acceleration operator +(Acceleration v1, Acceleration v2)
        {
            return Acceleration.FromMPS2(v1.MPS2 + v2.MPS2);
        }
        
        /// Subtract two accelerations.
        public static Acceleration operator -(Acceleration v1, Acceleration v2)
        {
            return Acceleration.FromMPS2(v1.MPS2 - v2.MPS2);
        }
        
        /// Multiply the acceleration.
        public static Acceleration operator *(Acceleration v, float f)
        {
            return Acceleration.FromMPS2(v.MPS2 * f);
        }
        
        /// Divide the acceleration.
        public static Acceleration operator /(Acceleration v, float f)
        {
            return Acceleration.FromMPS2(v.MPS2 / f);
        }
    }
}