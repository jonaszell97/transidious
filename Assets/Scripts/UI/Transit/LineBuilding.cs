﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    public abstract class LineBuilder
    {
        /// <summary>
        /// Describes the current state of line building, which works like a state machine.
        /// </summary>
        public enum CreationState
        {
            /// <summary>
            /// The default state when the line builder is inactive.
            /// </summary>
            Idle,
            
            /// <summary>
            /// The line building has just started, and no stop has been placed yet.
            /// </summary>
            FirstStop,

            /// <summary>
            /// There is at least one stop on the line, and additional ones can be placed.
            /// </summary>
            IntermediateStops,

            /// <summary>
            /// The line was closed and can be reviewed before finishing it.
            /// </summary>
            Review,
        }

        /// <summary>
        /// Describes the current state of line editing, which works like a state machine.
        /// </summary>
        public enum EditingState
        {
            /// <summary>
            /// The default state when the line builder is inactive.
            /// </summary>
            Idle,
        }

        /// Reference to the game controller.
        protected GameController game;

        /// Whether or not the game was already paused when we started line creation.
        private bool _wasPaused;

        /// The current line creation state.
        public CreationState creationState { get; protected set; }

        /// The transit type edited by this builder.
        protected TransitType transitType;

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
            this.creationState = CreationState.Idle;
            this.transitType = transitType;
            _temporaryStops = new List<TemporaryStop>();
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
        }

        /// Enable the event listeners for a particular state.
        public virtual void EnableListeners(CreationState state)
        {
            foreach (var id in eventListenerIDs)
            {
                game.input.EnableEventListener(id);
            }
        }

        /// Disable the event listeners for a particular state.
        public virtual void DisableListeners(CreationState state)
        {
            foreach (var id in eventListenerIDs)
            {
                game.input.DisableEventListener(id);
            }
        }

        public void StartLineCreation()
        {
            _wasPaused = game.EnterPause(true);
            game.mainUI.transitEditorPanel.SetActive(false);
            game.mainUI.ShowLineBuildingPanel();
            this.Transition(CreationState.FirstStop);
        }

        public void EndLineCreation()
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

            game.mainUI.transitEditorPanel.SetActive(true);
            game.mainUI.transitUI.activeBuilder = null;
            game.mainUI.ShowPanels();

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
                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("num_stops", "ui:line:num_stops", "", new UIInfoPanel.IconSettings
                {
                    icon = SpriteManager.GetSprite("Sprites/stop_ring"),
                });

                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("total_length", "ui:line:total_length", "", new UIInfoPanel.IconSettings
                {
                    icon = SpriteManager.GetSprite("Sprites/ui_length"),
                });

                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("construction_cost", "ui:construction_cost", "", new UIInfoPanel.IconSettings
                {
                    icon = SpriteManager.GetSprite("Sprites/bulldozer"),
                });

                game.mainUI.transitUI.confirmLineCreationInfo.AddItem("monthly_cost", "ui:monthly_cost", "", new UIInfoPanel.IconSettings
                {
                    icon = SpriteManager.GetSprite("Sprites/money"),
                });

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
            currentLine = new TemporaryLine
            {
                name = Translator.Get("tooltip:new_line", game.GetSystemName(type)),
                stops = new List<IMapObject>(),
                completePath = new List<Vector2>(),
                paths = new List<int>(),
                streetSegments = new List<List<TrafficSimulator.PathSegmentInfo>>(),
            };

            currentLine.stops.Add(firstStop);
            UpdateCosts();

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

        float GetLengthOfTemporaryPath()
        {
            var length = 0f;
            for (var i = 1; i < temporaryPath.Count; ++i)
            {
                length += (temporaryPath[i] - temporaryPath[i - 1]).magnitude;
            }

            return length / 1000f;
        }

        protected void UpdateLength()
        {
            var length = (decimal)GetLengthOfTemporaryPath();
            this.length += (float)length;

            totalConstructionCost += length * costPerKm;
            totalMonthlyCost += length * operatingCostPerKm;
            UpdateCosts();
        }

        protected TemporaryStop AddStop(string name, Vector2 pos)
        {
            Debug.Assert(temporaryPath != null, "invalid path!");

            var nextStop = CreateTempStop(name, pos);
            currentLine.stops.Add(nextStop);
            currentLine.completePath.AddRange(temporaryPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            UpdateLength();
            DrawExistingPath();

            return nextStop;
        }

        protected IMapObject AddStop(IMapObject nextStop)
        {
            Debug.Assert(temporaryPath != null, "invalid path!");

            currentLine.stops.Add(nextStop);
            currentLine.completePath.AddRange(temporaryPath);
            currentLine.paths.Add(currentLine.completePath.Count);

            UpdateLength();
            DrawExistingPath();

            return nextStop;
        }

        protected virtual void FinishLine()
        {
            ResetPath();

            game.financeController.Purchase(totalConstructionCost);
            game.financeController.AddExpense("ui:line:operating_cost", totalMonthlyCost);

            var line = game.loadedMap.CreateLine(transitType, currentLine.name,
                                                 Colors.GetDefaultSystemColor(transitType));
            
            currentLine.stops.Add(currentLine.stops.First());
            currentLine.completePath.AddRange(temporaryPath);
            currentLine.paths.Add(currentLine.completePath.Count);

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

                    tmpStop.Destroy();
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

            var newLine = line.Finish(new Schedule
            {
                dayHours = System.Tuple.Create(4, 22),
                nightHours = System.Tuple.Create(22, 1),
                operatingDays = Weekday.All,
                dayInterval = 20,
                nightInterval = 20,
            });

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

            _temporaryStops.Clear();
            this.EndLineCreation();
            game.transitEditor.CheckOverlappingRoutes(crossedStreets);

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
        /// <summary>
        /// Previous calculated paths for the temporary line.
        /// </summary>
        protected List<TrafficSimulator.PathSegmentInfo> temporarySegments;

        /// <summary>
        /// Protected c'tor.
        /// </summary>
        /// <param name="game"></param>
        protected StreetboundLineBuilder(GameController game, TransitType transitType) : base(game, transitType)
        {
            this.temporarySegments = new List<TrafficSimulator.PathSegmentInfo>();
        }

        public override void Initialize()
        {
            base.Initialize();

            this.eventListenerIDs = new int[]
            {
                game.input.RegisterEventListener(InputEvent.MouseEnter, (IMapObject obj) =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseEnter(street);
                    }
                    else
                    {
                        this.OnMouseEnter(obj);
                    }
                }),

                game.input.RegisterEventListener(InputEvent.MouseOver, (IMapObject obj) =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseOver(street);
                    }
                }),

                game.input.RegisterEventListener(InputEvent.MouseExit, (IMapObject obj) =>
                {
                    if (obj is StreetSegment || obj is Stop || obj is TemporaryStop)
                    {
                        this.OnMouseExit(obj);
                    }
                }),

                game.input.RegisterEventListener(InputEvent.MouseDown, (IMapObject obj) =>
                {
                    if (obj is StreetSegment street)
                    {
                        this.OnMouseDown(street);
                    }
                    else
                    {
                        this.OnMouseDown(obj);
                    }
                }),
            };

            this.keyboardEventIDs = new[]
            {
                game.input.RegisterKeyboardEventListener(KeyCode.Escape, _ =>
                {
                    
                }, false)
            };
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
            if (!(stop is Stop) && !(stop is TemporaryStop))
            {
                return;
            }

            switch (creationState)
            {
                case CreationState.FirstStop:
                    UIInstruction.Show("Click to create line");
                    break;
                case CreationState.IntermediateStops:
                    var s = stop as Stop;
                    if (s != null)
                    {
                        this.DrawTemporaryPath(s.location);
                    }
                    else
                    {
                        this.DrawTemporaryPath((stop as TemporaryStop).position);
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

        protected void OnMouseExit(IMapObject _)
        {
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
                    previousStop = this.CreateFirstStop(street);
                    this.Transition(CreationState.IntermediateStops);

                    break;
                case CreationState.IntermediateStops:
                    previousStop = this.AddStop(street);

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
                    previousStop = base.CreateFirstStop(transitType, stop);
                    this.Transition(CreationState.IntermediateStops);

                    break;
                case CreationState.IntermediateStops:
                    if (stop == currentLine.stops.First())
                    {
                        UpdateLength();
                        this.ShowConfirmationPanel();
                    }
                    else
                    {
                        previousStop = this.AddStop(stop);
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
            var nextStop = base.AddStop(street.street.name, game.input.GameCursorPosition);

            currentLine.streetSegments.Add(temporarySegments);
            this.temporarySegments = new List<TrafficSimulator.PathSegmentInfo>();

            return nextStop;
        }

        protected new virtual IMapObject AddStop(IMapObject nextStop)
        {
            nextStop = base.AddStop(nextStop);

            currentLine.streetSegments.Add(temporarySegments);
            this.temporarySegments = new List<TrafficSimulator.PathSegmentInfo>();

            return nextStop;
        }

        protected override void FinishLine()
        {
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
        /// <summary>
        /// Public c'tor.
        /// </summary>
        /// <param name="game"></param>
        public BusLineBuilder(GameController game) : base(game, TransitType.Bus)
        {

        }

        protected override decimal costPerKm
        {
            get
            {
                return 0m;
            }
        }

        protected override decimal operatingCostPerKm
        {
            get
            {
                return 25m;
            }
        }

        protected override decimal costPerStop
        {
            get
            {
                return 1000m;
            }
        }

        protected override decimal operatingCostPerStop
        {
            get
            {
                return 75m;
            }
        }

        /// <summary>
        /// Initialize the event listeners.
        /// </summary>
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

        /// <summary>
        /// Public c'tor.
        /// </summary>
        /// <param name="game"></param>
        public TramLineBuilder(GameController game) : base(game, TransitType.Tram)
        {
            this.addedTramTracks = new HashSet<StreetSegment>();
        }

        protected override decimal costPerKm
        {
            get
            {
                // Only if tram tracks need to be built.
                return 0m;
            }
        }

        protected override decimal operatingCostPerKm
        {
            get
            {
                return 35m;
            }
        }

        protected override decimal costPerStop
        {
            get
            {
                return 1500m;
            }
        }

        protected override decimal operatingCostPerStop
        {
            get
            {
                return 100m;
            }
        }

        protected static readonly decimal costPerKmTrack = 10000m;

        /// <summary>
        /// Initialize the event listeners.
        /// </summary>
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
            //temporaryPath = MeshBuilder.GetOffsetPath(temporaryPath, -3f);

            base.DrawCurrentPath();
        }
    }
}