using System.Linq;

namespace RailConductor;

public class AddNodePhase : TrackGraphBuildPhase
{
    public override int PhaseOrder => TrackGraphBuildPhaseOrder.AddNodes;

    public override void Process(TrackGraph graph, TrackData data, TrackSettings settings)
    {
        foreach (var node in data.GetNodes())
        {
            var graphNode = new TrackGraphNode(
                node.Id,
                node.Position,
                node.NodeType,
                node.IsIsolator
            );

            graphNode.PairedLinks = node.PairedLinks.ToArray();
            graph.AddNode(graphNode);
        }
    }
}