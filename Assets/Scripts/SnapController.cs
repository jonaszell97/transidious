using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    // BRING ME THANOOOOS!
    public class SnapController
    {
        abstract class ActiveSnap
        {
            internal int id;
            internal Sprite snapCursor;
            internal Color snapCursorColor;
            internal Vector3 snapCursorScale;

            internal abstract Type GetApplicableType();
        }

        class StreetSnap : ActiveSnap
        {
            internal bool snapToEnd;
            internal bool snapToLane;
            internal bool snapToRivers;

            internal override Type GetApplicableType()
            {
                return typeof(StreetSegment);
            }
        }

        class MapObjectSnap : ActiveSnap
        {
            internal Type type;

            internal override Type GetApplicableType()
            {
                return type;
            }
        }

        GameController game;
        int snapCount;
        List<ActiveSnap> activeSnaps;
        HashSet<int> disabledSnaps;

        public SnapController(GameController game)
        {
            this.game = game;
            this.snapCount = 0;
            this.activeSnaps = new List<ActiveSnap>();
            this.disabledSnaps = new HashSet<int>();

            game.input.RegisterEventListener(InputController.InputEvent.MouseOver,
                                             (MapObject obj) =>
                                             {
                                                 this.HandleMouseOver(obj);
                                             });
            game.input.RegisterEventListener(InputController.InputEvent.MouseExit,
                                             (MapObject obj) =>
                                             {
                                                 this.HandleMouseExit(obj);
                                             });
        }

        public int AddStreetSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                                 bool snapToEnd, bool snapToLane, bool snapToRivers)
        {
            int id = snapCount++;
            var snap = new StreetSnap
            {
                id = id,
                snapCursor = snapCursor,
                snapCursorColor = snapCursorColor,
                snapCursorScale = snapCursorScale,
                snapToEnd = snapToEnd,
                snapToLane = snapToLane,
                snapToRivers = snapToRivers,
            };

            activeSnaps.Add(snap);
            return id;
        }

        public int AddSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                           Type type)
        {
            int id = snapCount++;
            var snap = new MapObjectSnap
            {
                id = id,
                snapCursor = snapCursor,
                snapCursorColor = snapCursorColor,
                snapCursorScale = snapCursorScale,
                type = type,
            };

            activeSnaps.Add(snap);
            return id;
        }

        public void EnableSnap(int id)
        {
            disabledSnaps.Remove(id);
        }

        public void DisableSnap(int id)
        {
            disabledSnaps.Add(id);
        }

        ActiveSnap GetSnapForObject(MapObject obj)
        {
            foreach (var snap in activeSnaps)
            {
                if (disabledSnaps.Contains(snap.id))
                {
                    continue;
                }
                if (snap.GetApplicableType() != obj.GetType())
                {
                    continue;
                }

                return snap;
            }

            return null;
        }

        public void HandleMouseOver(MapObject obj)
        {
            var snap = GetSnapForObject(obj);
            if (snap == null)
            {
                return;
            }

            if (snap.GetApplicableType() != obj.GetType())
            {
                return;
            }

            if (snap is StreetSnap)
            {
                SnapToStreet(snap as StreetSnap, obj as StreetSegment);
            }
            else if (snap is MapObjectSnap)
            {
                SnapToMapObject(snap as MapObjectSnap, obj);
            }
        }

        public void HandleMouseExit(MapObject obj)
        {
            Unsnap();
        }

        static readonly float endSnapThreshold = 5f * Map.Meters;

        void SnapToStreet(StreetSnap snapSettings, StreetSegment street)
        {
            if (snapSettings.snapToRivers)
            {
                if (street.street.type != Street.Type.River)
                {
                    return;
                }
            }
            else if (street.street.type == Street.Type.River)
            {
                return;
            }

            Cursor.visible = false;

            var cursorPos = game.input.NativeCursorPosition;

            System.Tuple<Vector3, Math.PointPosition> closestPtAndPos;
            if (snapSettings.snapToLane)
            {
                closestPtAndPos = street.GetClosestPointAndPosition(cursorPos);
                var positions = game.sim.trafficSim.GetPath(
                    street,
                    (closestPtAndPos.Item2 == Math.PointPosition.Right || street.street.isOneWay)
                        ? street.RightmostLane
                        : street.LeftmostLane);

                closestPtAndPos = StreetSegment.GetClosestPointAndPosition(cursorPos, positions);
            }
            else
            {
                closestPtAndPos = street.GetClosestPointAndPosition(cursorPos);
            }

            var closestPt = closestPtAndPos.Item1;
            var pos = closestPtAndPos.Item2;

            if (snapSettings.snapToEnd)
            {
                var distanceFromStart = (street.drivablePositions.First() - closestPt).magnitude;
                if (distanceFromStart < endSnapThreshold)
                {
                    closestPt = street.drivablePositions.First();
                    pos = Math.PointPosition.OnLine;
                }

                var distanceFromEnd = (street.drivablePositions.Last() - closestPt).magnitude;
                if (distanceFromEnd < endSnapThreshold)
                {
                    closestPt = street.drivablePositions.Last();
                    pos = Math.PointPosition.OnLine;
                }
            }

            var cursorObj = game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.snapCursor;
            spriteRenderer.color = snapSettings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.snapCursorScale;

            cursorObj.SetActive(true);
            cursorObj.transform.position = new Vector3(closestPt.x, closestPt.y,
                                                       Map.Layer(MapLayer.Cursor));

                Debug.Log("snapping");
            game.input.gameCursorPosition = cursorObj.transform.position;
        }

        void SnapToMapObject(MapObjectSnap snapSettings, MapObject obj)
        {
            var cursorObj = game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.snapCursor;
            spriteRenderer.color = snapSettings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.snapCursorScale;

            Cursor.visible = false;
            cursorObj.SetActive(true);
            cursorObj.transform.position = new Vector3(obj.transform.position.x,
                                                       obj.transform.position.y,
                                                       Map.Layer(MapLayer.Cursor));

            game.input.gameCursorPosition = cursorObj.transform.position;
        }

        void Unsnap()
        {
            Cursor.visible = true;

            var cursorObj = game.CreateCursorSprite;
            cursorObj.SetActive(false);
        }
    }
}