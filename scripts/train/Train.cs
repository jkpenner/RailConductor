using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Train : Node2D
{
    private TrackGraph? _graph;
    private TrackLocation? _lead;
    private TrackLocation? _rear;


    public void SetTrack(TrackGraph graph, TrackGraphLink link, TrackGraphNode face)
    {
        _graph = graph;
        _lead = new TrackLocation(link, face);
        _rear = new TrackLocation(link, face);
    }

    public override void _Process(double delta)
    {
        var move = Input.GetAxis("move_backward", "move_forward");

        if (_graph is not null)
        {
            if (_lead is not null)
            {
                _graph.Move(_lead, move);
            }

            if (_rear is not null)
            {
                _graph.Move(_rear, move);
            }
        }
        
        // Sync Train movement based on its wheels.
        var wheel = _lead?.GetGlobalPosition() ?? GlobalPosition;

        GlobalPosition = wheel;
    }
}