using System;
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
        public Map map;

        public TransitType type;
        public Color color;
        public Material material;

        public bool wasModified = false;

        public Stop depot;
        public List<Stop> stops;
        public List<Route> routes;
        public List<TransitVehicle> vehicles;
        public float length;
        public TimeSpan stopDuration;
        public TimeSpan endOfLineWaitTime;
        public Velocity velocity;

        public int weeklyPassengers;

        public Dictionary<Stop, float> scheduleOffsets;
        public Schedule schedule;

        public Distance distance => Distance.FromMeters(length);

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

        /// <summary>
        /// Average time spent waiting at a stop (in seconds).
        /// </summary>
        public TimeSpan AverageStopDuration
        {
            get
            {
                return TimeSpan.Zero;
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

            this.schedule = Schedule.GetDefaultSchedule(type);
            this.material = new Material(GameController.instance.unlitMaterial)
            {
                color = color,
            };

            this.stopDuration = AverageStopDuration;
            this.velocity = AverageSpeed;
            
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
        public Route AddRoute(Stop begin, Stop end, List<Vector3> positions, bool oneWay, bool isBackRoute)
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

            if (!oneWay)
            {
                GameObject backRouteObject = GameObject.Instantiate(GameController.instance.loadedMap.routePrefab);
                Route backRoute = backRouteObject.GetComponent<Route>();
                backRoute.Initialize(this, end, begin, positions, true);

                routes.Add(backRoute);
                map.RegisterRoute(backRoute);

                end.AddOutgoingRoute(backRoute);
                begin.AddIncomingRoute(backRoute);
            }

            begin.wasModified = true;
            end.wasModified = true;
            this.wasModified = true;
            this.length += route.length;

            return route;
        }

        public void FinalizeLine()
        {
#if DEBUG
            if (routes.Count == 0)
            {
                return;
            }
#endif

            Debug.Assert(routes.Count > 0, "empty line!");

            var sim = Game.sim;
            var interval = TimeSpan.FromMinutes(schedule.dayInterval);
            var earliestDeparture = sim.GameTime;

            var lineDuration = distance / AverageSpeed;
            lineDuration += AverageStopDuration.Multiply(stops.Count - 1);

            var neededVehiclesExact = (float) (lineDuration.TotalSeconds / interval.TotalSeconds);
            var neededVehicles = (int)Mathf.Ceil(neededVehiclesExact);

            var extraPercentage = (neededVehicles / neededVehiclesExact) - 1f;
            var extraTime = lineDuration.Multiply(extraPercentage);

            // Increase stop duration to ensure equal spacing between vehicles.
            this.endOfLineWaitTime = extraTime;

            if (!Game.ImportingMap)
            {
                var departure = earliestDeparture;
                for (var i = 0; i < neededVehicles; ++i)
                {
                    var v = sim.CreateVehicle(this);
                    this.vehicles.Add(v);

                    v.nextStopTime = departure;
                    departure = departure.Add(interval).Add(AverageStopDuration);
                }
            }

            var totalTravelTime = TimeSpan.Zero;
            var firstDeparture = earliestDeparture;

            for (var i = 0; i < routes.Count; ++i)
            {
                var route = routes[i];
                if (i == 0)
                {
                    route.beginStop.SetSchedule(this, new ContinuousSchedule(firstDeparture, interval));
                }

                totalTravelTime += route.distance / AverageSpeed;

                if (i != routes.Count - 1)
                {
                    route.endStop.SetSchedule(this, new ContinuousSchedule(
                        firstDeparture.Add(totalTravelTime), interval));
                }
                
                totalTravelTime += stopDuration;
            }
        }

        public void UpdateMesh()
        {
            if (!wasModified)
            {
                return;
            }

            foreach (var route in routes)
            {
                route.UpdatePath();
            }

            wasModified = false;
        }

        public new Serialization.Line ToProtobuf()
        {
            var result = new Serialization.Line
            {
                MapObject = base.ToProtobuf(),
                DepotID = (uint)(depot?.id ?? 0),
                Color = color.ToProtobuf(),
            };

            result.StopIDs.AddRange(stops.Select(s => (uint)s.id));
            result.RouteIDs.AddRange(routes.Select(s => (uint)s.id));

            return result;
        }

        public void Deserialize(Serialization.Line line, Map map)
        {
            base.Deserialize(line.MapObject);

            this.map = map;
            stops = line.StopIDs.Select(id => map.GetMapObject<Stop>((int)id)).ToList();
            routes = line.RouteIDs.Select(id => map.GetMapObject<Route>((int)id)).ToList();
            depot = map.GetMapObject<Stop>((int)line.DepotID);

            this.length = this.routes.Sum(r => r.length);

            foreach (var stop in stops)
            {
                stop.AddLine(this);
            }

            wasModified = true;
            this.FinalizeLine();
        }
    }
}
