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

    public class Line : DynamicMapObject
    {
        public Map map;

        public TransitType type;
        public Color color;
        public ColorGradient gradient;
        public Material material;

        public bool wasModified = false;

        public Stop depot;
        public List<Stop> stops;
        public List<Route> routes;
        public List<TransitVehicle> vehicles;
        public float length;
        public float stopDuration;
        public float endOfLineWaitTime = 0f;
        public float velocity;

        public Dictionary<Stop, float> scheduleOffsets;
        public Schedule schedule;

        public GameObject routePrefab;

        /// \return The average travel speed of vehicles on this line, in km/h.
        public float AverageSpeed
        {
            get
            {
                switch (type)
                {
                case TransitType.Bus: return 45.0f;
                case TransitType.Tram: return 45.0f;
                case TransitType.Subway: return 50.0f;
                case TransitType.LightRail: return 60.0f;
                case TransitType.IntercityRail: return 80.0f;
                case TransitType.Ferry: return 10.0f;
                default:
                    Debug.LogError("Unknown transit type!");
                    return 50.0f;
                }
            }
        }

        /// <summary>
        /// Average time spent waiting at a stop (in seconds).
        /// </summary>
        public float AverageStopDuration
        {
            get
            {
                switch (type)
                {
                    default:
                    case TransitType.Bus: return 30.0f;
                    case TransitType.Tram: return 30.0f;
                    case TransitType.Subway: return 20.0f;
                    case TransitType.LightRail: return 20.0f;
                    case TransitType.IntercityRail: return 180.0f;
                    case TransitType.Ferry: return 180.0f;
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

        public void Initialize(Map map, string name, TransitType type, Color color, int id)
        {
            base.Initialize(MapObjectKind.Line, id, new Vector2());
            this.map = map;
            this.name = name;
            this.color = color;
            this.type = type;
            this.schedule = Schedule.GetDefaultSchedule(type);

            this.material = new Material(Shader.Find("Unlit/Color"));
            this.material.color = color;

            this.stopDuration = AverageStopDuration;
            this.velocity = AverageSpeed;
        }

        public void SetName(string name)
        {
            this.name = name;
        }

        public void SetColor(Color color)
        {
            this.color = color;
            this.material.color = color;
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
            GameObject routeObject = Instantiate(routePrefab);
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
                GameObject backRouteObject = Instantiate(routePrefab);
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
            var fixedUpdateInterval = sim.FixedUpdateInterval / 1000f;
            var interval = schedule.dayInterval * 60f;
            var speedMetersPerSecond = AverageSpeed / 3.6f;
            var earliestDeparture = sim.GameTime;

            // Round to the nearest fixed update interval.
            interval = Mathf.Ceil(interval / fixedUpdateInterval) * fixedUpdateInterval;

            var lineDurationSeconds = (length / speedMetersPerSecond) * sim.BaseSpeedMultiplier;
            lineDurationSeconds += stops.Count * AverageStopDuration;

            var neededVehiclesExact = lineDurationSeconds / interval;
            var neededVehicles = (int)Mathf.Ceil(neededVehiclesExact);

            var extraPercentage = (neededVehicles / neededVehiclesExact) - 1f;
            var extraTime = extraPercentage * lineDurationSeconds;// - stopDuration;

            //var actualInterval = lineDurationSeconds / neededVehicles;
            //Debug.Assert(actualInterval <= interval);

            // Increase stop duration to ensure equal spacing between vehicles.
            this.endOfLineWaitTime = Mathf.Ceil(
               extraTime / fixedUpdateInterval) * fixedUpdateInterval;

            var departure = earliestDeparture.AddSeconds(stopDuration);
            departure = sim.RoundToNextFixedUpdate(departure);

            for (var i = 0; i < neededVehicles; ++i)
            {
                var v = sim.CreateVehicle(this);
                this.vehicles.Add(v);

                v.nextStopTime = departure;
                departure = departure.AddSeconds(interval);
            }

            var totalTravelTimeSeconds = 0f;
            var first = true;
            var firstDeparture = earliestDeparture.AddSeconds(stopDuration);
            firstDeparture = sim.RoundToNextFixedUpdate(departure);

            foreach (var route in routes)
            {
                if (first)
                {
                    first = false;
                    route.beginStop.SetSchedule(this, new ContinuousSchedule(firstDeparture, interval));
                }

                totalTravelTimeSeconds += (route.length / speedMetersPerSecond) * 60f;
                totalTravelTimeSeconds += stopDuration;

                route.endStop.SetSchedule(this, new ContinuousSchedule(
                    firstDeparture.AddSeconds(totalTravelTimeSeconds), interval));
            }

            /*Stop previousStop = null;
            var stopDurationInSeconds = AverageStopDuration / 60f;
            var travelTimeSincePrevStop = 0f;
            var nroute = 0;
            var vehicleSpawns = new List<int>();

            foreach (var stop in stops)
            {
                if (previousStop != null)
                {
                    var route = routes[nroute - 1];
                    travelTimeSincePrevStop += (route.length / (AverageSpeed * 1000f)) * 60f;
                    travelTimeSincePrevStop += stopDurationInSeconds;
                }

                if (previousStop == null || travelTimeSincePrevStop >= interval)
                {
                    // Spawn a new vehicle at this stop to meet the schedule.
                    vehicleSpawns.Add(nroute);
                    stop.SetSchedule(this, schedule.OffsetBy(stopDurationInSeconds));

                    travelTimeSincePrevStop = stopDurationInSeconds;
                }
                else
                {
                    // No new vehicle required, depart at the earliest possible time.
                    stop.SetSchedule(this, schedule.OffsetBy(travelTimeSincePrevStop));
                }

                previousStop = stop;
                ++nroute;
            }

            foreach (var routeNo in vehicleSpawns)
            {
                var stop = stops[routeNo];
                var v = sim.CreateVehicle(this, stop);
                this.vehicles.Add(v);

                var departure = stop.NextDeparture(this, sim.GameTime);
                sim.ScheduleEvent(departure, () =>
                {
                    v.StartDrive(routeNo);
                });
            }*/

            /*Debug.Assert(routes.Count > 0, "empty line!");
            scheduleOffsets = new Dictionary<Stop, float>();

            Stop previousStop = null;
            var numPreviousStops = 1;
            
            foreach (var stop in stops)
            {
                if (previousStop == null)
                {
                    stop.SetSchedule(this, schedule);

                    scheduleOffsets.Add(stop, 0);
                    previousStop = stop;

                    continue;
                }
                if (scheduleOffsets.ContainsKey(stop))
                {
                    break;
                }

                var distance = (previousStop.location - stop.location).magnitude;
                var durationInMins = (distance / (AverageSpeed * 1000f)) * 60f;
                var duration = durationInMins + numPreviousStops * (AverageStopDuration / 60f);

                scheduleOffsets.Add(stop, duration);
                stop.SetSchedule(this, schedule.OffsetBy(duration));

                numPreviousStops++;
                previousStop = stop;
            }

            var sim = Game.sim;
            var interval = schedule.dayInterval;

            Debug.Assert(60 % interval == 0, "invalid interval");

            var neededVehicles = 60 / interval;
            var nextDepartures = schedule.NextDepartures(sim.GameTime, neededVehicles);
            this.vehicles = new List<TransitVehicle>(neededVehicles);

            for (var i = 0; i < neededVehicles; ++i)
            {
                var v = sim.CreateVehicle(this);
                this.vehicles.Add(v);

                sim.ScheduleEvent(nextDepartures[i], () =>
                {
                    v.StartDrive();
                });
            }*/
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

        void Awake()
        {
            routePrefab = Resources.Load("Prefabs/Route") as GameObject;
            routes = new List<Route>();
            stops = new List<Stop>();
        }

        void Update()
        {
            if (gradient != null)
            {
                this.material.color = gradient.CurrentColor;
            }
        }
    }
}
