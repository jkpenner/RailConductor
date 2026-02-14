using Godot;

namespace RailConductor.Plugin;

public static class PluginSettings
{
    // General ////////////////////////////////////////////////////////////////////////////////////
    public static readonly Color SelectedColor = new(0.9f, 0.9f, 0.2f);
    public const string FontPath = "res://addons/rail_conductor/fonts/default.tres";
    public const float MaxSelectDistance = 10f;

    // Nodes //////////////////////////////////////////////////////////////////////////////////////
    public const int NodeRadius = 6;
    public static readonly Color NodeNormalColor = new(0.2f, 0.2f, 0.7f);
    public static readonly Color NodeHoverColor = new(0.2f, 0.2f, 0.9f);
    public static readonly Color NodeDisabledColor = new(0.1f, 0.1f, 0.3f);
    
    public static readonly Color NodeFillNormalColor = new(0.65f, 0.65f, 0.7f);
    public static readonly Color NodeFillHoverColor = new(0.9f, 0.9f, 0.95f);
    public static readonly Color NodeFillDisabledColor = new(0.3f, 0.3f, 0.3f);
    
    // Links //////////////////////////////////////////////////////////////////////////////////////
    public const int LinkWidth = 4;
    public static readonly Color LinkNormalColor = new(0.2f, 0.2f, 0.7f);
    public static readonly Color LinkHoverColor =  new(0.2f, 0.2f, 0.9f);
    public static readonly Color LinkDisabledColor =  new(0.2f, 0.2f, 0.4f);

    // Switches ///////////////////////////////////////////////////////////////////////////////////
    public static readonly Color SwitchPrimaryColor = new(0.4f, 0.9f, 0.2f);
    public static readonly Color SwitchSecondaryColor = new(0.9f, 0.4f, 0.2f);
    
    // Signals ////////////////////////////////////////////////////////////////////////////////////
    public static readonly Color SignalNormalColor = new(0.6f, 0.1f, 0.05f);
    public static readonly Color SignalHoverColor = new(0.4f, 0.1f, 0.03f);
    public static readonly Color SignalDisabledColor = new(0.2f, 0.1f, 0.01f);
    public static readonly Color SignalSelectedColor = new(0.9f, 0.2f, 0.1f);
    
    // Platforms //////////////////////////////////////////////////////////////////////////////////
    public static readonly Vector2 PlatformVerticalSize = new Vector2(10, 60);
    public static readonly Vector2 PlatformHorizontalSize = new Vector2(60, 10);
    public static readonly Color PlatformNormalColor = new(0.9f, 0.9f, 0.9f);
    public static readonly Color PlatformHoverColor = new(0.9f, 0.9f, 0.9f);
    public static readonly Color PlatformDisabledColor = new(0.9f, 0.9f, 0.9f);
}