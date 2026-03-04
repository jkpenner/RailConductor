using System;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class ScheduleTrip : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The duty id used to
    /// </summary>
    [Export]
    public string DutyId { get; set; } = string.Empty;
    
    [Export]
    public int RouteCode { get; set; }
    
    [Export]
    public int OverheadCode { get; set; }

    [Export]
    public Godot.Collections.Array<ScheduleTimePoint> TimePoints { get; set; } = [];
}