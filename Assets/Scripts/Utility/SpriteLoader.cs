using System.Collections;
using System.IO;
using UnityEngine;

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

        public static IEnumerator LoadNewSpriteAsync(string FilePath, float PixelsPerUnit = 100.0f)
        {
            var asyncTexture = new DataCoroutine<Texture2D>(LoadTextureAsync(FilePath));
            yield return asyncTexture;

            var SpriteTexture = asyncTexture.result;
            if (!SpriteTexture)
            {
                Debug.LogError("Sprite not found: " + FilePath);
                yield break;
            }

            yield return Sprite.Create(
                SpriteTexture,
                new Rect(0, 0, SpriteTexture.width, SpriteTexture.height),
                new Vector2(0.5f, 0.5f), PixelsPerUnit);
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

        public static IEnumerator LoadTextureAsync(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                yield break;
            }

            using (var stream = File.OpenRead(FilePath))
            {
                var data = new byte[stream.Length];
                yield return stream.ReadAsync(data, 0, (int)stream.Length);

                var Tex2D = new Texture2D(0, 0);
                if (!Tex2D.LoadImage(data))
                {
                    yield break;
                }

                yield return Tex2D;
            }
        }
    }
}