using System;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Schedule : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Export]
    public int TrainId { get; set; }
    
    [Export]
    public Godot.Collections.Array<ScheduleTrip> Trips { get; set; } = [];
}