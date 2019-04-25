using UnityEngine;
using System.Collections;
using Transidious;

public class Route : MonoBehaviour
{
    [System.Serializable]
    public struct SerializedRoute
    {
        public int id;
        public int lineID;
        public Path.SerializedPath path;
        public Path.SerializedPath originalPath;

        public int beginStopID;
        public int endStopID;

        public float totalTravelTime;
        public bool isBackRoute;
    }

    public int id;
    public Line line;

    public Path path;
    public Path originalPath;

    public Stop beginStop;
    public Stop.Slot beginSlot;

    public Stop endStop;
    public Stop.Slot endSlot;

    public float totalTravelTime;
    public bool isBackRoute = false;

    MeshFilter meshFilter;
    Renderer m_Renderer;

    public void Initialize(Line line, Stop beginStop, Stop endStop, Path path, bool isBackRoute = false)
    {
        this.line = line;
        this.path = path;
        this.beginStop = beginStop;
        this.beginSlot = null;
        this.endStop = endStop;
        this.endSlot = null;
        this.transform.position = new Vector3(0, 0, 0);
        this.transform.SetParent(line.transform);
        this.isBackRoute = isBackRoute;
        this.name = "(" + line.name + ") " + beginStop.name + " -> " + endStop.name;
        this.originalPath = path;

        UpdatePath();

        Route previousRoute = beginStop.GetIncomingRouteFromDepot(line);
        if (previousRoute == null)
        {
            this.totalTravelTime = this.TravelTime;
        }
        else
        {
            this.totalTravelTime = previousRoute.totalTravelTime + this.TravelTime;
        }
    }

    public float TravelTime
    {
        get
        {
            return path.length / line.AverageSpeed;
        }
    }

    public void UpdatePath()
    {
        bool update = false;
        Vector3 beginLoc;
        Vector3 endLoc;

        if (beginSlot == null)
        {
            beginLoc = beginStop.location;
        }
        else
        {
            beginLoc = beginStop.GetSlotLocation(this, beginSlot);
        }

        if (endSlot == null)
        {
            endLoc = endStop.location;
        }
        else
        {
            endLoc = endStop.GetSlotLocation(this, endSlot);
        }

        if (path == null)
        {
            path = new Path(beginLoc, endLoc);
            originalPath = new Path(path);
            update = true;
        }
        else
        {
            path = new Path(originalPath);
            update = true;

            if (path.Start != beginLoc)
            {
                path.AdjustStart(beginLoc, false, false);
            }
            if (path.End != endLoc)
            {
                path.AdjustEnd(endLoc, false, false);
            }
        }

        if (isBackRoute && !line.map.input.renderBackRoutes)
        {
            meshFilter.mesh = new Mesh();
            return;
        }

        if (beginStop.appearance == Stop.Appearance.LargeRect && beginSlot != null)
        {
            float factor = 0.1f;

            var dir = Math.ClassifyDirection(originalPath.BeginAngle);
            float spaceBetweenLines;

            if (dir == CardinalDirection.North || dir == CardinalDirection.South)
            {
                spaceBetweenLines = (beginStop.spacePerSlotHorizontal - line.map.input.lineWidth * 2f);
            }
            else
            {
                spaceBetweenLines = (beginStop.spacePerSlotVertical- line.map.input.lineWidth * 2f);
            }

            factor += beginSlot.assignment.parallelPositionInbound
                * Mathf.Sqrt(2 * spaceBetweenLines * spaceBetweenLines);

            path.RemoveStartAngle(Math.DirectionVector(dir) * factor);
            update = true;
        }
        if (endStop.appearance == Stop.Appearance.LargeRect && endSlot != null)
        {
            float factor = 0.1f;

            var dir = Math.ClassifyDirection(originalPath.EndAngle);
            float spaceBetweenLines;

            if (dir == CardinalDirection.North || dir == CardinalDirection.South)
            {
                spaceBetweenLines = (endStop.spacePerSlotHorizontal - line.map.input.lineWidth * 2f);
            }
            else
            {
                spaceBetweenLines = (endStop.spacePerSlotVertical - line.map.input.lineWidth * 2f);
            }

            factor += endSlot.assignment.parallelPositionOutbound
                * Mathf.Sqrt(2 * spaceBetweenLines * spaceBetweenLines);

            path.RemoveEndAngle(Math.DirectionVector(dir) * -factor);
            update = true;
        }

        if (update)
        {
            path.width = line.map.input.lineWidth;
            UpdateMesh();

            // Some stops might need to be updated.
            if (beginStop.appearance == Stop.Appearance.SmallRect)
            {
                beginStop.UpdateMesh(true);
            }
            if (endStop.appearance == Stop.Appearance.SmallRect)
            {
                endStop.UpdateMesh(true);
            }
        }
    }

    void UpdateMesh()
    {
        if (isBackRoute && !line.map.input.renderBackRoutes)
        {
            return;
        }

        meshFilter.mesh = path.CreateMesh();
        m_Renderer.material.color = line.color;

        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         Map.Layer(MapLayer.TransitLines));
    }

    public void UpdateScale()
    {
        if (!path.width.Equals(line.map.input.lineWidth))
        {
            path.width = line.map.input.lineWidth;
            UpdateMesh();
        }
    }

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        m_Renderer = GetComponent<Renderer>();
    }

    public SerializedRoute Serialize()
    {
        return new SerializedRoute
        {
            id = id,
            lineID = line.id,

            path = path.Serialize(),
            originalPath = originalPath.Serialize(),

            beginStopID = beginStop.id,
            endStopID = endStop.id,

            totalTravelTime = totalTravelTime,
            isBackRoute = isBackRoute
        };
    }

    public void Deserialize(SerializedRoute route, Map map)
    {
        id = route.id;
        Initialize(map.transitLineIDMap[route.lineID], map.transitStopIDMap[route.beginStopID],
                   map.transitStopIDMap[route.endStopID],
                   Path.Deserialize(route.path), route.isBackRoute);

        originalPath = Path.Deserialize(route.originalPath);
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
