using Godot;

namespace RailConductor;

public class TrackSegmentBuildPhase : ProcessNodeBuildPhase<TrackSegment>
{
    public override int GraphBuildPhase => TrackBuildPhase.Links;

    protected override void ProcessNode(Track track, TrackGraph graph, TrackSegment segment)
    {
        var keyA = segment.EndA.GetTrackKey();
        var nodeA = graph.GetNode(keyA);
        if (nodeA is null)
        {
            GD.PushWarning($"Track node {keyA} not registered");
            return;
        }

        var keyB = segment.EndB.GetTrackKey();
        var nodeB = graph.GetNode(keyB);
        if (nodeB is null)
        {
            GD.PushWarning($"Track node {keyB} not registered");
            return;
        }

        graph.AddLink(nodeA, nodeB);
    }
}