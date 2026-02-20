using Godot;

namespace RailConductor.Plugin;

public static class PlatformDataExtensions
{
    public static Vector2 GetSize(this PlatformData platformData)
    {
        return platformData.IsVertical
            ? PluginSettings.PlatformVerticalSize
            : PluginSettings.PlatformHorizontalSize;
    }

    public static Vector2 GetHalfSize(this PlatformData platformData) 
        => GetSize(platformData) * 0.5f;

    public static Rect2 GetRect(this PlatformData platformData)
    {
        return new Rect2(
            platformData.Position - GetHalfSize(platformData), 
            GetSize(platformData));
    }
}