using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class ColorPicker : MonoBehaviour
    {
        public class ChangeEvent : UnityEvent<Color>
        {
            public ChangeEvent()
            {
            }
        }

        public Color borderColor = Color.gray;
        public int borderWidth = 2;
        public GameObject hueCursor;
        public GameObject rgbCursor;
        public GameObject hue;
        public GameObject rgb;
        public Image hueBackground;
        public Image selectedColor;
        public TMP_InputField selectedColorText;
        bool newlySelected;

        Canvas canvas;
        float[] rgbBounds;
        float[] hueBounds;
        float orthoSize;
        Vector2 cameraPos;
        float r, g, b;
        float h, s, v;

        public ChangeEvent onChange;

        public Color SelectedColor
        {
            get
            {
                return new Color(r, g, b);
            }
        }

        char ValidateHexInput(string text, int charIndex, char addedChar)
        {
            if (newlySelected)
            {
                text = "";
                selectedColorText.SetTextWithoutNotify("");
                newlySelected = false;
            }
            else if (text.Length == 7)
            {
                return '\0';
            }

            if (addedChar == '#' && text.Length == 0)
            {
                return addedChar;
            }

            bool valid = false;
            if (addedChar >= '0' && addedChar <= '9')
            {
                valid = true;
            }
            else if (addedChar >= 'A' && addedChar <= 'F')
            {
                valid = true;
            }
            else if (addedChar >= 'a' && addedChar <= 'f')
            {
                valid = true;
                addedChar = (char)('A' + (addedChar - 'a'));
            }

            if (valid)
            {
                return addedChar;
            }

            return '\0';
        }

        void SelectHexInput(string text)
        {
            if (text.Length != 0 && text[0] != '#')
            {
                text = "#" + text;
            }

            if (ColorUtility.TryParseHtmlString(text, out Color c))
            {
                SetColor(c, true);
                return;
            }

            selectedColorText.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(SelectedColor));
        }

        Texture2D CreateHueTexture()
        {
            var rectTransform = rgb.GetComponent<RectTransform>();
            var width = (int)rectTransform.rect.width;
            var height = (int)rectTransform.rect.height;
            var tex = new Texture2D(width, height);

            for (int y = 0; y < height; ++y)
            {
                var color = Color.HSVToRGB((float)y / (float)height, 1, 1);
                for (int x = 0; x < width; ++x)
                {
                    if (x < borderWidth || y < borderWidth || x >= width - borderWidth || y >= height - borderWidth)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            tex.Apply();
            System.IO.File.WriteAllBytes("/home/jonas/Downloads/hue_mask.png", tex.EncodeToPNG());

            return tex;
        }

        Texture2D CreateSatBrightTexture()
        {
            var rectTransform = hue.GetComponent<RectTransform>();
            var width = (int)rectTransform.rect.width;
            var height = (int)rectTransform.rect.height;
            var tex = new Texture2D(width, height);
            var threshold = (1f / 6f) * height;

            for (int y = 0; y < height; ++y)
            {
                var yVal = (float)y / (float)height;
                for (int x = 0; x < width; ++x)
                {
                    var xVal = (float)x / (float)width;
                    if (x < borderWidth || y < borderWidth || x >= width - borderWidth || y >= height - borderWidth)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        float alpha;
                        if (x < threshold || y < threshold)
                        {
                            alpha = 1f;
                        }
                        else
                        {
                            alpha = 1f - Mathf.Min((float)(x - threshold) / (float)(width - threshold),
                                                   (float)(y - threshold) / (float)(height - threshold));
                        }

                        var color = new Color(yVal, yVal, yVal, alpha);
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            tex.Apply();
            System.IO.File.WriteAllBytes("/home/jonas/Downloads/satbright_mask.png", tex.EncodeToPNG());

            return tex;
        }

        public void SetColor(Color rgb, bool fireEvent = false)
        {
            UpdateBoundingBoxes();

            Color.RGBToHSV(rgb, out float h, out float s, out float v);
            OnDrag_RGBCursor_World(new Vector2(0, rgbBounds[0] + h * (rgbBounds[1] - rgbBounds[0])));
            OnDrag_HueCursor_World(new Vector2(hueBounds[0] + s * (hueBounds[1] - hueBounds[0]),
                                               hueBounds[2] + v * (hueBounds[3] - hueBounds[2])));

            if (fireEvent)
            {
                this.onChange.Invoke(rgb);
            }
        }

        public void SetColor(string hexString, bool fireEvent = false)
        {
            Color rgb;
            if (ColorUtility.TryParseHtmlString(hexString, out rgb))
            {
                SetColor(rgb, fireEvent);
            }
        }

        Rect GetWorldBoundingRect(RectTransform rectTransform)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                return new Rect(corners[0].x, corners[0].y, corners[3].x - corners[0].x, corners[2].y - corners[0].y);
            }
            else
            {
                var transformedPos = Camera.main.ScreenToWorldPoint(new Vector2(
                    rectTransform.position.x, rectTransform.position.y - rectTransform.rect.height));
                var baseSize = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
                var transformedSize = Camera.main.ScreenToWorldPoint(
                    new Vector2(rectTransform.rect.width, rectTransform.rect.height));

                return new Rect(transformedPos.x, transformedPos.y,
                                transformedSize.x - baseSize.x,
                                transformedSize.y - baseSize.y);
            }
        }

        public void UpdateBoundingBoxes(bool force = false)
        {
            if (!force)
            {
                if (!gameObject.activeInHierarchy)
                {
                    return;
                }
                if ((orthoSize == Camera.main.orthographicSize) && cameraPos.Equals(Camera.main.transform.position))
                {
                    return;
                }
            }

            {
                var rgbTransform = rgb.GetComponent<RectTransform>();
                var rect = GetWorldBoundingRect(rgbTransform);

                rgbBounds = new float[] {
                    rect.y,
                    rect.y + rect.height,
                };

                var rgbCursorTransform = rgbCursor.GetComponent<RectTransform>();
                rect = GetWorldBoundingRect(rgbCursorTransform);

                var halfCursorHeight = rect.height * .5f;
                rgbBounds[1] -= halfCursorHeight;
                rgbBounds[0] += halfCursorHeight;
            }
            {
                var hueTransform = hue.GetComponent<RectTransform>();
                var rect = GetWorldBoundingRect(hueTransform);

                hueBounds = new float[] {
                    rect.x,
                    rect.x + rect.width,
                    rect.y,
                    rect.y + rect.height,
                };
            }

            orthoSize = Camera.main.orthographicSize;
            cameraPos = Camera.main.transform.position;
        }

        void Awake()
        {
            this.canvas = GetComponentInParent<Canvas>();
            this.onChange = new ChangeEvent();

            var hueSprite = CreateHueTexture();
            rgb.GetComponent<Image>().sprite = Sprite.Create(
                hueSprite,
                new Rect(0, 0, hueSprite.width, hueSprite.height),
                new Vector2(hueSprite.width / 2, hueSprite.height / 2));

            // var satBrightSprite = CreateSatBrightTexture();
            // hue.GetComponent<Image>().sprite = Sprite.Create(
            //     satBrightSprite,
            //     new Rect(0, 0, satBrightSprite.width, satBrightSprite.height),
            //     new Vector2(satBrightSprite.width / 2, satBrightSprite.height / 2));

            selectedColorText.onValidateInput = this.ValidateHexInput;
            selectedColorText.onSubmit.AddListener(this.SelectHexInput);
            selectedColorText.onSelect.AddListener((string txt) =>
            {
                this.newlySelected = true;
            });
        }

        void Start()
        {
            rgb.GetComponent<Draggable>().dragHandler = this.OnDrag_RGBCursor;
            hue.GetComponent<Draggable>().dragHandler = this.OnDrag_HueCursor;

            rgb.GetComponent<Clickable>().mouseDown = this.OnDrag_RGBCursor;
            hue.GetComponent<Clickable>().mouseDown = this.OnDrag_HueCursor;

            orthoSize = -1f;
            cameraPos = Vector3.positiveInfinity;
            UpdateBoundingBoxes();

            OnDrag_RGBCursor_World(rgbCursor.transform.position);
            OnDrag_HueCursor_World(hueCursor.transform.position);
        }

        void UpdateHue(Vector2 cursorScreenPos)
        {
            var height = rgbBounds[1] - rgbBounds[0];
            var relativePos = cursorScreenPos.y - rgbBounds[0];
            var percentage = Mathf.Clamp(relativePos / height, 0f, 1f);

            h = percentage;
            s = 1;
            v = 1;

            hueBackground.color = Color.HSVToRGB(h, s, v);
        }

        void UpdateSaturationAndBrightness(Vector2 cursorScreenPos)
        {
            var width = hueBounds[1] - hueBounds[0];
            var height = hueBounds[3] - hueBounds[2];
            var relativePosX = cursorScreenPos.x - hueBounds[0];
            var relativePosY = cursorScreenPos.y - hueBounds[2];
            var percentageX = Mathf.Clamp(relativePosX / width, 0f, 1f);
            var percentageY = Mathf.Clamp(relativePosY / height, 0f, 1f);

            s = percentageX;
            v = percentageY;

            var result = Color.HSVToRGB(h, s, v);
            selectedColor.color = result;
            selectedColorText.text = "#" + ColorUtility.ToHtmlStringRGB(result);

            var textColor = Math.ContrastColor(result);
            selectedColorText.textComponent.color = textColor;
            selectedColorText.textComponent.outlineColor = textColor;

            r = result.r;
            g = result.g;
            b = result.b;
        }

        Vector3 RGBCursorPos
        {
            get
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    return rgbCursor.transform.position;
                }
                else
                {
                    return Camera.main.ScreenToWorldPoint(rgbCursor.transform.position);
                }
            }
        }

        Vector3 SatBrightCursorPos
        {
            get
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    return hueCursor.transform.position;
                }
                else
                {
                    return Camera.main.ScreenToWorldPoint(hueCursor.transform.position);
                }
            }
        }

        void OnDrag_HueCursor(Vector2 screenPos)
        {
            UpdateBoundingBoxes();

            OnDrag_HueCursor_World(Camera.main.ScreenToWorldPoint(screenPos));
            OnDrag_RGBCursor_World(RGBCursorPos);

            this.onChange.Invoke(SelectedColor);
        }

        void OnDrag_HueCursor_World(Vector2 worldPos)
        {
            var x = Mathf.Clamp(worldPos.x, hueBounds[0], hueBounds[1]);
            var y = Mathf.Clamp(worldPos.y, hueBounds[2], hueBounds[3]);

            Vector2 newScreenPos;
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                newScreenPos = new Vector2(x, y);
            }
            else
            {
                newScreenPos = Camera.main.WorldToScreenPoint(new Vector2(x, y));
            }

            hueCursor.transform.position = new Vector3(newScreenPos.x, newScreenPos.y,
                                                       hueCursor.transform.position.z);

            UpdateSaturationAndBrightness(worldPos);
        }

        void OnDrag_RGBCursor(Vector2 screenPos)
        {
            UpdateBoundingBoxes();

            OnDrag_RGBCursor_World(Camera.main.ScreenToWorldPoint(screenPos));
            OnDrag_HueCursor_World(SatBrightCursorPos);

            this.onChange.Invoke(SelectedColor);
        }

        void OnDrag_RGBCursor_World(Vector2 worldPos)
        {
            var y = Mathf.Clamp(worldPos.y, rgbBounds[0], rgbBounds[1]);

            float screenY;
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                screenY = y;
            }
            else
            {
                screenY = Camera.main.WorldToScreenPoint(new Vector2(0, y)).y;
            }

            rgbCursor.transform.position = new Vector3(rgbCursor.transform.position.x,
                                                       screenY,
                                                       rgbCursor.transform.position.z);

            UpdateHue(worldPos);
        }
    }
}