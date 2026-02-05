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
        
        PlaceTrain(train, segment);
    }

    public bool PlaceTrain(Train train, TrackSegment segment)
    {
        var keyA = segment.EndA.GetTrackKey();
        var keyB = segment.EndB.GetTrackKey();

        var graph = track?.GetGraph();
        var link = graph?.GetLink(keyA, keyB);
        var node = graph?.GetNode(keyB);

        if (graph is null || link is null || node is null || !link.Contains(node))
        {
            GD.PushError($"Failed to spawn train, invalid position details.");
            return false;
        }

        train.SetTrack(graph, link, node);
        return true;
    }
}