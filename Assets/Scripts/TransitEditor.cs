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
                tooltip.text.SetText(text);
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
            switch (mode) {
            case EditingMode.CreateNewLine:
                // Remove transparency.
                foreach (var route in map.transitRoutes) {
                    route.ResetTransparency();
                }

                break;
            case EditingMode.ModifyUnfinishedLine:
                // Add transparency to all other lines to make the new line easier to see.
                foreach (var route in map.transitRoutes) {
                    route.SetTransparency(.5f);
                }

                break;
            case EditingMode.ModifyFinishedLine:
                // Add transparency to all other lines to make the new line easier to see.
                foreach (var route in map.transitRoutes) {
                    route.SetTransparency(.5f);
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
                            ActivateTooltip(selectedSystem.Value, "Cannot add stop here");
                            break;
                        }

                        ActivateTooltip(selectedSystem.Value, "Add Stop");
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
                            ActivateTooltip(selectedSystem.Value, "Cannot add stop here");
                            break;
                        }

                        ActivateTooltip(selectedSystem.Value, "Add Stop");
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
                    if (!street.hasTramTracks)
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
            StreetHovered(null);

            if (currentLine == null || stop != currentLine.stops.First())
            {
                return;
            }

            ActivateTooltip(selectedSystem.Value, "Finish Line");
            game.snapController.EnableSnap(this.temporaryStopSnapSettingsId);
        }

        void StopHovered(Stop stop)
        {
            StreetHovered(null);

            if (currentLine == null || stop != currentLine.stops.First())
            {
                return;
            }

            ActivateTooltip(selectedSystem.Value, "Finish Line");
            game.snapController.EnableSnap(this.stopSnapSettingsId);
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

                    route.AddStreetSegmentOffset(segAndLane.segment, segAndLane.offset, segAndLane.length);
                }

                ++j;
            }

            currentLine = null;
            currentPath = null;
            previousStop = null;

            existingPathMesh?.SetActive(false);
            this.tooltip.Hide();

            EnterMode(EditingMode.CreateNewLine);
            CheckOverlappingRoutes(crossedStreets);
        }

        void CheckOverlappingRoutes(HashSet<Tuple<StreetSegment, int>> segments)
        {
            foreach (var seg in segments)
            {
                CheckOverlappingRoutes(seg.Item1, seg.Item2);
            }
        }

        struct OverlappingRouteInfo
        {
            public int start;
            public int end;
            public int numParallelRoutes;
            public int position;
        }

        void CheckOverlappingRoutes(StreetSegment seg, int lane)
        {
            var routes = seg.GetTransitRoutes(lane);
            var path = game.sim.trafficSim.GetPath(seg, lane);
            var numIntersectingRoutes = routes.Count;

            var isLeftLane = lane < seg.street.lanes / 2;
            var halfStreetWidth = seg.GetStreetWidth(InputController.RenderingDistance.Near) / 2f;
            var spacePerLine = halfStreetWidth / numIntersectingRoutes;

            var laneOffset = seg.LanePositionFromMiddle(lane, true);
            if (isLeftLane)
            {
                laneOffset = -laneOffset;
            }

            // Check where the routes overlap.
            var i = 0;
            foreach (var route in routes)
            {
                var position = i++; // FIXME

                var offset = spacePerLine * position;
                if (isLeftLane)
                {
                    offset = -offset;
                }

                var offsets = route.GetStreetSegmentOffsets(seg);
                var newPath = new List<Vector3>(route.positions);

                foreach (var offsetAndLength in offsets)
                {
                    var range = route.positions.GetRange(offsetAndLength.Item1, offsetAndLength.Item2);
                    var offsetRange = MeshBuilder.GetOffsetPath(range, laneOffset + offset);

                    for (int j = offsetAndLength.Item1; j < offsetAndLength.Item2; ++j)
                    {
                        newPath[j] = offsetRange[j - offsetAndLength.Item1];
                    }
                }

                var mesh = MeshBuilder.CreateSmoothLine(newPath, spacePerLine / 2f, 20);
                route.GetComponent<MeshFilter>().mesh = mesh;
            }

            // var overlappingRouteCount = new List<int>();
            // overlappingRouteCount.AddRange(Enumerable.Repeat(0, path.Length));

            // foreach (var route in routes)
            // {
            //     var offsets = route.GetStreetSegmentOffsets(seg);
            //     foreach (var offsetAndLength in offsets)
            //     {
            //         var firstPosOnStreet = route.positions[offsetAndLength.Item1];
            //         var i = 0;

            //         while (!path[i].Equals(firstPosOnStreet))
            //         {
            //             ++i;
            //         }

            //         for (int j = 0; j < offsetAndLength.Item2; ++j)
            //         {
            //             overlappingRouteCount[i + j]++;
            //         }
            //     }
            // }

            // Debug.Log("overlapping routes: " + overlappingRouteCount.ToString());
        }
    }
}