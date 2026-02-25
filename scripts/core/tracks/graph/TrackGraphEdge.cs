using Godot;

namespace RailConductor;

public sealed class TrackGraphEdge
{
    public string Id { get; }
    
    public TrackGraphNode NodeA { get; set; }
    public TrackGraphNode NodeB { get; set; }
    public float Length { get; private set; }

    /// <summary>
    /// An alternate not unique id that references another edge. Used in
    /// cases of inserting an edge not contained within the original track
    /// data, such as when inserting extra edge for switches.
    /// </summary>
    public string AltId { get; set;  } = string.Empty;
    
    public PlatformData? Platform { get; set; }

    public TrackGraphEdge(string id, TrackGraphNode nodeA, TrackGraphNode nodeB, float length)
    {
        Id = id;
        NodeA = nodeA;
        NodeB = nodeB;
        Length = length;
    }

    public TrackGraphNode GetOtherNode(TrackGraphNode target)
    {
        return NodeA == target ? NodeB : NodeA;
    }

    public void ReplaceNode(TrackGraphNode target, TrackGraphNode replacement)
    {
        if (NodeA == target)
        {
            NodeA = replacement;
        }
        else if (NodeB == target)
        {
            NodeB = replacement;
        }
    }
}