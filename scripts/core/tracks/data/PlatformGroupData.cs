using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class PlatformGroupData : Resource
{
    [Export] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Export] public string DisplayName { get; set; } = "Station";
    
    [Export(PropertyHint.ArrayType, "PlatformData")]
    public Godot.Collections.Array<string> PlatformIds { get; set; } = [];
}