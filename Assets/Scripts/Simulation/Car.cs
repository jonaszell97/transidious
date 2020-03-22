using UnityEngine;
using System;
using System.Collections.Generic;

namespace Transidious
{
    public class Car
    {
        static Tuple<float, Color>[] colorDistribution =
        {
            new Tuple<float, Color>(0.24f, Color.white),
            new Tuple<float, Color>(0.16f, new Color(.7f, .7f, .7f)),    // silver
            new Tuple<float, Color>(0.19f, Color.black),
            new Tuple<float, Color>(0.15f, Color.gray),
            new Tuple<float, Color>(0.10f, new Color(.57f, 0f, 0f)),     // red
            new Tuple<float, Color>(0.05f, new Color(.82f, .71f, .55f)), // tan
            new Tuple<float, Color>(0.03f, new Color(.082f, .305f, 0f)), // green
        };

        static uint lastAssignedID = 0;

        public uint id;
        public int model;
        public Citizen driver;
        public float maxVelocity;
        public float acceleration;
        public Color color;

        public Color RandomCarColor
        {
            get
            {
                var rnd = UnityEngine.Random.Range(0f, 1f);

                var i = 0;
                var sum = colorDistribution[i].Item1;

                while (rnd > sum)
                {
                    if (++i == colorDistribution.Length)
                    {
                        return Utility.RandomColor;
                    }

                    sum += colorDistribution[i].Item1;
                }

                return colorDistribution[i].Item2;
            }
        }

        public Car(SimulationController sim, Citizen driver,
                   Color? c = null, int carModel = -1, uint id = 0)
        {
            if (carModel == -1)
            {
                carModel = UnityEngine.Random.Range(1, 6);
            }

            this.color = c ?? RandomCarColor;
            this.model = carModel;

            switch (carModel)
            {
            default:
            case 0:
                this.maxVelocity = 33.333f; // 120 km/h
                this.acceleration = 0.73f;
                break;
            case 1:
                this.maxVelocity = 19.444f; // 70 km/h
                this.acceleration = 0.73f;
                break;
            case 2:
                this.maxVelocity = 27.777f; // 100 km/h
                this.acceleration = 0.73f;
                break;
            case 3:
                this.maxVelocity = 27.777f; // 100 km/h
                this.acceleration = 0.73f;
                break;
            case 4:
                this.maxVelocity = 22.222f; // 80 km/h
                this.acceleration = 0.73f;
                break;
            }

            if (id == 0)
            {
                this.id = ++lastAssignedID;
            }
            else
            {
                this.id = id;
                lastAssignedID = System.Math.Max(id, lastAssignedID);
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
            };
        }
    }
}