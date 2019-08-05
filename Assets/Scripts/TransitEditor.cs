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

        public UIModal lineInfoModal;

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
                        crossedStreets.Add(new Tuple<StreetSegment, int>(segAndLane.segment, segAndLane.lane));
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

        void CheckOverlappingRoutes(HashSet<Tuple<StreetSegment, int>> segments)
        {
            var linesPerPositionMap = new Dictionary<Route, List<int>>();
            var linePositionMap = new Dictionary<Tuple<StreetSegment, int, int>, int>();

            foreach (var seg in segments)
            {
                CheckOverlappingRoutes(seg.Item1, seg.Item2, linesPerPositionMap, linePositionMap);
            }

            UpdateRouteMeshes(segments, linesPerPositionMap);
        }

        struct OverlappingRouteInfo
        {
            public int start;
            public int end;
            public int numParallelRoutes;
            public int position;
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
                        list.Value[i] = new Tuple<int, int>(entry.Item1, entry.Item2 + 1);
                    }
                    else if (insertPos <= entry.Item1)
                    {
                        list.Value[i] = new Tuple<int, int>(entry.Item1 + 1, entry.Item2 + 1);
                    }
                }
            }
        }

        void CheckOverlappingRoutes(StreetSegment seg, int lane,
                                    Dictionary<Route, List<int>> linesPerPositionMap,
                                    Dictionary<Tuple<StreetSegment, int, int>, int> linePositionMap)
        {
            var routes = seg.GetTransitRoutes(lane);
            var path = game.sim.trafficSim.GetPath(seg, lane).ToList();

            // For a path like 'A -> B -> C', remember how many lines drive on each route (A->B), (B->C)
            var overlappingRouteCount = new List<int>();
            overlappingRouteCount.AddRange(Enumerable.Repeat(0, path.Count - 1));

            var occupiedSegments = new Dictionary<Route, List<Tuple<int, int>>>();
            foreach (var route in routes)
            {
                // Remember how many lines drive at each position on the route's path.
                List<int> linesPerPosition;
                if (!linesPerPositionMap.ContainsKey(route))
                {
                    linesPerPosition = new List<int>();
                    linesPerPosition.AddRange(Enumerable.Repeat(0, route.positions.Count));
                    linesPerPositionMap.Add(route, linesPerPosition);
                }
                else
                {
                    linesPerPosition = linesPerPositionMap[route];
                }

                occupiedSegments.Add(route, new List<Tuple<int, int>>());

                // Since the route stores the positions of all street segments in a single vector,
                // we need to get the offsets of every street segment individually.
                var offsets = route.GetStreetSegmentOffsets(seg, lane);
                foreach (var offsetAndLength in offsets)
                {
                    var i = 0;
                    for (i = offsetAndLength.offset; i < offsetAndLength.offset + offsetAndLength.length; ++i)
                    {
                        if (linesPerPosition[i] == 0)
                        {
                            linesPerPosition[i] = 1;
                        }
                    }

                    // If this route is not partial, increase the line count of every path segment.
                    if (!offsetAndLength.partialStart && !offsetAndLength.partialEnd)
                    {
                        for (i = 0; i < overlappingRouteCount.Count; ++i)
                        {
                            ++overlappingRouteCount[i];
                        }

                        occupiedSegments[route].Add(new Tuple<int, int>(0, overlappingRouteCount.Count));
                        continue;
                    }

                    // Find the first and last positions on the street that overlap with the path.
                    var firstPosOnStreet = route.positions[offsetAndLength.offset];
                    var lastPosOnStreet = route.positions[offsetAndLength.offset + offsetAndLength.length - 1];

                    float startDistance = seg.GetDistanceFromStartStopLine(firstPosOnStreet);
                    float endDistance = seg.GetDistanceFromStartStopLine(lastPosOnStreet);
                    
                    var minIdx = 0;
                    if (offsetAndLength.partialStart)
                    {
                        for (i = 1; i < path.Count; ++i)
                        {
                            float distance = distance = seg.GetDistanceFromStartStopLine(path[i]);
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
                        for (i = 1; i < path.Count; ++i)
                        {
                            float distance = seg.GetDistanceFromStartStopLine(path[i]);
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

                    Debug.Assert(minIdx < maxIdx);

                    for (i = minIdx; i < maxIdx; ++i)
                    {
                        ++overlappingRouteCount[i];
                    }

                    occupiedSegments[route].Add(new Tuple<int, int>(minIdx, maxIdx));
                }
            }

            Debug.Log("overlapping routes for " + seg.name + " (lane " + lane
                + "): [" + String.Join(", ", overlappingRouteCount) + "]");

            foreach (var route in routes)
            {
                var linesPerPosition = linesPerPositionMap[route];
                var offsets = route.GetStreetSegmentOffsets(seg, lane);
                
                var idx = 0;
                foreach (var offsetAndLength in offsets)
                {
                    var occupiedInfo = occupiedSegments[route][idx];
                    var startPos = occupiedInfo.Item1;
                    var endPos = occupiedInfo.Item2;
                    var maxPos = 0;

                    for (var i = startPos; i < endPos; ++i)
                    {
                        var key = new Tuple<StreetSegment, int, int>(
                            offsetAndLength.segment, offsetAndLength.lane, i);

                        if (!linePositionMap.ContainsKey(key))
                        {
                            linePositionMap.Add(key, 1);
                        }
                        else
                        {
                            maxPos = System.Math.Max(maxPos, linePositionMap[key]++);
                        }
                    }

                    Debug.Log("[" + seg.name + ", " + lane + "] '" + route.name + "' occupies segments "
                        + startPos + " - " + endPos + ", assigned position " + maxPos);

                    offsetAndLength.linePos = maxPos;

                    for (var i = offsetAndLength.offset; i < offsetAndLength.offset + offsetAndLength.length; ++i)
                    {
                        if (i == offsetAndLength.offset + offsetAndLength.length - 1)
                        {
                            linesPerPosition[i] = linesPerPosition[i - 1];
                        }
                        else
                        {
                            linesPerPosition[i] = System.Math.Max(
                                linesPerPosition[i], overlappingRouteCount[i - offsetAndLength.offset]);
                        }
                    }

                    ++idx;
                }
            }
        }

        void AddPositions(List<Vector3> positions,
                          IEnumerable<Vector3> range,
                          List<float> widths,
                          IEnumerable<float> widthRange)
        {
            positions.AddRange(range);
            widths.AddRange(widthRange);

            // var prevWidth = widths.Count == 0 ? -1f : widths.Last();
            // if (false && !prevWidth.Equals(-1f) && !prevWidth.Equals(width))
            // {
            //     var steps = System.Math.Min(range.Count, 3);
            //     var diff = Mathf.Abs(prevWidth - width);
            //     var step = diff / steps;

            //     // Gradually move to the new width.
            //     for (var i = 0; i < steps; ++i)
            //     {
            //         if (width > prevWidth)
            //         {
            //             widths.Add(prevWidth + (i + 1) * step);
            //         }
            //         else
            //         {
            //             widths.Add(prevWidth - (i + 1) * step);
            //         }
            //     }

            //     if (steps < range.Count)
            //     {
            //         widths.AddRange(Enumerable.Repeat(width, range.Count - steps));
            //     }
            // }
            // else
            // {
            //     if (prevWidths == null)
            //     {
            //         widths.AddRange(Enumerable.Repeat(width, range.Count));
            //     }
            //     else
            //     {
            //         widths.AddRange(prevWidths.GetRange(idx, le));
            //     }
            // }
        }

        void UpdateRouteMeshes(HashSet<Tuple<StreetSegment, int>> segments,
                               Dictionary<Route, List<int>> linesPerPositionMap)
        {
            foreach (var data in linesPerPositionMap)
            {
                var route = data.Key;
                var linesPerPosition = data.Value;

                Debug.Log("lines per position for " + route.name + ": [" + String.Join(", ", linesPerPosition) + "]");

                var positions = route.positions;
                var currentPositions = route.CurrentPositions;
                var currentWidths = route.CurrentWidths;

                var newPositions = new List<Vector3>();
                var newWidths = new List<float>();

                StreetSegment prevSegment = null;
                int prevLane = 0;
                int prevLines = 0;
                int prevLinePos = 0;

                for (var i = 0; i < currentPositions.Count;)
                {
                    var begin = i;
                    var segInfo = route.GetSegmentForPosition(i);
                    var lines = linesPerPosition[i++];

                    while (i < currentPositions.Count && linesPerPosition[i] == lines)
                    {
                        var otherSeg = route.GetSegmentForPosition(i);
                        if (segInfo == null)
                        {
                            if (otherSeg != null)
                            {
                                break;
                            }

                            ++i;
                            continue;
                        }
                        if (otherSeg == null)
                        {
                            if (segInfo != null)
                            {
                                break;
                            }

                            ++i;
                            continue;
                        }

                        if (otherSeg.segment != segInfo.segment || otherSeg.lane != segInfo.lane)
                        {
                            break;
                        }

                        ++i;
                    }

                    List<Vector3> range;
                    IEnumerable<float> widthRange;

                    var isIntersectionPath = false; // lines == 0;
                    if (!(isIntersectionPath && prevSegment != null) && (segInfo == null || lines <= 1))
                    {
                        range = currentPositions.GetRange(begin, i - begin);
                        if (currentWidths != null)
                        {
                            widthRange = currentWidths.GetRange(begin, range.Count);
                        }
                        else
                        {
                            widthRange = Enumerable.Repeat(route.line.LineWidth, range.Count);
                        }

                        AddPositions(newPositions, range, newWidths, widthRange);

                        if (!isIntersectionPath)
                        {
                            prevSegment = null;
                        }

                        continue;
                    }

                    StreetSegment seg;
                    int lane;
                    int linePos;

                    if (isIntersectionPath)
                    {
                        seg = prevSegment;
                        lane = prevLane;
                        linePos = prevLinePos;
                        lines = prevLines;
                    }
                    else
                    {
                        seg = segInfo.segment;
                        lane = segInfo.lane;
                        linePos = segInfo.linePos;
                    }

                    var lanes = seg.street.lanes;
                    var width = seg.GetStreetWidth(InputController.RenderingDistance.Near);
                    var halfWidth = width * 0.5f;
                    var spacePerLine = halfWidth / lines;

                    var gap = spacePerLine * 0.1f;
                    var lineWidth = spacePerLine - gap;
                    var halfLineWidth = lineWidth * 0.5f;
    
                    var baseOffset = -(route.line.LineWidth / 2f);
                    
                    float offset;
                    if (linePos == 0)
                    {
                        offset = baseOffset;// + halfLineWidth;
                    }
                    // else if (linePos == lines - 1)
                    // {
                    //     offset = baseOffset + linePos * spacePerLine + (spacePerLine - lineWidth);
                    // }
                    else
                    {
                        offset = baseOffset + (linePos * spacePerLine) + gap;// + halfLineWidth;
                    }

                    range = MeshBuilder.GetOffsetPath(positions.GetRange(begin, i - begin), offset);
                    widthRange = Enumerable.Repeat(halfLineWidth, range.Count);

                    AddPositions(newPositions, range, newWidths, widthRange);

                    prevSegment = seg;
                    prevLane = lane;
                    prevLines = lines;
                    prevLinePos = linePos; 
                }

                var collider = route.GetComponent<PolygonCollider2D>();
                collider.pathCount = 0;

                var mesh = MeshBuilder.CreateSmoothLine(newPositions, newWidths, 20, 0, collider);
                route.UpdateMesh(mesh, newPositions, newWidths);
                route.line.wasModified = false;
            }
        }
    }
}