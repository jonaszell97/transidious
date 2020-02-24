using UnityEngine;
using UnityEngine.Events;
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
        public GameObject satValCursor;
        public GameObject hueMask;
        public GameObject satValMask;
        public Image hueBackground;
        public Image selectedColor;
        public TMP_InputField selectedColorText;
        bool newlySelected;

        Canvas canvas;
        float[] hueBounds;
        float[] satValBounds;
        float hueSize;
        Vector2 satValSize;
        Vector2 pos;
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
            var rectTransform = satValMask.GetComponent<RectTransform>();
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
            // System.IO.File.WriteAllBytes("/home/jonas/Downloads/hue_mask.png", tex.EncodeToPNG());

            return tex;
        }

        Texture2D CreateSatBrightTexture()
        {
            var rectTransform = hueMask.GetComponent<RectTransform>();
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
            // System.IO.File.WriteAllBytes("/home/jonas/Downloads/satbright_mask.png", tex.EncodeToPNG());

            return tex;
        }

        public void SetColor(Color rgb, bool fireEvent = false)
        {
            Color.RGBToHSV(rgb, out float h, out float s, out float v);
            OnHueCursorDrag(new Vector2(0, hueBounds[0] + h * (hueBounds[1] - hueBounds[0])));
            OnSatValCursorDrag(new Vector2(satValBounds[0] + s * (satValBounds[1] - satValBounds[0]),
                                           satValBounds[2] + v * (satValBounds[3] - satValBounds[2])));

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

        public void UpdateBoundingBoxes(bool force = false)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (!force)
            {
                if (!gameObject.activeInHierarchy)
                {
                    return;
                }
                if (screenPos.Equals(this.pos))
                {
                    return;
                }
            }

            {
                var hueTransform = hueMask.GetComponent<RectTransform>();
                var rect = Utility.RectTransformToScreenSpace(Camera.main, hueTransform);

                hueBounds = new float[] {
                    rect.y,
                    rect.y + rect.height,
                };

                hueSize = Mathf.Abs(hueTransform.rect.height);
            }
            {
                var satValTransform = satValMask.GetComponent<RectTransform>();
                var rect = Utility.RectTransformToScreenSpace(Camera.main, satValTransform);

                satValBounds = new float[] {
                    rect.x,
                    rect.x + rect.width,
                    rect.y,
                    rect.y + rect.height,
                };

                satValSize = new Vector2(Mathf.Abs(satValTransform.rect.width),
                                         Mathf.Abs(satValTransform.rect.height));
            }

            this.pos = screenPos;
        }

        void Awake()
        {
            this.canvas = GetComponentInParent<Canvas>();
            this.onChange = new ChangeEvent();

            var hueSprite = CreateHueTexture();
            hueMask.GetComponent<Image>().sprite = Sprite.Create(
                hueSprite,
                new Rect(0, 0, hueSprite.width, hueSprite.height),
                new Vector2(hueSprite.width / 2, hueSprite.height / 2));

            selectedColorText.onValidateInput = this.ValidateHexInput;
            selectedColorText.onSubmit.AddListener(this.SelectHexInput);
            selectedColorText.onSelect.AddListener((string txt) =>
            {
                this.newlySelected = true;
            });
        }

        void Start()
        {
            satValMask.GetComponent<Draggable>().dragHandler = this.OnSatValCursorDrag;
            hueMask.GetComponent<Draggable>().dragHandler = this.OnHueCursorDrag;

            satValMask.GetComponent<Clickable>().mouseDown = this.OnSatValCursorDrag;
            hueMask.GetComponent<Clickable>().mouseDown = this.OnHueCursorDrag;

            UpdateBoundingBoxes();
            UpdateHue(0f);
            UpdateSaturationAndBrightness(0f, 0f);
        }

        void UpdateHue(float percentage)
        {
            h = percentage;
            s = 1;
            v = 1;

            hueBackground.color = Color.HSVToRGB(h, s, v);
        }

        void UpdateSaturationAndBrightness(float percentageX, float percentageY)
        {
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

            onChange.Invoke(result);
        }

        void OnHueCursorDrag(Vector2 screenPos)
        {
            UpdateBoundingBoxes();

            var min = hueBounds[0];
            var max = hueBounds[1];
            var y = Mathf.Clamp(screenPos.y, min, max);

            var percentage = (y - min) / (max - min);

            var rect = hueCursor.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, percentage * hueSize);

            UpdateHue(percentage);
            UpdateSaturationAndBrightness(s, v);
        }

        void OnSatValCursorDrag(Vector2 screenPos)
        {
            UpdateBoundingBoxes();

            var minX = satValBounds[0];
            var maxX = satValBounds[1];
            var x = Mathf.Clamp(screenPos.x, minX, maxX);

            var minY = satValBounds[2];
            var maxY = satValBounds[3];
            var y = Mathf.Clamp(screenPos.y, minY, maxY);

            var percentageX = (x - minX) / (maxX - minX);
            var percentageY = (y - minY) / (maxY - minY);

            var rect = satValCursor.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(percentageX * satValSize.x,
                                                percentageY * satValSize.y);

            UpdateSaturationAndBrightness(percentageX, percentageY);
        }
    }
}