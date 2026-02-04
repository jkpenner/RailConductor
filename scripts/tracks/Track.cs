using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Track : Node2D
{
    private TrackGraph? _graph;

    public override void _Ready()
    {
        RecalculateGraph();
    }

    public TrackGraph GetGraph()
    {
        _graph ??= BuildGraph();
        return _graph;
    }

    public TrackGraph BuildGraph()
    {
        return TrackGraphBuilder.Build(this);
    }
    
    public void RecalculateGraph()
    {
        _graph = BuildGraph();
    }
}