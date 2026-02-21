using Godot;
using System.Collections.Generic;

namespace RailConductor;

[GlobalClass, Tool]
public partial class RouteDefinition : Resource
{
    [Export] public string RouteCode { get; set; } = "";

    [Export] public bool IsRange { get; set; } = false;
    [Export] public string RangeStart { get; set; } = "";
    [Export] public string RangeEnd { get; set; } = "";

    [Export] public bool IsAnyAvailable { get; set; } = false;

    // For IsAnyAvailable = true → list of alternatives
    [Export(PropertyHint.ArrayType, nameof(Route))]
    public Godot.Collections.Array<Route> Routes { get; set; } = [];
    
    // Helpers for clean undo/redo
    public void AddRoute(Route route)
    {
        if (route != null && !Routes.Contains(route))
            Routes.Add(route);
    }

    public void RemoveRoute(int index)
    {
        if (index >= 0 && index < Routes.Count)
            Routes.RemoveAt(index);
    }

    public void ClearRoutes()
    {
        Routes.Clear();
    }

    public bool Matches(string requestedCode)
    {
        string norm = requestedCode.PadLeft(3, '0');

        if (IsAnyAvailable)
            return norm.StartsWith("ANY") || requestedCode == "ANY";  // or customize

        if (!IsRange)
            return norm == RouteCode.PadLeft(3, '0');

        string start = RangeStart.PadLeft(3, '0');
        string end   = RangeEnd.PadLeft(3, '0');

        return string.Compare(norm, start) >= 0 && string.Compare(norm, end) <= 0;
    }
}