using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Attach/Detach mode for platforms to platform groups.
/// 
/// Flow:
/// • Click a group → becomes current
/// • Click a platform → add/remove it from the group
/// • Visual feedback: green dashed line = will add, red = will remove
/// </summary>
public class AttachPlatformToGroupMode : PluginModeHandler
{
    private string _currentGroupId = string.Empty;
    private string _hoveredPlatformId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        RestrictTo(SelectionType.Platform | SelectionType.PlatformGroup, ctx);
        ResetCurrent();
        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ResetRestrictions(ctx);
        ResetCurrent();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                UpdateHover(ctx, motion.Position);
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                HandleLeftClick(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (!string.IsNullOrEmpty(_currentGroupId))
                {
                    ResetCurrent(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;
        }

        return false;
    }

    private void UpdateHover(PluginContext ctx, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(_currentGroupId))
        {
            _hoveredPlatformId = string.Empty;
            return;
        }

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        _hoveredPlatformId = ctx.TrackData.FindClosestPlatform(localPos);
    }

    private void HandleLeftClick(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        // Clicked a group → switch current group
        var clickedGroupId = ctx.TrackData.FindClosestPlatformGroup(localPos);
        if (!string.IsNullOrEmpty(clickedGroupId))
        {
            _currentGroupId = clickedGroupId;
            ctx.SelectOnly(clickedGroupId);
            _hoveredPlatformId = string.Empty;
            return;
        }

        // Clicked a platform while group is active
        if (string.IsNullOrEmpty(_currentGroupId) || string.IsNullOrEmpty(_hoveredPlatformId))
            return;

        var group = ctx.TrackData.GetPlatformGroup(_currentGroupId);
        var platform = ctx.TrackData.GetPlatform(_hoveredPlatformId);
        if (group == null || platform == null) return;

        if (platform.GroupId == _currentGroupId)
        {
            TrackEditorActions.RemovePlatformFromGroup(ctx, platform, group);
        }
        else
        {
            TrackEditorActions.AddPlatformToGroup(ctx, platform, group);
        }

        ctx.SelectOnly(_currentGroupId);
    }

    private void ResetCurrent(PluginContext ctx = null)
    {
        _currentGroupId = string.Empty;
        _hoveredPlatformId = string.Empty;
        ctx?.ClearSelection();
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_currentGroupId) || string.IsNullOrEmpty(_hoveredPlatformId))
            return;

        var group = ctx.TrackData.GetPlatformGroup(_currentGroupId);
        var platform = ctx.TrackData.GetPlatform(_hoveredPlatformId);
        if (group == null || platform == null) return;

        var groupCenter = group.Position; // you'll add Position to PlatformGroupData
        var platformCenter = platform.Position;

        var isInGroup = platform.GroupId == _currentGroupId;
        var color = isInGroup 
            ? new Color(1f, 0.3f, 0.3f, 0.8f)   // red = will remove
            : new Color(0.3f, 1f, 0.3f, 0.8f);  // green = will add

        overlay.DrawDashedLine(
            PluginUtility.WorldToScreen(ctx.Track.ToGlobal(groupCenter)),
            PluginUtility.WorldToScreen(ctx.Track.ToGlobal(platformCenter)),
            color, width: 4f, dash: 10f);
    }
}