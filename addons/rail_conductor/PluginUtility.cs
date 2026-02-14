using Godot;

namespace RailConductor.Plugin;

public static class PluginUtility
{
    public static (int, int) GetLinkId(int nodeId1, int nodeId2)
    {
        return (Mathf.Min(nodeId1, nodeId2), Mathf.Max(nodeId1, nodeId2));
    }
    
    public static float GetZoom()
    {
        if (!Engine.IsEditorHint())
        {
            return 1f;
        }

        var viewport = EditorInterface.Singleton.GetEditorViewport2D();
        return viewport.GetFinalTransform().X.X;
    }
    
    public static Vector2 ScreenToWorldSnapped(Vector2 screenPosition)
    {
        return SnapPosition(ScreenToWorld(screenPosition));
    }

    public static Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var editor = EditorInterface.Singleton;
        var viewport = editor.GetEditorViewport2D();
        return viewport.GlobalCanvasTransform.AffineInverse() * screenPosition;
    }
    
    public static Vector2 WorldToScreen(Vector2 worldPosition)
    {
        var editor = EditorInterface.Singleton;
        var viewport = editor.GetEditorViewport2D();
        return viewport.GlobalCanvasTransform * worldPosition;
    }

    public static Vector2 SnapPosition(Vector2 localPos)
    {
        var settings = EditorInterface.Singleton.GetEditorSettings();
        var snapStep = settings.GetSetting("interface/editors/2d/snap_step").AsVector2();
        var snapOffset = settings.GetSetting("interface/editors/2d/snap_offset").AsVector2();

        if (snapStep == Vector2.Zero)
        {
            snapStep = new Vector2(16, 16); // Fallback default
        }

        // Apply offset then snap
        var adjusted = localPos - snapOffset;
        adjusted = adjusted.Snapped(snapStep);
        return adjusted + snapOffset;
    }
    
    /// <summary>
    /// Calculates the shortest distance from a point to a Rect2.
    /// Returns 0 if the point is inside or on the boundary of the rectangle.
    /// </summary>
    /// <param name="point">The point to measure distance from</param>
    /// <param name="rect">The rectangle to measure distance to</param>
    /// <returns>Distance in pixels/units (≥ 0)</returns>
    public static float DistanceToRect(Vector2 point, Rect2 rect)
    {
        // If point is inside the rectangle → distance = 0
        if (rect.HasPoint(point))
        {
            return 0f;
        }

        // Find the closest point on the rectangle's boundary
        var closestX = Mathf.Clamp(point.X, rect.Position.X, rect.End.X);
        var closestY = Mathf.Clamp(point.Y, rect.Position.Y, rect.End.Y);

        var closestPoint = new Vector2(closestX, closestY);
        
        // Distance from point to the closest edge/corner
        return point.DistanceTo(closestPoint);
    }
}