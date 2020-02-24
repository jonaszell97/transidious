using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Transidious
{
    public class UILineView : MonoBehaviour
    {
        /// The line this view is for.
        public Line line;

        /// The stops to display.
        public List<Stop> stops;

        /// The maximum number of stops to display (-1 for no limit).
        public int maxStops = 5;

        /// The line background sprite.
        [SerializeField] Image lineSprite;

        /// The background sprites.
        [SerializeField] Sprite[] lineSprites;

        /// The stop sprites.
        [SerializeField] Sprite[] stopSprites;

        /// The layout group containing the stop names.
        [SerializeField] GameObject stopNameGrid;

        /// The layout group containing the stop sprites.
        [SerializeField] GameObject stopSpriteGrid;

        /// The layout group containing the crossing lines.
        [SerializeField] GameObject crossingLineGrid;

        /// The prefab for creating new stop names.
        [SerializeField] GameObject stopNamePrefab;

        /// The prefabs for creating new stop sprites.
        [SerializeField] GameObject[] stopSpritePrefabs;

        /// The prefab for creating new crossing line grids.
        [SerializeField] GameObject crossingLineGridPrefab;

        /// The prefab for creating new crossing line sprites.
        [SerializeField] GameObject crossingLinePrefab;

        /// The height of a stop sprite.
        static readonly float stopHeight = 75f;

        /// The padding of the stop grid.
        static readonly float stopGridPadding = 25f;

        public void UpdateLayout(Line line, int maxStops = -1)
        {
            Debug.Assert(line.stops.Count != 0);

            bool partialEnd = maxStops != -1 && maxStops < line.stops.Count;
            UpdateLayout(line, line.stops.GetRange(0, partialEnd ? maxStops : line.stops.Count), false, partialEnd);
        }

        public void UpdateLayout(Line line, Stop stop, int maxStops = -1)
        {
            if (maxStops == -1)
            {
                maxStops = line.stops.Count;
            }

            foreach (var s in line.stops)
            {
                s.Equals(stop);
            }

            var stopIdx = line.stops.IndexOf(stop);
            Debug.Assert(stopIdx != -1, "stop is not on line");

            var stopsPerSide = (maxStops - 1) / 2;
            var stopsBefore = Mathf.Min(stopIdx, stopsPerSide);
            var stopsAfter = Mathf.Min(line.stops.Count - stopIdx - 1, stopsPerSide);

            var stops = line.stops.GetRange(stopIdx - stopsBefore, stopsBefore + 1 + stopsAfter);
            var partialStart = stopIdx > stopsPerSide;
            var partialEnd = (line.stops.Count - stopIdx - 1) > stopsPerSide;

            UpdateLayout(line, stops, partialStart, partialEnd);
        }

        public void UpdateLayout(Line line, List<Stop> stops, bool partialStart, bool partialEnd)
        {
            this.line = line;
            this.stops = stops;

            this.stopNameGrid.RemoveAllChildren();
            this.stopSpriteGrid.RemoveAllChildren();
            this.crossingLineGrid.RemoveAllChildren();

            lineSprite = Instantiate(stopSpritePrefabs[2]).GetComponent<Image>();

            var rectTransform = lineSprite.GetComponent<RectTransform>();
            rectTransform.SetParent(stopSpriteGrid.transform, false);

            for (var i = 0; i < stops.Count; ++i)
            {
                AddStop(stops[i], (i == 0 && !partialStart), (i == stops.Count - 1 && !partialEnd));
            }

            var currentHeight = stopSpriteGrid.GetComponent<RectTransform>().rect.height;
            var cellHeight = currentHeight / stops.Count;

            var offset = 0f;
            if (!partialEnd)
            {
                offset = (cellHeight * .5f) + (stopHeight * .5f);
            }

            var requiredHeight = currentHeight;
            if (!partialStart)
            {
                requiredHeight -= (cellHeight * .5f) + stopGridPadding;
            }
            if (!partialEnd)
            {
                requiredHeight -= (cellHeight * .5f) + stopGridPadding;
            }

            rectTransform.localScale = new Vector3(1f,
                requiredHeight / currentHeight,
                1f);

            rectTransform.anchoredPosition = new Vector2(0f, offset);
            lineSprite.color = line.color;

            if (partialStart && partialEnd)
            {
                lineSprite.sprite = lineSprites[1];
            }
            else if (partialStart)
            {
                lineSprite.sprite = lineSprites[3];
            }
            else if (partialEnd)
            {
                lineSprite.sprite = lineSprites[2];
            }
            else
            {
                lineSprite.sprite = lineSprites[0];
            }
        }

        void AddStop(Stop stop, bool firstOnLine = false, bool lastOnLine = false)
        {
            var name = Instantiate(stopNamePrefab);
            name.transform.SetParent(stopNameGrid.transform, false);
            name.GetComponent<TMP_Text>().text = stop.name;

            var link = name.GetComponent<UILocationLink>();
            link.SetLocation(stop.location);
            link.postMoveListener = () =>
            {
                stop.ActivateModal();
            };

            //name.transform.localScale = new Vector3(1f, 1f, 1f);
            //name.transform.localPosition = new Vector3(0, 0, 0);

            var firstOrLast = firstOnLine || lastOnLine;
            var sprite = Instantiate(stopSpritePrefabs[(stop.lineData.Count > 1 || firstOrLast) ? 1 : 0]);
            sprite.transform.SetParent(stopSpriteGrid.transform, false);
            sprite.transform.localScale = new Vector3(1f, 1f, 1f);
            sprite.transform.localPosition = new Vector3(0, 0, 0);

            if (!firstOrLast && stop.lineData.Count == 1)
            {
                sprite.GetComponentInChildren<Image>().color = this.line.color;
            }

            var crossingLines = Instantiate(crossingLineGridPrefab);
            crossingLines.transform.SetParent(crossingLineGrid.transform, false);
            //crossingLines.transform.localScale = new Vector3(1f, 1f, 1f);
            //crossingLines.transform.localPosition = new Vector3(0, 0, 0);

            var lineCount = 0;
            foreach (var entry in stop.lineData)
            {
                if (entry.Key == this.line)
                {
                    continue;
                }

                var crossingLine = Instantiate(crossingLinePrefab);
                crossingLine.transform.SetParent(crossingLines.transform, false);
                //crossingLine.transform.localScale = new Vector3(1f, 1f, 1f);
                //crossingLine.transform.localPosition = new Vector3(0, 0, 0);

                var lineLogo = crossingLine.GetComponentInChildren<UILineLogo>();
                lineLogo.SetLine(entry.Key, true);

                if (++lineCount == 3)
                {
                    break;
                }
            }
        }
    }
}