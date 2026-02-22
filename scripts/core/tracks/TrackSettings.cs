using Godot;

namespace RailConductor;

[GlobalClass]
public partial class TrackSettings : Resource
{
    [Export] public bool GenerateCollision { get; set; } = true;
    [Export] public float SegmentWidth { get; set; } = 8f;
    [Export] public Color TrackColor { get; set; } = new Color(0.2f, 0.2f, 0.7f);
    [Export] public float SwitchSpacing = 10f;
    
    
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
    
    [ExportGroup("Scenes")]
    [Export] public PackedScene? TrackSegmentScene { get; set; }
    [Export] public PackedScene? BasicNodeScene { get; set; }
    [Export] public PackedScene? SwitchNodeScene { get; set; }
    [Export] public PackedScene? CrossoverNodeScene { get; set; }
    [Export] public PackedScene? SignalScene { get; set; }
    [Export] public PackedScene? PlatformScene { get; set; }
    [Export] public PackedScene? PlatformGroupScene { get; set; }
    
    [ExportGroup("Containers")]
    [Export] public NodePath SegmentsContainerPath { get; set; } = "Segments";
    [Export] public NodePath NodesContainerPath { get; set; } = "Nodes";
    [Export] public NodePath SignalsContainerPath { get; set; } = "Signals";
    [Export] public NodePath PlatformsContainerPath { get; set; } = "Platforms";
}