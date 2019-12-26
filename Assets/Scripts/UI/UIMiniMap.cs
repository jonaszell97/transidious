using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Transidious
{
    public class UIMiniMap : MonoBehaviour
    {
        public static Sprite mapSprite;
        [SerializeField] UILineRenderer lineRenderer;

        void Start()
        {
            if (mapSprite == null)
            {
                Debug.LogWarning("no mini map texture loaded");
                return;
            }

            var img = GetComponent<Image>();
            img.sprite = mapSprite;
            img.preserveAspect = true;
            lineRenderer.gameObject.SetActive(false);
        }

        public void DrawLine(IReadOnlyList<Vector3> worldPath, Color c, float lineWidth = .4f)
        {
            var rt = GetComponent<RectTransform>();
            var rect = rt.rect;
            var map = GameController.instance.loadedMap;

            var width = rect.width;
            var height = rect.height;

            var minX = 0f;
            var minY = (height - width) * .5f;

            height -= (height - width);

            var screenPositions = new Vector2[worldPath.Count];
            for (var i = 0; i < worldPath.Count; ++i)
            {
                var pt = worldPath[i];
                screenPositions[i] = new Vector3(
                    minX + (map.minX + pt.x / map.width) * width,
                    minY + (map.minY + pt.y / map.height) * height
                );
            }

            lineRenderer.color = c;
            lineRenderer.LineThickness = lineWidth;

            lineRenderer.gameObject.SetActive(true);
            lineRenderer.Points = screenPositions;
        }
    }
}