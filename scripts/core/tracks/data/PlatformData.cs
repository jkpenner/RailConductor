using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class PlatformData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public Vector2 Position { get; set; }

    [Export]
    public bool IsVertical { get; set; }

    [Export(PropertyHint.ArrayType, nameof(TrackLinkData))]
    public Godot.Collections.Array<string> LinkedLinkIds { get; set; } = [];

    [Export]
    public string GroupId { get; set; } = string.Empty;

    
    
    public void AddLink(string linkId)
    {
        if (!string.IsNullOrEmpty(linkId) && !LinkedLinkIds.Contains(linkId))
        {
            LinkedLinkIds.Add(linkId);
        }
    }

    public void RemoveLink(string linkId)
    {
        LinkedLinkIds.Remove(linkId);
    }

    public void ClearLinks()
    {
        LinkedLinkIds.Clear();
    }

    public bool IsLinkedTo(string linkId)
    {
        return LinkedLinkIds.Contains(linkId);
    }
}