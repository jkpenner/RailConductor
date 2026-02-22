using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class RouteDefinition : Resource
{
    [Export] public int MinRouteCode { get; set; } = 1;
    [Export] public int MaxRouteCode { get; set; } = 1;

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

    /// <summary>
    /// Returns true if the requested code falls inside this definition (inclusive).
    /// Single code when Min == Max; range when Min != Max.
    /// </summary>
    public bool Matches(string requestedCode)
    {
        if (string.IsNullOrEmpty(requestedCode)) return false;

        if (IsAnyAvailable)
            return requestedCode.ToUpper().StartsWith("ANY") || requestedCode.ToUpper() == "ANY";

        if (!int.TryParse(requestedCode, out int code))
            return false;

        return code >= MinRouteCode && code <= MaxRouteCode;
    }

    /// <summary>
    /// Nice display string for UI lists (e.g. "005" or "012-018")
    /// </summary>
    public string GetDisplayCode()
    {
        if (MinRouteCode == MaxRouteCode)
            return MinRouteCode.ToString("D3");

        return $"{MinRouteCode:D3}-{MaxRouteCode:D3}";
    }
}