using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackSignalData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public string LinkId { get; set; } = string.Empty;

    [Export]
    public string DirectionNodeId { get; set; } = string.Empty;
}