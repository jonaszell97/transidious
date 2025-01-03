﻿using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Transidious
{
    /// Represents the different public transit systems.
    public enum TransitType
    {
        /// A bus line.
        Bus = 0,

        /// A tram line.
        Tram,

        /// A subway line.
        Subway,

        /// A regional train line.
        IntercityRail,

        /// A ferry line.
        Ferry,

        /// A light rail line.
        LightRail,
    }

    public class Line : StaticMapObject
    {
        /// Reference to the loaded map.
        public Map map;

        /// The transit system of this line.
        public TransitType type;
        
        /// The current color of the line.
        public Color color;

        /// The material of the line.
        public Material material;

        /// The first stop on the line.
        public Stop depot;
        
        /// All of the stops on the line.
        public List<Stop> stops;
        
        /// The routes connecting the stops on the line.
        public List<Route> routes;
        
        /// The current vehicles on the line.
        public List<TransitVehicle> vehicles;
        
        /// The total length of the line.
        public float length;
        
        /// Total weekly passengers on the line.
        public int weeklyPassengers;

        /// Cumulative lengths, indexed by route index.
        public float[] cumulativeLengths;

        /// The current schedule of the line.
        public Schedule schedule;

        /// Fare for a trip on the line.
        public decimal TripFare;

        /// The total travel time of the line without stopping.
        public TimeSpan TotalTravelTime => (distance / AverageSpeed) + AverageStopDuration.Multiply(routes.Count - 1);

        /// The total length of the line.
        public Distance distance => Distance.FromMeters(length);

        /// Average velocity of a vehicle on the line.
        public Velocity AverageSpeed
        {
            get
            {
                switch (type)
                {
                case TransitType.Bus: return Velocity.FromRealTimeKPH(45.0f);
                case TransitType.Tram: return Velocity.FromRealTimeKPH(45.0f);
                case TransitType.Subway: return Velocity.FromRealTimeKPH(50.0f);
                case TransitType.LightRail: return Velocity.FromRealTimeKPH(60.0f);
                case TransitType.IntercityRail: return Velocity.FromRealTimeKPH(80.0f);
                case TransitType.Ferry: return Velocity.FromRealTimeKPH(10.0f);
                default:
                    Debug.LogError("Unknown transit type!");
                    return Velocity.FromRealTimeKPH(50.0f);
                }
            }
        }

        /// Average time spent waiting at a stop (in seconds).
        public TimeSpan AverageStopDuration
        {
            get
            {
                switch (type)
                {
                    default:
                    case TransitType.Bus: return TimeSpan.FromSeconds(90.0f);
                    case TransitType.Tram: return TimeSpan.FromSeconds(90.0f);
                    case TransitType.Subway: return TimeSpan.FromSeconds(120.0f);
                    case TransitType.LightRail: return TimeSpan.FromSeconds(240.0f);
                    case TransitType.IntercityRail: return TimeSpan.FromSeconds(300.0f);
                    case TransitType.Ferry: return TimeSpan.FromSeconds(300.0f);
                }
            }
        }

        /// The default trip fare for a transit system.
        public decimal DefaultTripFare => GetDefaultTripFare(type);

        /// The default trip fare for a transit system.
        public static decimal GetDefaultTripFare(TransitType type)
        {
            switch (type)
            {
                case TransitType.Bus: return 2.5m;
                case TransitType.Tram: return 3m;
                case TransitType.Subway: return 5m;
                case TransitType.LightRail: return 5m;
                case TransitType.IntercityRail: return 25m;
                case TransitType.Ferry: return 3m;
                default:
                    Debug.LogError("Unknown transit type!");
                    return 2.5m;
            }
        }

        public float LineWidth
        {
            get
            {
                switch (type)
                {
                case TransitType.Bus: return 1.25f;
                case TransitType.Tram: return 1.25f;
                case TransitType.Subway: return 3f;
                case TransitType.LightRail: return 3f;
                case TransitType.IntercityRail: return 3f;
                case TransitType.Ferry: return 1.25f;
                default:
                    Debug.LogError("Unknown transit type!");
                    return 1.25f;
                }
            }
        }

        public Line(Map map, string name, TransitType type, Color color, int id)
        {
            base.Initialize(MapObjectKind.Line, id);
            this.map = map;
            this.name = name;
            this.color = color;
            this.type = type;

            this.TripFare = DefaultTripFare;
            this.schedule = Schedule.GetDefaultSchedule(type);
            this.material = new Material(GameController.instance.unlitMaterial)
            {
                color = color,
            };

            this.routes = new List<Route>();
            this.stops = new List<Stop>();
            this.vehicles = new List<TransitVehicle>();
        }

        public void SetColor(Color color)
        {
            if (color == this.color)
            {
                return;
            }

            this.color = color;
            this.material.color = color;

            foreach (var vehicle in vehicles)
            {
                vehicle.UpdateColor();
            }
        }

        public void SetTransparency(float a)
        {
            material.color = Math.ApplyTransparency(material.color, a);
        }

        public void ResetTransparency()
        {
            material.color = this.color;
        }

        /// Add a stop to the end of this line.
        public Route AddRoute(Stop begin, Stop end, List<Vector2> positions, bool isBackRoute = false)
        {
            GameObject routeObject = GameObject.Instantiate(GameController.instance.loadedMap.routePrefab);
            Route route = routeObject.GetComponent<Route>();
            route.Initialize(this, begin, end, positions, isBackRoute);

            if (stops.Count == 0)
            {
                stops.Add(begin);
                begin.AddLine(this);
            }

            stops.Add(end);
            end.AddLine(this);

            routes.Add(route);
            map.RegisterRoute(route);

            begin.AddOutgoingRoute(route);
            end.AddIncomingRoute(route);

            return route;
        }

        /// Return a route by index, wrapping around to the first route on overflow.
        public Route GetRoute(int routeIndex)
        {
            return routes[routeIndex % routes.Count];
        }

        /// Calculate length (and cumulative length) of the line.
        void CalculateLength()
        {
            cumulativeLengths = new float[routes.Count];

            for (var i = 0; i < routes.Count; ++i)
            {
                var route = routes[i];
                length += route.length;
                cumulativeLengths[i] = length;
            }
        }

        public void FinalizeLine()
        {
            if (routes.Count == 0)
            {
                return;
            }

            CalculateLength();
            Debug.Assert(routes.Count > 0, "empty line!");

            var sim = Game.sim;
            var interval = TimeSpan.FromMinutes(schedule.dayInterval);
            var earliestDeparture = sim.GameTime;

            var lineDuration = distance / AverageSpeed;
            lineDuration += AverageStopDuration.Multiply(stops.Count - 1);

            var neededVehiclesExact = (float) (lineDuration.TotalSeconds / interval.TotalSeconds);
            var neededVehicles = (int)Mathf.Ceil(neededVehiclesExact);

#if DEBUG
            if (Game.ImportingMap)
            {
                return;
            }
#endif

            TransitVehicle next = null;

            int i;
            for (i = 0; i < neededVehicles; ++i)
            {
                var v = sim.CreateVehicle(this, next);
                vehicles.Add(v);
                next = v;
            }

            var first = vehicles[0];
            first.First = true;
            first.Next = next;

            // Initialize opposite stops
            foreach (var stop in stops)
            {
                if (stop.oppositeStop != null)
                    continue;

                const float threshold = 100f*100f;
                var minDist = float.PositiveInfinity;
                Stop minStop = null;

                foreach (var otherStop in stops)
                {
                    if (stop == otherStop || otherStop.oppositeStop != null)
                        continue;

                    var dist = (stop.Location - otherStop.Location).sqrMagnitude;
                    if (dist < minDist && dist <= threshold)
                    {
                        minDist = dist;
                        minStop = otherStop;
                    }
                }

                if (minStop != null)
                {
                    stop.oppositeStop = minStop;
                    minStop.oppositeStop = stop;
                }
            }
        }

        public void StartVehicles()
        {
            if (vehicles.Count == 0)
                return;

            var first = vehicles[0];
            first.InitializeNextDepartures();
            first.StartDrive();
        }

        public new Serialization.Line ToProtobuf()
        {
            var result = new Serialization.Line
            {
                MapObject = base.ToProtobuf(),
                DepotID = (uint)(depot?.id ?? 0),
                Color = color.ToProtobuf(),
                Type = (Serialization.TransitType)type,
            };

            result.StopIDs.AddRange(stops.Select(s => (uint)s.id));
            result.RouteIDs.AddRange(routes.Select(s => (uint)s.id));

            return result;
        }

        public void Deserialize(Serialization.Line line, Map map)
        {
            base.Deserialize(line.MapObject);

            this.map = map;
            type = (TransitType) line.Type;
            stops = line.StopIDs.Select(id => map.GetMapObject<Stop>((int)id)).ToList();
            routes = line.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();
            depot = map.GetMapObject<Stop>((int)line.DepotID);

            foreach (var stop in stops)
            {
                stop.AddLine(this);
            }

            this.FinalizeLine();
            this.StartVehicles();
        }
    }
}
