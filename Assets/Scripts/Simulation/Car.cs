using System;
using UnityEngine;

namespace Transidious
{
    public class Car
    {
        static readonly Tuple<float, Color>[] _colorDistribution =
        {
            new Tuple<float, Color>(0.24f, Color.white),
            new Tuple<float, Color>(0.16f, new Color(.7f, .7f, .7f)),    // silver
            new Tuple<float, Color>(0.19f, Color.black),
            new Tuple<float, Color>(0.15f, Color.gray),
            new Tuple<float, Color>(0.10f, new Color(.57f, 0f, 0f)),     // red
            new Tuple<float, Color>(0.05f, new Color(.82f, .71f, .55f)), // tan
            new Tuple<float, Color>(0.03f, new Color(.082f, .305f, 0f)), // green
        };

        static uint _lastAssignedId = 0;

        /// The ID of this car.
        public uint id;

        /// The model ID of this car.
        public int model;

        /// The citizen this car belongs to.
        public Citizen driver;

        /// The color of this car.
        public Color color;

        /// The parking lot this car is currently parked at.
        public IMapObject parkingLot;

        public Velocity MaxVelocity
        {
            get
            {
                switch (model)
                {
                    default:
                    case 0:
                        return Velocity.FromRealTimeKPH(120f);
                    case 1:
                        return Velocity.FromRealTimeKPH(70f);
                    case 2:
                        return Velocity.FromRealTimeKPH(100f);
                    case 3:
                        return Velocity.FromRealTimeKPH(100f);
                    case 4:
                        return Velocity.FromRealTimeKPH(80f);
                }
            }
        }

        public float Acceleration
        {
            get
            {
                switch (model)
                {
                    default:
                    case 0:
                        return 0.73f;
                    case 1:
                        return 0.73f;
                    case 2:
                        return 0.73f;
                    case 3:
                        return 0.73f;
                    case 4:
                        return 0.73f;
                }
            }
        }

        public Color RandomCarColor
        {
            get
            {
                var rnd = UnityEngine.Random.Range(0f, 1f);

                var i = 0;
                var sum = _colorDistribution[i].Item1;

                while (rnd > sum)
                {
                    if (++i == _colorDistribution.Length)
                    {
                        return Utility.RandomColor;
                    }

                    sum += _colorDistribution[i].Item1;
                }

                return _colorDistribution[i].Item2;
            }
        }

        public int RandomCarModel
        {
            get
            {
                var rnd = UnityEngine.Random.Range(0f, 1f);
                if (rnd < 0.4f)
                    return 1;

                if (rnd < 0.8f)
                    return 4;

                if (rnd < .85f)
                    return 2;

                if (rnd < .9f)
                    return 5;

                return 3;
            }
        }

        public Car(SimulationController sim, Citizen driver,
                   Color? c = null, int carModel = -1, uint id = 0)
        {
            this.color = c ?? RandomCarColor;
            this.model = carModel < 0 || carModel > 5 ? RandomCarModel : carModel;

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
            driver.car = this;
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
            };
        }
    }
}