namespace RailConductor;

public partial class CrossoverVisual : TrackVisual
{
    public override void Sync(Track track, string id)
    {
        var node = track.Graph?.GetNode(id);
        if (node is null)
        {
            return;
        }

        Position = node.Position;
    }
}