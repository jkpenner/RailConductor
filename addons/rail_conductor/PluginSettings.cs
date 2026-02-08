using Godot;

namespace RailConductor.Plugin;

public static class PluginSettings
{
    public const int LinkWidth = 4;
    public static readonly Color LinkColor = new(0.2f, 0.2f, 0.9f);

    public const int NodeRadius = 6;
    public static readonly Color NodePrimaryColor = new(0.2f, 0.2f, 0.9f);
    public static readonly Color NodeNormalColor = new(0.65f, 0.65f, 0.7f);
    public static readonly Color NodeHoverColor = new(0.9f, 0.9f, 0.95f);

    public static readonly Color SelectedColor = new(0.9f, 0.9f, 0.2f);
    
    
    public static readonly Color SwitchPrimaryColor = new(0.4f, 0.9f, 0.2f);
    public static readonly Color SwitchSecondaryColor = new(0.9f, 0.4f, 0.2f);
}