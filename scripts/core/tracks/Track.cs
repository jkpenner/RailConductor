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
}