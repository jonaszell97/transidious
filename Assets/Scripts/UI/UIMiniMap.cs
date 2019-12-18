using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious
{
    public class UIMiniMap : MonoBehaviour
    {
        public static Texture2D mapTexture;

        void Start()
        {
            if (mapTexture == null)
            {
                Debug.LogWarning("no mini map texture loaded");
                return;
            }

            Debug.Log(mapTexture.GetPixel(mapTexture.width / 2, mapTexture.height / 2));

            var img = GetComponent<Image>();
            img.sprite = Sprite.Create(mapTexture, new Rect(0, 0, mapTexture.width, mapTexture.height), new Vector2(.5f, .5f));
            img.preserveAspect = true;
        }

        public void DrawLine(IReadOnlyList<Vector3> worldPath, Color c, float width = 5f)
        {
            var renderer = gameObject.GetComponent<LineRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<LineRenderer>();
            }

            renderer.positionCount = worldPath.Count;

            var cam = Camera.main;
            var rt = GetComponent<RectTransform>();
            var worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            var map = GameController.instance.loadedMap;
            var screenMin = cam.WorldToScreenPoint(worldCorners[0]);
            var screenMax = cam.WorldToScreenPoint(worldCorners[2]);
            var screenWidth = screenMax.x - screenMin.x;
            var screenHeight = screenMax.y - screenMin.y;
            
            var aspect = (float)mapTexture.width / (float)mapTexture.height;
            if (aspect > 1f)
            {
                var newHeight = ((float)mapTexture.height / (float)mapTexture.width) * screenWidth;
                screenMin.y += (screenHeight - screenHeight) / 2;
                screenHeight = newHeight;
            }
            else if (aspect < 1f)
            {
                var newWidth = aspect * screenHeight;
                screenMin.x += (screenWidth - newWidth) / 2;
                screenWidth = newWidth;
            }

            var screenPositions = new Vector3[worldPath.Count];
            for (var i = 0; i < worldPath.Count; ++i)
            {
                var pt = worldPath[i];
                screenPositions[i] = new Vector3(
                    screenMin.x + ((pt.x - map.minX) / map.width) * screenWidth,
                    screenMin.y + ((pt.y - map.minY) / map.height) * screenHeight,
                    0f
                );

                screenPositions[i] = cam.ScreenToWorldPoint(screenPositions[i]);
                screenPositions[i].z = cam.transform.position.z - 30f;
            }

            renderer.SetPositions(screenPositions);
            renderer.sharedMaterial = GameController.GetUnlitMaterial(c);
            renderer.startWidth = width;
            renderer.endWidth = width;
        }
    }
}