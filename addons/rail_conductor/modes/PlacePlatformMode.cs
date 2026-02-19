using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Placement mode for Platforms (uses reusable drag logic from DraggableModeHandler).
/// 
/// Updated as requested:
/// • No platform preview/ghost is shown when idle (not dragging).
/// • The platform only becomes visible once you left-click to place it and start dragging.
/// • Right-click or Escape while dragging deletes it (undoable).
/// • All other behaviour (vertical toggle on idle right-click, etc.) preserved.
/// </summary>
public class PlacePlatformMode : DraggableModeHandler
{
    private string _placingId = string.Empty;
    private Vector2 _placePosition;
    private bool _placeVertical = false;

    protected override void OnEnable(PluginContext ctx)
    {
        ResetRestrictions(ctx);
        CleanupDrag();
        RequestOverlayUpdate();
    }

    protected override bool IsDraggable(string id, TrackData data) => data.IsPlatformId(id);

    protected override (string Id, Vector2 Delta)[] BuildDragItems(PluginContext ctx, Vector2 initialLocalPos)
    {
        if (string.IsNullOrEmpty(_placingId)) return [];
        var platform = ctx.TrackData.GetPlatform(_placingId);
        return platform is null ? [] : new[] { (_placingId, platform.Position - initialLocalPos) };
    }

    protected override void ApplyPosition(PluginContext ctx, string id, Vector2 newLocalPos)
    {
        var platform = ctx.TrackData.GetPlatform(id);
        if (platform != null) platform.Position = newLocalPos;
    }

    protected override void CommitItem(PluginContext ctx, string id, Vector2 finalPos, Vector2 originalPos)
    {
        var platform = ctx.TrackData.GetPlatform(id);
        if (platform is null) return;

        ctx.UndoRedo!.AddDoProperty(platform, nameof(PlatformData.Position), finalPos);
        ctx.UndoRedo!.AddUndoProperty(platform, nameof(PlatformData.Position), originalPos);
    }

    protected override void OnCancelDrag(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_placingId) || ctx.UndoRedo is null) return;

        var platform = ctx.TrackData.GetPlatform(_placingId);
        if (platform != null)
        {
            TrackEditorActions.DeleteTrackPlatform(ctx.TrackData, platform, ctx.UndoRedo);
        }

        _placingId = string.Empty;
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                var globalPos = PluginUtility.ScreenToWorldSnapped(motion.Position);
                _placePosition = ctx.Track.ToLocal(globalPos);

                if (IsDragging)
                {
                    LiveDragPreview(ctx, motion.Position);
                }
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                if (IsDragging)
                {
                    CommitDrag(ctx, btn.Position);
                    Cleanup();
                }
                else
                {
                    StartNewPlatform(ctx, btn);
                }
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                if (IsDragging)
                {
                    CommitDrag(ctx, ((InputEventMouseButton)e).Position);
                    Cleanup();
                    RequestOverlayUpdate();
                    return true;
                }
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (IsDragging)
                {
                    CancelDrag(ctx);
                }
                else
                {
                    _placeVertical = !_placeVertical;
                }
                RequestOverlayUpdate();
                return true;

            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (IsDragging)
                {
                    CancelDrag(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;
        }

        return false;
    }

    private void StartNewPlatform(PluginContext ctx, InputEventMouseButton btn)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPos = ctx.Track.ToLocal(globalPos);

        var platform = new PlatformData
        {
            Position = localPos,
            IsVertical = _placeVertical
        };

        _placingId = platform.Id;
        _placePosition = localPos;

        ctx.SelectOnly(platform.Id);
        TrackEditorActions.AddTrackPlatform(ctx, platform);

        StartDrag(ctx, btn.Position);
    }

    private void Cleanup()
    {
        _placingId = string.Empty;
        CleanupDrag();
    }
}