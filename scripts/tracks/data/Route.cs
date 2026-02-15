using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class Route : Resource
{
    /// <summary>
    /// The target link this route option leads into / terminates at.
    /// Occupancy will be checked on this link.
    /// </summary>
    [Export]
    public string TargetLinkId { get; set; } = "";

    /// <summary>
    /// Higher priority = tried first when multiple routes are available
    /// </summary>
    [Export]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Required switch states (nodeId → alignment) to set for this route
    /// </summary>
    [Export]
    public Godot.Collections.Dictionary<string, SwitchAlignment> SwitchAlignments { get; set; } = [];
}