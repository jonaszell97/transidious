using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Transidious
{
    public struct SnapSettings
    {
        public Sprite snapCursor;
        public Color snapCursorColor;
        public Vector3 snapCursorScale;

        public UnityAction onSnapEnter;
        public UnityAction onSnapOver;
        public UnityAction onSnapExit;

        public bool hideCursor;

        // Only applies to street snaps.
        public bool snapToEnd;
        public bool snapToLane;
        public bool snapToRivers;
    }

    // BRING ME THANOOOOS!
    public class SnapController
    {

        abstract class ActiveSnap
        {
            internal int id;
            internal SnapSettings settings;

            internal abstract Type GetApplicableType();
        }

        class StreetSnap : ActiveSnap
        {
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
        ActiveSnap activeSnap;

        public SnapController(GameController game)
        {
            this.game = game;
            this.snapCount = 0;
            this.activeSnaps = new List<ActiveSnap>();
            this.disabledSnaps = new HashSet<int>();

            game.input.RegisterEventListener(InputEvent.MouseOver,
                                             (IMapObject obj) =>
                                             {
                                                 this.HandleMouseOver(obj);
                                             });
            game.input.RegisterEventListener(InputEvent.MouseExit,
                                             (IMapObject obj) =>
                                             {
                                                 this.HandleMouseExit(obj);
                                             });
        }

        public int AddStreetSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                                 bool snapToEnd, bool snapToLane, bool snapToRivers)
        {
            int id = snapCount++;
            var settings = new SnapSettings
            {
                snapCursor = snapCursor,
                snapCursorColor = snapCursorColor,
                snapCursorScale = snapCursorScale,
                snapToEnd = snapToEnd,
                snapToLane = snapToLane,
                snapToRivers = snapToRivers,
            };

            var snap = new StreetSnap
            {
                id = id,
                settings = settings,
            };

            activeSnaps.Add(snap);
            return id;
        }

        public int AddSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                           Type type)
        {
            int id = snapCount++;
            var settings = new SnapSettings
            {
                snapCursor = snapCursor,
                snapCursorColor = snapCursorColor,
                snapCursorScale = snapCursorScale,
            };

            var snap = new MapObjectSnap
            {
                id = id,
                settings = settings,
                type = type,
            };

            activeSnaps.Add(snap);
            return id;
        }

        public int AddSnap(Type type, SnapSettings settings, bool enabled = true)
        {
            int id = snapCount++;
            ActiveSnap snap;

            if (type == typeof(StreetSegment))
            {
                snap = new StreetSnap
                {
                    id = id,
                    settings = settings,
                };
            }
            else
            {
                snap = new MapObjectSnap
                {
                    id = id,
                    settings = settings,
                    type = type,
                };
            }

            activeSnaps.Add(snap);
            
            if (!enabled)
            {
                disabledSnaps.Add(id);
            }

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

        ActiveSnap GetSnapForObject(IMapObject obj)
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

        public void HandleMouseOver(IMapObject obj)
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

            if (activeSnap != snap)
            {
                activeSnap = snap;
                snap.settings.onSnapEnter?.Invoke();
            }

            snap.settings.onSnapOver?.Invoke();

            if (snap is StreetSnap)
            {
                SnapToStreet(snap as StreetSnap, obj as StreetSegment);
            }
            else if (snap is MapObjectSnap)
            {
                SnapToMapObject(snap as MapObjectSnap, obj);
            }
        }

        public void HandleMouseExit(IMapObject obj)
        {
            if (activeSnap != null)
            {
                activeSnap.settings.onSnapExit?.Invoke();
            }

            Unsnap();
            activeSnap = null;
        }

        static readonly float endSnapThreshold = 5f;

        void SnapToStreet(StreetSnap snapSettings, StreetSegment street)
        {
            if (snapSettings.settings.snapToRivers)
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

            if (snapSettings.settings.hideCursor)
            {
                Cursor.visible = false;
            }

            Vector2 cursorPos = game.input.NativeCursorPosition;

            Tuple<Vector2, Math.PointPosition> closestPtAndPos;
            if (snapSettings.settings.snapToLane)
            {
                closestPtAndPos = street.GetClosestPointAndPosition(cursorPos);
                var positions = game.sim.trafficSim.StreetPathBuilder.GetPath(
                    street,
                    (closestPtAndPos.Item2 == Math.PointPosition.Right || street.IsOneWay)
                        ? street.RightmostLane
                        : street.LeftmostLane).Points;

                closestPtAndPos = StreetSegment.GetClosestPointAndPosition(cursorPos, positions);
            }
            else
            {
                closestPtAndPos = street.GetClosestPointAndPosition(cursorPos);
            }

            var closestPt = closestPtAndPos.Item1;
            var pos = closestPtAndPos.Item2;

            if (snapSettings.settings.snapToEnd)
            {
                var distanceFromStart = ((Vector2)street.drivablePositions.First() - closestPt).magnitude;
                if (distanceFromStart < endSnapThreshold)
                {
                    closestPt = street.drivablePositions.First();
                    pos = Math.PointPosition.OnLine;
                }

                var distanceFromEnd = ((Vector2)street.drivablePositions.Last() - closestPt).magnitude;
                if (distanceFromEnd < endSnapThreshold)
                {
                    closestPt = street.drivablePositions.Last();
                    pos = Math.PointPosition.OnLine;
                }
            }

            var cursorObj = game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.settings.snapCursor;
            spriteRenderer.color = snapSettings.settings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.settings.snapCursorScale;

            cursorObj.SetActive(true);
            cursorObj.transform.position = new Vector3(closestPt.x, closestPt.y,
                                                       Map.Layer(MapLayer.Cursor));

            game.input.gameCursorPosition = cursorObj.transform.position;
        }

        void SnapToMapObject(MapObjectSnap snapSettings, IMapObject obj)
        {
            var cursorObj = game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.settings.snapCursor;
            spriteRenderer.color = snapSettings.settings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.settings.snapCursorScale;

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