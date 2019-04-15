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

    public Map map;

    float aspectRatio = 0.0f;
    float windowWidth = 0.0f;
    float windowHeight = 0.0f;

    Dictionary<ControlType, Tuple<KeyCode, KeyCode>> keyBindings;
    float minFov = 1.0f;
    float maxFov = 50.0f;
    float zoomSensitivity = 5.0f;

    float panSensitivityX = 0.5f;
    float panSensitivityY = 0.5f;

    public float lineWidth;
    public float stopWidth;

    public Shader circleShader;
    public Shader defaultShader;

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
        stopWidth = 0.2f;
    }

    void UpdateLineWidth()
    {
        lineWidth = 0.175f * stopWidth;
    }

    // Update the zoom based on mouse wheel input.
    void UpdateZoom()
    {
        float fov = Camera.main.orthographicSize;
        fov += Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);

        if (!fov.Equals(Camera.main.orthographicSize))
        {
            Camera.main.orthographicSize = fov;

            UpdateStopWidth();
            UpdateLineWidth();

            map.UpdateScale();
        }
    }

    // Update the camera position.
    void UpdatePosition()
    {
        Vector3 position = Camera.main.transform.position;
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

        Camera.main.transform.position = position;
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

        aspectRatio = Camera.main.aspect;
        windowWidth = Camera.main.pixelRect.width;
        windowHeight = Camera.main.pixelRect.height;

        UpdateStopWidth();
        UpdateLineWidth();
        map.UpdateScale();

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
}
