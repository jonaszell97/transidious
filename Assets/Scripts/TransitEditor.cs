using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Transidious
{
    public class TransitEditor : MonoBehaviour
    {
        public enum EditingMode
        {
            /// In this mode, no line is currently being edited. A mouse click will create a new line.
            CreateNewLine,

            /// In this mode, we are adding stops to a newly created line.
            ModifyUnfinishedLine,

            /// In this mode, existing stops can be moved or deleted.
            ModifyFinishedLine,
        }

        public GameController game;
        public Map map;

        public bool active;
        public TransitType? selectedSystem;
        public EditingMode editingMode;

        public GameObject transitUI;
        public Button[] systemButtons;
        public int[] snapSettings;
        public int temporaryStopSnapSettingsId;
        public int stopSnapSettingsId;
        public Tooltip tooltip;

        TemporaryLine currentLine;
        MapObject previousStop;
        List<Vector3> currentPath;
        List<TrafficSimulator.PathSegmentInfo> currentSegments;
        GameObject existingPathMesh;
        GameObject plannedPathMesh;
        public GameObject temporaryStopPrefab;
        int[] listenerIDs;

        public UILineModal lineInfoModal;

        void Awake()
        {
            this.active = false;
            this.selectedSystem = null;
            this.currentSegments = new List<TrafficSimulator.PathSegmentInfo>();
            this.editingMode = EditingMode.CreateNewLine;
            this.snapSettings = null;

            this.tooltip = game.CreateTooltip(null, Color.black);
            this.tooltip.Hide();
        }

        void Start()
        {
            RegisterCallbacks();
        }

        void InitSnapSettings()
        {
            this.snapSettings = new int[] {
                // Bus
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.Bus),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    true,
                    false
                ),

                // Tram
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.Tram),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    true,
                    false
                ),

                // Subway
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.Subway),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    false,
                    false
                ),

                // Light Rail
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.LightRail),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    false,
                    false
                ),

                // Intercity
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.IntercityRail),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    false,
                    false
                ),

                // Ferry
                game.snapController.AddStreetSnap(
                    game.createStreetSprite,
                    game.GetDefaultSystemColor(TransitType.Ferry),
                    new Vector3(.3f, .3f, .3f),
                    false,
                    true,
                    true
                )
            };

            foreach (var id in snapSettings)
            {
                game.snapController.DisableSnap(id);
            }

            this.temporaryStopSnapSettingsId = game.snapController.AddSnap(
                null,
                Color.white,
                Vector3.one,
                typeof(TemporaryStop)
            );

            this.stopSnapSettingsId = game.snapController.AddSnap(
                null,
                Color.white,
                Vector3.one,
                typeof(Stop)
            );

            game.snapController.DisableSnap(this.temporaryStopSnapSettingsId);
            game.snapController.DisableSnap(this.stopSnapSettingsId);
        }

        Button GetButton(TransitType system)
        {
            return systemButtons[(int)system];
        }

        void RegisterCallbacks()
        {
            for (int i = 0; i < systemButtons.Length; ++i)
            {
                var system = (TransitType)(i);
                systemButtons[i].onClick.AddListener(() =>
                {
                    this.ActivateSystem(system);
                });
            }

            this.listenerIDs = new int[] {
                game.input.RegisterEventListener(InputController.InputEvent.MouseOver, (MapObject obj) => {
                    this.MapObjectHovered(obj);
                }, false),
                game.input.RegisterEventListener(InputController.InputEvent.MouseExit, (MapObject obj) => {
                    this.MapObjectHoverExit(obj);
                }, false),
                game.input.RegisterEventListener(InputController.InputEvent.MouseDown, (MapObject obj) => {
                    this.MapObjectClicked(obj);
                }, false),
            };

            game.input.RegisterEventListener(InputController.InputEvent.MouseEnter,
                                             (MapObject obj) =>
            {
                this.MapObjectEntered(obj);
            });

            game.input.RegisterEventListener(InputController.InputEvent.MouseExit,
                                             (MapObject obj) =>
            {
                this.MapObjectExited(obj);
            });
        }

        void HighlightSystemButton(TransitType? system)
        {
            for (int i = 0; i < systemButtons.Length; ++i)
            {
                if (system.HasValue && i == (int)system.Value)
                {
                    systemButtons[i].GetComponent<Image>().color =
                        systemButtons[i].colors.highlightedColor;
                }
                else
                {
                    systemButtons[i].GetComponent<Image>().color =
                        systemButtons[i].colors.normalColor;
                }
            }
        }

        public void ActivateTooltip(TransitType system, string text)
        {
            if (tooltip.Text == null)
            {
                tooltip.SetText(game.loadedMap.CreateText(Vector3.zero, text, Color.white, 3f));
            }
            else
            {
                tooltip.UpdateText(text);
            }

            tooltip.SetPosition(game.input.GameCursorPosition);
            tooltip.Display();
        }

        void ActivateSystem(TransitType system)
        {
            this.selectedSystem = system;
            this.game.snapController.EnableSnap(snapSettings[(int)system]);
            this.HighlightSystemButton(system);
            EnterMode(EditingMode.CreateNewLine);

            foreach (var id in listenerIDs)
            {
                game.input.EnableEventListener(id);
            }
        }

        void DeactivateSystem()
        {
            this.game.snapController.DisableSnap(snapSettings[(int)selectedSystem.Value]);
            this.selectedSystem = null;
            this.HighlightSystemButton(null);
            this.tooltip.Hide();

            ResetTemporaryLine();
            EnterMode(EditingMode.CreateNewLine);

            foreach (var id in listenerIDs)
            {
                game.input.DisableEventListener(id);
            }

            plannedPathMesh?.SetActive(false);
            existingPathMesh?.SetActive(false);
        }

        void ResetTemporaryLine()
        {
            if (currentLine == null)
            {
                return;
            }

            foreach (var stop in currentLine.stops)
            {
                Destroy(stop.gameObject);
            }

            currentLine = null;
        }

        public void Toggle()
        {
            if (active)
            {
                Deactivate();
            }
            else
            {
                Activate();
            }
        }

        public void Activate()
        {
            if (this.snapSettings == null)
            {
                InitSnapSettings();
            }

            Debug.Assert(!this.active, "TransitEditor is active!");

            this.active = true;
            this.map = game.loadedMap;
            this.transitUI.SetActive(true);

            this.game.transitEditorButton.GetComponent<Image>().color =
                this.game.transitEditorButton.colors.highlightedColor;

            // Disable collision for all existing routes while we're editing.
            foreach (var route in map.transitRoutes)
            {
                route.DisableCollision();
            }
        }

        public void Deactivate()
        {
            Debug.Assert(this.active, "TransitEditor is not active!");

            if (selectedSystem != null)
            {
                DeactivateSystem();
            }

            // Reenable collision.
            foreach (var route in map.transitRoutes)
            {
                route.EnableCollision();
            }

            this.active = false;
            this.map = null;
            this.transitUI.SetActive(false);

            this.game.transitEditorButton.GetComponent<Image>().color =
                this.game.transitEditorButton.colors.normalColor;
        }

        void EnterMode(EditingMode mode)
        {
            switch (mode)
            {
            case EditingMode.CreateNewLine:
                // Remove transparency.
                foreach (var line in map.transitLines)
                {
                    line.ResetTransparency();
                }

                break;
            case EditingMode.ModifyUnfinishedLine:
                // Add transparency to all other lines to make the new line easier to see.
                foreach (var line in map.transitLines)
                {
                    line.SetTransparency(.5f);
                }

                break;
            case EditingMode.ModifyFinishedLine:
                // Add transparency to all other lines to make the new line easier to see.
                foreach (var line in map.transitLines)
                {
                    line.SetTransparency(.5f);
                }

                break;
            }

            this.editingMode = mode;
        }

        void UpdateExistingPath()
        {
            if (this.existingPathMesh == null)
            {
                this.existingPathMesh = Instantiate(game.loadedMap.meshPrefab);
                this.existingPathMesh.transform.position
                    = new Vector3(0, 0, Map.Layer(MapLayer.TemporaryLines));
            }

            var color = game.GetDefaultSystemColor(selectedSystem.Value);
            var mesh = MeshBuilder.CreateSmoothLine(currentLine.completePath, 1.25f, 10);
            var renderer = existingPathMesh.GetComponent<MeshRenderer>();
            var filter = existingPathMesh.GetComponent<MeshFilter>();

            filter.mesh = mesh;
            renderer.material = GameController.GetUnlitMaterial(color);

            existingPathMesh.SetActive(true);
        }

        void ResetPath()
        {
            if (this.plannedPathMesh == null)
            {
                return;
            }

            var filter = plannedPathMesh.GetComponent<MeshFilter>();
            filter.mesh = null;
        }

        void DrawPath(PathPlanning.PathPlanningResult result)
        {
            if (this.plannedPathMesh == null)
            {
                this.plannedPathMesh = Instantiate(game.loadedMap.meshPrefab);
                this.plannedPathMesh.transform.position
                    = new Vector3(0, 0, Map.Layer(MapLayer.TemporaryLines));
            }

            currentSegments.Clear();

            var color = game.GetDefaultSystemColor(selectedSystem.Value);
            currentPath = game.sim.trafficSim.GetCompletePath(result, currentSegments);

            var mesh = MeshBuilder.CreateSmoothLine(currentPath, 1.25f, 10);
            var renderer = plannedPathMesh.GetComponent<MeshRenderer>();
            var filter = plannedPathMesh.GetComponent<MeshFilter>();

            filter.mesh = mesh;
            renderer.material = GameController.GetUnlitMaterial(color);

            plannedPathMesh.SetActive(true);
        }

        public void MapObjectEntered(MapObject obj)
        {

        }

        public void MapObjectExited(MapObject obj)
        {

        }

        public void MapObjectHovered(MapObject obj)
        {
            var s = obj as StreetSegment;
            if (s != null)
            {
                StreetHovered(s);
                return;
            }

            var tmpStop = obj as TemporaryStop;
            if (tmpStop != null)
            {
                TemporaryStopHovered(tmpStop);
                return;
            }

            var stop = obj as Stop;
            if (stop != null)
            {
                StopHovered(stop);
                return;
            }
        }

        public void MapObjectHoverExit(MapObject obj)
        {
            var s = obj as StreetSegment;
            if (s != null)
            {
                StreetHoverExit(s);
                return;
            }

            var tmpStop = obj as TemporaryStop;
            if (tmpStop != null)
            {
                TemporaryStopHoverExit(tmpStop);
                return;
            }

            var stop = obj as Stop;
            if (stop != null)
            {
                StopHoverExit(stop);
                return;
            }
        }

        public void MapObjectClicked(MapObject obj)
        {
            var s = obj as StreetSegment;
            if (s != null)
            {
                StreetClicked(s);
            }

            var tmpStop = obj as TemporaryStop;
            if (tmpStop != null)
            {
                TemporaryStopClicked(tmpStop);
                return;
            }

            var stop = obj as Stop;
            if (stop != null)
            {
                StopClicked(stop);
                return;
            }
        }

        public void StreetHovered(StreetSegment street)
        {
            switch (selectedSystem)
            {
            case TransitType.Bus:
                {
                    if (previousStop == null)
                    {
                        ActivateTooltip(selectedSystem.Value, "Create Line");
                        break;
                    }

                    Debug.Log("finding path");

                    var options = new PathPlanning.PathPlanningOptions { allowWalk = false };
                    var planner = new PathPlanning.PathPlanner(options);

                    var result = planner.FindClosestDrive(game.loadedMap, previousStop.transform.position,
                                                          game.input.GameCursorPosition);

                    if (result == null)
                    {
                        ResetPath();
                        ActivateTooltip(selectedSystem.Value, "Cannot add stop here");
                        break;
                    }

                    if (street)
                    {
                        ActivateTooltip(selectedSystem.Value, "Add Stop");
                    }

                    DrawPath(result);
                }

                break;
            case TransitType.Tram:
                {
                    if (street != null && !street.hasTramTracks)
                    {
                        ActivateTooltip(selectedSystem.Value, "Build Tram Tracks");
                        break;
                    }
                    if (previousStop == null)
                    {
                        ActivateTooltip(selectedSystem.Value, "Create Line");
                        break;
                    }

                    var options = new PathPlanning.PathPlanningOptions { allowWalk = false };
                    var planner = new PathPlanning.PathPlanner(options);
                    var result = planner.FindClosestDrive(game.loadedMap,
                                                          previousStop.transform.position,
                                                          game.input.GameCursorPosition);

                    if (result == null || !result.ValidForTram)
                    {
                        ResetPath();
                        ActivateTooltip(selectedSystem.Value, "Cannot add stop here");
                        break;
                    }

                    if (street)
                    {
                        ActivateTooltip(selectedSystem.Value, "Add Stop");
                    }

                    DrawPath(result);
                }

                break;
            default:
                break;
            }
        }

        public void StreetHoverExit(StreetSegment street)
        {
            this.tooltip.Hide();
            this.plannedPathMesh?.SetActive(false);
        }

        public void StreetClicked(StreetSegment street)
        {
            if (!this.tooltip.gameObject.activeSelf)
            {
                return;
            }

            switch (selectedSystem)
            {
            case TransitType.Bus:
                if (Cursor.visible)
                {
                    return;
                }
                if (previousStop == null)
                {
                    previousStop = CreateStop(street);
                    EnterMode(EditingMode.ModifyUnfinishedLine);

                    return;
                }
                if (currentPath == null)
                {
                    break;
                }

                previousStop = AddStop(street);
                break;
            case TransitType.Tram:
                if (street && !street.hasTramTracks)
                {
                    street.AddTramTracks();
                    break;
                }

                if (previousStop == null)
                {
                    previousStop = CreateStop(street);
                    EnterMode(EditingMode.ModifyUnfinishedLine);

                    return;
                }
                if (currentPath == null)
                {
                    break;
                }

                previousStop = AddStop(street);
                break;
            default:
                break;
            }
        }

        void TemporaryStopHovered(TemporaryStop stop)
        {
            if (currentLine == null || stop != currentLine.stops.First())
            {
                StreetHovered(null);
                return;
            }

            ActivateTooltip(selectedSystem.Value, "Finish Line");
            game.snapController.EnableSnap(this.temporaryStopSnapSettingsId);
            game.snapController.HandleMouseOver(stop);

            StreetHovered(null);
        }

        void StopHovered(Stop stop)
        {
            if (currentLine == null)
            {
                ActivateTooltip(selectedSystem.Value, "Create New Line");
            }
            else if (stop != currentLine.stops.First())
            {
                ActivateTooltip(selectedSystem.Value, "Add Stop");
            }
            else
            {
                ActivateTooltip(selectedSystem.Value, "Finish Line");
            }

            game.snapController.EnableSnap(this.stopSnapSettingsId);
            game.snapController.HandleMouseOver(stop);

            StreetHovered(null);
        }

        void TemporaryStopHoverExit(TemporaryStop stop)
        {
            game.snapController.DisableSnap(this.temporaryStopSnapSettingsId);
            ActivateTooltip(selectedSystem.Value, "Add Stop");
        }

        void StopHoverExit(Stop stop)
        {
            game.snapController.DisableSnap(this.stopSnapSettingsId);
            ActivateTooltip(selectedSystem.Value, "Add Stop");
        }

        void TemporaryStopClicked(TemporaryStop stop)
        {
            if (stop != currentLine.stops.First())
            {
                return;
            }

            FinishLine();
        }

        void StopClicked(Stop stop)
        {
            if (currentLine == null)
            {
                StreetClicked(null);
                return;
            }

            var firstStop = currentLine.stops.First() as Stop;
            if (stop == firstStop)
            {
                FinishLine();
                return;
            }

            previousStop = AddStop(stop);
        }

        TemporaryStop CreateTempStop(string name, Vector3 pos)
        {
            var obj = Instantiate(temporaryStopPrefab);
            obj.transform.SetParent(this.transform);

            var stop = obj.GetComponent<TemporaryStop>();
            stop.Initialize(game, name, pos);

            return stop;
        }

        TemporaryStop CreateStop(StreetSegment hoveredStreet)
        {
            currentLine = new TemporaryLine
            {
                name = Translator.Get("tooltip:new_line",
                                      game.GetSystemName(selectedSystem.Value)),
                stops = new List<MapObject>(),
                completePath = new List<Vector3>(),
                paths = new List<int>(),
                streetSegments = new List<List<TrafficSimulator.PathSegmentInfo>>(),
            };

            var firstStop = CreateTempStop(hoveredStreet.street.name, game.input.GameCursorPosition);
            currentLine.stops.Add(firstStop);

            return firstStop;
        }

        TemporaryStop AddStop(StreetSegment hoveredStreet)
        {
            Debug.Assert(currentPath != null, "invalid path!");

            var nextStop = CreateTempStop(hoveredStreet.street.name,
                                          game.input.GameCursorPosition);

            currentLine.stops.Add(nextStop);
            currentLine.completePath.AddRange(currentPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            currentLine.streetSegments.Add(currentSegments);
            this.currentSegments = new List<TrafficSimulator.PathSegmentInfo>();

            UpdateExistingPath();
            return nextStop;
        }

        Stop AddStop(Stop nextStop)
        {
            Debug.Assert(currentPath != null, "invalid path!");

            currentLine.stops.Add(nextStop);
            currentLine.completePath.AddRange(currentPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            currentLine.streetSegments.Add(currentSegments);
            this.currentSegments = new List<TrafficSimulator.PathSegmentInfo>();

            UpdateExistingPath();
            return nextStop;
        }

        void RemoveDuplicates(List<Vector3> path)
        {
            for (var i = 1; i < path.Count;)
            {
                if (path[i].Equals(path[i - 1]))
                {
                    path.RemoveAt(i - 1);
                }
                else
                {
                    ++i;
                }
            }
        }

        void FinishLine()
        {
            var type = selectedSystem.Value;
            var line = game.loadedMap.CreateLine(type, currentLine.name,
                                                 game.GetDefaultSystemColor(type));

            currentLine.stops.Add(currentLine.stops.First());
            currentLine.completePath.AddRange(currentPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            currentLine.streetSegments.Add(currentSegments);
            this.currentSegments = new List<TrafficSimulator.PathSegmentInfo>();

            Stop firstStop = null;
            var pathIdx = 0;
            for (var i = 0; i < currentLine.stops.Count; ++i)
            {
                var nextStop = currentLine.stops[i];

                Stop stop;
                if (i == currentLine.stops.Count - 1)
                {
                    stop = firstStop;
                }
                else if (nextStop is Stop)
                {
                    stop = nextStop as Stop;
                }
                else
                {
                    var tmpStop = nextStop as TemporaryStop;
                    stop = game.loadedMap.CreateStop(tmpStop.name, tmpStop.position);

                    Destroy(tmpStop.gameObject);
                }

                if (firstStop == null)
                {
                    firstStop = stop;
                }

                if (i != 0)
                {
                    line.AddStop(stop, true, false, currentLine.completePath.GetRange(
                        pathIdx, currentLine.paths[i - 1] - pathIdx));

                    pathIdx = currentLine.paths[i - 1];
                }
                else
                {
                    line.AddStop(stop, true, false);
                }
            }

            var newLine = line.Finish();

            // Disable collision for the new routes.
            var j = 0;
            var crossedStreets = new HashSet<Tuple<StreetSegment, int>>();
            foreach (var route in newLine.routes)
            {
                route.DisableCollision();

                // Note which streets this route passes over.
                foreach (var segAndLane in currentLine.streetSegments[j])
                {
                    var routesOnSegment = segAndLane.segment.GetTransitRoutes(segAndLane.lane);
                    routesOnSegment.Add(route);

                    if (routesOnSegment.Count > 1)
                    {
                        crossedStreets.Add(Tuple.Create(segAndLane.segment, segAndLane.lane));
                    }

                    route.AddStreetSegmentOffset(segAndLane);
                }

                ++j;
            }

            currentLine = null;
            currentPath = null;
            previousStop = null;

            plannedPathMesh?.SetActive(false);
            existingPathMesh?.SetActive(false);
            this.tooltip.Hide();

            EnterMode(EditingMode.CreateNewLine);
            CheckOverlappingRoutes(crossedStreets);
        }

        public void InitOverlappingRoutes()
        {
            var crossedStreets = new HashSet<Tuple<StreetSegment, int>>();
            foreach (var route in game.loadedMap.transitRoutes)
            {
                foreach (var entry in route.GetStreetSegmentOffsetInfo())
                {
                    foreach (var segInfo in entry.Value)
                    {
                        var routesOnSegment = segInfo.segment.GetTransitRoutes(segInfo.lane);
                        routesOnSegment.Add(route);

                        crossedStreets.Add(Tuple.Create(segInfo.segment, segInfo.lane));
                    }
                }
            }

            CheckOverlappingRoutes(crossedStreets);
        }

        void CheckOverlappingRoutes(HashSet<Tuple<StreetSegment, int>> segments)
        {
            var linesPerPositionMap = new Dictionary<Tuple<Vector2, int>, int>();
            var linePositionMap = new Dictionary<Tuple<int, int>, int>();
            var allPositionsMap = new Dictionary<Tuple<StreetSegment, int>, List<Vector3>>();
            var coveredPositions = new HashSet<Vector2>();
            var affectedRoutes = new HashSet<Route>();

            foreach (var seg in segments)
            {
                var allPositions = CheckOverlappingRoutes(seg.Item1, seg.Item2,
                                                          linesPerPositionMap,
                                                          linePositionMap,
                                                          coveredPositions,
                                                          affectedRoutes);

                allPositionsMap.Add(seg, allPositions);
            }

            // Update intersection paths, these are always the same so this is much simpler.
            foreach (var route in affectedRoutes)
            {
                for (var i = 0; i < route.positions.Count; ++i)
                {
                    if (route.GetSegmentForPosition(i) != null)
                    {
                        continue;
                    }

                    var pos = route.positions[i];
                    var key = new Tuple<Vector2, int>(pos, 0);
                    if (!linesPerPositionMap.ContainsKey(key))
                    {
                        linesPerPositionMap.Add(key, 1);
                        continue;
                    }

                    ++linesPerPositionMap[key];
                }
            }

            UpdateRouteMeshes(affectedRoutes, linesPerPositionMap, allPositionsMap);
        }

        void UpdateOccupiedSegments(Dictionary<Route, List<Tuple<int, int>>> occupiedSegments,
                                    int insertPos)
        {
            var keys = new List<Route>(occupiedSegments.Keys);
            foreach (var list in occupiedSegments)
            {
                for (var i = 0; i < list.Value.Count; ++i)
                {
                    var entry = list.Value[i];
                    if (insertPos > entry.Item1 && insertPos <= entry.Item2)
                    {
                        list.Value[i] = Tuple.Create(entry.Item1, entry.Item2 + 1);
                    }
                    else if (insertPos <= entry.Item1)
                    {
                        list.Value[i] = Tuple.Create(entry.Item1 + 1, entry.Item2 + 1);
                    }
                }
            }
        }

        List<Vector3> CheckOverlappingRoutes(StreetSegment seg, int lane,
                                             Dictionary<Tuple<Vector2, int>, int> linesPerPositionMap,
                                             Dictionary<Tuple<int, int>, int> linePositionMap,
                                             HashSet<Vector2> coveredPositions,
                                             HashSet<Route> affectedRoutes)
        {
            var routes = seg.GetTransitRoutes(lane);
            var path = game.sim.trafficSim.GetPath(seg, lane).ToList();

            // For a path like 'A -> B -> C', remember how many lines drive on each route (A->B), (B->C)
            var overlappingRouteCount = new List<int>();
            overlappingRouteCount.AddRange(Enumerable.Repeat(0, path.Count - 1));

            var occupiedSegments = new Dictionary<Route, List<Tuple<int, int>>>();
            foreach (var route in routes)
            {
                affectedRoutes.Add(route);
                occupiedSegments.Add(route, new List<Tuple<int, int>>());

                // Since the route stores the positions of all street segments in a single vector,
                // we need to get the offsets of every street segment individually.
                var offsets = route.GetStreetSegmentOffsets(seg, lane);
                foreach (var offsetAndLength in offsets)
                {
                    var i = 0;

#if DEBUG
                    var _coveredPositions = new List<Vector2>();
                    for (var j = offsetAndLength.offset; j < offsetAndLength.offset + offsetAndLength.length; ++j)
                    {
                        _coveredPositions.Add(route.positions[j]);
                    }
                    Debug.Log("route " + route.name + " covers positions ["
                        + string.Join(", ", _coveredPositions) + "] on " + seg.name + ", lane " + lane);
#endif

                    // If this route is not partial, increase the line count of every path segment.
                    if (!offsetAndLength.partialStart && !offsetAndLength.partialEnd)
                    {
                        for (i = 0; i < overlappingRouteCount.Count; ++i)
                        {
                            ++overlappingRouteCount[i];
                        }

                        occupiedSegments[route].Add(Tuple.Create(0, overlappingRouteCount.Count));
                        continue;
                    }

                    // Find the first and last positions on the street that overlap with the path.
                    var firstPosOnStreet = route.positions[offsetAndLength.offset];
                    var lastPosOnStreet = route.positions[offsetAndLength.offset + offsetAndLength.length - 1];

                    float startDistance = seg.GetDistanceFromStartStopLine(firstPosOnStreet, path);
                    float endDistance = seg.GetDistanceFromStartStopLine(lastPosOnStreet, path);

                    var minIdx = 0;
                    if (offsetAndLength.partialStart)
                    {
                        for (i = 0; i < path.Count; ++i)
                        {
                            float distance = distance = seg.GetDistanceFromStartStopLine(path[i], path);
                            if (distance >= startDistance)
                            {
                                minIdx = i;

                                // Insert the partial position into our position vector.
                                // This simplifies things later.
                                if (!distance.Equals(startDistance))
                                {
                                    var insertPos = i;

                                    var beforeCount = overlappingRouteCount[insertPos - 1];
                                    path.Insert(insertPos, firstPosOnStreet);
                                    overlappingRouteCount.Insert(insertPos - 1, beforeCount);

                                    UpdateOccupiedSegments(occupiedSegments, insertPos);
                                }

                                break;
                            }
                        }
                    }

                    var maxIdx = path.Count - 1;
                    if (offsetAndLength.partialEnd)
                    {
                        for (i = 0; i < path.Count; ++i)
                        {
                            float distance = seg.GetDistanceFromStartStopLine(path[i], path);
                            if (distance >= endDistance)
                            {
                                maxIdx = i;

                                // Insert the partial position into our position vector.
                                if (!distance.Equals(endDistance))
                                {
                                    var insertPos = i;

                                    var afterCount = overlappingRouteCount[insertPos - 1];
                                    path.Insert(insertPos, lastPosOnStreet);
                                    overlappingRouteCount.Insert(insertPos - 1, afterCount);

                                    UpdateOccupiedSegments(occupiedSegments, insertPos);
                                }

                                break;
                            }
                        }
                    }

                    if (offsetAndLength.backward)
                    {
                        if (offsetAndLength.partialStart && offsetAndLength.partialEnd)
                        {
                            var prevMax = maxIdx;
                            maxIdx = minIdx;
                            minIdx = prevMax;
                        }
                        else if (offsetAndLength.partialStart)
                        {
                            var prevMax = maxIdx;
                            maxIdx = minIdx;
                            minIdx = path.Count - prevMax - 1;
                        }
                        else if (offsetAndLength.partialEnd)
                        {
                            var prevMin = minIdx;
                            minIdx = maxIdx;
                            maxIdx = path.Count - prevMin - 1;
                        }
                    }

                    Debug.Assert(minIdx <= maxIdx);

                    for (i = minIdx; i < maxIdx; ++i)
                    {
                        ++overlappingRouteCount[i];
                    }

                    occupiedSegments[route].Add(Tuple.Create(minIdx, maxIdx));
                }
            }

            Debug.Log("overlapping routes for " + seg.name + " (lane " + lane
                + "): [" + String.Join(", ", overlappingRouteCount) + "]");

            Debug.Assert(path.Count - 1 == overlappingRouteCount.Count);

            for (var i = 1; i < path.Count; ++i)
            {
                int idx;
                if (i == 0)
                {
                    idx = 0;
                }
                else
                {
                    idx = i - 1;
                }

                var overlappingRoutes = overlappingRouteCount[idx];
                var key = new Tuple<Vector2, int>(path[i], lane);

                if (linesPerPositionMap.ContainsKey(key))
                {
                    Debug.LogError("position " + path[i] + " is on two streets");
                    continue;
                }

                linesPerPositionMap.Add(new Tuple<Vector2, int>(path[i], lane), overlappingRoutes);
                coveredPositions.Add(path[i]);
            }

            return path;
        }

        void AddPositions(List<Vector3> positions,
                          IReadOnlyList<Vector3> range,
                          List<float> widths,
                          IReadOnlyList<float> widthRange,
                          bool excludeFirst,
                          bool excludeLast)
        {
            Debug.Assert(range.Count() == widthRange.Count());

            var begin = 0;
            var end = range.Count();

            if (excludeFirst)
            {
                begin = 1;
            }
            if (excludeLast)
            {
                --end;
            }

            for (var i = begin; i < end; ++i)
            {
                positions.Add(range[i]);
                widths.Add(widthRange[i]);
            }
        }

        void UpdateRouteMeshes(HashSet<Route> routes,
                               Dictionary<Tuple<Vector2, int>, int> linesPerPositionMap,
                               Dictionary<Tuple<StreetSegment, int>, List<Vector3>> allPositionsMap)
        {
            // Go line by line so every line has a consistent offset.
            var lineSet = new HashSet<Line>();
            foreach (var route in routes)
            {
                lineSet.Add(route.line);
            }

            var lines = lineSet.ToList();
            lines.Sort((Line l1, Line l2) =>
            {
                return l1.name.CompareTo(l2.name);
            });

            var offsetMap = new Dictionary<Tuple<Vector2, int>, int>();
            foreach (var entry in allPositionsMap)
            {
                foreach (var pos in entry.Value)
                {
                    var key = Tuple.Create((Vector2)pos, entry.Key.Item2);
                    offsetMap.Add(key, 0);
                }
            }

            foreach (var line in lines)
            {
                foreach (var route in line.routes)
                {
                    if (!routes.Contains(route))
                    {
                        continue;
                    }

                    UpdateRouteMesh(route, linesPerPositionMap, allPositionsMap, offsetMap);
                }
            }
        }

        void GetLineWidthAndOffset(Route route, StreetSegment streetSeg,
                                   int currentLines, int currentLineOffset,
                                   out float halfLineWidth, out float offset)
        {
            if (currentLines == 1)
            {
                offset = 0f;
                halfLineWidth = route.line.LineWidth;

                return;
            }

            float availableSpace;
            if (currentLines < 3)
            {
                availableSpace = route.line.LineWidth * 2f;
            }
            else
            {
                int lanes = streetSeg?.street.lanes ?? 2;
                float width = StreetSegment.GetStreetWidth(
                    streetSeg?.street.type ?? Street.Type.Secondary,
                    lanes, InputController.RenderingDistance.Near);

                availableSpace = width * .7f;
            }

            var spacePerLine = availableSpace / currentLines;
            var gap = spacePerLine * 0.1f;
            var lineWidth = spacePerLine - gap;
            halfLineWidth = lineWidth * 0.5f;

            var baseOffset = -(availableSpace * .5f) + halfLineWidth;
            offset = baseOffset + (currentLineOffset * spacePerLine);
        }

        void IncreaseOffsets(StreetSegment segment, int lane, bool backward,
                             Route route, int startIdx, int endIdx,
                             Dictionary<Tuple<StreetSegment, int>, List<Vector3>> allPositionsMap,
                             Dictionary<Tuple<Vector2, int>, int> offsetMap)
        {
            if (startIdx == endIdx)
            {
                return;
            }

            Vector2 startPos;
            Vector2 endPos;

            if (backward)
            {
                startPos = route.positions[endIdx];
                endPos = route.positions[startIdx];
            }
            else
            {
                startPos = route.positions[startIdx];
                endPos = route.positions[endIdx];
            }

            var offsetPath = game.sim.trafficSim.GetPath(segment, lane);
            var startDistance = segment.GetDistanceFromStartStopLine(startPos, offsetPath);
            var endDistance = segment.GetDistanceFromStartStopLine(endPos, offsetPath);

            var allPositionsKey = Tuple.Create(segment, lane);
            var allPositions = allPositionsMap[allPositionsKey];
            for (var i = 0; i < allPositions.Count; ++i)
            {
                var pos = allPositions[i];
                var distance = segment.GetDistanceFromStartStopLine(pos, offsetPath);

                if (!backward && startIdx == 0 && distance.Equals(startDistance))
                {
                    continue;
                }
                if (backward && startIdx == 0 && distance.Equals(endDistance))
                {
                    continue;
                }

                if (distance >= startDistance && distance <= endDistance)
                {
                    var key = new Tuple<Vector2, int>(pos, lane);
                    ++offsetMap[key];
                }
            }
        }

        void UpdateRouteMesh(Route route,
                             Dictionary<Tuple<Vector2, int>, int> linesPerPositionMap,
                             Dictionary<Tuple<StreetSegment, int>, List<Vector3>> allPositionsMap,
                             Dictionary<Tuple<Vector2, int>, int> offsetMap)
        {
            var positions = route.positions;
            var currentPositions = route.CurrentPositions;
            var currentWidths = route.CurrentWidths;

            var newPositions = new List<Vector3>();
            var newWidths = new List<float>();

            var segments = new List<StreetSegment>();
            var numLines = new List<int>();
            var lineOffsets = new List<int>();

            var addedInfo = false;
            var prevWasIntersection = true;

            for (var i = 0; i < route.positions.Count;)
            {
                var startIdx = i;

                var pathSegment = route.GetSegmentForPosition(i);
                var isOrphaned = pathSegment != null && pathSegment.length == 1;

                if (!isOrphaned && pathSegment != null && i < route.positions.Count - 1)
                {
                    ++i;
                }

                var pos = route.positions[i];
                var lane = pathSegment?.lane ?? 0;
                var streetSeg = pathSegment?.segment;

                if (streetSeg?.name == "Hardenbergplatz (3) 3")
                {
                    Debug.Break();
                }

                var key = new Tuple<Vector2, int>(pos, lane);
                var currentLines = linesPerPositionMap.GetOrPutDefault(key, 1);
                var currentLineOffset = offsetMap.GetOrPutDefault(key, 0);

                if (streetSeg != null && !addedInfo)
                {
                    segments.Add(streetSeg);
                    numLines.Add(currentLines);
                    lineOffsets.Add(currentLineOffset);
                }

                addedInfo = false;

                if (isOrphaned)
                {
                    Debug.Assert(!pathSegment.direction.Equals(default(Vector2)));

                    float offset, halfLineWidth;
                    GetLineWidthAndOffset(route, streetSeg, currentLines, currentLineOffset,
                                          out halfLineWidth, out offset);

                    newPositions.Add(MeshBuilder.GetOffsetPoint(pos, offset, pathSegment.direction));
                    newWidths.Add(halfLineWidth);

                    prevWasIntersection = false;

                    ++i;
                    continue;
                }

                var startOffset = 0;
                var endOffset = 0;

                if (streetSeg?.name == "Kurfürstendamm 16")
                {
                    // Debug.Break();
                }

                while (++i < route.positions.Count)
                {
                    var nextPos = route.positions[i];
                    var nextSegment = route.GetSegmentForPosition(i);

                    var nextIsOrphaned = nextSegment != null && nextSegment.length == 1;
                    if (nextSegment != pathSegment && !nextIsOrphaned
                    && nextSegment != null
                    && i < route.positions.Count - 1)
                    {
                        nextPos = route.positions[i + 1];
                        nextSegment = route.GetSegmentForPosition(i + 1);
                    }
                    
                    var nextLane = nextSegment?.lane ?? 0;
                    var nextStreetSeg = nextSegment?.segment;

                    var nextKey = new Tuple<Vector2, int>(nextPos, nextLane);
                    var nextLines = linesPerPositionMap.GetOrPutDefault(nextKey, 1);
                    var nextLineOffset = offsetMap.GetOrPutDefault(nextKey, 0);

                    if (nextSegment != null && !addedInfo)
                    {
                        segments.Add(nextStreetSeg);
                        numLines.Add(nextLines);
                        lineOffsets.Add(nextLineOffset);

                        addedInfo = true;
                    }

                    if ((nextStreetSeg != streetSeg || nextLane != lane))
                    {
                        break;
                    }
                    if ((nextLines != currentLines || nextLineOffset != currentLineOffset))
                    {
                        break;
                    }
                }

                var length = i - startIdx;
                var excludeFirst = false;
                var excludeLast = false;

                // This can happen if there is a stop after the first position of the street.
                if (length == 1)
                {
                    Debug.Log("-------");
                    Debug.Log(route.name);
                    Debug.Log(streetSeg?.name);
                    Debug.Log(i + "/" + positions.Count);
                    Debug.Log(currentLines);
                    Debug.Log(pos);
                    Debug.Log("-------");

                    length = 2;
                    if (i < positions.Count)
                    {
                        excludeLast = true;
                    }
                    else
                    {
                        excludeFirst = true;
                        --startIdx;
                    }
                }

                List<Vector3> range;
                List<float> widthRange;

                if (pathSegment != null && currentLines <= 1)
                {
                    range = currentPositions.GetRange(startIdx, length);
                    if (currentWidths != null)
                    {
                        widthRange = currentWidths.GetRange(startIdx, length);
                    }
                    else
                    {
                        widthRange = Enumerable.Repeat(route.line.LineWidth, length).ToList();
                    }

                    prevWasIntersection = false;
                    AddPositions(newPositions, range, newWidths, widthRange, excludeFirst, excludeLast);

                    continue;
                }

                if (pathSegment != null)
                {
                    prevWasIntersection = false;
                    IncreaseOffsets(streetSeg, lane, pathSegment.backward, route,
                                    startIdx + startOffset, i - 1 - endOffset, allPositionsMap, offsetMap);

                    float offset, halfLineWidth;
                    GetLineWidthAndOffset(route, streetSeg, currentLines, currentLineOffset,
                                          out halfLineWidth, out offset);

                    range = MeshBuilder.GetOffsetPath(positions.GetRange(startIdx, length), offset);
                    widthRange = Enumerable.Repeat(halfLineWidth, length).ToList();
                }
                else
                {
                    prevWasIntersection = true;
                    for (var j = startIdx; j < i; ++j)
                    {
                        key = new Tuple<Vector2, int>(route.positions[j], lane);
                        offsetMap.GetOrPutDefault(key, 0);
                        ++offsetMap[key];
                    }

                    Debug.Assert(segments.Count >= 2, "path should not start with intersection");

                    var prevSegment = segments[segments.Count - 2];
                    var nextSegment = segments[segments.Count - 1];

                    var prevLines = numLines[numLines.Count - 2];
                    var nextLines = numLines[numLines.Count - 1];

                    var prevLineOffset = lineOffsets[lineOffsets.Count - 2];
                    var nextLineOffset = lineOffsets[lineOffsets.Count - 1];

                    float prevOffset, prevHalfLineWidth;
                    GetLineWidthAndOffset(route, prevSegment, prevLines, prevLineOffset,
                                          out prevHalfLineWidth, out prevOffset);

                    float nextOffset, nextHalfLineWidth;
                    GetLineWidthAndOffset(route, prevSegment, nextLines, nextLineOffset,
                                          out nextHalfLineWidth, out nextOffset);

                    var offsetDiff = nextOffset - prevOffset;
                    var widthDiff = nextHalfLineWidth - prevHalfLineWidth;

                    var offsetStep = offsetDiff / (length - 1);
                    var widthStep = widthDiff / (length - 1);

                    widthRange = new List<float>();
                    for (var j = 0; j < length; ++j)
                    {
                        widthRange.Add(prevHalfLineWidth + j * widthStep);
                    }

                    var offsets = new List<float>();
                    for (var j = 0; j < length; ++j)
                    {
                        offsets.Add(prevOffset + j * offsetStep);
                    }

                    range = MeshBuilder.GetOffsetPath(positions.GetRange(startIdx, length), offsets);
                }

                AddPositions(newPositions, range, newWidths, widthRange, excludeFirst, excludeLast);
            }

            var collider = route.GetComponent<PolygonCollider2D>();
            collider.pathCount = 0;

            var mesh = MeshBuilder.CreateSmoothLine(newPositions, newWidths, 20, 0, collider);
            route.UpdateMesh(mesh, newPositions, newWidths);
            route.line.wasModified = false;
            route.EnableCollision();
        }
    }
}