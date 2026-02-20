using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Places a new PlatformGroup (station/container).
/// • Click to place the group at mouse position (center).
/// • Group is immediately selectable and draggable in SelectMode.
/// • Shows semi-transparent background immediately (even when empty).
/// </summary>
public class PlacePlatformGroupMode : DraggableModeHandler
{
    private string _placingId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        CleanupDrag();
        RequestOverlayUpdate();
    }

    protected override bool IsDraggable(string id, TrackData data) => data.IsPlatformGroupId(id);

    protected override (string Id, Vector2 Delta)[] BuildDragItems(PluginContext ctx, Vector2 initialLocalPos)
    {
        if (string.IsNullOrEmpty(_placingId)) return [];
        var group = ctx.TrackData.GetPlatformGroup(_placingId);
        return group is null ? [] : new[] { (_placingId, Vector2.Zero) }; // groups don't move platforms yet
    }

    protected override void ApplyPosition(PluginContext ctx, string id, Vector2 newLocalPos)
    {
        var group = ctx.TrackData.GetPlatformGroup(id);
        if (group != null) group.Position = newLocalPos;
    }

    protected override void CommitItem(PluginContext ctx, string id, Vector2 finalPos, Vector2 originalPos)
    {
        var group = ctx.TrackData.GetPlatformGroup(id);
        if (group is null) return;

        ctx.UndoRedo!.AddDoProperty(group, nameof(PlatformGroupData.Position), finalPos);
        ctx.UndoRedo!.AddUndoProperty(group, nameof(PlatformGroupData.Position), originalPos);
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                if (IsDragging)
                {
                    CommitDrag(ctx, btn.Position);
                    Cleanup();
                }
                else
                {
                    StartNewGroup(ctx, btn);
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
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (IsDragging)
                {
                    CancelDrag(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;

            case InputEventMouseMotion mouseMotion:
                if (IsDragging)
                {
                    LiveDragPreview(ctx, mouseMotion.Position);
                }
                RequestOverlayUpdate();
                break;
        }

        return false;
    }

    private void StartNewGroup(PluginContext ctx, InputEventMouseButton btn)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPos = ctx.Track.ToLocal(globalPos);
        localPos = PluginUtility.SnapPosition(localPos);

        var newGroup = new PlatformGroupData
        {
            Position = localPos,
            DisplayName = "New Station"
        };

        _placingId = newGroup.Id;

        ctx.SelectOnly(newGroup.Id);
        TrackEditorActions.AddPlatformGroup(ctx.TrackData, newGroup, ctx.UndoRedo!);

        StartDrag(ctx, btn.Position);
    }

    private void Cleanup()
    {
        _placingId = string.Empty;
        CleanupDrag();
    }
}