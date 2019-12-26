using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Transidious
{
    public enum RenderingDistance
    {
        Near,
        Far,
        VeryFar,
        Farthest,
    }

    public enum InputEvent
    {
        MouseOver = 0,
        MouseEnter,
        MouseExit,
        MouseDown,
        Zoom,
        Pan,
        ScaleChange,
        DisplayModeChange,
        _EventCount,
    }

    public enum ControlType
    {
        ZoomOut,
        ZoomIn,

        PanUp,
        PanDown,
        PanRight,
        PanLeft,
    }

    public class InputController : MonoBehaviour
    {
        public GameController controller;
        public RenderingDistance renderingDistance = RenderingDistance.Near;

        public delegate void InputEventListener(IMapObject mapObject);
        Dictionary<int, InputEventListener>[] inputEventListeners;
        int eventListenerCount;
        HashSet<int> disabledListeners;

        public delegate void KeyboardEventListener(KeyCode keyCode);
        Dictionary<KeyCode, List<KeyboardEventListener>> keyboardEventListeners;
        public bool controlListenersEnabled = true;

        float aspectRatio = 0.0f;

        public float maxX = float.PositiveInfinity;
        public float maxY = float.PositiveInfinity;

        public float minX = float.NegativeInfinity;
        public float minY = float.NegativeInfinity;

        Dictionary<ControlType, Tuple<KeyCode, KeyCode>> keyBindings;
        public static float minZoom = 100f * Map.Meters;
        public static float maxZoom = 15000f * Map.Meters;
        static float zoomSensitivityMouse = 25.0f;
        static float zoomSensitivityTrackpad = 5.0f;
        static float zoomSensitivityTouchscreen = 5.0f;
        float zoomSensitivity;

        static float panSensitivityX = 0.5f;
        static float panSensitivityXMobile = panSensitivityX / 20f;
        static float panSensitivityY = 0.5f;
        static float panSensitivityYMobile = panSensitivityY / 20f;

        public float lineWidth;
        public float stopWidth;

        static readonly float farThreshold = 650f * Map.Meters;
        static readonly float veryFarThreshold = 2000f * Map.Meters;
        static readonly float farthestThreshold = 7000f * Map.Meters;

        public bool combineStreetMeshes = false;
        public bool combineNatureMeshes = true;
        public bool combineBuildingMeshes = true;

        new public Camera camera;

        public bool renderBackRoutes = false;

        public Vector3 NativeCursorPosition
        {
            get
            {
                return Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        public Vector3 gameCursorPosition;

        public Vector3 GameCursorPosition
        {
            get
            {
                if (Cursor.visible)
                {
                    return NativeCursorPosition;
                }

                return gameCursorPosition;
            }
        }

        public Vector2 MaxCameraSizeWorld
        {
            get
            {
                var orthoSize = camera.orthographicSize;
                camera.orthographicSize = maxZoom;

                var max = camera.ViewportToWorldPoint(new Vector2(1f, 1f));
                var min = camera.ViewportToWorldPoint(new Vector2(0f, 0f));

                camera.orthographicSize = orthoSize;

                return max - min;
            }
        }

        public bool IsPressed(ControlType type)
        {
            if (keyBindings.TryGetValue(type, out Tuple<KeyCode, KeyCode> binding))
            {
                return Input.GetKey(binding.Item1) || Input.GetKey(binding.Item2);
            }

            return false;
        }

        public int RegisterEventListener(InputEvent e, InputEventListener eventListener,
                                         bool enabled = true)
        {
            var id = eventListenerCount++;
            inputEventListeners[(int)e].Add(id, eventListener);

            if (!enabled)
            {
                disabledListeners.Add(id);
            }

            return id;
        }

        public void RemoveEventListener(InputEvent e, int id)
        {
            inputEventListeners[(int)e].Remove(id);
        }

        public int RegisterKeyboardEventListener(KeyCode key, KeyboardEventListener eventListener,
                                                 bool enabled = true)
        {
            var id = eventListenerCount++;
            if (!keyboardEventListeners.TryGetValue(key, out List<KeyboardEventListener> listeners))
            {
                listeners = new List<KeyboardEventListener>();
                keyboardEventListeners.Add(key, listeners);
            }

            if (!enabled)
            {
                disabledListeners.Add(id);
            }

            listeners.Add(eventListener);
            return id;
        }

        public void DisableEventListener(int id)
        {
            disabledListeners.Add(id);
        }

        public void EnableEventListener(int id)
        {
            disabledListeners.Remove(id);
        }

        public void FireEvent(InputEvent type, DynamicMapObject target = null)
        {
            var eventListeners = inputEventListeners[(int)type];
            foreach (var listener in eventListeners)
            {
                listener.Value(target);
            }
        }

        public void DisableControls()
        {
            controlListenersEnabled = false;
        }

        public void EnableControls()
        {
            controlListenersEnabled = true;
        }

        public bool IsPointerOverUIElement()
        {
            if (EventSystem.current?.IsPointerOverGameObject() ?? false)
            {
                // Debug.Log(EventSystem.current.currentSelectedGameObject);
                // if (EventSystem.current.currentSelectedGameObject != null)
                // {
                //     if (EventSystem.current.currentSelectedGameObject.GetComponent<CanvasRenderer>() != null)
                //     {
                //         return true;
                //     }
                //     else
                //     {
                //         return false;
                //     }
                // }
                // else
                // {
                //     return false;
                // }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void MouseOverMapObject(IMapObject obj)
        {
            if (IsPointerOverUIElement())
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseOver])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                listener.Value(obj);
            }
        }

        public void MouseEnterMapObject(IMapObject obj)
        {
            if (IsPointerOverUIElement())
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseEnter])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                listener.Value(obj);
            }
        }

        public void MouseExitMapObject(IMapObject obj)
        {
            if (IsPointerOverUIElement())
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseExit])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                listener.Value(obj);
            }
        }

        public void MouseDownMapObject(IMapObject obj)
        {
            if (IsPointerOverUIElement())
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseDown])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                listener.Value(obj);
            }
        }

        public float GetScreenSpaceFontSize(float worldSpaceSize)
        {
            var p1 = camera.ScreenToWorldPoint(new Vector3(worldSpaceSize, 0, 0));
            return p1.x;
        }

        public float GetScreenSpaceFontScale()
        {
            return camera.orthographicSize / minZoom;
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
            return -((prevTouchDeltaMag - touchDeltaMag) / 30f);
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

            var cmp = input.CompareTo(0f);
            if (cmp == 0)
            {
                return;
            }

            if ((cmp < 0 && camera.orthographicSize.Equals(maxZoom))
                || (cmp > 0 && camera.orthographicSize.Equals(minZoom)))
            {
                return;
            }

            var currRenderingDist = renderingDistance;
            var prevSize = camera.orthographicSize;

            ZoomOrthoCamera(camera.ScreenToWorldPoint(Input.mousePosition), input * zoomSensitivity);

            if (prevSize.Equals(camera.orthographicSize))
            {
                return;
            }

            panSensitivityX = camera.orthographicSize * 0.1f;
            panSensitivityY = camera.orthographicSize * 0.1f;
            zoomSensitivity = camera.orthographicSize * 0.5f;

            controller.mainUI.UpdateScaleBar();

            FireEvent(InputEvent.Zoom);

            if (currRenderingDist == renderingDistance)
                return;

            FireEvent(InputEvent.ScaleChange);
        }

        public void SetRenderingDistance(RenderingDistance dist, bool forceUpdate = false)
        {
            var currRenderingDist = renderingDistance;
            this.renderingDistance = dist;

            if (forceUpdate || currRenderingDist != renderingDistance)
            {
                FireEvent(InputEvent.ScaleChange);
            }
        }

        public void UpdateRenderingDistance()
        {
            if (controller.ImportingMap)
            {
                renderingDistance = RenderingDistance.Near;
                return;
            }

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

        private Vector3 mouseOrigin;
        private bool isPanning;

        Vector2 GetNewDesktopPosition(Vector2 position)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                mouseOrigin = Input.mousePosition;
                isPanning = true;
            }
            else if (!Input.GetMouseButton(1))
            {
                isPanning = false;
            }

            if (isPanning)
            {
                Vector3 diff = Camera.main.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

                position.x -= diff.x * panSensitivityX;
                position.y -= diff.y * panSensitivityY;

                return position;
            }

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

        Vector2 GetNewMobilePosition(Vector2 position)
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).tapCount == 2)
            {
                camera.orthographicSize = 5f;
                camera.transform.position = controller.loadedMap.startingCameraPos;
            }
            else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
                position.x += -touchDeltaPosition.x * panSensitivityXMobile;
                position.y += -touchDeltaPosition.y * panSensitivityYMobile;
            }

            return position;
        }

        // Update the camera position.
        void UpdatePosition()
        {
            Vector2 position = Camera.main.transform.position;
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

            SetCameraPosition(position);
        }

        public void SetCameraPosition(Vector2 position)
        {
            if (position.x.Equals(camera.transform.position.x)
            && position.y.Equals(camera.transform.position.y))
            {
                return;
            }

            if (controller != null)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            camera.transform.position = new Vector3(position.x, position.y,
                                                    camera.transform.position.z);

            FireEvent(InputEvent.Pan);
        }

        public void UpdateZoomLevels()
        {
            minZoom = 30f * Map.Meters;
            maxZoom = (maxY - minY) / 2f;

            camera.orthographicSize = maxZoom;
            FireEvent(InputEvent.Zoom);
        }

        public Vector3 WorldToUISpace(Canvas parentCanvas, Vector3 worldPos)
        {
            //Convert the world for screen point so that it can be used with ScreenPointToLocalPointInRectangle function
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            Vector2 movePos;

            //Convert the screenpoint to ui rectangle local point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out movePos);
            //Convert the local point to world point
            return parentCanvas.transform.TransformPoint(movePos);
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

            this.eventListenerCount = 0;
            this.inputEventListeners = new Dictionary<int, InputEventListener>[(int)InputEvent._EventCount];
            this.disabledListeners = new HashSet<int>();
            this.keyboardEventListeners = new Dictionary<KeyCode, List<KeyboardEventListener>>();

            for (var i = 0; i < this.inputEventListeners.Length; ++i)
            {
                this.inputEventListeners[i] = new Dictionary<int, InputEventListener>();
            }

            camera = Camera.main;
            aspectRatio = camera.aspect;
            camera.orthographicSize = 5000f * Map.Meters;

            UpdateRenderingDistance();

            if (aspectRatio > 1.0f)
            {
                panSensitivityY = 1f / aspectRatio;
            }
            else if (aspectRatio < 1.0f)
            {
                panSensitivityX = 1f / aspectRatio;
            }

            switch (Application.platform)
            {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                zoomSensitivity = zoomSensitivityTouchscreen;
                break;
            default:
                zoomSensitivity = zoomSensitivityMouse;
                break;
            }
        }

        // Use this for initialization
        void Start()
        {

        }

#if DEBUG
        public bool debugClickTest = false;
        StreetSegment highlighted = null;

        public bool debugRouteTest = false;
        Vector3? fromPos;
        MultiMesh routeMesh;
#endif

        void CheckKeyboardEvents()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            foreach (var entry in keyboardEventListeners)
            {
                if (Input.GetKey(entry.Key))
                {
                    foreach (var listener in entry.Value)
                    {
                        listener(entry.Key);
                    }
                }
            }
        }

        void Update()
        {
            if (controlListenersEnabled)
            {
                UpdateZoom();
                UpdatePosition();
            }

            CheckKeyboardEvents();

            if (controller.mainUI != null)
            {
                if (controller.mainUI.fadeScaleBarTime > 0f)
                {
                    controller.mainUI.fadeScaleBarTime -= Time.deltaTime;
                    if (controller.mainUI.fadeScaleBarTime <= 0f)
                    {
                        controller.mainUI.fadeScaleBarTime = 0f;
                        controller.mainUI.FadeScaleBar();
                    }
                }
            }

#if DEBUG
            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F1))
            {
                var cursorPos = camera.ScreenToWorldPoint(Input.mousePosition);
                var stops = controller.loadedMap.GetMapObjectsInRadius<IMapObject>(cursorPos, 500f);

                transform.position = cursorPos;
                gameObject.DrawCircle(500f, 2f, Color.red);

                foreach (var s in stops)
                {
                    Debug.Log(s.Name);
                }
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F2))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (fromPos == null)
                {
                    Debug.Log("first point set");
                    fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions
                    {
                        maxWalkingDistance = 0f,
                    };

                    var planner = new PathPlanning.PathPlanner(options);
                    var ppResult = planner.FindClosestPath(controller.loadedMap,
                                                           fromPos.Value,
                                                           clickedPos);

                    fromPos = null;
                    if (ppResult == null)
                    {
                        Debug.Log("no result");
                    }
                    else
                    {
                        Debug.Log(ppResult.ToString());
                    }
                }
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F3))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (fromPos == null)
                {
                    Debug.Log("first point set");
                    fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions
                    {
                        maxWalkingDistance = 100f,
                        allowCar = false,
                        time = controller.sim.GameTime,
                    };

                    var planner = new PathPlanning.PathPlanner(options);
                    var map = controller.loadedMap;
                    var from = fromPos.Value;
                    var to = clickedPos;

                    var transitResult = planner.FindFastestTransitRoute(map, from, to);
                    if (transitResult != null)
                    {
                        Debug.Log($"transit route found with cost {transitResult.cost}");
                        Debug.Log(transitResult.ToString());
                    }

                    var carResult = planner.FindClosestDrive(map, from, to);
                    if (carResult != null)
                    {
                        Debug.Log($"car route found with cost {carResult.cost}");
                        Debug.Log(carResult.ToString());
                    }

                    fromPos = null;
                }
            }

            if (debugClickTest && Input.GetMouseButtonDown(0))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var posOnStreet = controller.loadedMap.GetClosestStreet(clickedPos);

                if (posOnStreet != null)
                {
                    if (highlighted != null)
                    {
                        highlighted.ResetColor(renderingDistance);
                    }

                    highlighted = posOnStreet.seg;
                    posOnStreet.seg.UpdateColor(Color.red);

                    this.gameObject.transform.position = posOnStreet.pos;
                    this.gameObject.DrawCircle(10f * Map.Meters, 10f * Map.Meters, Color.blue);
                }
            }

            if (debugRouteTest && Input.GetMouseButtonDown(0) && !IsPointerOverUIElement())
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (fromPos == null)
                {
                    fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions();
                    var planner = new PathPlanning.PathPlanner(options);

                    var ppResult = planner.FindClosestDrive(controller.loadedMap,
                                                            fromPos.Value, clickedPos);

                    fromPos = null;
                    if (ppResult == null)
                    {
                        Debug.Log("no result");
                    }
                    else
                    {
                        Destroy(routeMesh?.gameObject ?? null);
                        routeMesh = controller.loadedMap.CreateMultiMesh();

                        controller.sim.trafficSim.SpawnCar(ppResult, new Citizien(controller.sim));

                        routeMesh.CreateMeshes();
                        routeMesh.UpdateScale(renderingDistance);
                    }

                    debugRouteTest = false;
                }
            }
#endif
        }
    }
}
