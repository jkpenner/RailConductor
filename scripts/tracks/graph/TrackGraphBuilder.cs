using System;
using System.Linq;
using Godot;

namespace RailConductor;

public interface ITrackGraphBuildHandler
{
    int GraphBuildPhase { get; }
    void OnGraphBuildPhase(TrackGraph graph);
}

public static class BuildPhase
{
    /// <summary>
    /// Create Nodes
    /// </summary>
    public const int Nodes = 0x10;
    /// <summary>
    /// Create Links
    /// </summary>
    public const int Links = 0x20;
    /// <summary>
    /// Setup Switches / Junctions
    /// </summary>
    public const int Junctions = 0x30;
    /// <summary>
    /// Add Signals / Isolators / Speed Restrictions
    /// </summary>
    public const int Restrictions = 0x40;
    /// <summary>
    /// Add Track Circuits
    /// </summary>
    public const int Circuits = 0x50;
    /// <summary>
    /// Validation and Post processing of tracks
    /// </summary>
    public const int Validation = 0x60;
}

public static class TrackGraphBuilder
{
    public static TrackGraph Build(Track track)
    {
        var graph = new TrackGraph();
        var handlers = track.FindChildren("*", nameof(Node), true, false)
            .OfType<ITrackGraphBuildHandler>()
            .OrderBy(h => h.GraphBuildPhase)
            .ToList();

        if (handlers.Count == 0)
        {
            GD.PushWarning($"No {nameof(ITrackGraphBuildHandler)} implementations found under Track node.");
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler.OnGraphBuildPhase(graph);
            }
            catch (Exception e)
            {
                GD.PushError($"Error in graph build handler {handler.GetType().Name} (phase {handler.GraphBuildPhase}): {e.Message}\n{e.StackTrace}");
            }
        }

        return graph;
    }
}