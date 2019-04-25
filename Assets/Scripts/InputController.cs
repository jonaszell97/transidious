using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class InputController : MonoBehaviour
{
    public enum ControlType
    {
        ZoomOut,
        ZoomIn,

        PanUp,
        PanDown,
        PanRight,
        PanLeft,
    }

    public enum RenderingDistance
    {
        Near,
        Far,
        VeryFar,
        Farthest,
    }

    public Map map;
    public RenderingDistance renderingDistance = RenderingDistance.Near;

    float aspectRatio = 0.0f;
    float windowWidth = 0.0f;
    float windowHeight = 0.0f;

    public float maxX = 0f;
    public float maxY = 0f;

    public float minX = float.PositiveInfinity;
    public float minY = float.PositiveInfinity;

    Dictionary<ControlType, Tuple<KeyCode, KeyCode>> keyBindings;
    float minZoom = 0.1f;
    float maxZoom = 20.0f;
    float zoomSensitivity = 5.0f;

    float panSensitivityX = 0.5f;
    float panSensitivityY = 0.5f;

    public float lineWidth;
    public float stopWidth;
    public float boundaryWidth;

    public Shader circleShader;
    public Shader defaultShader;

    static readonly float farThreshold = 0.65f;
    static readonly float veryFarThreshold = 2f;
    static readonly float farthestThreshold = 7f;

    new public Camera camera;

    public bool renderBackRoutes = false;

    public bool IsPressed(ControlType type)
    {
        if (keyBindings.TryGetValue(type, out Tuple<KeyCode, KeyCode> binding))
        {
            return Input.GetKey(binding.Item1) || Input.GetKey(binding.Item2);
        }

        return false;
    }

    void UpdateStopWidth()
    {
        stopWidth = 0.07f;
    }

    void UpdateLineWidth()
    {
        switch (renderingDistance)
        {
            case RenderingDistance.VeryFar:
            case RenderingDistance.Farthest:
                lineWidth = 0.005f;
                break;
            case RenderingDistance.Far:
                lineWidth = 0.006f;
                break;
            case RenderingDistance.Near:
                lineWidth = 0.01f;
                break;
            default:
                throw new System.ArgumentException(string.Format("Illegal enum value {0}", renderingDistance));
        }
    }

    void UpdateBoundaryWidth()
    {
        switch (renderingDistance)
        {
            case RenderingDistance.VeryFar:
            case RenderingDistance.Farthest:
                boundaryWidth = 0.04f;
                break;
            case RenderingDistance.Far:
                boundaryWidth = 0.02f;
                break;
            case RenderingDistance.Near:
                boundaryWidth = 0.01f;
                break;
            default:
                throw new System.ArgumentException(string.Format("Illegal enum value {0}", renderingDistance));
        }

        boundaryWidth = 0.02f;
    }

    void ZoomOrthoCamera(Vector3 zoomTowards, float amount)
    {
        // Calculate how much we will have to move towards the zoomTowards position
        float multiplier = (1.0f / camera.orthographicSize * amount);

        // Move camera
        camera.transform.position += (zoomTowards - camera.transform.position) * multiplier;

        // Zoom camera
        camera.orthographicSize -= amount;

        // Limit zoom
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, minZoom, maxZoom);

        UpdateRenderingDistance();
    }

    float GetMouseZoom()
    {
        return Input.GetAxis("Mouse ScrollWheel");
    }

    float GetPinchZoom()
    {
        if (Input.touchCount != 2)
        {
            return 0f;
        }

        // Store both touches.
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        // Find the position in the previous frame of each touch.
        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        // Find the magnitude of the vector (the distance) between the touches in each frame.
        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        // Find the difference in the distances between each frame.
        return prevTouchDeltaMag - touchDeltaMag;
    }

    // Update the zoom based on mouse wheel input.
    void UpdateZoom()
    {
        float input;
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                input = GetPinchZoom();
                break;
            default:
                input = GetMouseZoom();
                break;
        }

        if (input.Equals(0f))
        {
            return;
        }

        ZoomOrthoCamera(Camera.main.ScreenToWorldPoint(Input.mousePosition), input * zoomSensitivity);

        panSensitivityX = camera.orthographicSize * 0.1f;
        panSensitivityY = camera.orthographicSize * 0.1f;

        zoomSensitivity = camera.orthographicSize * 0.5f;

        UpdateStopWidth();
        UpdateLineWidth();
        UpdateBoundaryWidth();

        map.UpdateScale();
    }

    void UpdateRenderingDistance()
    {
        float orthoSize = camera.orthographicSize;
        if (orthoSize <= farThreshold)
        {
            renderingDistance = RenderingDistance.Near;
        }
        else if (orthoSize <= veryFarThreshold)
        {
            renderingDistance = RenderingDistance.Far;
        }
        else if (orthoSize <= farthestThreshold)
        {
            renderingDistance = RenderingDistance.VeryFar;
        }
        else
        {
            renderingDistance = RenderingDistance.Farthest;
        }
    }

    Vector3 GetNewDesktopPosition(Vector3 position)
    {
        if (IsPressed(ControlType.PanUp))
        {
            position.y += panSensitivityY;
        }
        if (IsPressed(ControlType.PanDown))
        {
            position.y -= panSensitivityY;
        }
        if (IsPressed(ControlType.PanRight))
        {
            position.x += panSensitivityX;
        }
        if (IsPressed(ControlType.PanLeft))
        {
            position.x -= panSensitivityX;
        }

        return position;
    }

    Vector3 GetNewMobilePosition(Vector3 position)
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2)
        {
            camera.orthographicSize = 5f;
            camera.transform.position = map.startingCameraPos;
        }
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
            position.x *= -touchDeltaPosition.x * panSensitivityX;
            position.y *= -touchDeltaPosition.y * panSensitivityY;
        }

        return position;
    }

    // Update the camera position.
    void UpdatePosition()
    {
        Vector3 position = Camera.main.transform.position;
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                position = GetNewMobilePosition(position);
                break;
            default:
                position = GetNewDesktopPosition(position);
                break;
        }

        camera.transform.position = new Vector3(Mathf.Clamp(position.x, minX, maxX),
                                                Mathf.Clamp(position.y, minY, maxY),
                                                camera.transform.position.z);
    }

    void InitKeybindings()
    {
        keyBindings = new Dictionary<ControlType, Tuple<KeyCode, KeyCode>>
        {
            { ControlType.PanUp, new Tuple<KeyCode, KeyCode>(KeyCode.UpArrow, KeyCode.W) },
            { ControlType.PanDown, new Tuple<KeyCode, KeyCode>(KeyCode.DownArrow, KeyCode.S) },
            { ControlType.PanRight, new Tuple<KeyCode, KeyCode>(KeyCode.RightArrow, KeyCode.D) },
            { ControlType.PanLeft, new Tuple<KeyCode, KeyCode>(KeyCode.LeftArrow, KeyCode.A) },
        };
    }

    void Awake()
    {
        InitKeybindings();

        camera = Camera.main;
        aspectRatio = camera.aspect;
        windowWidth = camera.pixelRect.width;
        windowHeight = camera.pixelRect.height;

        UpdateRenderingDistance();

        UpdateStopWidth();
        UpdateLineWidth();
        UpdateBoundaryWidth();

        if (aspectRatio > 1.0f)
        {
            panSensitivityY = 1f / aspectRatio;
        }
        else if (aspectRatio < 1.0f)
        {
            panSensitivityX = 1f / aspectRatio;
        }

        this.circleShader = Resources.Load("Shaders/CircleShader") as Shader;
        this.defaultShader = Shader.Find("Unlit/Color");
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateZoom();
        UpdatePosition();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(new Vector3(minX, minY), 1f);
        Gizmos.DrawSphere(new Vector3(maxX, minY), 1f);
        Gizmos.DrawSphere(new Vector3(minX, maxY), 1f);
        Gizmos.DrawSphere(new Vector3(maxX, maxY), 1f);
    }
}
