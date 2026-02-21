using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class SignalData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public string LinkId { get; set; } = string.Empty;

    [Export]
    public string DirectionNodeId { get; set; } = string.Empty;

    [Export(PropertyHint.ArrayType, "RouteDefinition")]
    public Godot.Collections.Array<RouteDefinition> RouteDefinitions { get; set; } = new();
    
    [Export]
    public string InterlockingGroupId { get; set; } = "";
    
    public void AddRouteDefinition(RouteDefinition def)
    {
        if (def != null && !RouteDefinitions.Contains(def))
            RouteDefinitions.Add(def);
    }

    public void RemoveRouteDefinitionAt(int index)
    {
        if (index >= 0 && index < RouteDefinitions.Count)
            RouteDefinitions.RemoveAt(index);
    }

    public void ClearRouteDefinitions()
    {
        RouteDefinitions.Clear();
    }
}