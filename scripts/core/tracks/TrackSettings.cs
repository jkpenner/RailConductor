using Godot;

namespace RailConductor;

[GlobalClass]
public partial class TrackSettings : Resource
{
    
    
    
    [Export]
    public Color SegmentNormalColor { get; set; } = Colors.White;

    [Export]
    public Color SegmentOccupiedColor { get; set; } = Colors.Cyan;

    [ExportGroup("Switches")]
    [Export]
    public Color SwitchNormalRouteColor { get; set; } = Colors.Green;

    [Export]
    public Color SwitchDivergingRouteColor { get; set; } = Colors.Yellow;

    [ExportGroup("Signals")]
    [Export]
    public Color SignalNormalColor { get; set; } = Colors.Red;

    [Export]
    public Color SignalPendingColor { get; set; } = Colors.Yellow;

    [Export]
    public Color SignalProperColor { get; set; } = Colors.Green;
}