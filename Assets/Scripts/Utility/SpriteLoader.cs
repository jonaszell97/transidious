using UnityEngine;
using System.IO;

namespace Transidious
{
    public class SpriteLoader
    {
        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f)
        {
            Texture2D SpriteTexture = LoadTexture(FilePath);
            if (!SpriteTexture)
            {
                Debug.LogError("Sprite not found: " + FilePath);
                return null;
            }

            Sprite NewSprite = Sprite.Create(
                SpriteTexture,
                new Rect(0, 0, SpriteTexture.width, SpriteTexture.height),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);

            return NewSprite;
        }

        public static Texture2D LoadTexture(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var FileData = File.ReadAllBytes(FilePath);
            var Tex2D = new Texture2D(2, 2);

            if (!Tex2D.LoadImage(FileData))
            {
                return null;
            }

            return Tex2D;
        }
    }
}