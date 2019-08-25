using UnityEngine;
using System;

namespace Transidious
{
    public class Tooltip : MonoBehaviour
    {
        GameController game;
        public Text text;
        DynamicMapObject attachedObject;
        SpriteRenderer spriteRenderer;

        public Text Text
        {
            get
            {
                return text;
            }
        }

        public void Initialize(GameController game, Text text,
                               Color backgroundColor,
                               DynamicMapObject attachedObject = null)
        {
            this.game = game;
            this.attachedObject = attachedObject;
            this.spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
            this.spriteRenderer.sprite = game.uiButtonSprite;
            this.spriteRenderer.color = backgroundColor;
            this.spriteRenderer.drawMode = SpriteDrawMode.Sliced;

            SetText(text);

            if (attachedObject != null)
            {
                SetPosition(attachedObject.transform.position);
            }
            else
            {
                SetPosition(game.input.NativeCursorPosition);
            }
        }

        public void SetText(Text text)
        {
            if (text == null)
            {
                return;
            }

            this.text = text;

            var size = this.text.Size;
            size.x += 2f;
            size.y += 1f;

            this.spriteRenderer.size = size;
        }

        public void UpdateText(string txt)
        {
            this.text.SetText(txt);

            var size = this.text.Size;
            size.x += 2f;
            size.y += 1f;

            this.spriteRenderer.size = size;
        }

        public void SetPosition(Vector3 position)
        {
            position.y -= 7f;

            position.z = Map.Layer(MapLayer.Foreground, 0);
            this.transform.position = position;

            if (text != null)
            {
                position.z = Map.Layer(MapLayer.Foreground, 1);
                text.transform.position = position;
            }
        }

        void Update()
        {
            var scale = game.input.GetScreenSpaceFontScale();
            this.transform.localScale = new Vector3(scale, scale);

            if (text != null)
            {
                text.textMesh.transform.localScale = this.transform.localScale;
            }
        }

        public void Display()
        {
            gameObject.SetActive(true);

            if (text != null)
            {
                text.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            if (text != null)
            {
                text.gameObject.SetActive(false);
            }
        }
    }
}