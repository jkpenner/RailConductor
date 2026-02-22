using Godot;

namespace RailConductor.GraphOld;

public class TrackSwitchBuildPhase : ProcessNodeBuildPhase<TrackSwitch>
{
    public override int GraphBuildPhase => TrackBuildPhase.Junctions;
    
    protected override void ProcessNode(Track track, TrackGraph graph, TrackSwitch trackSwitch)
    {
        var key =  TrackKey.From(trackSwitch.GlobalPosition);
        var node = graph.GetNode(key);
        if (node is null)
        {
            GD.PushWarning($"Track node {key} not registered");
            return;
        }

        var inSegment = graph.GetLink(
            trackSwitch.InSegment.EndA.GetTrackKey(),
            trackSwitch.InSegment.EndB.GetTrackKey()
        );

        var outSegmentA = graph.GetLink(
            trackSwitch.OutSegmentA.EndA.GetTrackKey(),
            trackSwitch.OutSegmentA.EndB.GetTrackKey()
        );

        var outSegmentB = graph.GetLink(
            trackSwitch.OutSegmentB.EndA.GetTrackKey(),
            trackSwitch.OutSegmentB.EndB.GetTrackKey()
        );
        
        if (inSegment is null || outSegmentA is null || outSegmentB is null)
        {
            GD.PushWarning("Failed to get associated links for switch.");
            return;
        }

        node.IsSwitch = true;

        node.ActiveIncomingLink = 0;
        node.IncomingLinks = [inSegment];

        node.ActiveOutgoingLink = 0;
        node.OutgoingLinks = [outSegmentA, outSegmentB];
        
        // Inject required graph information.
        // trackSwitch.Graph = graph;
        // trackSwitch.Node = node;
    }
}