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
    }

    public Train? SpawnTrain(int linkId, int nodeId)
    {
        if (track is null)
        {
            GD.PushError("Track is null");
            return null;
        }
        
        var graph = track.GetGraph();
        
        var link = graph.GetLink(linkId);
        var node = graph.GetNode(nodeId);

        if (link is null || node is null || !link.Contains(node))
        {
            GD.PushError($"Failed to spawn train, invalid position details.");
            return null;
        }

        train?.SetTrack(graph, link, node);
        return train;
    }
}