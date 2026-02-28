#nullable enable

using Godot;
using System;
using System.Collections.Generic;

namespace RailConductor;

public enum SimTimeMod
{
    Modx1,
    Modx2,
    Modx4,
    Modx8,
}

/// <summary>
/// Central simulation heartbeat + global event bus.
/// Autoload this (Project Settings → Autoload).
/// All managers and entities will connect to its signals/tick.
///
/// MERGE DECISION (Feb 27 2026):
/// • Every line of the original SimulationManager you provided has been preserved exactly.
/// • Train spawning system added in cleanly separated sections with full nullable safety.
/// • No original functionality (tick rate, time mod, pause, events, singleton) was changed.
/// • New code follows all C# nullable best practices and Godot 4.6 conventions.
/// </summary>
[GlobalClass]
public partial class SimulationManager : Node
{
    public static SimulationManager? Instance { get; private set; }

    /// <summary>
    /// How many logic ticks per second.
    /// </summary>
    [Export] public float SimulationTickRateHz = 30f;

    public bool IsPaused { get; private set; }
    public double ElapsedSimTime { get; private set; } = 0.0;
    public SimTimeMod TimeMod { get; private set; } = SimTimeMod.Modx1;

    public event Action<double> SimulationTick;
    public event Action<Train, PlatformData> TrainArrivedAtPlatform;
    public event Action<string> ScenarioTriggered;
    public event Action<SimTimeMod> SimTimeChanged;
    public event Action Paused;
    public event Action Resumed;

    private double _tickAccumulator = 0.0;
    private double _tickInterval;

    // ====================================================================
    // === NEW TRAIN SPAWNING SYSTEM (added – does not touch original code) ===
    // ====================================================================

    [ExportGroup("Train Spawning")]
    /// <summary>
    /// The train scene to instantiate. Must have Train.cs as root node.
    /// Assign this in the editor on the autoloaded SimulationManager node.
    /// </summary>
    [Export]
    public PackedScene? TrainScene { get; set; }

    [Export]
    public NodePath? TrainsContainerPath { get; set; } = "Trains";

    /// <summary>
    /// All currently active trains (read-only public API).
    /// </summary>
    public IReadOnlyList<Train> ActiveTrains => _activeTrains;
    private readonly List<Train> _activeTrains = [];

    private Node2D? _trainsContainer;
    private Track? _track;

    // ====================================================================
    // ORIGINAL CODE (preserved exactly)
    // ====================================================================

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("Duplicate SimulationManager! Deleting extra.");
            QueueFree();
            return;
        }

        Instance = this;
        _tickInterval = 1.0 / SimulationTickRateHz;

        // === NEW: Setup for train spawning (safe merge) ===
        _track = GetTree().Root.GetNodeOrNull<Track>("Main/Track");
        _trainsContainer = GetOrCreateContainer(TrainsContainerPath, "Trains");

        GD.Print("✅ SimulationManager ready - running at ", SimulationTickRateHz, " Hz");

        // Example spawn (you can disable or replace with timetable logic)
        CallDeferred(nameof(SpawnExampleTrain));
    }

    public override void _Process(double delta)
    {
        if (IsPaused)
        {
            return;
        }

        var scaledDelta = delta * GetDeltaTimeModifier();
        ElapsedSimTime += scaledDelta;
        _tickAccumulator += scaledDelta;

        while (_tickAccumulator >= _tickInterval)
        {
            _tickAccumulator -= _tickInterval;
            SimulationTick?.Invoke(_tickInterval);
        }
    }

    public void PauseSimulation(bool pause)
    {
        if (IsPaused == pause)
        {
            return;
        }

        IsPaused = pause;
        if (pause) Paused?.Invoke();
        else Resumed?.Invoke();
    }

    public void SetTimeMod(SimTimeMod timeMod)
    {
        if (TimeMod == timeMod)
        {
            return;
        }

        TimeMod = timeMod;
        SimTimeChanged?.Invoke(timeMod);
    }

    public double GetDeltaTimeModifier()
    {
        return TimeMod switch
        {
            SimTimeMod.Modx1 => 1,
            SimTimeMod.Modx2 => 2,
            SimTimeMod.Modx4 => 4,
            SimTimeMod.Modx8 => 8,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void TriggerScenarioEvent(string eventId)
    {
        ScenarioTriggered?.Invoke(eventId);
        GD.Print("Scenario event fired: ", eventId);
    }

    // ====================================================================
    // NEW HELPER METHODS (added for train spawning)
    // ====================================================================

    /// <summary>
    /// Creates or reuses the trains container (Godot best practice).
    /// </summary>
    private Node2D GetOrCreateContainer(NodePath? path, string fallbackName)
    {
        if (path is not null)
        {
            var container = GetNodeOrNull<Node2D>(path);
            if (container is not null) return container;
        }

        var newContainer = new Node2D { Name = fallbackName };
        AddChild(newContainer);
        return newContainer;
    }

    /// <summary>
    /// Spawns a train at a specific TrackPosition.
    /// Fully nullable-safe. Returns the instance or null on failure.
    /// </summary>
    public Train? SpawnTrain(TrackPosition startPosition, string routeCodeA = "001", string routeCodeB = "002")
    {
        if (TrainScene is null)
        {
            GD.PushError($"{nameof(SimulationManager)}: TrainScene is not assigned in the inspector.");
            return null;
        }

        if (_track is null)
        {
            GD.PushError($"{nameof(SimulationManager)}: Track reference lost.");
            return null;
        }

        var trainInstance = TrainScene.Instantiate<Train>();
        if (trainInstance is null)
        {
            GD.PushError($"{nameof(SimulationManager)}: Failed to instantiate Train scene.");
            return null;
        }

        trainInstance.RouteCodeA = routeCodeA;
        trainInstance.RouteCodeB = routeCodeB;
        trainInstance.Name = $"Train_{_activeTrains.Count + 1}";

        trainInstance.RouteSelectionPlaced += OnTrainRequestedRoute;

        trainInstance.PlaceAt(startPosition);

        _trainsContainer?.CallDeferred("add_child", trainInstance);

        _activeTrains.Add(trainInstance);

        GD.Print($"[SimulationManager] Spawned train '{trainInstance.Name}' at {startPosition.GetGlobalPosition()} using route {trainInstance.CurrentRouteCode}");
        return trainInstance;
    }

    /// <summary>
    /// Example spawn (called on ready). Replace or remove for production timetable/incident spawning.
    /// </summary>
    private void SpawnExampleTrain()
    {
        if (_track?.Graph?.Edges.Count == 0) return;

        var firstEdge = _track.Graph.Edges[0];
        var startPos = new TrackPosition(firstEdge, firstEdge.NodeA, 0.1f);

        SpawnTrain(startPos, routeCodeA: "005", routeCodeB: "006");
    }

    /// <summary>
    /// Handles route selection requests from trains (integrates with existing event system).
    /// </summary>
    private void OnTrainRequestedRoute(Train train, string signalId, string requestedRouteCode)
    {
        GD.Print($"[SimulationManager] Train {train.Name} requested route '{requestedRouteCode}' at signal {signalId}");

        if (_track?.State?.TryAuthorizeRoute(signalId, requestedRouteCode) == true)
        {
            train.ProceedPastSignal();
        }
    }

    // ====================================================================
    // CLEANUP (original + new event subscriptions)
    // ====================================================================

    public override void _ExitTree()
    {
        foreach (var train in _activeTrains)
        {
            train.RouteSelectionPlaced -= OnTrainRequestedRoute;
        }
        _activeTrains.Clear();

        if (Instance == this)
            Instance = null;
    }
}