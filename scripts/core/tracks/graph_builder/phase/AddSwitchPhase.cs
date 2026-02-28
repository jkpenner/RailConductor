#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

/// <summary>
/// Inserts spacer nodes on switch branches for clean visuals.
/// 
/// FIXED (Feb 27 2026):
/// • After ReplaceNode we now explicitly add the original long edge to spacer.OutgoingEdges.
/// • This ensures GetNextEdge() on the spacer can find the continuation.
/// • PairedLinks are rebuilt correctly on both switch and spacers.
/// </summary>
public class AddSwitchPhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddSwitches;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var dataNode in data.GetNodes().Where(n => n.NodeType == TrackNodeType.Switch))
        {
            var switchNode = graph.GetNode(dataNode.Id);
            if (switchNode is null) continue;

            var originalPairs = dataNode.PairedLinks.ToArray();
            var branchToNewShortEdge = new Dictionary<string, TrackGraphEdge>();

            foreach (var longEdge in switchNode.OutgoingEdges.ToList())
            {
                var maxSpace = GetMaxSpacerDistance(data, longEdge.Id);
                if (maxSpace <= Mathf.Epsilon || settings.SwitchSpacing <= Mathf.Epsilon)
                    continue;

                var otherNode = longEdge.GetOtherNode(switchNode);
                var space = Mathf.Min(settings.SwitchSpacing, maxSpace);
                var spacerPos = switchNode.Position + (otherNode.Position - switchNode.Position).Normalized() * space;

                switchNode.OutgoingEdges.Remove(longEdge);

                var spacer = new TrackGraphNode(
                    Guid.NewGuid().ToString(),
                    spacerPos,
                    TrackNodeType.Basic
                );
                spacer.AltId = longEdge.Id;

                // CRITICAL FIX: Update the long edge to start at spacer instead of switch
                longEdge.ReplaceNode(switchNode, spacer);

                // ADD THE LONG EDGE TO THE SPACER'S OUTGOING LIST (this was missing!)
                spacer.OutgoingEdges.Add(longEdge);

                // Create short edge: switch → spacer
                var shortEdge = new TrackGraphEdge(
                    Guid.NewGuid().ToString(),
                    switchNode,
                    spacer,
                    space
                );
                shortEdge.AltId = longEdge.Id;

                graph.AddEdge(shortEdge);

                if (originalPairs.Any(p => p.Contains(longEdge.Id)))
                    branchToNewShortEdge[longEdge.Id] = shortEdge;
            }

            // Rebuild PairedLinks on switch (now points to short edges)
            var newPairs = new List<TrackLinkPairData>(originalPairs.Length);
            foreach (var origPair in originalPairs)
            {
                var linkA = origPair.LinkAId;
                var linkB = origPair.LinkBId;

                if (branchToNewShortEdge.TryGetValue(linkA, out var shortA)) linkA = shortA.Id;
                if (branchToNewShortEdge.TryGetValue(linkB, out var shortB)) linkB = shortB.Id;

                newPairs.Add(new TrackLinkPairData { LinkAId = linkA, LinkBId = linkB });
            }
            switchNode.PairedLinks = newPairs.ToArray();

            // Set PairedLinks on each spacer (single pair: short ↔ long)
            foreach (var shortEdge in branchToNewShortEdge.Values)
            {
                var spacer = shortEdge.NodeB;
                if (spacer is null) continue;

                spacer.PairedLinks = new[]
                {
                    new TrackLinkPairData
                    {
                        LinkAId = shortEdge.Id,
                        LinkBId = shortEdge.AltId
                    }
                };
            }
        }
    }

    private float GetMaxSpacerDistance(TrackData data, string linkId)
    {
        var link = data.GetLink(linkId);
        if (link is null) return 0f;

        var nodeA = data.GetNode(link.NodeAId);
        var nodeB = data.GetNode(link.NodeBId);
        if (nodeA is null || nodeB is null) return 0f;

        return (nodeA.Position - nodeB.Position).Length() * 0.4f;
    }
}