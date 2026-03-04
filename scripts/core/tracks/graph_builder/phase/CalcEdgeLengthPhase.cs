namespace RailConductor;

public class CalcEdgeLengthPhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddSwitches + 1;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var edge in graph.Edges)
        {
            edge.Length = (edge.NodeA.Position - edge.NodeB.Position).Length();
        }
    }
}