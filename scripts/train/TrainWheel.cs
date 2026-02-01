using Godot;

namespace RailConductor;

[GlobalClass]
public partial class TrainWheel : Node2D
{
    public Track? _track;
    public TrackSegment? _trackSegment;
    public TrackKey? _forwardKey;
    
    public void SetTrack(Track track, TrackSegment segment)
    {
        _track = track;
        _trackSegment = segment;
        _forwardKey = _trackSegment.GetEndAKey();
        
        GlobalPosition = _trackSegment.FindClosestPoint(GlobalPosition);
        
    }


    private bool IsValid()
    {
        return true;
    }

    private bool IsAtConnection()
    {
        return true;
    }

    public void Move(float distance)
    {
        if (_forwardKey is null)
        {
            return;
        }
        
        if (_trackSegment is null)
        {
            return;
        }
        
        var direction = _trackSegment.GetConnectionDirection(_forwardKey.Value);
        if (direction is null)
        {
            return;
        }
        
        GlobalPosition = _trackSegment.GetConnectionPosition(_forwardKey.Value)!.Value;
    }
    
    private void MoveToNextSegment()
    {
        if (!IsValid() || !IsAtConnection())
        {
            return;
        }

        // var key = TrackKey.From(GlobalPosition);
        // var segments = _track.GetSegments(key);
        //
        // if (_track.GetConnection)
        //
        // var nextSegment = Track.Get
    }
}