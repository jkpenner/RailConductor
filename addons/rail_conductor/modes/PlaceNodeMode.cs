using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Placement mode for Track Nodes (now uses reusable drag logic).
/// Cancel during drag now correctly deletes the object.
/// </summary>
public class PlaceNodeMode : DraggableModeHandler
{
    private string _placingId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        CleanupDrag();
        _placingId = string.Empty;
        RequestOverlayUpdate();
    }

    protected override bool IsDraggable(string id, TrackData data) => data.IsNodeId(id);

    protected override (string Id, Vector2 Delta)[] BuildDragItems(PluginContext ctx, Vector2 initialLocalPos)
    {
        if (string.IsNullOrEmpty(_placingId)) return [];
        var node = ctx.TrackData.GetNode(_placingId);
        return node is null ? [] : new[] { (_placingId, node.Position - initialLocalPos) };
    }

    protected override void ApplyPosition(PluginContext ctx, string id, Vector2 newLocalPos)
    {
        var node = ctx.TrackData.GetNode(id);
        if (node != null) node.Position = newLocalPos;
    }

    protected override void CommitItem(PluginContext ctx, string id, Vector2 finalPos, Vector2 originalPos)
    {
        var node = ctx.TrackData.GetNode(id);
        if (node is null) return;

        ctx.UndoRedo!.AddDoProperty(node, nameof(TrackNodeData.Position), finalPos);
        ctx.UndoRedo!.AddUndoProperty(node, nameof(TrackNodeData.Position), originalPos);
        ctx.UndoRedo!.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
    }

    // ========================================================================
    // CANCEL OVERRIDE — DELETE THE OBJECT (this fixes the bug)
    // ========================================================================

    protected override void OnCancelDrag(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_placingId) || ctx.UndoRedo is null) return;

        var node = ctx.TrackData.GetNode(_placingId);
        if (node != null)
        {
            TrackEditorActions.DeleteTrackNode(ctx.TrackData, node, ctx.UndoRedo);
        }

        _placingId = string.Empty;
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                if (IsDragging)
                {
                    CommitDrag(ctx, btn.Position);
                    CleanupDrag();
                }
                else
                {
                    StartNewNode(ctx, btn);
                }
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                if (IsDragging)
                {
                    CommitDrag(ctx, ((InputEventMouseButton)e).Position);
                    CleanupDrag();
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

    private void StartNewNode(PluginContext ctx, InputEventMouseButton btn)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPos = ctx.Track.ToLocal(globalPos);

        var newNode = new TrackNodeData { Position = localPos };

        _placingId = newNode.Id;

        ctx.SelectOnly(newNode.Id);
        TrackEditorActions.AddTrackNode(ctx.TrackData, newNode, ctx.UndoRedo!);

        StartDrag(ctx, btn.Position);
    }

    private void Cleanup()
    {
        _placingId = string.Empty;
        CleanupDrag();
    }
}