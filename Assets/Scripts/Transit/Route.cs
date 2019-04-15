using UnityEngine;
using System.Collections;
using Transidious;

public class Route : MonoBehaviour
{
    public Line line;
    public Path path;
    public Path originalPath;

    public Stop beginStop;
    public Stop.Slot beginSlot;

    public Stop endStop;
    public Stop.Slot endSlot;

    public float totalTravelTime;
    public bool isBackRoute = false;

    public GameObject jointPrefab;

    private MeshFilter meshFilter;
    private Renderer m_Renderer;
    private GameObject joinObject;

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
        this.joinObject = null;
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

        line.map.RegisterRoute(this);
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

            if (path.Start != beginLoc)
            {
                path.AdjustStart(beginLoc, false, false);
                update = true;
            }
            if (path.End != endLoc)
            {
                path.AdjustEnd(endLoc, false, false);
                update = true;
            }
        }

        if (isBackRoute && !line.map.input.renderBackRoutes)
        {
            meshFilter.mesh = new Mesh();
            return;
        }

        if (beginStop.appearance == Stop.Appearance.LargeRect)
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
        if (endStop.appearance == Stop.Appearance.LargeRect)
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

            factor += beginSlot.assignment.parallelPositionOutbound
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

        // Check if we need smooth joints between segments.
        if (path.segments.Count > 1)
        {
            if (joinObject == null)
            {
                joinObject = Instantiate(jointPrefab);
                joinObject.transform.SetParent(this.transform);
                joinObject.transform.position = new Vector3(0, 0, 0.1f);
            }

            var joinMeshFilter = joinObject.GetComponent<MeshFilter>();
            var joinRenderer = joinObject.GetComponent<MeshRenderer>();

            joinMeshFilter.mesh = path.CreateJoints();
            joinRenderer.material = new Material(m_Renderer.material)
            {
                shader = line.map.input.circleShader
            };

            joinRenderer.material.SetColor("_Color", line.color);
            joinRenderer.material.SetColor("_InnerColor", line.color);
            joinRenderer.material.SetFloat("_Radius", path.width * 2);
            joinRenderer.material.SetFloat("_Thickness", 1.0f);
            joinRenderer.material.SetFloat("_Dropoff", 0.5f);

            joinRenderer.sortingOrder = m_Renderer.sortingOrder;
            joinRenderer.sortingLayerID = m_Renderer.sortingLayerID;
        }
        else if (joinObject != null)
        {
            Destroy(joinObject);
            joinObject = null;
        }
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

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
