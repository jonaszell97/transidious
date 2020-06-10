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
        public Func<Vector2, bool> snapCondition;

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

        /// Reference to the game.
        private readonly GameController _game;
        
        /// Number of active snaps.
        private int _snapCount;
        
        /// List of active snaps.
        private readonly List<ActiveSnap> _activeSnaps;
        
        /// Set of disabled snaps.
        private readonly HashSet<int> _disabledSnaps;
        
        /// The current active snap.
        private ActiveSnap _activeSnap;

        /// Settings for grid snapping.
        private SnapSettings? _gridSnapSettings;

        /// If > 0, snap to the grid if the distance between the cursor and a grid point is less than this.
        private float _snapToGridThreshold = 0f;

        /// Whether or not we're currently snapped to the grid.
        private bool _snappedToGrid;

        /// Whether or not we're currently snapped to the grid.
        public bool IsSnappedToGrid => _snappedToGrid;

        public SnapController(GameController game)
        {
            this._game = game;
            this._snapCount = 0;
            this._activeSnaps = new List<ActiveSnap>();
            this._disabledSnaps = new HashSet<int>();

            game.input.RegisterEventListener(InputEvent.MouseOver, this.HandleMouseOver);
            game.input.RegisterEventListener(InputEvent.MouseExit, this.HandleMouseExit);
        }

        public void Update()
        {
            if (_gridSnapSettings != null && !_game.input.IsPointerOverUIElement())
            {
                var pos = Input.mousePosition;
                SnapToGrid(Camera.main.ScreenToWorldPoint(pos));
            }
        }

        public void EnableGridSnap(SnapSettings gridSnapSettings, float threshold)
        {
            _gridSnapSettings = gridSnapSettings;
            _snapToGridThreshold = threshold;
        }

        public void DisableGridSnap()
        {
            _gridSnapSettings = null;
            _snapToGridThreshold = 0f;
            Unsnap();
        }

        public int AddStreetSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                                 bool snapToEnd, bool snapToLane, bool snapToRivers)
        {
            int id = ++_snapCount;
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

            _activeSnaps.Add(snap);
            return id;
        }

        public int AddSnap(Sprite snapCursor, Color snapCursorColor, Vector3 snapCursorScale,
                           Type type)
        {
            int id = ++_snapCount;
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

            _activeSnaps.Add(snap);
            return id;
        }

        public int AddSnap(Type type, SnapSettings settings, bool enabled = true)
        {
            int id = ++_snapCount;
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

            _activeSnaps.Add(snap);
            
            if (!enabled)
            {
                _disabledSnaps.Add(id);
            }

            return id;
        }

        public void EnableSnap(int id)
        {
            _disabledSnaps.Remove(id);
        }

        public void DisableSnap(int id)
        {
            _disabledSnaps.Add(id);
        }

        ActiveSnap GetSnapForObject(IMapObject obj)
        {
            foreach (var snap in _activeSnaps)
            {
                if (_disabledSnaps.Contains(snap.id))
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

            if (_activeSnap != snap)
            {
                _activeSnap = snap;
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
            if (_activeSnap != null)
            {
                _activeSnap.settings.onSnapExit?.Invoke();
            }

            Unsnap();
            _activeSnap = null;
        }

        private void SnapToGrid(Vector2 mousePosWorld)
        {
            Debug.Assert(_gridSnapSettings != null);

            var nearestGridPt = GameController.instance.loadedMap.GetNearestGridPt(mousePosWorld);
            var diff = (nearestGridPt - mousePosWorld).sqrMagnitude;

            if (diff >= _snapToGridThreshold)
            {
                if (_snappedToGrid)
                {
                    _snappedToGrid = false;
                    _gridSnapSettings.Value.onSnapExit?.Invoke();
                    Unsnap();
                }

                return;
            }

            if (_gridSnapSettings.Value.snapCondition != null)
            {
                if (!_gridSnapSettings.Value.snapCondition(nearestGridPt))
                {
                    return;
                }
            }

            _gridSnapSettings.Value.onSnapOver?.Invoke();

            if (_snappedToGrid && _game.input.gameCursorPosition.Equals(nearestGridPt))
            {
                return;
            }

            _snappedToGrid = true;
            _gridSnapSettings.Value.onSnapEnter?.Invoke();
            SnapToPosition(_gridSnapSettings.Value, nearestGridPt);
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

            Vector2 cursorPos = _game.input.NativeCursorPosition;

            Tuple<Vector2, Math.PointPosition> closestPtAndPos;
            if (snapSettings.settings.snapToLane)
            {
                closestPtAndPos = street.GetClosestPointAndPosition(cursorPos);
                var positions = _game.sim.trafficSim.StreetPathBuilder.GetPath(
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

            var cursorObj = _game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.settings.snapCursor;
            spriteRenderer.color = snapSettings.settings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.settings.snapCursorScale;

            cursorObj.SetActive(true);
            cursorObj.transform.position = new Vector3(closestPt.x, closestPt.y,
                                                       Map.Layer(MapLayer.Cursor));

            _game.input.gameCursorPosition = cursorObj.transform.position;
        }

        void SnapToMapObject(MapObjectSnap snapSettings, IMapObject obj)
        {
            SnapToPosition(snapSettings.settings, obj.transform.position);
        }

        void SnapToPosition(SnapSettings snapSettings, Vector2 pos)
        {
            var cursorObj = _game.CreateCursorSprite;
            var spriteRenderer = cursorObj.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite = snapSettings.snapCursor;
            spriteRenderer.color = snapSettings.snapCursorColor;
            spriteRenderer.transform.localScale = snapSettings.snapCursorScale;

            Cursor.visible = false;
            cursorObj.SetActive(true);
            cursorObj.transform.position = new Vector3(pos.x, pos.y,
                                                       Map.Layer(MapLayer.Cursor));

            _game.input.gameCursorPosition = cursorObj.transform.position;
        }

        void Unsnap()
        {
            Cursor.visible = true;

            var cursorObj = _game.CreateCursorSprite;
            cursorObj.SetActive(false);
        }
    }
}