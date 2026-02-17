namespace RailConductor;

public class TrackNodeBuildPhase : ProcessNodeBuildPhase<TrackNode>
{
    public override int GraphBuildPhase => TrackBuildPhase.Nodes;

    protected override void ProcessNode(Track track, TrackGraph graph, TrackNode node)
    {
        graph.CreateOrGetNode(node.GlobalPosition);
    }
}