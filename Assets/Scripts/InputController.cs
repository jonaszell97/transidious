using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static readonly float panThresholdY = 1000f;
        public static readonly float panThresholdX = panThresholdY * (16f / 9f);

        public float maxX = float.PositiveInfinity;
        public float maxY = float.PositiveInfinity;

        public float minX = float.NegativeInfinity;
        public float minY = float.NegativeInfinity;

        Dictionary<ControlType, Tuple<KeyCode, KeyCode>> keyBindings;

        public static float minZoom = 50f;
        public static float maxZoom;
        static float zoomSensitivityMouse = 25.0f;
        static float zoomSensitivityTrackpad = 5.0f;
        static float zoomSensitivityTouchscreen = 5.0f;
        float zoomSensitivity;

        static float panSensitivityX = 0.5f;
        static float panSensitivityXMobile = panSensitivityX / 20f;
        static float panSensitivityY = 0.5f;
        static float panSensitivityYMobile = panSensitivityY / 20f;

        static readonly float farThreshold = 650f;
        static readonly float veryFarThreshold = 2000f;
        static readonly float farthestThreshold = 7000f;
        
        new public Camera camera;

        public Vector3 NativeCursorPosition => camera.ScreenToWorldPoint(Input.mousePosition);

        public Vector3 gameCursorPosition;

        public Vector3 GameCursorPosition => Cursor.visible ? NativeCursorPosition : gameCursorPosition;

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
            
            UpdateCameraBoundaries(controller.loadedMap);
            controller.mainUI.UpdateScaleBar();

            FireEvent(InputEvent.Zoom);

            if (currRenderingDist == renderingDistance)
                return;

            FireEvent(InputEvent.ScaleChange);
        }

        public void SetZoomLevel(float zoom)
        {
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            if (zoom.Equals(camera.orthographicSize))
            {
                return;
            }

            var currRenderingDist = renderingDistance;
            camera.orthographicSize = zoom;
            UpdateRenderingDistance();

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
            if (position == (Vector2)camera.transform.position)
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

        bool moving = false;
        Vector2 movingTowards;
        float movementSpeed;

        public void MoveTowards(Vector2 worldPos, float speed = 0f)
        {
            if (speed <= 0f)
            {
                speed = (worldPos - (Vector2)camera.transform.position).magnitude * 2f;
            }

            movingTowards = worldPos;
            movementSpeed = speed;
            moving = true;
        }

        public enum FollowingMode
        {
            Center,
            Visible,
        }

        GameObject followObject;
        FollowingMode followingMode;

        public void FollowObject(GameObject obj, FollowingMode mode = FollowingMode.Visible)
        {
            this.followObject = obj;
            this.followingMode = mode;
        }

        public void StopFollowing()
        {
            followObject = null;
        }

        public void UpdateZoomLevels(Map map)
        {
            var backgroundRect = new Rect(map.minX - panThresholdX, map.minY - panThresholdY,
                                          map.width + 2 * panThresholdX, map.height + 2 * panThresholdY);

            // Set max zoom so that the camera can't go past the background rect.
            var aspect = camera.aspect;
            if (aspect >= 1f)
            {
                maxZoom = (backgroundRect.width * .5f) / aspect;
            }
            else
            {
                maxZoom = backgroundRect.height * .5f * aspect;
            }

            camera.orthographicSize = maxZoom;
            UpdateCameraBoundaries(map);
        }

        public void UpdateCameraBoundaries(Map map)
        {
            var aspect = camera.aspect;
            var orthoSize = camera.orthographicSize;

            minX = map.minX - panThresholdX + orthoSize * aspect;
            maxX = map.maxX + panThresholdX - orthoSize * aspect;
            minY = map.minY - panThresholdY + orthoSize;
            maxY = map.maxY + panThresholdY - orthoSize;

            panSensitivityX = orthoSize * 0.1f;
            panSensitivityY = orthoSize * 0.1f;
            zoomSensitivity = orthoSize * 0.5f;
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

#if DEBUG
            RegisterKeyboardEventListener(
                KeyCode.Alpha1, 
                _ => controller.sim.SetSimulationSpeed(SimulationController.SimulationSpeed.Speed1));
            RegisterKeyboardEventListener(
                KeyCode.Alpha2, 
                _ => controller.sim.SetSimulationSpeed(SimulationController.SimulationSpeed.Speed2));
            RegisterKeyboardEventListener(
                KeyCode.Alpha3, 
                _ => controller.sim.SetSimulationSpeed(SimulationController.SimulationSpeed.Speed3));
            RegisterKeyboardEventListener(
                KeyCode.Alpha4, 
                _ => controller.sim.SetSimulationSpeed(SimulationController.SimulationSpeed.Speed4));
#endif

#if UNITY_EDITOR
            _screenRes = Screen.currentResolution;
#endif
        }


        public bool IsPositionVisibleByCamera(Vector2 worldPos)
        {
            var vp = camera.WorldToViewportPoint(worldPos);
            return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
        }

#if DEBUG
        public bool debugClickTest = false;
        StreetSegment _highlighted = null;

        public bool debugRouteTest = false;
        Vector3? _fromPos;
        MultiMesh _routeMesh;
#endif

#if UNITY_EDITOR
        private Resolution _screenRes;
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
#if UNITY_EDITOR
            if (!_screenRes.Equals(Screen.currentResolution))
            {
                Debug.Log("changed screen resolution");
                UpdateZoomLevels(controller.loadedMap);
            }
#endif

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

            if (moving)
            {
                var newPos = Vector2.MoveTowards(camera.transform.position, movingTowards, movementSpeed * Time.deltaTime);
                if (newPos.Equals(movingTowards))
                {
                    moving = false;
                }

                SetCameraPosition(newPos);
            }
            else if (followObject != null)
            {
                Vector2 pos = followObject.transform.position;

                if (followingMode == FollowingMode.Center)
                {
                    SetCameraPosition(pos);
                }
                else if (followingMode == FollowingMode.Visible)
                {
                    if (!IsPositionVisibleByCamera(pos))
                    {
                        var direction = pos - (Vector2)camera.transform.position;
                        SetCameraPosition((Vector2)camera.transform.position + direction.normalized * Time.deltaTime);
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
                if (_fromPos == null)
                {
                    Debug.Log("first point set");
                    _fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions
                    {
                        maxWalkingDistance = 0f,
                    };

                    var planner = new PathPlanning.PathPlanner(options);
                    var ppResult = planner.FindClosestPath(controller.loadedMap,
                                                           _fromPos.Value,
                                                           clickedPos);

                    _fromPos = null;
                    if (ppResult == null)
                    {
                        Debug.Log("no result");
                    }
                    else
                    {
                        Debug.Log(ppResult.ToString());
                        ppResult.DebugDraw();
                    }
                }
            }

            if (controlListenersEnabled && (Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.F4)))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (_fromPos == null)
                {
                    Debug.Log("first point set");
                    _fromPos = clickedPos;
                }
                else
                {
                    var c = controller.sim.CreateCitizen(true);

                    var planner = new PathPlanning.PathPlanner(c.transitPreferences);
                    var map = controller.loadedMap;
                    var from = _fromPos.Value;
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

                    if (Input.GetKeyDown(KeyCode.F3) && transitResult != null)
                    {
                        c.FollowPath(transitResult);
                    }
                    else if (Input.GetKeyDown(KeyCode.F4) && carResult != null)
                    {
                        c.FollowPath(carResult);
                    }

                    _fromPos = null;
                }
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F5))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var found = controller.loadedMap.FindClosest(
                    out NaturalFeature parkingLot, clickedPos,
                    feature => feature.type == NaturalFeature.Type.Parking);

                if (found)
                {
                    Utility.DrawLine(parkingLot.outlinePositions[0].Select(
                        v2 => new Vector3(v2.x, v2.y, Map.Layer(MapLayer.Foreground))).ToArray(), 
                        3f, Color.red);
                }
                else
                {
                    Debug.Log("no parking lot found????");
                }
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F6))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var pos = controller.loadedMap.GetClosestStreet(clickedPos);

                if (pos.seg != null)
                {
                    Utility.DrawCircle(pos.pos, 3f, 3f, Color.red);
                }
            }

            if (debugClickTest && Input.GetMouseButtonDown(0))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var posOnStreet = controller.loadedMap.GetClosestStreet(clickedPos);

                if (posOnStreet != null)
                {
                    if (_highlighted != null)
                    {
                        _highlighted.ResetColor();
                    }

                    _highlighted = posOnStreet.seg;
                    posOnStreet.seg.UpdateColor(Color.red);

                    this.gameObject.transform.position = posOnStreet.pos;
                    this.gameObject.DrawCircle(10f, 10f, Color.blue);
                }
            }

            if (debugRouteTest && Input.GetMouseButtonDown(0) && !IsPointerOverUIElement())
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (_fromPos == null)
                {
                    _fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions();
                    var planner = new PathPlanning.PathPlanner(options);

                    var ppResult = planner.FindClosestDrive(controller.loadedMap,
                                                            _fromPos.Value, clickedPos);

                    _fromPos = null;
                    if (ppResult == null)
                    {
                        Debug.Log("no result");
                    }
                    else
                    {
                        Destroy(_routeMesh?.gameObject ?? null);
                        _routeMesh = controller.loadedMap.CreateMultiMesh();

                        var c = controller.sim.CreateCitizen(true);
                        c.FollowPath(ppResult);
                        
                        _routeMesh.CreateMeshes();
                    }

                    debugRouteTest = false;
                }
            }
#endif
        }
    }
}
