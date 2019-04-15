using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

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

    public Map map;
    public TransitType type;
    public Color color;
    public bool wasModified = true;

    public Stop depot;
    public List<Stop> stops;
    public List<Route> routes;

    private MeshFilter meshFilter;
    private Renderer m_Renderer;
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
    public Route AddRoute(Stop begin, Stop end, Path path, bool oneWay, bool isBackRoute)
    {
        GameObject routeObject = Instantiate(routePrefab);
        Route route = routeObject.GetComponent<Route>();
        route.Initialize(this, begin, end, path, isBackRoute);
        routes.Add(route);

        begin.AddOutgoingRoute(route);
        end.AddIncomingRoute(route);

        if (!oneWay)
        {
            GameObject backRouteObject = Instantiate(routePrefab);
            Route backRoute = backRouteObject.GetComponent<Route>();
            backRoute.Initialize(this, end, begin, route.path, true);

            routes.Add(backRoute);
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

    void Awake()
    {
        routePrefab = Resources.Load("Prefabs/Route") as GameObject;
        meshFilter = GetComponent<MeshFilter>();
        m_Renderer = GetComponent<Renderer>();
    }

    // Use this for initialization
    void Start()
    {
        map.RegisterLine(this);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
