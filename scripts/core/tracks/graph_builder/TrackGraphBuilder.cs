using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

public sealed class TrackGraphBuilder
{
    private readonly List<TrackGraphBuildPhase> _phases = [];

    public static TrackGraphBuilder Create()
    {
        var builder = new TrackGraphBuilder();
        builder.AddPhase(new AddNodePhase());
        builder.AddPhase(new AddEdgePhase());
        return builder;
    }

    public void AddPhase(TrackGraphBuildPhase phase) => _phases.Add(phase);

    public TrackGraph? Build(TrackData data, TrackSettings settings)
    {
        if (_phases.Count == 0)
        {
            GD.PushError($"Failed to build track, not build phases assigned.");
            return null;
        }

        var graph = new TrackGraph();

        foreach (var phase in _phases.OrderBy(p => p.PhaseOrder))
        {
            try
            {
                phase.Process(graph, data, settings);
            }
            catch (Exception e)
            {
                GD.PushError($"Error occured during build phase ({phase.GetType().Name}): {e.Message}");
                return null;
            }
        }

        return graph;
    }
}