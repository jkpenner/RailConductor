using System.Linq;
using Godot;

namespace RailConductor;

public static class TrackGraphBuilder
{
    public static TrackGraph Build(Track track)
    {
        var graph = new TrackGraph();

        // Register all track nodes.
        foreach (var node in track.FindChildren("*", nameof(TrackNode)))
        {
            if (node is not TrackNode trackNode)
            {
                continue;
            }

            var graphNode = graph.CreateOrGetNode(trackNode.GlobalPosition);

            // Setup TrackGraphNode here...
        }

        // Register all track links
        foreach (var segment in track.FindChildren("*", nameof(TrackSegment)))
        {
            if (segment is not TrackSegment trackSegment)
            {
                continue;
            }

            var keyA = trackSegment.EndA.GetTrackKey();
            var nodeA = graph.GetNode(keyA);
            if (nodeA is null)
            {
                GD.PushWarning($"Track node {keyA} not registered");
                continue;
            }

            var keyB = trackSegment.EndA.GetTrackKey();
            var nodeB = graph.GetNode(keyB);
            if (nodeB is null)
            {
                GD.PushWarning($"Track node {keyB} not registered");
                continue;
            }

            var link = graph.AddLink(nodeA, nodeB);

            // Setup TrackGraphLink here...
        }

        // Register all track nodes.
        foreach (var child in track.FindChildren("*", nameof(TrackIsolator)))
        {
            if (child is not TrackIsolator isolator)
            {
                continue;
            }

            var key = TrackKey.From(isolator.GlobalPosition);
            var node = graph.GetNode(key);
            if (node is null)
            {
                GD.PushWarning($"Track node {key} not registered");
                continue;
            }

            node.IsCircuitIsolator = true;
        }

        foreach (var child in track.FindChildren("*", nameof(TrackSwitch)))
        {
            if (child is not TrackSwitch trackSwitch)
            {
                continue;
            }

            var key = trackSwitch.Node.GetTrackKey();
            var node = graph.GetNode(key);
            if (node is null)
            {
                GD.PushWarning($"Track node {key} not registered");
                continue;
            }

            var inSegment = GetLink(graph, trackSwitch.InSegment);
            var outSegmentA = GetLink(graph, trackSwitch.OutSegmentA);
            var outSegmentB = GetLink(graph, trackSwitch.OutSegmentB);
            if (inSegment is null || outSegmentA is null || outSegmentB is null)
            {
                GD.PushWarning("Failed to get associated links for switch.");
                continue;
            }

            node.IsSwitch = true;

            node.ActiveIncomingLink = 0;
            node.IncomingLinks = [inSegment];

            node.ActiveOutgoingLink = 0;
            node.OutgoingLinks = [outSegmentA, outSegmentB];
        }

        foreach (var node in graph.GetNodes())
        {
            if (node.IsSwitch)
            {
                continue;
            }

            var links = graph.GetLinks(node.Id).ToList();
            if (links.Count == 0)
            {
                continue;
            }

            // First link assigned to incoming
            if (links.Count > 0)
            {
                node.ActiveIncomingLink = 0;
                node.IncomingLinks = [links[0]];
            }

            // Second link assigned to outgoing
            if (links.Count > 1)
            {
                node.ActiveOutgoingLink = 0;
                node.OutgoingLinks = [links[1]];
            }

            if (links.Count > 2)
            {
                GD.PushWarning("Node has more then 2 links as non-switch. Ignoring extra links.");
            }
        }

        return graph;
    }

    private static TrackGraphLink? GetLink(TrackGraph graph, TrackSegment segment)
    {
        var segKeyA = segment.EndA.GetTrackKey();
        var segNodeA = graph.GetNode(segKeyA);
        if (segNodeA is null)
        {
            return null;
        }

        var segKeyB = segment.EndB.GetTrackKey();
        var segNodeB = graph.GetNode(segKeyB);
        if (segNodeB is null)
        {
            return null;
        }

        return graph.GetLink(segNodeA.Id, segNodeB.Id);
    }
}