namespace RailConductor;

public partial class SignalVisual : TrackVisual
{
    public override void OnAttach(Track track, string id)
    {
        var state = track.State?.GetSignalState(id);
        if (state is null)
        {
            return;
        }

        state.Changed += OnStateChanged;
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

        var edge = track.Graph?.GetEdge(signal.LinkId);
        if (edge is null)
        {
            return;
        }

        var node = GetTargetNode(edge, signal.Id);
        if (node is null)
        {
            return;
        }
        
        // Todo: Get the signal's correct position.
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