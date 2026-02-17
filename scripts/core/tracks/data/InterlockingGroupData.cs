using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class InterlockingGroupData : Resource
{
    [Export] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export] public Godot.Collections.Array<string> SwitchNodeIds { get; set; } = new();     // TrackNodeData.Id where NodeType == Switch

    [Export] public Godot.Collections.Array<string> SignalIds { get; set; } = new();        // TrackSignalData.Id

    // Runtime only – can be moved to a manager later
    public Godot.Collections.Dictionary<string, SwitchAlignment> CurrentSwitchStates { get; set; } = new();
}