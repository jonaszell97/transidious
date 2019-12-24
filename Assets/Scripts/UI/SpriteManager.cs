using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Transidious
{
    public class SpriteManager : MonoBehaviour
    {
        /// Prefab for creating sprites.
        public GameObject spritePrefab;

        /// Sprites for the play/pause button.
        public Sprite playSprite;
        public Sprite pauseSprite;
        public Sprite uiButtonSprite;

        /// The simulation speed sprites.
        public Sprite[] simSpeedSprites;

        /// The traffic light sprites.
        public Sprite[] trafficLightSprites;

        /// The car sprites.
        public Sprite[] carSpritesOutlined;

        public Sprite[] carSprites;

        /// The street direction arrow sprite.
        public Sprite streetArrowSprite;

        public Sprite squareSprite;
        public Sprite roundedRectSprite;
        public Sprite[] happinessSprites;

        public static SpriteManager instance;
        Dictionary<string, Sprite> otherSprites;

        void Awake()
        {
            otherSprites = new Dictionary<string, Sprite>();
            instance = this;
        }

        public static Sprite GetSprite(string name)
        {
            return instance.GetSpriteImpl(name);
        }

        Sprite GetSpriteImpl(string name)
        {
            if (otherSprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }

            var tex = (Texture2D)Resources.Load(name);
            if (tex == null)
            {
                return null;
            }

            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            if (sprite != null)
            {
                otherSprites.Add(name, sprite);
            }

            return sprite;
        }

        public static GameObject CreateSprite(Sprite s)
        {
            return instance.CreateSpriteImpl(s);
        }

        GameObject CreateSpriteImpl(Sprite s)
        {
            var obj = Instantiate(spritePrefab);
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = s;

            return obj;
        }
    }
}