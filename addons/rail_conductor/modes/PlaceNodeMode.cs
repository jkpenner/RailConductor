using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Placement mode for Track Nodes.
/// 
/// Flow:
/// • Left press: creates node at snapped position + immediately starts dragging
/// • Mouse motion (button held): live drag preview
/// • Left release: commits final position with undoable Move
/// • Right-click or Escape while dragging: deletes via TrackEditorActions (undoable)
/// </summary>
public class PlaceNodeMode : PluginModeHandler
{
    private bool _isDragging;
    private string _placingId = string.Empty;
    private Vector2 _originalPosition;

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

            // Cancel with Right-click or Escape
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (_isDragging)
                {
                    CancelPlacement(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;

            case InputEventMouseMotion mouseMotion:
                if (_isDragging)
                {
                    UpdateDragPosition(ctx, mouseMotion.Position);
                }
                RequestOverlayUpdate();
                break;
        }

        return false;
    }

    private void StartPlacement(PluginContext ctx, InputEventMouseButton btn)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPos = ctx.Track.ToLocal(globalPos);

        var newNode = new TrackNodeData { Position = localPos };

        _placingId = newNode.Id;
        _originalPosition = localPos;
        _isDragging = true;

        ctx.SelectOnly(newNode.Id);
        TrackEditorActions.AddTrackNode(ctx.TrackData, newNode, ctx.UndoRedo!);
    }

    private void UpdateDragPosition(PluginContext ctx, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(_placingId)) return;

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        var node = ctx.TrackData.GetNode(_placingId);
        if (node != null)
            node.Position = localPos;
    }

    private void FinalizePlacement(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_placingId)) return;

        var node = ctx.TrackData.GetNode(_placingId);
        if (node != null && node.Position != _originalPosition)
        {
            TrackEditorActions.MoveTrackNode(
                ctx.TrackData,
                node,
                node.Position,
                _originalPosition,
                ctx.UndoRedo!
            );
        }

        ctx.ClearSelection();
        Cleanup();
    }

    private void CancelPlacement(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_placingId) || ctx.UndoRedo is null) return;

        var node = ctx.TrackData.GetNode(_placingId);
        if (node != null)
        {
            // Uses the proper delete action (undoable)
            TrackEditorActions.DeleteTrackNode(ctx.TrackData, node, ctx.UndoRedo);
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