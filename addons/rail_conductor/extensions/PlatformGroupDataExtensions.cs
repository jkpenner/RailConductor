using Godot;

namespace RailConductor.Plugin;

public static class PlatformGroupDataExtensions
{
    /// <summary>
    /// Returns the screen-space rectangle around the group name label.
    /// Used for precise selection/hover detection.
    /// </summary>
    public static Rect2 GetLabelRect(this PlatformGroupData data)
    {
        var font = ResourceLoader.Load<Font>(PluginSettings.FontPath);
        if (font is null)
            return new Rect2(data.Position, new Vector2(180, 40)); // fallback

        var labelText = string.IsNullOrEmpty(data.DisplayName) ? "Platform Group" : data.DisplayName;
        var textSize = font.GetStringSize(labelText, fontSize: 14);

        var boxSize = new Vector2(textSize.X + 32, textSize.Y + 18); // padding

        return new Rect2(
            data.Position - boxSize * 0.5f,
            boxSize
        );
    }
}