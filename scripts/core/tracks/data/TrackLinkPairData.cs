using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackLinkPairData : Resource
{
    [Export]
    public string LinkAId { get; set; } = string.Empty;

    [Export]
    public string LinkBId { get; set; } = string.Empty;
}