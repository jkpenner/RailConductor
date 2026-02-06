using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class Track : Node2D
{
    [Export]
    public TrackData? Data { get; set; }
    
    
    private readonly TrackGraphBuilder _builder;
    private TrackGraph? _graph;

    public Track()
    {
        _builder = new TrackGraphBuilder();
        _builder.AddBuildPhase(new TrackNodeBuildPhase());
        _builder.AddBuildPhase(new TrackSegmentBuildPhase());
        _builder.AddBuildPhase(new TrackIsolatorBuildPhase());
        _builder.AddBuildPhase(new TrackSwitchBuildPhase());
        _builder.AddBuildPhase(new ValidateConnectionBuildPhase());
    }

    public override void _Ready()
    {
        RecalculateGraph();
    }

    public TrackGraph GetGraph()
    {
        _graph ??= BuildGraph();
        return _graph;
    }

    public TrackGraph BuildGraph() => _builder.Build(this);
    public void RecalculateGraph() => _graph = BuildGraph();
}