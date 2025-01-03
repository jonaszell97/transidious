﻿using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using Transidious.UI;
using UnityEngine.Events;

namespace Transidious
{
    public enum RenderingDistance
    {
        Near,
        Far,
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
        ResolutionChange,
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
        /// The main camera.
        public static Camera mainCamera;

        public GameController controller;
        public RenderingDistance renderingDistance = RenderingDistance.Near;

        /// Set to true if we fired a mouse down event already in this frame.
        private bool _firedMouseDown;
        
        /// Set to true if we fired a mouse over event already in this frame.
        private bool _firedMouseOver;

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

        public static readonly float minZoom = 50f;
        public static float MaxZoom;

        public static float ZoomSensitivityMouse { get; private set; } = 25.0f;
        public static float zoomSensitivityTrackpad { get; private set; } = 5.0f;
        public static float ZoomSensitivityTouchscreen => ZoomSensitivityMouse / 20f;
        float _zoomSensitivity;

        public static float PanSensitivityX { get; private set; } = 0.5f;
        public static float PanSensitivityXMobile => PanSensitivityX / 5f;
        public static float PanSensitivityY { get; private set; } = 0.5f;
        public static float PanSensitivityYMobile => PanSensitivityY / 5f;

        public void SetPanSensitivity(float x, float y)
        {
            PanSensitivityX = x;
            PanSensitivityY = y;
        }

        static readonly float farRenderingThreshold = 650f;

        public EventSystem eventSystem;
        public static bool PointerOverUIObject = false;

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

        public int[] RegisterKeyboardEventListener(KeyCode[] keys, KeyboardEventListener eventListener,
                                                  bool enabled = true)
        {
            var ids = new int[keys.Length];

            var i = 0;
            foreach (var key in keys)
            {
                ids[i++] = RegisterKeyboardEventListener(key, eventListener, enabled);
            }

            return ids;
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

        private bool CheckPointerOverUIObject()
        {
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (eventSystem.IsPointerOverGameObject(touch.fingerId))
                    {
                        return true;
                    }
                }
            }

            return eventSystem.IsPointerOverGameObject();
        }

        public void MouseOverMapObject(IMapObject obj)
        {
            if (PointerOverUIObject)
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseOver])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                _firedMouseOver = true;
                listener.Value(obj);
            }
        }

        public void MouseEnterMapObject(IMapObject obj)
        {
            if (PointerOverUIObject)
            {
                return;
            }

            foreach (var listener in inputEventListeners[(int)InputEvent.MouseEnter])
            {
                if (disabledListeners.Contains(listener.Key))
                {
                    continue;
                }

                _firedMouseOver = true;
                listener.Value(obj);
            }
        }

        public void MouseExitMapObject(IMapObject obj)
        {
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
            if (PointerOverUIObject)
            {
                return;
            }

            _firedMouseDown = true;

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
            var currentSize = camera.orthographicSize;

            // Zoom camera
            var newSize = currentSize - amount;
            camera.orthographicSize = Mathf.Clamp(newSize, minZoom, MaxZoom);

            // Update min / max positions based on new zoom.
            UpdateCameraBoundaries(controller.loadedMap);

            // Calculate how much we will have to move towards the zoomTowards position
            float multiplier = (1.0f / currentSize * amount);

            // Move camera
            var tf = camera.transform;
            var pos = tf.position;
            pos += (zoomTowards - pos) * multiplier;
            tf.SetPositionInLayer(Mathf.Clamp(pos.x, minX, maxX), Mathf.Clamp(pos.y, minY, maxY));

        }

        private static readonly string MouseAxisName = "Mouse ScrollWheel";
        
        float GetMouseZoom()
        {
            return Input.GetAxis(MouseAxisName);
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
            if (PointerOverUIObject)
            {
                return;
            }
            
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

            if ((cmp < 0 && camera.orthographicSize.Equals(MaxZoom))
                || (cmp > 0 && camera.orthographicSize.Equals(minZoom)))
            {
                return;
            }

            var prevSize = camera.orthographicSize;
            ZoomOrthoCamera(camera.ScreenToWorldPoint(Input.mousePosition), input * _zoomSensitivity);

            var newSize = camera.orthographicSize;
            if (prevSize.Equals(newSize))
            {
                return;
            }

            ZoomLevelChanged(newSize);
        }

        public void SetZoomLevel(float zoom)
        {
            zoom = Mathf.Clamp(zoom, minZoom, MaxZoom);
            
            if (zoom.Equals(camera.orthographicSize))
            {
                return;
            }

            camera.orthographicSize = zoom;
            
            // Update min / max positions based on new zoom.
            UpdateCameraBoundaries(controller.loadedMap);
            ZoomLevelChanged(zoom);
        }

        void ZoomLevelChanged(float orthographicSize)
        {
            var currRenderingDist = renderingDistance;
            UpdateRenderingDistance(orthographicSize);
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

        public void UpdateRenderingDistance(float orthographicSize)
        {
            if (controller.ImportingMap)
            {
                renderingDistance = RenderingDistance.Near;
                return;
            }

            renderingDistance = orthographicSize <= farRenderingThreshold
                ? RenderingDistance.Near : RenderingDistance.Far;
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
                Vector3 diff = camera.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

                position.x -= diff.x * PanSensitivityX;
                position.y -= diff.y * PanSensitivityY;

                return position;
            }

            if (IsPressed(ControlType.PanUp))
            {
                position.y += PanSensitivityY;
            }
            if (IsPressed(ControlType.PanDown))
            {
                position.y -= PanSensitivityY;
            }
            if (IsPressed(ControlType.PanRight))
            {
                position.x += PanSensitivityX;
            }
            if (IsPressed(ControlType.PanLeft))
            {
                position.x -= PanSensitivityX;
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
                position.x += -touchDeltaPosition.x * PanSensitivityXMobile;
                position.y += -touchDeltaPosition.y * PanSensitivityYMobile;
            }

            return position;
        }

        // Update the camera position.
        void UpdatePosition()
        {
            Vector2 position = camera.transform.position;
            Vector2 newPosition;
            
            switch (Application.platform)
            {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                newPosition = GetNewMobilePosition(position);
                break;
            default:
                newPosition = GetNewDesktopPosition(position);
                break;
            }

            if (newPosition.Equals(position))
            {
                return;
            }

            _followObject = null;
            _movingTowards = null;
            
            SetCameraPosition(newPosition);
        }

        public void SetCameraPosition(Vector2 position, bool clampToMapBounds = true)
        {
            var tf = camera.transform;
            if (clampToMapBounds)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            tf.position = new Vector3(position.x, position.y, tf.position.z);
            FireEvent(InputEvent.Pan);
        }

        /// The point the camera is moving towards automatically.
        Vector2? _movingTowards;

        /// The camera's movement speed.
        float movementSpeed;

        /// The completion callback once moving is finished.
        UnityAction onMoveDone;

        public void MoveTowards(Vector2 worldPos, float speed = 0f, UnityAction onDone = null)
        {
            if (speed <= 0f)
            {
                speed = (worldPos - (Vector2)camera.transform.position).magnitude * 2f;
            }

            _movingTowards = worldPos;
            movementSpeed = speed;
            onMoveDone = onDone;

            _followObject = null;
        }

        public enum FollowingMode
        {
            Center,
            Visible,
        }

        /// The object the camera is tracking.
        GameObject _followObject;
        
        /// The tracking mode.
        FollowingMode _followingMode;

        public void FollowObject(GameObject obj, FollowingMode mode = FollowingMode.Visible)
        {
            this._followObject = obj;
            this._followingMode = mode;
            this._movingTowards = null;
        }

        public void StopFollowing()
        {
            _followObject = null;
        }

        public void UpdateZoomLevels(Map map)
        {
            var backgroundRect = new Rect(map.minX - panThresholdX, map.minY - panThresholdY,
                                          map.width + 2 * panThresholdX, map.height + 2 * panThresholdY);

            // Set max zoom so that the camera can't go past the background rect.
            var aspect = camera.aspect;
            if (aspect >= 1f)
            {
                MaxZoom = (backgroundRect.width * .5f) / aspect;
            }
            else
            {
                MaxZoom = backgroundRect.height * .5f * aspect;
            }

            camera.orthographicSize = MaxZoom;
            UpdateCameraBoundaries(map);

#if UNITY_EDITOR
            _screenRes = Screen.currentResolution;
#endif
        }

        public void UpdateCameraBoundaries(Map map)
        {
            var aspect = camera.aspect;
            var orthoSize = camera.orthographicSize;

            minX = map.minX - panThresholdX + orthoSize * aspect;
            maxX = map.maxX + panThresholdX - orthoSize * aspect;
            minY = map.minY - panThresholdY + orthoSize;
            maxY = map.maxY + panThresholdY - orthoSize;

            PanSensitivityX = orthoSize * 0.1f;
            PanSensitivityY = orthoSize * 0.1f;
            _zoomSensitivity = orthoSize * 0.5f;
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
            mainCamera = camera;
            aspectRatio = camera.aspect;

            UpdateRenderingDistance(camera.orthographicSize);

            if (aspectRatio > 1.0f)
            {
                PanSensitivityY = 1f / aspectRatio;
            }
            else if (aspectRatio < 1.0f)
            {
                PanSensitivityX = 1f / aspectRatio;
            }

            switch (Application.platform)
            {
            case RuntimePlatform.IPhonePlayer:
            case RuntimePlatform.Android:
                _zoomSensitivity = ZoomSensitivityTouchscreen;
                break;
            default:
                _zoomSensitivity = ZoomSensitivityMouse;
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

            RegisterKeyboardEventListener(KeyCode.F12, _ => DeveloperConsole.instance.Toggle());
            RegisterKeyboardEventListener(KeyCode.Space, _ => controller.TogglePause());
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

#if UNITY_EDITOR
        private static readonly float _resolutionCheckInterval = 1000f;
        private float _timeSinceLastResolutionCheck = 0f;
        
#endif

        void Update()
        {
#if UNITY_EDITOR
            _timeSinceLastResolutionCheck += Time.deltaTime;
            if (_timeSinceLastResolutionCheck >= _resolutionCheckInterval)
            {
                _timeSinceLastResolutionCheck = 0f;
                if (!_screenRes.Equals(Screen.currentResolution))
                {
                    UpdateZoomLevels(controller.loadedMap);
                    FireEvent(InputEvent.ResolutionChange);
                }
            }
#endif

            if (controlListenersEnabled)
            {
                UpdateZoom();
                UpdatePosition();
            }

            PointerOverUIObject = CheckPointerOverUIObject();
            Debug.Log($"Frame #{Time.frameCount}: {PointerOverUIObject}");

            CheckKeyboardEvents();

            var mainUI = controller.mainUI;
            if (mainUI != null)
            {
                if (mainUI.fadeScaleBarTime > 0f)
                {
                    mainUI.fadeScaleBarTime -= Time.deltaTime;
                    if (mainUI.fadeScaleBarTime <= 0f)
                    {
                        mainUI.fadeScaleBarTime = 0f;
                        mainUI.FadeScaleBar();
                    }
                }

                var missionProgress = mainUI.missionProgress;
                if (missionProgress.shouldFade && !missionProgress.hovered)
                {
                    if (missionProgress.fadeTime > 0f)
                    {
                        missionProgress.fadeTime -= Time.deltaTime;
                        if (missionProgress.fadeTime <= 0f)
                        {
                            missionProgress.fadeTime = 0f;
                            missionProgress.Fade();
                        }
                    }
                }
            }

            controller.snapController.Update();

            if (_movingTowards.HasValue)
            {
                var currentPos = camera.transform.position;
                var destination = _movingTowards.Value;
                var delta = movementSpeed * Time.deltaTime;

                var newPos = Vector2.MoveTowards(currentPos, destination, delta);
                if (newPos.Equals(_movingTowards))
                {
                    onMoveDone?.Invoke();
                    _movingTowards = null;
                }

                SetCameraPosition(newPos, false);
            }
            else if (_followObject != null)
            {
                Vector2 pos = _followObject.transform.position;

                if (_followingMode == FollowingMode.Center)
                {
                    SetCameraPosition(pos);
                }
                else if (_followingMode == FollowingMode.Visible)
                {
                    if (!IsPositionVisibleByCamera(pos))
                    {
                        Vector2 cameraPos = camera.transform.position;
                        var direction = pos - cameraPos;
                        SetCameraPosition(cameraPos + direction.normalized * Time.deltaTime);
                    }
                }
            }

            if (!_firedMouseDown && Input.GetMouseButtonDown(0) && !PointerOverUIObject)
            {
                FireEvent(InputEvent.MouseDown);
            }

            _firedMouseDown = false;
            _firedMouseOver = false;

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
                    var ppResult = planner.FindClosestPath(controller.loadedMap, _fromPos.Value, clickedPos);

                    _fromPos = null;
                    if (ppResult == null)
                    {
                        Debug.Log("no result");
                    }
                    else
                    {
                        Debug.Log(ppResult.ToString());
                        ppResult.path.DebugDraw();
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
                    var c = CitizenBuilder.Create().WithCar(true).Build();

                    var planner = new PathPlanning.PathPlanner(c.TransitPreferences);
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

                    var closestPt = SaveManager.loadedMap.GetClosestStreet(parkingLot.Centroid);
                    Utility.DrawCircle(closestPt.pos, 3f, 3f, Color.yellow);
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

                if (pos.street != null)
                {
                    Utility.DrawCircle(pos.pos, 3f, 3f, Color.red);
                }
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F7))
            {
                mainUI.missionProgress.SetProgress(0f, false, false);

                var value = RNG.Next(.30f, 1.00f);
                mainUI.missionProgress.SetProgress(value);
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F8))
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (_fromPos == null)
                {
                    Debug.Log("first point set");
                    _fromPos = clickedPos;
                }
                else
                {
                    Vector2 from = _fromPos.Value;
                    Vector2 to = clickedPos;

                    Debug.Log($"distance: {(from-to).magnitude}m");
                }
            }
            
            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F9))
            {
                controller.loadedMap.ToggleGrid();
            }

            if (controlListenersEnabled && Input.GetKeyDown(KeyCode.F10))
            {
                controller.mainUI.dialogPanel.Show(new []
                {
                    new UIDialogPanel.DialogItem
                    {
                        Message = "Message #1",
                        Icon = "Sprites/occupation_businessman",
                    },
                    new UIDialogPanel.DialogItem
                    {
                        Message = "Message #2 aoisdoiashdoiahsoidhaioshdioashdoiahsodihaoishdoiaoishdoihaioshdioahsdoi",
                        Icon = "Sprites/occupation_businessman",
                        Highlight = Tuple.Create(controller.mainUI.settingsIcons[2].gameObject.GetComponent<RectTransform>(), 5f, true, false),
                    },
                    new UIDialogPanel.DialogItem
                    {
                        Message = "Message #3 aoisdoiashdoiahsoidhaioshdioashdoiahsodihaoishdoiaoishdoihaioshdioahsdoi aoisdoiashdoiahsoidhaioshdioashdoiahsodihaoishdoiaoishdoihaioshdioahsdoi aoisdoiashdoiahsoidhaioshdioashdoiahsodihaoishdoiaoishdoihaioshdioahsdoi",
                        Icon = "Sprites/occupation_businessman",
                        Highlight = Tuple.Create(controller.mainUI.settingsIcons[0].gameObject.GetComponent<RectTransform>(), 5f, true, true),
                    },
                    new UIDialogPanel.DialogItem
                    {
                        Message = "Message #4",
                        Icon = "Sprites/occupation_doctor",
                        Highlight = Tuple.Create(controller.mainUI.settingsIcons[1].gameObject.GetComponent<RectTransform>(), 5f, true, false),
                    },
                }, () =>
                {
                    Debug.Log("Done!");
                });
                // controller.mainUI.highlightOverlay.Highlight(controller.mainUI.settingsIcons[2].gameObject, 5f, true);
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

                    _highlighted = posOnStreet.street;
                    posOnStreet.street.UpdateColor(Color.red);

                    this.gameObject.transform.position = posOnStreet.pos;
                    this.gameObject.DrawCircle(10f, 10f, Color.blue);
                }
            }

            if (debugRouteTest && Input.GetMouseButtonDown(0) && !PointerOverUIObject)
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

                        var c = CitizenBuilder.Create().WithCar(true).Build();
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
