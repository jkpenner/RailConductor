using Godot;

namespace RailConductor;

public partial class Main : Node
{
    [Export] public Track? track;
    [Export] public TrackSegment? segment;
    [Export] public Train? train;

    public override void _Ready()
    {
        if (track is null || segment is null || train is null)
        {
            return;
        }
        
        train.SetTrack(track, segment);
    }
}