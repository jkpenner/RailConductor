using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Placement mode for Platforms.
/// 
/// Flow:
/// • Left press: creates platform + immediately starts dragging
/// • Mouse motion (button held): live drag
/// • Left release: finalizes placement
/// • Right-click while dragging: deletes via TrackEditorActions (undoable)
/// • Right-click when NOT dragging: toggles vertical/horizontal (original feature)
/// • Escape while dragging: deletes
/// </summary>
public class PlacePlatformMode : PluginModeHandler
{
    private bool _isDragging;
    private string _placingId = string.Empty;
    private Vector2 _placePosition;
    private bool _placeVertical = false;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        Cleanup();
        RequestOverlayUpdate();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                var globalPos = PluginUtility.ScreenToWorldSnapped(motion.Position);
                _placePosition = ctx.Track.ToLocal(globalPos);

                if (_isDragging)
                {
                    var platform = ctx.TrackData.GetPlatform(_placingId);
                    if (platform != null)
                        platform.Position = _placePosition;
                }
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                if (_isDragging)
                {
                    FinalizePlacement(ctx);
                }
                else
                {
                    StartPlacement(ctx, btn);
                }
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                if (_isDragging)
                {
                    FinalizePlacement(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;

            // Right-click: cancel if dragging, otherwise toggle orientation
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (_isDragging)
                {
                    CancelPlacement(ctx);
                }
                else
                {
                    _placeVertical = !_placeVertical;
                }
                RequestOverlayUpdate();
                return true;

            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (_isDragging)
                {
                    CancelPlacement(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;
        }

        return false;
    }

    private void StartPlacement(PluginContext ctx, InputEventMouseButton btn)
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
        _isDragging = true;

        ctx.SelectOnly(platform.Id);
        TrackEditorActions.AddTrackPlatform(ctx, platform);
    }

    private void FinalizePlacement(PluginContext ctx)
    {
        ctx.ClearSelection();
        Cleanup();
    }

    private void CancelPlacement(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_placingId) || ctx.UndoRedo is null) return;

        var platform = ctx.TrackData.GetPlatform(_placingId);
        if (platform != null)
        {
            // Uses the new DeleteTrackPlatform (undoable)
            TrackEditorActions.DeleteTrackPlatform(ctx.TrackData, platform, ctx.UndoRedo);
        }

        ctx.ClearSelection();
        Cleanup();
    }

    private void Cleanup()
    {
        _placingId = string.Empty;
        _isDragging = false;
    }
}