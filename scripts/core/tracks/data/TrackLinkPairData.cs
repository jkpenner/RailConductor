using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackLinkPairData : Resource
{
    [Export]
    public string LinkAId { get; set; } = string.Empty;

    [Export]
    public string LinkBId { get; set; } = string.Empty;

    public bool Contains(string id)
        => LinkAId == id || LinkBId == id;
    
    /// <summary>
    /// Returns the other link ID in the pair (the one that is NOT the given ID).
    /// Used by GetNextEdge() on Basic nodes and spacers.
    /// </summary>
    public string GetOther(string linkId)
    {
        if (LinkAId == linkId) return LinkBId;
        if (LinkBId == linkId) return LinkAId;
        return string.Empty; // not in this pair
    }
}