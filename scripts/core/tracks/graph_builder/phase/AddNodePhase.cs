namespace RailConductor;

public class AddNodePhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddNodes;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var node in data.GetNodes())
        {
            graph.AddNode(new TrackGraphNode(
                node.Id,
                node.Position,
                node.NodeType,
                node.IsIsolator
            ));
        }
    }
}