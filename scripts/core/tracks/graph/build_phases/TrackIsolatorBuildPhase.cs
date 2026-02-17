using Godot;

namespace RailConductor;

public class TrackIsolatorBuildPhase : ProcessNodeBuildPhase<TrackIsolator>
{
    public override int GraphBuildPhase => TrackBuildPhase.Restrictions;

    protected override void ProcessNode(Track track, TrackGraph graph, TrackIsolator isolator)
    {
        var key = TrackKey.From(isolator.GlobalPosition);
        var node = graph.GetNode(key);
        if (node is null)
        {
            GD.PushWarning($"Track node {key} not registered");
            return;
        }

        node.IsCircuitIsolator = true;
    }
}