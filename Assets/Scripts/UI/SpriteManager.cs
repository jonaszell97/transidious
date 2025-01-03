﻿using System.Collections;
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

            sprite = Resources.Load<Sprite>(name);
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
        
        public static GameObject CreateSprite(string s)
        {
            return instance.CreateSpriteImpl(GetSprite(s));
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