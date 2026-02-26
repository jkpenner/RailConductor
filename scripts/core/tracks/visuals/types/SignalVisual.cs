using Godot;

namespace RailConductor;

public partial class SignalVisual : TrackVisual
{
    private Node2D _visual = null!;
    private Track? _track;

    public override void _Ready()
    {
        _visual = GetNode<Node2D>("Visual");
    }

    public override void OnAttach(Track track, string id)
    {
        var state = track.State?.GetSignalState(id);
        if (state is null)
        {
            return;
        }
        
        _track = track;

        state.Changed += OnStateChanged;
        OnStateChanged(state);
    }
    
    public override void OnDetach(Track track, string id)
    {
        var state = track.State?.GetSignalState(id);
        if (state is null)
        {
            return;
        }

        state.Changed -= OnStateChanged;
    }

    private void OnStateChanged(SignalState state)
    {
        
    }
    
    public override void Sync(Track track, string id)
    {
        var signal = track.Data?.GetSignal(id);
        if (signal is null)
        {
            return;
        }

        var orientation = track.Data?.GetSignalPosition(signal, _track?.Settings.SignalOffset ?? 12);
        if (orientation is null)
        {
            return;
        }
        
        var (position, angle) = orientation.Value;
        Position = position;
        _visual.Rotation = angle;
    }
    
    private TrackGraphNode? GetTargetNode(TrackGraphEdge edge, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        if (edge.NodeA.Id == nodeId || edge.NodeA.AltId == nodeId)
        {
            return edge.NodeA;
        }
        
        if (edge.NodeB.Id == nodeId || edge.NodeB.AltId == nodeId)
        {
            return edge.NodeB;
        }

        return null;
    }
}