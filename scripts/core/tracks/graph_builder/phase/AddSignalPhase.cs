namespace RailConductor;

public class AddSignalPhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddSignals;
    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var signal in data.GetSignals())
        {
            var edge = graph.GetEdge(signal.LinkId);
            var node = graph.GetNode(signal.DirectionNodeId);
        }
    }
}