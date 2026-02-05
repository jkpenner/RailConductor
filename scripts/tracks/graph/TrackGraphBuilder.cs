using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

public class TrackGraphBuilder
{
    private readonly List<TrackGraphBuildPhase> _phases = [];
    
    public void AddBuildPhase(TrackGraphBuildPhase phase)
    {
        _phases.Add(phase);
    }
    
    public TrackGraph Build(Track track)
    {
        if (_phases.Count == 0)
        {
            GD.PushWarning($"No {nameof(TrackGraphBuildPhase)} assigned.");
        }
        
        var graph = new TrackGraph();

        foreach (var phase in _phases.OrderBy(p => p.GraphBuildPhase))
        {
            try
            {
                phase.ExecuteBuildPhase(track, graph);
            }
            catch (Exception e)
            {
                GD.PushError($"Error in graph build handler {phase.GetType().Name} (phase {phase.GraphBuildPhase}): {e.Message}\n{e.StackTrace}");
            }
        }

        foreach (var node in graph.GetNodes())
        {
            node.RebuildConnections();
        }

        return graph;
    }
}