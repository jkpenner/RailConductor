#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

/// <summary>
/// Represents a node in the runtime track graph.
/// PairedLinks define the valid connections (Basic = 1 pair, Switch/Crossover = 2 pairs).
/// </summary>
public class TrackGraphNode
{
    public string Id { get; set; } = string.Empty;
    public TrackNodeType NodeType { get; set; }
    public Vector2 Position { get; set; }
    public bool IsIsolator { get; set; }

    public List<TrackGraphEdge> OutgoingEdges { get; } = [];
    public TrackLinkPairData[] PairedLinks { get; set; } = [];

    public string AltId { get; set; } = string.Empty;

    public TrackGraphNode(string id, Vector2 position, TrackNodeType type, bool isIsolator = false)
    {
        Id = id;
        Position = position;
        NodeType = type;
        IsIsolator = isIsolator;
    }

    /// <summary>
    /// Returns the correct next edge after arriving via incomingEdgeId (used for lead movement).
    /// </summary>
    public TrackGraphEdge? GetNextEdge(string incomingEdgeId, TrackState state)
    {
        if (NodeType == TrackNodeType.Basic)
        {
            if (PairedLinks.Length != 1)
            {
                return null;
            }

            var pair = PairedLinks[0];
            var otherEdgeId = pair.GetOther(incomingEdgeId);
            if (string.IsNullOrEmpty(otherEdgeId))
            {
                GD.PushWarning("Empty Edge Id");
                return null;
            }

            var otherEdge = OutgoingEdges.FirstOrDefault(e => e.Id == otherEdgeId);
            return state.GetSegmentState(otherEdgeId)?.IsVisible ?? false ? otherEdge : null;
        }

        if (NodeType == TrackNodeType.Crossover)
        {
            if (PairedLinks.Length != 2)
            {
                GD.PushError(
                    $"Crossover Node had unexpected number of pairs (Expected 2, Found {PairedLinks.Length}).");
                return null;
            }

            foreach (var pair in PairedLinks)
            {
                if (!pair.Contains(incomingEdgeId))
                {
                    continue;
                }

                var otherEdgeId = pair.GetOther(incomingEdgeId);
                if (string.IsNullOrEmpty(otherEdgeId))
                {
                    GD.PushWarning("Empty Edge Id");
                    return null;
                }

                var otherEdge = OutgoingEdges.FirstOrDefault(e => e.Id == otherEdgeId);
                return state.GetSegmentState(otherEdgeId)?.IsVisible ?? false ? otherEdge : null;
            }

            return null;
        }

        if (NodeType == TrackNodeType.Switch)
        {
            if (PairedLinks.Length != 2)
            {
                GD.PushError(
                    $"Crossover Node had unexpected number of pairs (Expected 2, Found {PairedLinks.Length}).");
                return null;
            }

            var switchState = state.GetSwitchState(Id);
            if (switchState is null)
            {
                return null;
            }

            var pairIndex = (int)switchState.Alignment;
            if (pairIndex < 0 || pairIndex >= PairedLinks.Length)
            {
                return null;
            }

            var pair = PairedLinks[pairIndex];
            var otherEdgeId = pair.GetOther(incomingEdgeId);
            if (string.IsNullOrEmpty(otherEdgeId))
            {
                GD.PushWarning("Empty Edge Id");
                return null;
            }

            var otherEdge = OutgoingEdges.FirstOrDefault(e => e.Id == otherEdgeId);
            return state.GetSegmentState(otherEdgeId)?.IsVisible ?? false ? otherEdge : null;
        }

        GD.PushWarning("Invalid Track Node Type");
        return null;
    }

    /// <summary>
    /// Returns the previous edge when moving backward through this node.
    /// Symmetric to GetNextEdge — uses PairedLinks for Basic/spacer nodes.
    /// </summary>
    public TrackGraphEdge? GetPreviousEdge(string outgoingEdgeId, TrackState state)
    {
        if (OutgoingEdges.Count == 0) return null;

        if (NodeType == TrackNodeType.Basic)
        {
            if (PairedLinks.Length == 0) return null;

            var pair = PairedLinks[0];
            var otherEdgeId = pair.GetOther(outgoingEdgeId);
            if (string.IsNullOrEmpty(otherEdgeId)) return null;

            var otherEdge = OutgoingEdges.FirstOrDefault(e =>
                e.Id == otherEdgeId || e.AltId == otherEdgeId);

            return state.GetSegmentState(otherEdgeId)?.IsVisible ?? false
                ? otherEdge
                : null;
        }

        // For switches and crossovers we reuse GetNextEdge (pairing is bidirectional)
        return GetNextEdge(outgoingEdgeId, state);
    }
}