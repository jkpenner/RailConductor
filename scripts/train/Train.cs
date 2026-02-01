using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Train : Node2D
{
    [Export]
    public TrainWheel? _wheel = null;


    public void SetTrack(Track track, TrackSegment segment)
    {
        _wheel?.SetTrack(track, segment);
    }

    public override void _Process(double delta)
    {
        var move = Input.GetAxis("move_backward", "move_forward");

        _wheel?.Move(move);
        
        // Sync Train movement based on it's wheels.
        var wheel = _wheel?.GetGlobalPosition() ?? GlobalPosition;

        GlobalPosition = wheel;
        _wheel?.SetGlobalPosition(wheel);
    }
}