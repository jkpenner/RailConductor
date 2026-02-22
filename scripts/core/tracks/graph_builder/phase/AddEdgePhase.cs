using Godot;
using RailConductor;

public class AddEdgePhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddEdges;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var link in data.GetLinks())
        {
            var nodeA = graph.GetNode(link.NodeAId);
            var nodeB = graph.GetNode(link.NodeBId);

            if (nodeA is null || nodeB is null)
            {
                GD.PushWarning($"Failed to add link, failed to find one or more nodes.");
                continue;
            }

            graph.AddEdge(new TrackGraphEdge(
                link.Id,
                nodeA,
                nodeB,
                (nodeA.Position - nodeB.Position).Length()
            ));
        }
    }
}