using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Train : Node2D
{
    private TrackGraph? _graph;
    private TrackLocation? _lead;
    private TrackLocation? _rear;

    [Export]
    public float TrainLength { get; set; } = 2f;

    [Export]
    public Node2D? LeadVisual { get; set; }

    [Export]
    public Node2D? RearVisual { get; set; }


    public void SetTrack(TrackGraph graph, TrackGraphLink link, TrackGraphNode face)
    {
        _graph = graph;
        _lead = new TrackLocation(link, face); ;
        _rear = graph.Move(_lead, -TrainLength);
    }

    public override void _Process(double delta)
    {
        var move = Input.GetAxis("move_backward", "move_forward");

        if (_graph is not null && _lead is not null)
        {
            _lead = _graph.Move(_lead, move);
            _rear = _graph.Move(_lead, -TrainLength);
        }

        // Sync Train movement based on its wheels.
        var wheel = _lead?.GetGlobalPosition() ?? GlobalPosition;

        GlobalPosition = wheel;

        if (LeadVisual is not null && _lead is not null)
        {
            LeadVisual.GlobalPosition = _lead.GetGlobalPosition();
        }

        if (RearVisual is not null && _rear is not null)
        {
            RearVisual.GlobalPosition = _rear.GetGlobalPosition();
        }
    }
}