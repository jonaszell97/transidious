using UnityEngine;
using System;
using System.Collections.Generic;

public class Map: MonoBehaviour
{
    public static readonly Dictionary<Line.TransitType, Color> defaultLineColors = new Dictionary<Line.TransitType, Color>
    {
        { Line.TransitType.Bus, new Color(0.58f, 0.0f, 0.83f)  },
        { Line.TransitType.Tram, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.Subway, new Color(0.09f, 0.02f, 0.69f)  },
        { Line.TransitType.STrain, new Color(37f/255f, 102f/255f, 10f/255f)  },
        { Line.TransitType.RegionalTrain, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.LongDistanceTrain, new Color(1.0f, 0.0f, 0.0f)  },
        { Line.TransitType.Ferry, new Color(0.14f, 0.66f, 0.79f)  }
    };

    /// Reference to the input controller.
    public InputController input;

    /// The width of the map (in meters).
    public int width;

    /// The height of the map (in meters).
    public int height;

    /// List of all public transit stops.
    public List<Stop> transitStops;

    /// List of all public transit routes.
    public List<Route> transitRoutes;

    /// Map of transit stops by name.
    public Dictionary<string, Stop> transitStopMap;

    /// List of all public transit lines.
    public List<Line> transitLines;

    /// Map of transit lines by name.
    public Dictionary<string, Line> transitLineMap;

    /// Prefab for creating stops.
    public GameObject stopPrefab;

    /// Prefab for creating lines.
    public GameObject linePrefab;

    public void Initialize(int width, int height)
    {
        this.width = width;
        this.height = height;
        this.transitStops = new List<Stop>();
        this.transitStopMap = new Dictionary<string, Stop>();
        this.transitLines = new List<Line>();
        this.transitLineMap = new Dictionary<string, Line>();
    }

    public Line GetLine(string name)
    {
        if (transitLineMap.TryGetValue(name, out Line l))
        {
            return l;
        }

        return null;

    }

    public class LineBuilder
    {
        Line line;
        Stop lastAddedStop;

        internal LineBuilder(Line line)
        {
            this.line = line;
            this.lastAddedStop = null;
        }

        public LineBuilder AddStop(string name, Vector2 position,
                                   bool oneWay = false, bool isBackRoute = false,
                                   Path path = null) {
            return AddStop(line.map.GetOrCreateStop(name, position), oneWay, isBackRoute, path);
        }

        public LineBuilder AddStop(Stop stop, bool oneWay = false,
                                   bool isBackRoute = false, Path path = null)
        {
            Debug.Assert(stop != null, "stop is null!");
            Debug.Assert(oneWay || !isBackRoute, "can't have a two-way back route!");

            if (lastAddedStop == null)
            {
                lastAddedStop = stop;
                line.depot = stop;

                return this;
            }

            line.AddRoute(lastAddedStop, stop, path, oneWay, isBackRoute);
            lastAddedStop = stop;

            return this;
        }

        public Line Finish()
        {
            return line;
        }
    }

    /// Create a new public transit line.
    public LineBuilder CreateLine(Line.TransitType type, string name, Color color)
    {
        GameObject lineObject = Instantiate(linePrefab);
        Line line = lineObject.GetComponent<Line>();
        line.transform.SetParent(this.transform);

        line.map = this;
        line.name = name;
        line.type = type;
        line.color = color;

        return new LineBuilder(line);
    }

    /// Create a new bus line.
    public LineBuilder CreateBusLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.Bus, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Bus]);
    }

    /// Create a new tram line.
    public LineBuilder CreateTramLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.Tram, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Tram]);
    }

    /// Create a new subway line.
    public LineBuilder CreateSubwayLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.Subway, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Subway]);
    }

    /// Create a new S-Train line.
    public LineBuilder CreateSTrainLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.STrain, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.STrain]);
    }

    /// Create a new regional train line.
    public LineBuilder CreateRegionalTrainLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.RegionalTrain, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.RegionalTrain]);
    }

    /// Create a new long distance train line.
    public LineBuilder CreateLongDistanceTrainLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.LongDistanceTrain, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.LongDistanceTrain]);
    }

    /// Create a new ferry line.
    public LineBuilder CreateFerryLine(string name, Color color = new Color())
    {
        return CreateLine(Line.TransitType.Ferry, name,
                          color.a > 0.0f ? color : defaultLineColors[Line.TransitType.Ferry]);
    }

    /// Register a new public transit line.
    public void RegisterLine(Line line)
    {
        transitLines.Add(line);
        transitLineMap.Add(line.name, line);
    }

    /// Register a new public transit line.
    public void RegisterStop(Stop stop)
    {
        transitStops.Add(stop);
        transitStopMap.Add(stop.name, stop);
    }

    /// Register a new public transit route.
    public void RegisterRoute(Route route)
    {
        transitRoutes.Add(route);
    }

    /// Register a new public transit stop.
    public Stop GetOrCreateStop(string name, Vector2 location)
    {
        if (transitStopMap.TryGetValue(name, out Stop stop))
        {
            return stop;
        }

        GameObject stopObject = Instantiate(stopPrefab);
        stop = stopObject.GetComponent<Stop>();
        stop.transform.SetParent(this.transform);

        stop.map = this;
        stop.name = name;
        stop.transform.position = location;

        RegisterStop(stop);
        return stop;
    }

    public void UpdateScale()
    {
        foreach (var stop in transitStops)
        {
            stop.UpdateScale();
        }

        foreach (var route in transitRoutes)
        {
            route.UpdateScale();
        }
    }

    // Use this for initialization
    void Awake()
    {
        Initialize(1000, 1000);
        stopPrefab = Resources.Load("Prefabs/Stop") as GameObject;
        linePrefab = Resources.Load("Prefabs/Line") as GameObject;
    }

    public bool done = false;

    // Use this for initialization
    void Start()
    {
        /*Stop kaiserdamm = GetOrCreateStop("Kaiserdamm", new Vector2(0, 0));
        Stop zoo = GetOrCreateStop("Zoologischer Garten", new Vector2(5f, -1f));
        Stop uhlandstrasse = GetOrCreateStop("Uhlandstraße", new Vector2(2.5f, -1.5f));
        Stop hbf = GetOrCreateStop("Hauptbahnhof", new Vector2(6.0f, 1.0f));
        Stop friedrichstrasse = GetOrCreateStop("Friedrichstraße", new Vector2(7.0f, 0.8f));
        Stop alexanderplatz = GetOrCreateStop("Alexanderplatz", new Vector2(8f, 0.4f));
        Stop memhardstrasse = GetOrCreateStop("Memhardstraße", new Vector2(8.1f, 0.5f));

        Stop bornholmerStrasse = GetOrCreateStop("Bornholmer Straße", new Vector2(5.75f, 2.5f));
        Stop gesundbrunnen = GetOrCreateStop("Gesundbrunnen", new Vector2(5.0f, 2.0f));
        Stop schoenhauserAllee = GetOrCreateStop("Schönhauser Allee", new Vector2(6.0f, 2.0f));
        Stop prenzlauerAllee = GetOrCreateStop("Prenzlauer Allee", new Vector2(8.0f, 2.0f));
        Stop landsbergerAllee = GetOrCreateStop("Landsberger Allee", new Vector2(10.0f, 2.0f));

        Path erpZooPath = new Path(new List<PathSegment> {
            new PathSegment(new Vector2(4.0f, 0), new Vector2(4.2f, 0)),
            new PathSegment(new Vector2(4.2f, 0), new Vector2(5.0f, -1.0f)),
        });

        CreateSTrainLine("S8", new Color(103f / 255f, 184f / 255f, 93f / 255f))
            .AddStop(bornholmerStrasse)
            .AddStop(schoenhauserAllee)
            .AddStop(prenzlauerAllee)
            .AddStop(landsbergerAllee);

        CreateSTrainLine("S41", new Color(169f / 255f, 73f / 255f, 50f / 255f))
            .AddStop(kaiserdamm, true)
            .AddStop(gesundbrunnen, true)
            .AddStop(schoenhauserAllee, true)
            .AddStop(prenzlauerAllee, true)
            .AddStop(landsbergerAllee, true);

        CreateSTrainLine("S42", new Color(182f / 255f, 111f / 255f, 50f / 255f))
            .AddStop(landsbergerAllee)
            .AddStop(prenzlauerAllee, true)
            .AddStop(schoenhauserAllee, true)
            .AddStop(gesundbrunnen, true)
            .AddStop(kaiserdamm, true);

        CreateSubwayLine("U2", new Color(243f/255f, 87f/255f, 33f/255f))
           .AddStop(kaiserdamm)
           .AddStop("Sophie-Charlotte-Platz", new Vector2(1f, 0))
           .AddStop("Bismarckstraße", new Vector2(2f, 0))
           .AddStop("Deutsche Oper", new Vector2(2.3f, 0))
           .AddStop("Ernst-Reuter-Platz", new Vector2(4f, 0))
           .AddStop(zoo, false, erpZooPath)
           .AddStop(schoenhauserAllee)
           .AddStop("Pankow", new Vector2(6f, 3f));

        CreateSubwayLine("U1", new Color(103f/255f, 184f/255f, 93f/255f))
            .AddStop(uhlandstrasse)
            .AddStop("Kurfürstendamm", new Vector2(3.75f, -1.5f))
            .AddStop(zoo);

        CreateRegionalTrainLine("RE1", new Color(236f / 255f, 124f / 255f, 102f / 255f))
            .AddStop(zoo)
            .AddStop(hbf)
            .AddStop(friedrichstrasse)
            .AddStop(alexanderplatz);

        CreateSTrainLine("S3", new Color(3f / 255f, 110f / 255f, 178f / 255f))
            .AddStop("Spandau", new Vector2(-3f, 1f))
            .AddStop("Charlottenburg", new Vector2(0.3f, -1f))
            .AddStop(zoo);

        CreateTramLine("M2")
            .AddStop(alexanderplatz)
            .AddStop(memhardstrasse)
            .AddStop(prenzlauerAllee);

        var options = new PathPlanningOptions
        {
            start = kaiserdamm,
            goal = schoenhauserAllee,
            time = DateTime.Now
        };

        var planner = new PathPlanner(options);
        //Debug.Log(planner.GetPath());
        */
        //done = true;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var stop in transitStops)
        {
            if (stop.wasModified)
            {
                stop.UpdateMesh();
            }
        }

        foreach (var line in transitLines)
        {
            if (line.wasModified)
            {
                line.UpdateMesh();
            }
        }
    }
}