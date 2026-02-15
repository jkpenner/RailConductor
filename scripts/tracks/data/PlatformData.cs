using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class PlatformData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public string DisplayName { get; set; } = string.Empty;
    
    [Export]
    public Vector2 Position { get; set; }

    [Export]
    public bool IsVertical { get; set; }
}