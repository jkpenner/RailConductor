using System;
using Godot;

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
/// </summary>
[GlobalClass]
public partial class SimulationManager : Node
{
    public static SimulationManager? Instance { get; private set; }

    /// <summary>
    /// How many logic ticks per second.
    /// </summary>
    [Export] public float SimulationTickRateHz = 30f; // How many logic ticks per second

    public bool IsPaused { get; private set; }
    public double ElapsedSimTime { get; private set; } = 0.0;
    public SimTimeMod TimeMod { get; private set; } = SimTimeMod.Modx1;

    public event Action<double> SimulationTick;
    public event Action<TrackCircuit> OccupancyChanged;
    public event Action<Train, PlatformData> TrainArrivedAtPlatform;
    public event Action<string> ScenarioTriggered;
    public event Action<SimTimeMod> SimTimeChanged;
    public event Action Paused;
    public event Action Resumed;


    private double _tickAccumulator = 0.0;
    private double _tickInterval;

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

        GD.Print("✅ SimulationManager ready - running at ", SimulationTickRateHz, " Hz");
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
        // EmitSignal(SignalName.ScenarioEventTriggered, eventId);
        GD.Print("Scenario event fired: ", eventId);
    }
}