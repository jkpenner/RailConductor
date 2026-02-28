using Godot;

namespace RailConductor;

[Tool]
public partial class Track : Node2D
{
    [Export]
    public TrackData? Data { get; set; }

    [Export]
    public TrackSettings Settings { get; set; } = new();

    public TrackGraph? Graph { get; private set; }
    public TrackState? State { get; private set; }

    private TrackGraphBuilder? _graphBuilder;
    private TrackVisualBuilder? _visualBuilder;

    public override void _Ready()
    {
        if (Data is null)
        {
            if (!Engine.IsEditorHint())
            {
                GD.PushWarning($"No {nameof(TrackData)} assigned!");
            }

            return;
        }

        Build();
    }

    public void Build()
    {
        if (Data is null || Engine.IsEditorHint())
        {
            return;
        }

        // Update/Build the graph
        _graphBuilder ??= TrackGraphBuilder.Create();
        Graph = _graphBuilder.Build(Data, Settings);

        if (Graph is null)
        {
            GD.PushWarning("Failed to build the track's graph.");
            return;
        }

        // Currently create a new state for each build
        State = new TrackState(Graph, Data);

        // Update/Build the visuals
        _visualBuilder ??= new TrackVisualBuilder(this);
        _visualBuilder.Build();
    }

    /// <summary>
    /// Moves a position forward along the track, automatically crossing nodes when reached.
    /// 
    /// FIXED (Feb 27 2026):
    /// • out parameter from Move() is the UNUSED/leftover distance (as you correctly pointed out).
    /// • We now assign directly: remaining = leftover;
    /// • Correct while condition and safety limit.
    /// 
    /// This guarantees constant speed no matter how many nodes/spacers/switches are crossed.
    /// </summary>
    public TrackPosition Move(TrackPosition position, float distance)
    {
        if (distance <= 0f) return position;

        var current = position;
        var remaining = distance;
        var safety = 0;

        while (remaining > Mathf.Epsilon && safety < 30)
        {
            safety++;

            // Move returns the leftover (unused) distance into 'remaining'
            current = current.Move(remaining, out remaining);

            // If we reached a node and still have distance left, continue to next edge
            if (remaining > Mathf.Epsilon && current.IsApproxAtNode())
            {
                var node = current.Face;
                var nextEdge = node.GetNextEdge(current.Edge.Id, State!);
                if (nextEdge is null)
                {
                    break; // true dead-end
                }

                var nextFace = nextEdge.GetOtherNode(node);
                current = new TrackPosition(nextEdge, nextFace, 0f);
            }
        }

        return current;
    }
}