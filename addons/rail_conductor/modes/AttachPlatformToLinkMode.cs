using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Attach/Detach mode for platforms and track links.
/// 
/// Fixed:
/// • Links now have priority over platforms when a platform is selected (no more "platform stealing" clicks/hovers)
/// • Preview dashed lines now start from the exact center of the platform rectangle
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
                HandleRightClick(ctx, ((InputEventMouseButton)e).Position);
                RequestOverlayUpdate();
                return true;

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

    // ========================================================================
    // HOVER – Links have priority when a platform is active
    // ========================================================================

    private void UpdateHover(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        if (!string.IsNullOrEmpty(_currentPlatformId))
        {
            // When attaching, links ALWAYS take priority
            _hoveredLinkId = ctx.TrackData.FindClosestLink(localPos);
            return;
        }

        // No active platform — just prepare for platform selection
        _hoveredLinkId = string.Empty;
    }

    // ========================================================================
    // LEFT CLICK – Same priority rule
    // ========================================================================

    private void HandleLeftClick(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        // 1. If we have a current platform → try link first
        if (!string.IsNullOrEmpty(_currentPlatformId))
        {
            var clickedLinkId = ctx.TrackData.FindClosestLink(localPos);
            if (!string.IsNullOrEmpty(clickedLinkId))
            {
                var platform = ctx.TrackData.GetPlatform(_currentPlatformId);
                if (platform != null && !platform.IsLinkedTo(clickedLinkId))
                {
                    TrackEditorActions.LinkPlatformToTrackLink(ctx, platform, clickedLinkId);
                }

                return;
            }
        }

        // 2. No link hit or no active platform → check for platform selection
        var clickedPlatformId = ctx.TrackData.FindClosestPlatform(localPos);
        if (!string.IsNullOrEmpty(clickedPlatformId))
        {
            _currentPlatformId = clickedPlatformId;
            ctx.SelectOnly(clickedPlatformId);
            _hoveredLinkId = string.Empty;
            return;
        }
    }

    // ========================================================================
    // RIGHT CLICK – Detach or cancel
    // ========================================================================

    private void HandleRightClick(PluginContext ctx, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(_currentPlatformId) || string.IsNullOrEmpty(_hoveredLinkId))
        {
            ResetCurrent(ctx);
            return;
        }

        var platform = ctx.TrackData.GetPlatform(_currentPlatformId);
        if (platform == null) return;

        if (platform.IsLinkedTo(_hoveredLinkId))
        {
            TrackEditorActions.UnlinkPlatformFromTrackLink(ctx, platform, _hoveredLinkId);
        }
        else
        {
            ResetCurrent(ctx);
        }
    }

    private void ResetCurrent(PluginContext ctx = null)
    {
        _currentPlatformId = string.Empty;
        _hoveredLinkId = string.Empty;
        ctx?.ClearSelection();
    }

    // ========================================================================
    // DRAW PREVIEW – From platform CENTER
    // ========================================================================

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

        // True center of the platform rectangle
        var size = platform.IsVertical
            ? PluginSettings.PlatformVerticalSize
            : PluginSettings.PlatformHorizontalSize;

        var halfSize = size * 0.5f;
        var platformCenterLocal = platform.Position + halfSize;
        var platformScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(platformCenterLocal));

        var midLink = nodeA.Position.Lerp(nodeB.Position, 0.5f);
        var linkScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(midLink));

        var isAlreadyAttached = platform.IsLinkedTo(_hoveredLinkId);
        var previewColor = isAlreadyAttached
            ? new Color(1f, 0.3f, 0.3f, 0.8f) // Red = will detach
            : new Color(0.4f, 0.85f, 1f, 0.7f); // Cyan = will attach

        overlay.DrawDashedLine(platformScreen, linkScreen, previewColor, width: 4f, dash: 10f);
    }
}