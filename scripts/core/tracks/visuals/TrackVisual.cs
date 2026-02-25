using Godot;

namespace RailConductor;

public abstract partial class TrackVisual : Node2D
{
    public virtual void OnAttach(Track track, string id) {}
    public virtual void OnDetach(Track track, string id) {}
    
    public abstract void Sync(Track track, string id);
}