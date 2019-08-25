using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious
{
    public class UIModal : MonoBehaviour
    {
        /// The background color of the modal.
        public Color backgroundColor = Color.gray;

        /// Event listener for the close event.
        public UnityEvent onClose;

        /// The title input field.
        public TMP_InputField titleInput;

        /// The close button.
        [SerializeField] Button closeButton;

        /// The canvas that contains the modal's content.
        [SerializeField] Canvas contentCanvas;

        /// The modal header.
        [SerializeField] Image headerImg;

        /// The modal body.
        [SerializeField] Image bodyImg;

        /// The modal arrow image.
        [SerializeField] Image arrowImg;

        /// The arrow at the bottom of the modal.
        [SerializeField] GameObject arrow;

        /// If true, the modal is resized to always occupy the same size on the screen.
        [SerializeField] bool ResizeWithZoom = true;

        /// If true, this modal can be dragged around by the header.
        [SerializeField] bool draggable = true;

        /// If true, stick to the original position when zooming or moving.
        [SerializeField] bool stickToPosition = true;

        /// For clients of this modal that want to perform additional initialization.
        public bool initialized = false;

        /// If true, this modal is visible by default.
        public bool visibleByDefault = false;

        /// Static list of all modals. Used to ensure only one is active at a time.
        static List<UIModal> modals;

        [SerializeField] Vector3 baseScale = Vector3.zero;
        [SerializeField] float baseOrthoSize = -1f;

        float orthoSize = -1f;
        Vector3? position;

        bool active = false;
        int zoomListenerID = -1;
        int panListenerID = -1;

        void Awake()
        {
            if (modals == null)
            {
                modals = new List<UIModal>();
            }

            modals.Add(this);
            onClose = new UnityEvent();
            closeButton.onClick.AddListener(this.Disable);

            if (baseOrthoSize.Equals(-1f))
            {
                baseOrthoSize = Camera.main.orthographicSize;
            }
            if (baseScale.Equals(Vector3.zero))
            {
                baseScale = transform.localScale;
            }

            if (draggable)
            {
                headerImg.gameObject.GetComponent<Draggable>().dragHandler = (Vector2 screenPos) =>
                {
                    arrow.SetActive(false);
                    MoveTo(Camera.main.ScreenToWorldPoint(screenPos));
                };
            }

            SetColor(backgroundColor);
        }

        void Start()
        {
            if (!visibleByDefault)
            {
                active = false;
                gameObject.SetActive(false);
            }
            else
            {
                active = true;
            }
        }

        // void Update()
        // {
        //     if (active && Input.GetMouseButton(0) && !GameController.instance.input.IsPointerOverUIElement())
        //     {
        //         Disable();
        //     }
        // }

        public void SetColor(Color color)
        {
            this.backgroundColor = color;
            this.headerImg.color = color;
            this.bodyImg.color = color;
            this.arrowImg.color = Math.ApplyTransparency(new Color(color.r, color.g, color.b), color.a);
        }

        enum ArrowPosition
        {
            BottomLeft,
            TopLeft,
            TopRight,
            BottomRight,
        }

        ArrowPosition GetBestArrowPosition(Vector3 worldPos, float width, float height)
        {
            var rectTransform = GetComponent<RectTransform>();

            var screenMax = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));
            var screenMin = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));

            if (worldPos.y + height <= screenMax.y)
            {
                if (worldPos.x + width <= screenMax.x)
                {
                    return ArrowPosition.BottomLeft;
                }

                return ArrowPosition.BottomRight;
            }

            if (worldPos.x + width <= screenMax.x)
            {
                return ArrowPosition.TopLeft;
            }

            return ArrowPosition.TopRight;
        }

        public void PositionAt(Vector3 pos, bool stick = true)
        {
            var corners = new Vector3[4];
            GetComponent<RectTransform>().GetWorldCorners(corners);

            var width = corners[3].x - corners[0].x;
            var height = corners[2].y - corners[0].y;

            arrow.SetActive(true);
            arrow.GetComponent<RectTransform>().GetWorldCorners(corners);

            var arrowOffset = .1f;
            var arrowWidth = corners[3].x - corners[0].x;
            var arrowHeight = corners[2].y - corners[0].y;
            var arrowPos = GetBestArrowPosition(pos, width, height);

            bool flip;
            float arrowX, arrowY, x, y;
            switch (arrowPos)
            {
            case ArrowPosition.BottomLeft:
                flip = false;
                arrowX = width * arrowOffset;
                arrowY = 0;//-arrowHeight; // * .5f;
                x = pos.x - (width * arrowOffset) + (width * .5f) - (arrowWidth * .5f);
                y = pos.y + (height * .5f) + arrowHeight;
                break;
            case ArrowPosition.TopLeft:
                flip = true;
                arrowX = width * arrowOffset;
                arrowY = height;
                x = pos.x - (width * arrowOffset) + (width * .5f) - (arrowWidth * .5f);
                y = pos.y - (height * .5f) + arrowHeight;
                break;
            case ArrowPosition.BottomRight:
                flip = false;
                arrowX = width * (1f - arrowOffset);
                arrowY = 0;//-arrowHeight; // * .5f;
                x = pos.x + (width * arrowOffset) - (width * .5f) - (arrowWidth * .5f);
                y = pos.y + (height * .5f) + arrowHeight;
                break;
            case ArrowPosition.TopRight:
                flip = true;
                arrowX = width * (1f - arrowOffset);
                arrowY = height;
                x = pos.x + (width * arrowOffset) - (width * .5f) - (arrowWidth * .5f);
                y = pos.y - (height * .5f) + arrowHeight;
                break;
            default:
                Debug.Assert(false, "invalid arrow position");
                return;
            }

            if (flip)
            {
                arrow.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            }
            else
            {
                arrow.transform.rotation = new Quaternion();
            }

            this.position = pos;
            this.stickToPosition = stick;

            this.transform.position = new Vector3(x, y, this.transform.position.z);
            arrow.transform.position = new Vector3(this.transform.position.x - (width * 0.5f) + arrowX,
                                                   this.transform.position.y - (height * 0.5f) + arrowY,
                                                   arrow.transform.position.z);
        }

        void MoveTo(Vector3 pos)
        {
            var corners = new Vector3[4];
            GetComponent<RectTransform>().GetWorldCorners(corners);

            var height = corners[2].y - corners[0].y;
            this.transform.position = new Vector3(pos.x,
                                                  pos.y - (height * .5f),
                                                  transform.position.z);

            stickToPosition = false;
        }

        public void Enable()
        {
            if (active)
            {
                return;
            }

            // Close other modals.
            foreach (var modal in modals)
            {
                if (modal == this)
                {
                    continue;
                }

                modal.Disable();
            }

            if (zoomListenerID == -1)
            {
                zoomListenerID = GameController.instance?.input?.RegisterEventListener(
                    InputEvent.Zoom, (DynamicMapObject _) =>
                {
                    if (!gameObject.activeSelf)
                    {
                        return;
                    }

                    AdjustSize();
                    AdjustPosition();
                }) ?? -1;
                panListenerID = GameController.instance?.input?.RegisterEventListener(
                    InputEvent.Pan, (DynamicMapObject _) =>
                {
                    if (!gameObject.activeSelf)
                    {
                        return;
                    }

                    AdjustPosition();
                }) ?? -1;
            }

            AdjustSize();
            this.gameObject.SetActive(true);

            GameController.instance?.input?.EnableEventListener(zoomListenerID);
            active = true;
        }

        public void Disable()
        {
            if (!active)
            {
                return;
            }

            this.gameObject.SetActive(false);
            GameController.instance?.input?.DisableEventListener(zoomListenerID);
            this.onClose.Invoke();
            active = false;
        }

        public void SetTitle(string title, bool editable = false)
        {
            this.titleInput.text = title;
            this.titleInput.interactable = editable;
        }

        void AdjustSize()
        {
            if (!ResizeWithZoom || orthoSize == Camera.main.orthographicSize)
            {
                return;
            }

            orthoSize = Camera.main.orthographicSize;

            var scale = orthoSize / baseOrthoSize;
            transform.localScale = new Vector3(baseScale.x * scale, baseScale.y * scale, baseScale.z);
        }

        void AdjustPosition()
        {
            if (stickToPosition && position.HasValue)
            {
                PositionAt(position.Value);
            }
        }
    }
}