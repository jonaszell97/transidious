using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Transidious
{
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

        public enum InputEvent
        {
            MouseOver = 0,
            MouseEnter,
            MouseExit,
            MouseDown,
            Zoom,
            Pan,
            _EventCount,
        }

        public GameController controller;
        public RenderingDistance renderingDistance = RenderingDistance.Near;

        public delegate void InputEventListener(MapObject mapObject);
        Dictionary<int, InputEventListener>[] inputEventListeners;
        int eventListenerCount;
        HashSet<int> disabledListeners;

        public delegate void KeyboardEventListener(KeyCode keyCode);
        Dictionary<KeyCode, List<KeyboardEventListener>> keyboardEventListeners;
        bool controlListenersEnabled = true;

        bool zooming = false;

        float aspectRatio = 0.0f;
        float windowWidth = 0.0f;
        float windowHeight = 0.0f;

        public float maxX = float.PositiveInfinity;
        public float maxY = float.PositiveInfinity;

        public float minX = float.NegativeInfinity;
        public float minY = float.NegativeInfinity;

        Dictionary<ControlType, Tuple<KeyCode, KeyCode>> keyBindings;
        public static float minZoom = 45f * Map.Meters;
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
        public float boundaryWidth;

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

        public void FireEvent(InputEvent type, MapObject target = null)
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
            if (EventSystem.current.IsPointerOverGameObject())
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

        public void MouseOverMapObject(MapObject obj)
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

        public void MouseEnterMapObject(MapObject obj)
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

        public void MouseExitMapObject(MapObject obj)
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

        public void MouseDownMapObject(MapObject obj)
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

        void UpdateStopWidth()
        {
            stopWidth = 10f;
        }

        void UpdateLineWidth()
        {
            switch (renderingDistance)
            {
            case RenderingDistance.VeryFar:
            case RenderingDistance.Farthest:
                lineWidth = 2.5f * Map.Meters;
                break;
            case RenderingDistance.Far:
                lineWidth = 3f * Map.Meters;
                break;
            case RenderingDistance.Near:
                lineWidth = 5f * Map.Meters;
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
                boundaryWidth = 40f * Map.Meters;
                break;
            case RenderingDistance.Far:
                boundaryWidth = 20f * Map.Meters;
                break;
            case RenderingDistance.Near:
                boundaryWidth = 10f * Map.Meters;
                break;
            default:
                throw new System.ArgumentException(string.Format("Illegal enum value {0}", renderingDistance));
            }

            boundaryWidth = 20f * Map.Meters;
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

            if (prevSize == camera.orthographicSize)
            {
                return;
            }

            controller?.loadedMap?.UpdateTextScale();

            panSensitivityX = camera.orthographicSize * 0.1f;
            panSensitivityY = camera.orthographicSize * 0.1f;
            zoomSensitivity = camera.orthographicSize * 0.5f;

            UpdateStopWidth();
            UpdateLineWidth();
            UpdateBoundaryWidth();
            UpdateScaleBar();

            FireEvent(InputEvent.Zoom);

            if (currRenderingDist == renderingDistance)
                return;

            controller?.loadedMap?.UpdateScale();
        }

        void FadeScaleBar()
        {
            controller.scaleBar.SetActive(false);
            controller.scaleText.gameObject.SetActive(false);
        }

        float fadeScaleBarTime = 0f;

        void UpdateScaleBar()
        {
            if (controller == null)
            {
                return;
            }

            controller.scaleBar.SetActive(true);
            controller.scaleText.gameObject.SetActive(true);

            var maxX = camera.ScreenToWorldPoint(new Vector3(100f, 0f, 0f)).x;
            var minX = camera.ScreenToWorldPoint(new Vector3(0f, 0f, 0f)).x;

            var maxLength = maxX - minX;
            var scale = 0f;
            var scaleText = "";

            if (maxLength >= 10000f * Map.Meters)
            {
                scaleText = "10km";
                scale = 10000f * Map.Meters;
            }
            else if (maxLength >= 5000f * Map.Meters)
            {
                scaleText = "5km";
                scale = 5000f * Map.Meters;
            }
            else if (maxLength >= 2000f * Map.Meters)
            {
                scaleText = "2km";
                scale = 2000f * Map.Meters;
            }
            else if (maxLength >= 1000f * Map.Meters)
            {
                scaleText = "1km";
                scale = 1000f * Map.Meters;
            }
            else if (maxLength >= 500f * Map.Meters)
            {
                scaleText = "500m";
                scale = 500f * Map.Meters;
            }
            else if (maxLength >= 200f * Map.Meters)
            {
                scaleText = "200m";
                scale = 200f * Map.Meters;
            }
            else if (maxLength >= 100f * Map.Meters)
            {
                scaleText = "100m";
                scale = 100f * Map.Meters;
            }
            else if (maxLength >= 50f * Map.Meters)
            {
                scaleText = "50m";
                scale = 50f * Map.Meters;
            }
            else if (maxLength >= 20f * Map.Meters)
            {
                scaleText = "20m";
                scale = 20f * Map.Meters;
            }
            else if (maxLength >= 10f * Map.Meters)
            {
                scaleText = "10m";
                scale = 10f * Map.Meters;
            }
            else if (maxLength >= 5f * Map.Meters)
            {
                scaleText = "5m";
                scale = 5f * Map.Meters;
            }
            else
            {
                scaleText = "1m";
                scale = 1f * Map.Meters;
            }

            controller.scaleText.text = scaleText;

            var img = controller.scaleBar.GetComponent<UnityEngine.UI.Image>();
            var rectTransform = img.rectTransform;

            rectTransform.sizeDelta = new Vector2(100 * (scale / maxLength), rectTransform.sizeDelta.y);

            fadeScaleBarTime = 3f;
        }

        public void UpdateRenderingDistance()
        {
            if (controller?.Editing ?? false)
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

            if (controlListenersEnabled)
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
            windowWidth = camera.pixelRect.width;
            windowHeight = camera.pixelRect.height;
            camera.orthographicSize = 5000f * Map.Meters;

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
        Vector3 fromPos = Vector3.zero;
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

        // Update is called once per frame
        void Update()
        {
            UpdateZoom();
            UpdatePosition();

            CheckKeyboardEvents();

            if (fadeScaleBarTime > 0f)
            {
                fadeScaleBarTime -= Time.deltaTime;
                if (fadeScaleBarTime <= 0f)
                {
                    fadeScaleBarTime = 0f;
                    FadeScaleBar();
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

            if (debugRouteTest && Input.GetMouseButtonDown(0) && controller.Viewing
                && !IsPointerOverUIElement())
            {
                var clickedPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (fromPos.Equals(Vector3.zero))
                {
                    fromPos = clickedPos;
                }
                else
                {
                    var options = new PathPlanning.PathPlanningOptions();
                    var planner = new PathPlanning.PathPlanner(options);

                    var ppResult = planner.FindClosestDrive(controller.loadedMap,
                                                            fromPos, clickedPos);

                    fromPos = Vector3.zero;
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
        }
    }
}
