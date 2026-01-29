using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackIsolator : Marker2D, ITrackObject
{
    public IEnumerable<TrackKey> GetConnections()
    {
        return [TrackKey.From(GlobalPosition)];
    }

    public override void _Draw()
    {
        if (Engine.IsEditorHint())
        {
            DrawCircle(Vector2.Zero, 5f, Colors.Red);
        }
    }
}