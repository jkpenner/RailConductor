using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


namespace RailConductor;

public class AddSwitchPhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddSwitches;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        var _edgesByLinkId = new Dictionary<string, TrackGraphEdge>();

        // Process all switches.
        foreach (var node in data.GetNodes().Where(n => n.NodeType == TrackNodeType.Switch))
        {
            var target = graph.GetNode(node.Id);
            if (target is null)
            {
                continue;
            }
            
            foreach (var edge in target.OutgoingEdges.ToList())
            {
                var maxSpace = GetMaxSpacerDistance(data, edge.Id);
                if (maxSpace <= Mathf.Epsilon || settings.SwitchSpacing <= Mathf.Epsilon)
                {
                    continue;
                }
                
                // Calculate the offset position for the new node.
                var otherNode = edge.GetOtherNode(target);
                var space = Mathf.Min(settings.SwitchSpacing, maxSpace);
                var direction = (otherNode.Position - target.Position).Normalized();
                var position = target.Position + direction * space;

                // Remove the working edge
                target.OutgoingEdges.Remove(edge);

                // Replace the target with new node
                var newNode = new TrackGraphNode(
                    Guid.NewGuid().ToString(),
                    position,
                    TrackNodeType.Basic
                );
                edge.ReplaceNode(target, newNode);

                // Add the new edge, outgoing update by Graph.AddEdge.
                var newEdge = new TrackGraphEdge(
                    Guid.NewGuid().ToString(),
                    target,
                    newNode,
                    space
                );
                graph.AddEdge(newEdge);
                
                // Store reference to new edge for switch edge assignments.
                _edgesByLinkId[edge.Id] = newEdge;
            }
            
            // Setup edges for switch
            
            // Clear stored references
            _edgesByLinkId.Clear();
        }
    }

    private float GetMaxSpacerDistance(TrackData data, string linkId)
    {
        var link = data.GetLink(linkId);
        if (link is null)
        {
            return 0f;
        }

        var nodeA = data.GetNode(link.NodeAId);
        var nodeB = data.GetNode(link.NodeBId);

        if (nodeA is null || nodeB is null)
        {
            return 0f;
        }

        // Only less then half of the link can be used for spacers.
        return (nodeA.Position - nodeB.Position).Length() * 0.4f;
    }
}