using UnityEngine;
using System;
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

        /// A light rail line.
        LightRail,

        /// A regional train line.
        IntercityRail,

        /// A ferry line.
        Ferry,
    }

    public class Line : MapObject
    {

        [System.Serializable]
        public struct SerializedLine
        {
            public string name;
            public int id;

            public TransitType type;
            public SerializableColor color;

            public int depotID;
            public List<int> stopIDs;
            public List<int> routeIDs;
        }

        public Map map;

        public TransitType type;
        public Color color;
        public ColorGradient gradient;
        public Material material;

        public bool wasModified = false;

        public Stop depot;
        public List<Stop> stops;
        public List<Route> routes;

        MeshFilter meshFilter;
        Renderer m_Renderer;

        public GameObject routePrefab;

        /// \return The average travel speed of vehicles on this line, in km/h.
        public float AverageSpeed
        {
            get
            {
                switch (type)
                {
                case TransitType.Bus: return 30.0f;
                case TransitType.Tram: return 30.0f;
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

        public void Initialize(Map map, string name, TransitType type, Color color)
        {
            this.map = map;
            this.name = name;
            this.color = color;
            this.type = type;

            this.material = new Material(Shader.Find("Unlit/Color"));
            this.material.color = color;
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

            return route;
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

        public SerializedLine Serialize()
        {
            return new SerializedLine
            {
                id = id,
                name = name,

                type = type,
                color = new SerializableColor(color),

                depotID = depot?.id ?? 0,
                stopIDs = stops.Select(s => s.id).ToList(),
                routeIDs = routes.Select(r => r.id).ToList(),
            };
        }

        public void Deserialize(SerializedLine line, Map map)
        {
            this.map = map;
            stops = line.stopIDs.Select(id => map.GetMapObject<Stop>(id)).ToList();
            routes = line.routeIDs.Select(id => map.GetMapObject<Route>(id)).ToList();
            depot = map.GetMapObject<Stop>(line.depotID);

            foreach (var stop in stops)
            {
                stop.AddLine(this);
            }

            wasModified = true;
        }

        void Awake()
        {
            routePrefab = Resources.Load("Prefabs/Route") as GameObject;
            meshFilter = GetComponent<MeshFilter>();
            m_Renderer = GetComponent<Renderer>();
            routes = new List<Route>();
            stops = new List<Stop>();
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (gradient != null)
            {
                this.material.color = gradient.CurrentColor;
            }
        }
    }
}
