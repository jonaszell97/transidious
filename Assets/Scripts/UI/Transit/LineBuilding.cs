using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    public abstract class LineBuilder
    {
        /// Describes the current state of line building, which works like a state machine.
        public enum CreationState
        {
            /// The default state when the line builder is inactive.
            Idle,
            
            /// The line building has just started, and no stop has been placed yet.
            FirstStop,

            /// There is at least one stop on the line, and additional ones can be placed.
            IntermediateStops,

            /// The line was closed and can be reviewed before finishing it.
            Review,
        }

        /// Describes the current state of line editing, which works like a state machine.
        public enum EditingState
        {
            /// The default state when the line builder is inactive.
            Idle,
        }

        /// Reference to the game controller.
        protected GameController game;

        /// Whether or not the game was already paused when we started line creation.
        private bool _wasPaused;

        /// The current line creation state.
        public CreationState creationState { get; protected set; }

        /// The undo / redo stack.
        protected readonly UndoStack UndoStack;

        /// The transit type edited by this builder.
        protected TransitType transitType;

        /// The stop type to use.
        protected Stop.StopType _stopType;

        /// Saved event flag for restoration.
        protected MapObjectKind savedClickEvents;
        protected MapObjectKind savedHoverEvents;

        /// ID of the snap settings needed for this line builder.
        protected int snapSettingID;

        /// IDs of the event listeners used by this line builder.
        protected int[] eventListenerIDs;

        /// IDs of the keyboard event listeners used by this line builder.
        protected int[] keyboardEventIDs;

        /// The line that is currently being created / edited.
        protected TemporaryLine currentLine;

        /// The previous stop on the line (can either be a Stop or a TemporaryStop).
        protected IMapObject previousStop;

        /// The path between the previous stop and the current cursor position.
        protected List<Vector2> temporaryPath;

        /// List of created temp stops.
        protected List<TemporaryStop> _temporaryStops;

        /// Game object used to render the path between stops that were added to the line.
        protected GameObject existingPathMesh;

        /// Game object used to render the path between the previous stop and the current cursor position.
        protected GameObject plannedPathMesh;

        /// Set of created temporary stop locations.
        protected HashSet<Vector2> _tempStopLocations;

        /// The length of the line in km.
        protected float length;

        /// The current accumulated construction cost of the line.
        protected decimal totalConstructionCost;

        /// The current accumulated monthly cost of the line.
        protected decimal totalMonthlyCost;

        /// The cost of constructing a km of the line.
        protected abstract decimal costPerKm { get; }

        /// The monthly cost of operating a km of the line.
        protected abstract decimal operatingCostPerKm { get; }

        /// The cost of construction per stop.
        protected abstract decimal costPerStop { get; }

        /// The monthly cost of operating a stop.
        protected abstract decimal operatingCostPerStop { get; }

        static bool confirmationPanelInitialized = false;

        /// Protected c'tor.
        protected LineBuilder(GameController game, TransitType transitType)
        {
            this.game = game;
            this.transitType = transitType;

            creationState = CreationState.Idle;
            _tempStopLocations = new HashSet<Vector2>();
            _temporaryStops = new List<TemporaryStop>();
            UndoStack = new UndoStack(false);
            _stopType = Stop.GetStopType(transitType);
        }

        /// Execute an action and push it to the undo stack.
        protected void PerformAction(UndoStack.Action.ActionFunc redo, UndoStack.Action.ActionFunc undo)
        {
            UndoStack.PushAndExecute(redo, undo);
            UpdateUndoRedoVisibility();
        }

        /// Undo the latest action.
        public void Undo()
        {
            UndoStack.Undo();
            UpdateUndoRedoVisibility();
        }

        /// Redo the latest action.
        public void Redo()
        {
            UndoStack.Redo();
            UpdateUndoRedoVisibility();
        }

        /// Auto-complete the line.
        public abstract void AutoComplete();

        /// Update visibility of undo / redo buttons.
        private void UpdateUndoRedoVisibility()
        {
            if (UndoStack.CanUndo)
            {
                game.mainUI.lineBuildingUndoButton.Enable();
            }
            else
            {
                game.mainUI.lineBuildingUndoButton.Disable();
            }

            if (UndoStack.CanRedo)
            {
                game.mainUI.lineBuildingRedoButton.Enable();
            }
            else
            {
                game.mainUI.lineBuildingRedoButton.Disable();
            }
        }

        /// Initialize the line builder. Needs to be called once before it can be used.
        public virtual void Initialize()
        {
            var ui = game.mainUI;
            ui.lineBuildingTrashButton.button.onClick.AddListener(() =>
            {
                ui.confirmPanel.Show("Are you sure you want to scrap this line?", () =>
                {
                    ui.transitUI.activeBuilder.EndLineCreation();
                });
            });

            ui.lineBuildingUndoButton.Disable();
            ui.lineBuildingRedoButton.Disable();

            this.keyboardEventIDs = new[]
            {
                game.input.RegisterKeyboardEventListener(KeyCode.Escape, _ =>
                {
                    game.mainUI.lineBuildingTrashButton.button.onClick.Invoke();
                }, false)
            };
        }

        /// Enable the event listeners for a particular state.
        public virtual void EnableListeners(CreationState state)
        {
            if (eventListenerIDs == null)
                return;
            
            foreach (var id in eventListenerIDs)
            {
                game.input.EnableEventListener(id);
            }
        }

        /// Disable the event listeners for a particular state.
        public virtual void DisableListeners(CreationState state)
        {
            if (eventListenerIDs == null)
                return;

            foreach (var id in eventListenerIDs)
            {
                game.input.DisableEventListener(id);
            }
        }

        public virtual void StartLineCreation()
        {
            // Enter pause during line construction
            _wasPaused = game.EnterPause(true);

            // Show line building UI
            game.mainUI.transitEditorPanel.SetActive(false);
            game.mainUI.ShowLineBuildingPanel();
            this.Transition(CreationState.FirstStop);

            // Disable transit vehicles
            game.sim.DisableTransitVehicles();

            // Disable undo / redo
            game.mainUI.lineBuildingUndoButton.Disable();
            game.mainUI.lineBuildingRedoButton.Disable();
        }

        public virtual void EndLineCreation()
        {
            if (!_wasPaused)
            {
                game.ExitPause(true);
            }
            else
            {
                game.UnblockPause();
            }

            foreach (var ts in _temporaryStops)
            {
                ts.Destroy();
            }

            _temporaryStops.Clear();
            currentLine = null;
            previousStop = null;
            temporaryPath = null;

            ResetPath();
            plannedPathMesh?.SetActive(false);
            existingPathMesh?.SetActive(false);
            UIInstruction.Hide();

            // Update UI
            game.mainUI.transitEditorPanel.SetActive(true);
            game.mainUI.transitUI.activeBuilder = null;
            game.mainUI.ShowPanels();

            // Enable transit vehicles
            game.sim.EnableTransitVehicles();

            this.Transition(CreationState.Idle);
        }

        protected void Transition(CreationState state)
        {
            switch (state)
            {
                case CreationState.Idle:
                    game.activeMouseDownEvents = savedClickEvents;
                    game.activeMouseOverEvents = savedHoverEvents;

                    // Disable snapping.
                    game.snapController.DisableSnap(this.snapSettingID);

                    // Disable listeners.
                    this.DisableListeners(state);

                    // Enable collision for all existing routes while we're editing.
                    foreach (var route in game.loadedMap.transitRoutes)
                    {
                        route.EnableCollision();
                    }

                    // Remove transparency of all other lines.
                    foreach (var line in game.loadedMap.transitLines)
                    {
                        line.ResetTransparency();
                    }

                    if (currentLine != null)
                    {
                        foreach (var stop in currentLine.stops)
                        {
                            var tmp = stop as TemporaryStop;
                            if (tmp != null)
                            {
                                tmp.Destroy();
                            }
                        }
                    }

                    currentLine = null;
                    previousStop = null;
                    temporaryPath = null;

                    this.existingPathMesh?.SetActive(false);
                    this.plannedPathMesh?.SetActive(false);
                    game.mainUI.HideConstructionCost();
                    UIInstruction.Hide();

                    game.mainUI.transitUI.HideTransitSystemOverviewPanel();

                    break;
                case CreationState.FirstStop:
                    game.mainUI.transitUI.confirmButton.onClick.RemoveAllListeners();
                    game.mainUI.transitUI.confirmButton.onClick.AddListener(() =>
                    {
                        game.input.EnableControls();
                        game.mainUI.HideOverlay();
                        game.mainUI.transitUI.confirmLineCreationPanel.SetActive(false);
                        this.FinishLine();
                    });

                    game.mainUI.transitUI.cancelButton.onClick.RemoveAllListeners();
                    game.mainUI.transitUI.cancelButton.onClick.AddListener(() =>
                    {
                        game.input.EnableControls();
                        game.mainUI.HideOverlay();
                        game.mainUI.transitUI.confirmLineCreationPanel.SetActive(false);
                        this.Transition(CreationState.IntermediateStops);
                    });

                    savedClickEvents = game.activeMouseDownEvents;
                    game.activeMouseDownEvents = MapObjectKind.None;

                    savedHoverEvents = game.activeMouseOverEvents;
                    game.activeMouseOverEvents = MapObjectKind.None;

                    // Enable snapping.
                    game.snapController.EnableSnap(this.snapSettingID);

                    // Enable listeners.
                    this.EnableListeners(state);

                    // Disable collision for all existing routes while we're editing.
                    foreach (var route in game.loadedMap.transitRoutes)
                    {
                        route.DisableCollision();
                    }

                    // Add transparency to all other lines to make the new line easier to see.
                    foreach (var line in game.loadedMap.transitLines)
                    {
                        line.SetTransparency(.5f);
                    }

                    // Disable auto-completion.
                    game.mainUI.lineBuildingAutocompleteButton.Disable();

                    totalConstructionCost = 0m;
                    totalMonthlyCost = 0m;

                    break;
                case CreationState.IntermediateStops:
                    break;
                case CreationState.Review:
                    break;
            }

            this.creationState = state;
        }

        protected void ShowConfirmationPanel()
        {
            if (!confirmationPanelInitialized)
            {
                game.mainUI.transitUI.confirmLineCreationInfo.Initialize();
                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("num_stops", "ui:line:num_stops", "", "Sprites/ui_bus_stop");
                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("total_length", "ui:line:total_length", "", "Sprites/ui_length");
                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("construction_cost", "ui:construction_cost", "", "Sprites/ui_bulldoze");
                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("monthly_cost", "ui:monthly_cost", "", "Sprites/ui_money");

                confirmationPanelInitialized = true;
            }

            this.Transition(CreationState.Review);

            game.input.DisableControls();
            game.mainUI.ShowOverlay();
            game.mainUI.HideConstructionCost();
            UIInstruction.Hide();

            game.mainUI.transitUI.confirmLineCreationPanel.SetActive(true);
            game.mainUI.transitUI.confirmLineCreationInfo.SetValue("num_stops", currentLine.stops.Count.ToString());
            game.mainUI.transitUI.confirmLineCreationInfo.SetValue("total_length", Translator.GetNumber(length) + " km");
            game.mainUI.transitUI.confirmLineCreationInfo.SetValue("construction_cost", Translator.GetCurrency(totalConstructionCost, true));
            game.mainUI.transitUI.confirmLineCreationInfo.SetValue("monthly_cost", Translator.GetCurrency(totalMonthlyCost, true));
        }

        protected TemporaryStop CreateTempStop(string name, Vector3 pos)
        {
            var obj = GameObject.Instantiate(game.mainUI.transitUI.temporaryStopPrefab);
            
            var stop = obj.GetComponent<TemporaryStop>();
            stop.Initialize(game, name, pos);

            totalConstructionCost += costPerStop;
            totalMonthlyCost += operatingCostPerStop;
            _temporaryStops.Add(stop);

            return stop;
        }

        protected IMapObject CreateFirstStop(TransitType type, IMapObject firstStop)
        {
            Debug.Assert(creationState == CreationState.FirstStop);

            PerformAction(() =>
            {
                currentLine = new TemporaryLine
                {
                    name = Translator.Get("tooltip:new_line", game.GetSystemName(type)),
                    stops = new List<IMapObject>(),
                    completePath = new List<Vector2>(),
                    paths = new List<int>(),
                    streetSegments = new List<List<TrafficSimulator.PathSegmentInfo>>(),
                };

                if (firstStop is TemporaryStop tmp)
                {
                    _tempStopLocations.Add(tmp.position);
                    tmp.gameObject.SetActive(true);
                }

                currentLine.stops.Add(firstStop);
                UpdateCosts();

                previousStop = firstStop;
                this.Transition(CreationState.IntermediateStops);
            }, () =>
            {
                if (firstStop is TemporaryStop tmp)
                {
                    _tempStopLocations.Remove(tmp.position);
                    tmp.gameObject.SetActive(false);
                }

                currentLine = null;
                totalConstructionCost = 0;
                totalMonthlyCost = 0;
                previousStop = null;
                creationState = CreationState.FirstStop;
            });

            return firstStop;
        }

        protected IMapObject CreateFirstStop(TransitType type, string name, Vector2 pos)
        {
            var firstStop = CreateTempStop(name, pos);
            return CreateFirstStop(type, firstStop);
        }

        protected void UpdateCosts()
        {
            game.mainUI.ShowConstructionCost(totalConstructionCost, totalMonthlyCost);
        }
        
        static float GetLengthOfPath(List<Vector2> path)
        {
            var length = 0f;
            for (var i = 1; i < path.Count; ++i)
            {
                length += (path[i] - path[i - 1]).magnitude;
            }

            return length / 1000f;
        }

        float GetLengthOfTemporaryPath()
        {
            return GetLengthOfPath(temporaryPath);
        }

        protected void UpdateLength()
        {
            var length = (decimal)GetLengthOfTemporaryPath();
            this.length += (float)length;

            totalConstructionCost += length * costPerKm;
            totalMonthlyCost += length * operatingCostPerKm;
            UpdateCosts();
        }

        protected IMapObject AddStop(IMapObject nextStop, List<TrafficSimulator.PathSegmentInfo> streetSegments = null)
        {
            Debug.Assert(temporaryPath != null, "invalid path!");

            var savedPath = temporaryPath.ToList();
            var savedPrevStop = previousStop;
            
            List<TrafficSimulator.PathSegmentInfo> savedSegments = null;
            if (streetSegments != null)
            {
                savedSegments = streetSegments.ToList();
            }

            PerformAction(() =>
            {
                if (nextStop is TemporaryStop tmp)
                {
                    _tempStopLocations.Add(tmp.position);
                    tmp.gameObject.SetActive(true);
                }
                
                currentLine.stops.Add(nextStop);
                currentLine.completePath.AddRange(savedPath);
                currentLine.paths.Add(currentLine.completePath.Count);

                UpdateLength();
                DrawExistingPath();
                
                if (savedSegments != null)
                {
                    currentLine.streetSegments.Add(savedSegments);
                }

                previousStop = nextStop;
                
                // Enable auto-completion.
                game.mainUI.lineBuildingAutocompleteButton.Enable();
            }, () =>
            {
                if (nextStop is TemporaryStop tmp)
                {
                    _tempStopLocations.Remove(tmp.position);
                    tmp.gameObject.SetActive(false);
                }

                currentLine.stops.RemoveLast();
                currentLine.completePath.RemoveRange(currentLine.completePath.Count - savedPath.Count, savedPath.Count);
                currentLine.paths.RemoveLast();

                if (savedSegments != null)
                {
                    currentLine.streetSegments.RemoveLast();
                }

                var length = (decimal)GetLengthOfPath(savedPath);
                this.length -= (float)length;

                totalConstructionCost -= length * costPerKm;
                totalMonthlyCost -= length * operatingCostPerKm;

                previousStop = savedPrevStop;

                UpdateCosts();
                DrawExistingPath();

                if (currentLine.stops.Count <= 1)
                {
                    // Disable auto-completion.
                    game.mainUI.lineBuildingAutocompleteButton.Disable();
                }
            });

            return nextStop;
        }

        protected TemporaryStop AddStop(string name, Vector2 pos,
                                        List<TrafficSimulator.PathSegmentInfo> streetSegments = null)
        {
            var nextStop = CreateTempStop(name, pos);
            AddStop(nextStop, streetSegments);

            return nextStop;
        }

        protected virtual void FinishLine()
        {
            ResetPath();

            game.financeController.Purchase(totalConstructionCost);
            game.financeController.AddExpense("ui:line:operating_cost", totalMonthlyCost);

            var line = game.loadedMap.CreateLine(transitType, currentLine.name,
                                                 Colors.GetDefaultSystemColor(transitType));

            Stop firstStop = null;
            var pathIdx = 0;
            var createdStops = new Dictionary<IMapObject, Stop>();

            for (var i = 0; i < currentLine.stops.Count; ++i)
            {
                var nextStop = currentLine.stops[i];

                Stop stop;
                if (createdStops.TryGetValue(nextStop, out Stop existingStop))
                {
                    stop = existingStop;
                }
                else if (nextStop is TemporaryStop tmpStop)
                {
                    stop = game.loadedMap.CreateStop(_stopType, tmpStop.name, tmpStop.position);
                    createdStops.Add(tmpStop, stop);
                }
                else
                {
                    stop = (Stop) nextStop;
                }

                if (firstStop == null)
                {
                    firstStop = stop;
                }

                var isBackRoute = transitType == TransitType.Subway && i > currentLine.stops.Count / 2;
                if (i != 0)
                {
                    line.AddStop(stop, isBackRoute, currentLine.completePath.GetRange(
                        pathIdx, currentLine.paths[i - 1] - pathIdx));

                    pathIdx = currentLine.paths[i - 1];
                }
                else
                {
                    line.AddStop(stop);
                }
            }

            var newLine = line.Finish(new Schedule
            {
                dayHours = System.Tuple.Create(4, 22),
                nightHours = System.Tuple.Create(22, 1),
                operatingDays = Weekday.All,
                dayInterval = 20,
                nightInterval = 20,
            });

            if (_stopType == Stop.StopType.StreetBound)
            {
                var crossedStreets = new HashSet<Tuple<StreetSegment, int>>();
                var j = 0;

                foreach (var route in newLine.routes)
                {
                    route.DisableCollision();

                    // Note which streets this route passes over.
                    foreach (var segAndLane in currentLine.streetSegments[j])
                    {
                        var routesOnSegment = segAndLane.segment.GetTransitRoutes(segAndLane.lane);
                        routesOnSegment.Add(route);

                        route.AddStreetSegmentOffset(segAndLane);
                        crossedStreets.Add(Tuple.Create(segAndLane.segment, segAndLane.lane));
                    }

                    ++j;
                }
                
                game.transitEditor.CheckOverlappingRoutes(crossedStreets);
            }
            else
            {
                game.TransitMap.UpdateAppearances(newLine);
            }

            foreach (var ts in _temporaryStops)
            {
                ts.Destroy();
            }

            _temporaryStops.Clear();
            this.EndLineCreation();

            newLine.StartVehicles();

            var modal = MainUI.instance.lineModal;
            modal.SetLine(newLine, newLine.routes.First());
            modal.modal.Enable();
        }

        protected void DrawExistingPath()
        {
            if (this.existingPathMesh == null)
            {
                this.existingPathMesh = GameObject.Instantiate(game.loadedMap.meshPrefab);
                this.existingPathMesh.transform.position
                    = new Vector3(0, 0, Map.Layer(MapLayer.TemporaryLines));
            }

            var color = Colors.GetDefaultSystemColor(transitType);
            var mesh = MeshBuilder.CreateSmoothLine(currentLine.completePath, 1.25f, 10, 0f);
            var renderer = existingPathMesh.GetComponent<MeshRenderer>();
            var filter = existingPathMesh.GetComponent<MeshFilter>();

            filter.mesh = mesh;
            renderer.material = GameController.instance.GetUnlitMaterial(color);

            existingPathMesh.SetActive(true);
        }

        protected void ResetPath()
        {
            if (this.plannedPathMesh == null)
            {
                return;
            }

            var filter = plannedPathMesh.GetComponent<MeshFilter>();
            filter.mesh = null;
        }

        protected void DrawCurrentPath()
        {
            if (this.plannedPathMesh == null)
            {
                this.plannedPathMesh = GameObject.Instantiate(game.loadedMap.meshPrefab);
                this.plannedPathMesh.transform.position
                    = new Vector3(0, 0, Map.Layer(MapLayer.TemporaryLines));
            }

            var color = Colors.GetDefaultSystemColor(transitType);
            var mesh = MeshBuilder.CreateSmoothLine(temporaryPath, 1.25f, 10, 0f);
            var renderer = plannedPathMesh.GetComponent<MeshRenderer>();
            var filter = plannedPathMesh.GetComponent<MeshFilter>();

            filter.mesh = mesh;
            renderer.material = GameController.instance.GetUnlitMaterial(color);

            plannedPathMesh.SetActive(true);
        }
    }

    public abstract class StreetboundLineBuilder : LineBuilder
    {
        /// Previous calculated paths for the temporary line.
        protected List<TrafficSimulator.PathSegmentInfo> temporarySegments;

        /// Protected c'tor.
        protected StreetboundLineBuilder(GameController game, TransitType transitType) : base(game, transitType)
        {
            this.temporarySegments = new List<TrafficSimulator.PathSegmentInfo>();
        }

        public override void Initialize()
        {
            base.Initialize();

            this.eventListenerIDs = new int[]
            {
                game.input.RegisterEventListener(InputEvent.MouseEnter, obj =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseEnter(street);
                    }
                    else
                    {
                        this.OnMouseEnter(obj);
                    }
                }, false),

                game.input.RegisterEventListener(InputEvent.MouseOver, obj =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseOver(street);
                    }
                }, false),

                game.input.RegisterEventListener(InputEvent.MouseExit, obj =>
                {
                    if (obj is StreetSegment || obj is Stop || obj is TemporaryStop)
                    {
                        this.OnMouseExit(obj);
                    }
                }, false),

                game.input.RegisterEventListener(InputEvent.MouseDown, obj =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseDown(street);
                    }
                    else
                    {
                        this.OnMouseDown(obj);
                    }
                }, false),
            };
        }

        /// Auto-complete the line.
        public override void AutoComplete()
        {
            Debug.Assert(currentLine != null && currentLine.stops.Count > 0);

            var firstStop = currentLine.stops.First();
            OnMouseEnter(firstStop);
            OnMouseDown(firstStop);
        }

        protected void OnMouseEnter(StreetSegment street)
        {
            switch (creationState)
            {
                case CreationState.FirstStop:
                    UIInstruction.Show("Click to create line");
                    break;
                default:
                    break;
            }
        }

        protected void DrawTemporaryPath(Vector2 pos)
        {
            var options = new PathPlanning.PathPlanningOptions { allowWalk = false };
            var planner = new PathPlanning.PathPlanner(options);

            var result = planner.FindClosestDrive(game.loadedMap, previousStop.transform.position,
                                                  pos);

            if (result == null)
            {
                ResetPath();
                UIInstruction.Show("Cannot add stop here");

                return;
            }

            UIInstruction.Show("Click to add stop");
            DrawTemporaryPath(result);
        }

        protected void OnMouseEnter(IMapObject stop)
        {
            if (stop.Kind != MapObjectKind.Stop && stop.Kind != MapObjectKind.TemporaryStop)
            {
                return;
            }

            stop.transform.localScale = new Vector3(1.3f, 1.3f, 1.0f);

            switch (creationState)
            {
                case CreationState.FirstStop:
                    UIInstruction.Show("Click to create line");
                    break;
                case CreationState.IntermediateStops:
                    if (stop != currentLine.stops.First() && currentLine.stops.Contains(stop))
                    {
                        UIInstruction.Show("This stop is already on the line");
                        return;
                    }

                    if (stop is Stop s)
                    {
                        if (s.Type != _stopType)
                        {
                            UIInstruction.Show("This stop can only be used for X lines");
                            return;
                        }

                        this.DrawTemporaryPath(s.location);
                    }
                    else
                    {
                        this.DrawTemporaryPath(((TemporaryStop)stop).position);
                    }

                    if (stop == currentLine.stops.First())
                    {
                        UIInstruction.Show("Click to complete line");
                    }

                    break;
                default:
                    break;
            }
        }

        protected void OnMouseOver(StreetSegment street)
        {
            switch (creationState)
            {
                case CreationState.IntermediateStops:
                    this.DrawTemporaryPath(game.input.GameCursorPosition);
                    break;
                default:
                    break;
            }
        }

        protected void OnMouseExit(IMapObject stop)
        {
            if (stop.Kind == MapObjectKind.Stop || stop.Kind == MapObjectKind.TemporaryStop)
            {
                stop.transform.localScale = Vector3.one;
            }

            switch (creationState)
            {
                case CreationState.FirstStop:
                    UIInstruction.Show("ui:line:bus:create_instruction0");
                    break;
                case CreationState.IntermediateStops:
                    ResetPath();
                    break;
                default:
                    break;
            }
        }

        protected void OnMouseDown(StreetSegment street)
        {
            switch (creationState)
            {
                case CreationState.FirstStop:
                    this.CreateFirstStop(street);
                    break;
                case CreationState.IntermediateStops:
                    this.AddStop(street);
                    break;
                default:
                    break;
            }
        }

        protected void OnMouseDown(IMapObject stop)
        {
            if (!(stop is Stop) && !(stop is TemporaryStop))
            {
                return;
            }

            switch (creationState)
            {
                case CreationState.FirstStop:
                    base.CreateFirstStop(transitType, stop);
                    break;
                case CreationState.IntermediateStops:
                    if (stop is Stop s)
                    {
                        if (s.Type != _stopType)
                        {
                            return;
                        }
                    }

                    if (stop == currentLine.stops.First())
                    {
                        UpdateLength();
                        this.ShowConfirmationPanel();
                    }
                    else if (!currentLine.stops.Contains(stop))
                    {
                        this.AddStop(stop);
                    }

                    break;
                default:
                    break;
            }
        }

        protected IMapObject CreateFirstStop(StreetSegment street)
        {
            return base.CreateFirstStop(transitType, street.street.name, game.input.GameCursorPosition);
        }

        protected virtual TemporaryStop AddStop(StreetSegment street)
        {
            var nextStop = base.AddStop(street.street.name, game.input.GameCursorPosition, temporarySegments);
            this.temporarySegments.Clear();

            return nextStop;
        }

        protected new virtual IMapObject AddStop(IMapObject nextStop)
        {
            nextStop = base.AddStop(nextStop, temporarySegments);
            this.temporarySegments.Clear();

            return nextStop;
        }

        protected override void FinishLine()
        {
            currentLine.stops.Add(currentLine.stops.First());
            currentLine.completePath.AddRange(temporaryPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            currentLine.streetSegments.Add(temporarySegments);
            temporarySegments = new List<TrafficSimulator.PathSegmentInfo>();

            base.FinishLine();

            temporaryPath = null;
        }

        protected virtual void DrawTemporaryPath(PathPlanning.PathPlanningResult path)
        {
            temporarySegments.Clear();
            temporaryPath = game.sim.trafficSim.GetCompletePath(path, temporarySegments);

            base.DrawCurrentPath();
        }
    }

    public class BusLineBuilder : StreetboundLineBuilder
    {
        /// Public c'tor.
        public BusLineBuilder(GameController game) : base(game, TransitType.Bus)
        {

        }

        protected override decimal costPerKm => 0m;

        protected override decimal operatingCostPerKm => 25m;

        protected override decimal costPerStop => 1000m;

        protected override decimal operatingCostPerStop => 75m;

        /// Initialize the event listeners.
        public override void Initialize()
        {
            base.Initialize();

            this.snapSettingID = game.snapController.AddSnap(typeof(StreetSegment), new SnapSettings
            {
                snapCursor = game.createStreetSprite,
                snapCursorColor = Colors.GetDefaultSystemColor(TransitType.Bus),
                snapCursorScale = new Vector3(3f, 3f, 1f),

                hideCursor = true,

                snapToEnd = false,
                snapToLane = true,
                snapToRivers = false,
            }, false);
        }
    }

    public class TramLineBuilder : StreetboundLineBuilder
    {
        HashSet<StreetSegment> addedTramTracks;

        /// Public c'tor.
        public TramLineBuilder(GameController game) : base(game, TransitType.Tram)
        {
            this.addedTramTracks = new HashSet<StreetSegment>();
        }

        // Only if tram tracks need to be built.
        protected override decimal costPerKm => 0m;

        protected override decimal operatingCostPerKm => 35m;

        protected override decimal costPerStop => 1500m;

        protected override decimal operatingCostPerStop => 100m;

        protected static readonly decimal costPerKmTrack = 10000m;

        /// Initialize the event listeners.
        public override void Initialize()
        {
            base.Initialize();

            this.snapSettingID = game.snapController.AddSnap(typeof(StreetSegment), new SnapSettings
            {
                snapCursor = game.createStreetSprite,
                snapCursorColor = Colors.GetDefaultSystemColor(TransitType.Tram),
                snapCursorScale = new Vector3(2f, 2f, 1f),

                hideCursor = true,

                snapToEnd = false,
                snapToLane = true,
                snapToRivers = false,
            }, false);
        }

        void AddTramTracksToTemporaryPath()
        {
            foreach (var seg in temporarySegments)
            {
                if (!seg.segment.hasTramTracks && !addedTramTracks.Contains(seg.segment))
                {
                    totalConstructionCost += ((decimal)seg.segment.length / 1000m) * costPerKmTrack;
                    addedTramTracks.Add(seg.segment);
                }
            }
        }

        protected override void FinishLine()
        {
            AddTramTracksToTemporaryPath();

            foreach (var seg in addedTramTracks)
            {
                seg.AddTramTracks();
            }

            base.FinishLine();
        }

        protected override TemporaryStop AddStop(StreetSegment street)
        {
            AddTramTracksToTemporaryPath();
            return base.AddStop(street);
        }

        protected override IMapObject AddStop(IMapObject nextStop)
        {
            AddTramTracksToTemporaryPath();
            return base.AddStop(nextStop);
        }

        protected override void DrawTemporaryPath(PathPlanning.PathPlanningResult path)
        {
            temporarySegments.Clear();
            temporaryPath = game.sim.trafficSim.GetCompletePath(path, temporarySegments);
            base.DrawCurrentPath();
        }
    }

    public class SubwayLineBuilder : LineBuilder
    {
        /// The grid snap settings.
        private SnapSettings _gridSnapSettings;

        /// The stop that the cursor is currently over, or null.
        private IMapObject _hoveredStop;

        /// Public c'tor.
        public SubwayLineBuilder(GameController game) : base(game, TransitType.Subway)
        {
            _gridSnapSettings = new SnapSettings
            {
                snapCursor = SpriteManager.GetSprite("Sprites/stop_ring"),
                snapCursorColor = Color.white,
                snapCursorScale = Vector3.one,

                hideCursor = true,
                snapToEnd = false,
                snapToLane = false,
                snapToRivers = false,

                snapCondition = pt =>
                {
                    if (_hoveredStop != null || _tempStopLocations.Contains(pt))
                    {
                        return false;
                    }

                    return !game.loadedMap.occupiedGridPoints.Contains(pt);
                },
                onSnapEnter = () =>
                {
                    switch (creationState)
                    {
                        case CreationState.FirstStop:
                            UIInstruction.Show("Left click to create line");
                            break;
                        case CreationState.IntermediateStops:
                            UIInstruction.Show("Left click to add stop");
                            DrawTemporaryPath(game.input.gameCursorPosition);
                            break;
                    }
                },

                onSnapExit = () =>
                {
                    UIInstruction.Hide();
                    plannedPathMesh?.SetActive(false);
                },
            };

            eventListenerIDs = new[]
            {
                game.input.RegisterEventListener(InputEvent.MouseEnter, obj =>
                {
                    if (obj.Kind == MapObjectKind.Stop || obj.Kind == MapObjectKind.TemporaryStop)
                    {
                        this.OnMouseEnter(obj);
                    }
                }, false),

                game.input.RegisterEventListener(InputEvent.MouseExit, obj =>
                {
                    if (obj.Kind == MapObjectKind.Stop || obj.Kind == MapObjectKind.TemporaryStop)
                    {
                        this.OnMouseExit(obj);
                    }
                }, false),

                game.input.RegisterEventListener(InputEvent.MouseDown, obj =>
                {
                    if (obj == null || (obj.Kind != MapObjectKind.Stop && obj.Kind != MapObjectKind.TemporaryStop))
                    {
                        this.OnMouseDown();
                    }
                    else
                    {
                        this.OnMouseDown(obj);
                    }
                }, false),
            };
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void StartLineCreation()
        {
            base.StartLineCreation();
            game.loadedMap.EnableGrid();
            game.snapController.EnableGridSnap(_gridSnapSettings, 2f);
        }

        public override void EndLineCreation()
        {
            base.EndLineCreation();
            game.loadedMap.DisableGrid();
            game.snapController.DisableGridSnap();
        }

        /// Auto-complete the line.
        public override void AutoComplete()
        {
            Debug.Assert(currentLine != null && currentLine.stops.Count > 0);
            this.ShowConfirmationPanel();
        }

        /// Finish the line.
        protected override void FinishLine()
        {
            for (var i = currentLine.stops.Count - 2; i >= 0; --i)
            {
                currentLine.stops.Add(currentLine.stops[i]);

                var startIdx = i == 0 ? 0 : currentLine.paths[i - 1];
                currentLine.completePath.AddRange(
                    currentLine.completePath.GetRange(startIdx, currentLine.paths[i] - startIdx)
                        .AsEnumerable().Reverse());

                currentLine.paths.Add(currentLine.completePath.Count);
            }

            base.FinishLine();
        }

        private void DrawTemporaryPath(Vector2 position)
        {
            if (temporaryPath == null)
            {
                temporaryPath = new List<Vector2>();
            }
            else
            {
                temporaryPath.Clear();
            }

            var startPos = previousStop.Centroid;
            temporaryPath.Add(startPos);
            temporaryPath.Add(position);
            base.DrawCurrentPath();
        }

        private void OnMouseEnter(IMapObject stop)
        {
            if (stop.Kind != MapObjectKind.Stop && stop.Kind != MapObjectKind.TemporaryStop)
            {
                return;
            }

            stop.transform.ScaleBy(1.3f);
            _hoveredStop = stop;

            switch (creationState)
            {
                case CreationState.FirstStop:
                    UIInstruction.Show("Click to create line");
                    break;
                case CreationState.IntermediateStops:
                    if (currentLine.stops.Contains(stop))
                    {
                        UIInstruction.Show("This stop is already on the line");
                        return;
                    }

                    if (stop is Stop s)
                    {
                        if (s.Type != _stopType)
                        {
                            UIInstruction.Show("This stop can only be used for X lines");
                            return;
                        }

                        this.DrawTemporaryPath(s.location);
                    }
                    else
                    {
                        this.DrawTemporaryPath(((TemporaryStop)stop).position);
                    }

                    break;
                default:
                    break;
            }
        }

        private void OnMouseExit(IMapObject stop)
        {
            if (stop.Kind != MapObjectKind.Stop && stop.Kind != MapObjectKind.TemporaryStop)
            {
                return;
            }

            stop.transform.ScaleBy(1f / 1.3f);
            _hoveredStop = null;

            ResetPath();
        }

        private void OnMouseDown(IMapObject stop)
        {
            if (stop.Kind != MapObjectKind.Stop && stop.Kind != MapObjectKind.TemporaryStop)
            {
                return;
            }

            switch (creationState)
            {
                case CreationState.FirstStop:
                    base.CreateFirstStop(transitType, stop);
                    break;
                case CreationState.IntermediateStops:
                    if (currentLine.stops.Contains(stop))
                    {
                        UIInstruction.Show("This stop is already on the line");
                        return;
                    }

                    if (stop is Stop s)
                    {
                        if (s.Type != _stopType)
                        {
                            UIInstruction.Show("This stop can only be used for X lines");
                            return;
                        }
                    }

                    base.AddStop(stop);
                    break;
            }
        }

        private void OnMouseDown()
        {
            if (!game.snapController.IsSnappedToGrid)
            {
                return;
            }

            switch (creationState)
            {
                case CreationState.FirstStop:
                    base.CreateFirstStop(transitType, "New Subway Stop", game.input.gameCursorPosition);
                    break;
                case CreationState.IntermediateStops:
                    base.AddStop("New Subway Stop", game.input.gameCursorPosition);
                    break;
            }

            game.snapController.Unsnap();
        }

        protected override decimal costPerKm => 100_000m;

        protected override decimal operatingCostPerKm => 500m;

        protected override decimal costPerStop => 50_000m;

        protected override decimal operatingCostPerStop => 1000m;
    }
}