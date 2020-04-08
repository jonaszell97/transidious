using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Transidious
{
    public class UIMiniMap : MonoBehaviour
    {
        public static Sprite mapSprite;
        [SerializeField] UILineRenderer lineRenderer;

        public void Initialize()
        {
            if (mapSprite == null)
            {
                mapSprite = SpriteManager.GetSprite($"Maps/{SaveManager.loadedMap.name}/minimap");
            }

            var img = GetComponent<Image>();
            img.sprite = mapSprite;
            img.preserveAspect = true;
            lineRenderer.gameObject.SetActive(false);
        }

        Vector3 GetCoordinate(Map map, float resolutionX, float resolutionY, float xOffset, float yOffset, Vector2 worldPos)
        {
            var baseX = map.minX;
            var baseY = map.minY;
            var width = map.width;
            var height = map.height;

            return new Vector2(
                xOffset + ((worldPos.x - baseX) / width) * resolutionX,
                yOffset + ((worldPos.y - baseY) / height) * resolutionY
            );
        }

        public void DrawLine(IReadOnlyList<Vector3> worldPath, Color c, float lineWidth = .4f)
        {
            var rect = GetComponent<RectTransform>().rect;
            var map = GameController.instance.loadedMap;

            var width = rect.width;
            var height = rect.height;

            var xOffset = 0f;
            var yOffset = 0f;

            // Account for gap caused by map aspect ratio.
            if (map.width >= map.height)
            {
                yOffset += ((map.width - map.height) / map.width) * height * .5f;
            }
            else
            {
                xOffset += ((map.height - map.width) / map.height) * width * .5f;
            }

            // Account for gap caused by the rect transform aspect ratio.
            if (rect.width >= rect.height)
            {
                xOffset += (rect.width - rect.height) * .5f;
            }
            else
            {
                yOffset += (rect.height - rect.width) * .5f;
            }

            var resolutionX = width - xOffset * 2f;
            var resolutionY = height - yOffset * 2f;

            var screenPositions = new Vector2[worldPath.Count];
            for (var i = 0; i < worldPath.Count; ++i)
            {
                var pt = worldPath[i];
                screenPositions[i] = GetCoordinate(map, resolutionX, resolutionY, xOffset, yOffset, pt);
            }

            lineRenderer.color = c;
            lineRenderer.LineThickness = lineWidth;

            lineRenderer.gameObject.SetActive(true);
            lineRenderer.Points = screenPositions;
        }
    }
}