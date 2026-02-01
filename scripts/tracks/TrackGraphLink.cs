using System.IO;

namespace RailConductor;

public class TrackGraphLink
{
    public required int Id { get; init; }
    public required TrackGraphNode NodeA { get; init; }
    public required TrackGraphNode NodeB { get; init; }

    public TrackGraphNode GetOtherNode(TrackGraphNode node)
    {
        if (!Contains(node))
        {
            throw new InvalidDataException($"{nameof(TrackGraphNode)} is not part of the {nameof(TrackGraphLink)}");
        }

        return node == NodeA ? NodeA : NodeB;
    }

    public bool Contains(TrackGraphNode node) => NodeA == node || NodeB == node;
    public float GetLength() => (NodeA.GlobalPosition - NodeB.GlobalPosition).Length();
}