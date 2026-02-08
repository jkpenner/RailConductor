using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackLinkData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public string NodeAId { get; set; } = string.Empty;

    [Export]
    public string NodeBId { get; set; } = string.Empty;


    public string GetOtherNode(string nodeId)
    {
        if (nodeId == NodeAId)
        {
            return NodeBId;
        }

        if (nodeId == NodeBId)
        {
            return NodeAId;
        }

        return string.Empty;
    }
}