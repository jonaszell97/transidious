using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Transidious
{
    public class Line : MonoBehaviour
    {
        /// Represents the different public transit systems.
        public enum TransitType
        {
            /// A bus line.
            Bus,

            /// A tram line.
            Tram,

            /// A subway line.
            Subway,

            /// An S-Train line.
            STrain,

            /// A regional train line.
            RegionalTrain,

            /// A long-distance train line.
            LongDistanceTrain,

            /// A ferry line.
            Ferry,
        }

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

        public int id;
        public Map map;

        public TransitType type;
        public Color color;
        public bool wasModified = true;

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
                    case TransitType.STrain: return 60.0f;
                    case TransitType.RegionalTrain: return 80.0f;
                    case TransitType.LongDistanceTrain: return 120.0f;
                    case TransitType.Ferry: return 10.0f;
                    default:
                        Debug.LogError("Unknown transit type!");
                        return 50.0f;
                }
            }
        }

        /// Add a stop to the end of this line.
        public Route AddRoute(Stop begin, Stop end, List<Vector3> positions, bool oneWay, bool isBackRoute)
        {
            GameObject routeObject = Instantiate(routePrefab);
            Route route = routeObject.GetComponent<Route>();
            route.Initialize(this, begin, end, positions, isBackRoute);

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
            stops = line.stopIDs.Select(id => map.transitStopIDMap[id]).ToList();
            routes = line.routeIDs.Select(id => map.transitRouteIDMap[id]).ToList();
            depot = map.transitStopIDMap[line.depotID];

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

        }
    }
}
