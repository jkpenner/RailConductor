using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Attach mode for connecting platforms to one or more track links.
/// 
/// Flow:
/// • Click a platform → it becomes the "current" platform (selected)
/// • Hover any link → dashed preview line from platform center to link midpoint
/// • Click the link → attach it to the current platform (multiple links allowed)
/// • Click another platform → switch the current platform
/// • Right-click or Escape → clear current platform selection
/// 
/// Fully supports the new multi-link PlatformData.LinkedLinkIds.
/// </summary>
public class AttachPlatformToLinkMode : PluginModeHandler
{
    private string _currentPlatformId = string.Empty;
    private string _hoveredLinkId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        RestrictTo(SelectionType.Platform | SelectionType.Link, ctx);
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
                if (!string.IsNullOrEmpty(_currentPlatformId))
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
        if (string.IsNullOrEmpty(_currentPlatformId))
        {
            _hoveredLinkId = string.Empty;
            return;
        }

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        _hoveredLinkId = ctx.TrackData.FindClosestLink(localPos);
    }

    private void HandleLeftClick(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        // Clicked a platform → make it the new current
        var clickedPlatformId = ctx.TrackData.FindClosestPlatform(localPos);
        if (!string.IsNullOrEmpty(clickedPlatformId))
        {
            _currentPlatformId = clickedPlatformId;
            ctx.SelectOnly(clickedPlatformId);
            _hoveredLinkId = string.Empty;
            return;
        }

        // Clicked a link while a platform is active → attach it
        if (string.IsNullOrEmpty(_currentPlatformId) || string.IsNullOrEmpty(_hoveredLinkId))
            return;

        var platform = ctx.TrackData.GetPlatform(_currentPlatformId);
        if (platform == null) return;

        if (platform.IsLinkedTo(_hoveredLinkId))
            return; // already attached

        // Attach via undoable action
        AttachLinkToPlatform(ctx, platform, _hoveredLinkId);

        // Stay on the same platform so user can attach more links
        ctx.SelectOnly(_currentPlatformId);
    }

    private void AttachLinkToPlatform(PluginContext ctx, PlatformData platform, string linkId)
    {
        if (ctx.UndoRedo is null) return;

        var oldLinks = new Godot.Collections.Array<string>(platform.LinkedLinkIds);

        ctx.UndoRedo.CreateAction("Attach Platform to Link");

        ctx.UndoRedo.AddDoMethod(platform, nameof(PlatformData.AddLink), linkId);
        ctx.UndoRedo.AddUndoMethod(platform, nameof(PlatformData.RemoveLink), linkId);

        // Refresh cache
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));

        ctx.UndoRedo.CommitAction();
    }

    private void ResetCurrent(PluginContext ctx = null)
    {
        _currentPlatformId = string.Empty;
        _hoveredLinkId = string.Empty;
        ctx?.ClearSelection();
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_currentPlatformId) || string.IsNullOrEmpty(_hoveredLinkId))
            return;

        var platform = ctx.TrackData.GetPlatform(_currentPlatformId);
        var link = ctx.TrackData.GetLink(_hoveredLinkId);
        if (platform == null || link == null) return;

        var nodeA = ctx.TrackData.GetNode(link.NodeAId);
        var nodeB = ctx.TrackData.GetNode(link.NodeBId);
        if (nodeA == null || nodeB == null) return;

        var platformScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(platform.Position));
        var midLink = nodeA.Position.Lerp(nodeB.Position, 0.5f);
        var linkScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(midLink));

        // Dashed preview line (same style as Link mode)
        var previewColor = new Color(0.4f, 0.85f, 1f, 0.7f);
        overlay.DrawDashedLine(platformScreen, linkScreen, previewColor, width: 4f, dash: 10f);
    }
}