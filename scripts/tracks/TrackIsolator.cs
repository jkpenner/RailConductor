using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackIsolator : Marker2D, ITrackGraphBuildHandler
{
    public int GraphBuildPhase => BuildPhase.Restrictions;

    public override void _Draw()
    {
        if (Engine.IsEditorHint())
        {
            DrawCircle(Vector2.Zero, 5f, Colors.Red);
        }
    }

    public void OnGraphBuildPhase(TrackGraph graph)
    {
        var key = TrackKey.From(GlobalPosition);
        var node = graph.GetNode(key);
        if (node is null)
        {
            GD.PushWarning($"Track node {key} not registered");
            return;
        }

        node.IsCircuitIsolator = true;
    }
}