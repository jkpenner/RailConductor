using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackConnection : Node2D
{
    public Action? LocalPositionChanged; 
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            SetNotifyLocalTransform(true);
        }
    }

    public override void _Notification(int what)
    {
        if (Engine.IsEditorHint())
        {
            if (what == NotificationLocalTransformChanged)
            {
                LocalPositionChanged?.Invoke();
            }
        }
    }
}