using System;
using UnityEngine;
using Random = System.Random;

namespace Transidious
{
    public class Car
    {
        public class DrivingBehaviour
        {
            /// Comfortable braking deceleration. (in m/s^2)
            public float B;

            /// Desired time headway: the minimum possible time to the vehicle in front (in seconds).
            public float T;

            /// Factor to apply to the speed limit.
            public float SpeedLimitFactor;
        }

        private static readonly DrivingBehaviour[] _drivingBehaviours =
        {
            // Normal
            new DrivingBehaviour
            {
                B = 2.0f,
                T = 1.5f,
                SpeedLimitFactor = 1f,
            },
            new DrivingBehaviour
            {
                B = 1.9f,
                T = 1.6f,
                SpeedLimitFactor = 1.05f,
            },
            
            // Careful
            new DrivingBehaviour
            {
                B = 1.25f,
                T = 1.75f,
                SpeedLimitFactor = .85f,
            },
            new DrivingBehaviour
            {
                B = 1.20f,
                T = 1.80f,
                SpeedLimitFactor = .9f,
            },
            
            // Reckless
            new DrivingBehaviour
            {
                B = 3.0f,
                T = 1.0f,
                SpeedLimitFactor = 1.25f,
            },
            new DrivingBehaviour
            {
                B = 2.80f,
                T = 0.80f,
                SpeedLimitFactor = 1.30f,
            },
        };
        
        static readonly Tuple<float, int>[] _behaviourDistribution =
        {
            // Normal
            Tuple.Create(0.25f, 0),
            Tuple.Create(0.25f, 1),
            
            // Careful
            Tuple.Create(0.125f, 2),
            Tuple.Create(0.125f, 3),
            
            // Reckless
            Tuple.Create(0.125f, 4),
            Tuple.Create(0.125f, 5),
        };

        static readonly Tuple<float, Color>[] _colorDistribution =
        {
            new Tuple<float, Color>(0.10f, new Color(.7f, .7f, .7f)),    // silver
            new Tuple<float, Color>(0.10f, new Color(.2f, .36f, .92f)),  // blue
            new Tuple<float, Color>(0.27f, Color.black),
            new Tuple<float, Color>(0.10f, new Color(.57f, 0f, 0f)),     // red
            new Tuple<float, Color>(0.05f, new Color(.82f, .71f, .55f)), // tan
            new Tuple<float, Color>(0.03f, new Color(.082f, .305f, 0f)), // green
        };

        public enum CarModel
        {
            Sedan1 = 1,
            Sedan2 = 2,
            Sedan3 = 3,
            Sedan4 = 4,
            Sports = 5,
            Pickup = 6,
        }

        static readonly Tuple<float, CarModel>[] _carModelDistribution =
        {
            Tuple.Create(.22f, CarModel.Sedan1),
            Tuple.Create(.17f, CarModel.Sedan2),
            Tuple.Create(.18f, CarModel.Sedan3),
            Tuple.Create(.18f, CarModel.Sedan4),
            Tuple.Create(.20f, CarModel.Pickup),
            Tuple.Create(.05f, CarModel.Sports),
        };

        static uint _lastAssignedId = 0;

        /// The ID of this car.
        public uint id;

        /// The model ID of this car.
        public CarModel model;

        /// The citizen this car belongs to.
        public Citizen driver;

        /// The color of this car.
        public Color color;

        /// The driving behaviour.
        private int _behaviourIndex;

        /// The parking lot this car is currently parked at.
        public IMapObject parkingLot;

        public Velocity MaxVelocity
        {
            get
            {
                switch (model)
                {
                    default:
                    case CarModel.Sedan1:
                    case CarModel.Sedan2:
                        return Velocity.FromRealTimeKPH(120f);
                    case CarModel.Sedan3:
                    case CarModel.Sedan4:
                    case CarModel.Pickup:
                        return Velocity.FromRealTimeKPH(80f);
                    case CarModel.Sports:
                        return Velocity.FromRealTimeKPH(200f);
                }
            }
        }

        public Acceleration Acceleration
        {
            get
            {
                switch (model)
                {
                    default:
                    case CarModel.Sedan1:
                    case CarModel.Sedan2:
                        return Acceleration.FromRealTimeMPS2(3.25f);
                    case CarModel.Sedan3:
                    case CarModel.Sedan4:
                        return Acceleration.FromRealTimeMPS2(2.5f);
                    case CarModel.Pickup:
                        return Acceleration.FromRealTimeMPS2(2f);
                    case CarModel.Sports:
                        return Acceleration.FromRealTimeMPS2(8f);
                }
            }
        }

        public Distance Length => Distance.FromMeters(5.6f);

        public Color RandomCarColor
        {
            get
            {
                var rnd = RNG.Next(0f, 1f);

                var i = 0;
                var sum = _colorDistribution[i].Item1;

                while (rnd > sum)
                {
                    if (++i == _colorDistribution.Length)
                    {
                        return RNG.RandomColor;
                    }

                    sum += _colorDistribution[i].Item1;
                }

                return _colorDistribution[i].Item2;
            }
        }

        public CarModel RandomCarModel
        {
            get
            {
                var rnd = RNG.Next(0f, 1f);

                var i = 0;
                var sum = _carModelDistribution[i].Item1;

                while (rnd > sum)
                {
                    sum += _carModelDistribution[++i].Item1;
                }

                return _carModelDistribution[i].Item2;
            }
        }
        
        private int RandomBehaviour
        {
            get
            {
                var rnd = RNG.Next(0f, 1f);

                var i = 0;
                var sum = _behaviourDistribution[i].Item1;

                while (rnd > sum)
                {
                    sum += _behaviourDistribution[++i].Item1;
                }

                return _behaviourDistribution[i].Item2;
            }
        }

        public DrivingBehaviour Behaviour => _drivingBehaviours[_behaviourIndex];

        public Car(SimulationController sim, Citizen driver,
                   Color? c = null, int carModel = -1, uint id = 0,
                   int behaviourIndex = -1)
        {
            this.color = c ?? RandomCarColor;
            this.model = carModel <= 0 || carModel > 5 ? RandomCarModel : (CarModel)carModel;
            this._behaviourIndex = behaviourIndex == -1 ? RandomBehaviour : behaviourIndex;

            if (id == 0)
            {
                this.id = ++_lastAssignedId;
            }
            else
            {
                this.id = id;
                _lastAssignedId = System.Math.Max(id, _lastAssignedId);
            }

            this.driver = driver;
            driver.Car = this;
        }

        public Serialization.Car ToProtobuf()
        {
            return new Serialization.Car
            {
                Id = id,
                CarModel = (uint)model,
                DriverId = driver.id,
                Color = color.ToProtobuf(),
                ParkingLotID = (uint) (parkingLot?.Id ?? 0),
                BehaviourID = (uint)_behaviourIndex,
            };
        }
    }
}