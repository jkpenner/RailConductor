using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class PlatformGroupData : Resource
{
    [Export] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Export] public string DisplayName { get; set; } = "New Station";

    // Center position (consistent with PlatformData)
    [Export] public Vector2 Position { get; set; }

    [Export(PropertyHint.ArrayType, "PlatformData")]
    public Godot.Collections.Array<string> PlatformIds { get; set; } = [];

    // Helpers
    public void AddPlatform(string platformId)
    {
        if (!string.IsNullOrEmpty(platformId) && !PlatformIds.Contains(platformId))
            PlatformIds.Add(platformId);
    }

    public void RemovePlatform(string platformId)
    {
        PlatformIds.Remove(platformId);
    }

    public bool ContainsPlatform(string platformId)
    {
        return PlatformIds.Contains(platformId);
    }
    
    public void ClearPlatforms()
    {
        PlatformIds.Clear();
    }

    public void SetPlatforms(Godot.Collections.Array<string> newIds)
    {
        PlatformIds.Clear();
        foreach (var id in newIds)
            if (!string.IsNullOrEmpty(id))
                PlatformIds.Add(id);
    }
}