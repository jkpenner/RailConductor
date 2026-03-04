using System;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class ScheduleTimePoint : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Export]
    public double Time { get; set; }

    [Export]
    public string PlatformId { get; set; } = string.Empty;
}