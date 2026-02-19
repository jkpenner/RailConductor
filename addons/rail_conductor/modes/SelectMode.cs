using System.Linq;
using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Handles selection, multi-object dragging (Nodes + Platforms), deletion, and hover.
/// Now inherits reusable drag logic from DraggableModeHandler.
/// </summary>
public class SelectMode : DraggableModeHandler
{
    private string _lastClickedId = string.Empty;
    private double _lastClickTime = 0;
    
    protected override bool IsDraggable(string id, TrackData data)
        => data.IsNodeId(id) || data.IsPlatformId(id);

    protected override (string Id, Vector2 Delta)[] BuildDragItems(PluginContext ctx, Vector2 initialLocalPos)
    {
        return ctx.Selected
            .Where(id => IsDraggable(id, ctx.TrackData))
            .Select(id =>
            {
                Vector2 pos = Vector2.Zero;
                if (ctx.TrackData.IsNodeId(id))
                    pos = ctx.TrackData.GetNode(id)?.Position ?? Vector2.Zero;
                else if (ctx.TrackData.IsPlatformId(id))
                    pos = ctx.TrackData.GetPlatform(id)?.Position ?? Vector2.Zero;

                return (id, pos - initialLocalPos);
            })
            .ToArray();
    }

    protected override void ApplyPosition(PluginContext ctx, string id, Vector2 newLocalPos)
    {
        if (ctx.TrackData.IsNodeId(id))
        {
            var node = ctx.TrackData.GetNode(id);
            if (node != null) node.Position = newLocalPos;
        }
        else if (ctx.TrackData.IsPlatformId(id))
        {
            var platform = ctx.TrackData.GetPlatform(id);
            if (platform != null) platform.Position = newLocalPos;
        }
    }

    protected override void CommitItem(PluginContext ctx, string id, Vector2 finalPos, Vector2 originalPos)
    {
        if (ctx.TrackData.IsNodeId(id))
        {
            var node = ctx.TrackData.GetNode(id);
            if (node is null) return;

            ctx.UndoRedo!.AddDoProperty(node, nameof(TrackNodeData.Position), finalPos);
            ctx.UndoRedo!.AddUndoProperty(node, nameof(TrackNodeData.Position), originalPos);
            ctx.UndoRedo!.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        }
        else if (ctx.TrackData.IsPlatformId(id))
        {
            var platform = ctx.TrackData.GetPlatform(id);
            if (platform is null) return;

            ctx.UndoRedo!.AddDoProperty(platform, nameof(PlatformData.Position), finalPos);
            ctx.UndoRedo!.AddUndoProperty(platform, nameof(PlatformData.Position), originalPos);
        }
    }

    // FIXED: Now receives ctx so we can check the type safely
    protected override string GetUndoActionName(PluginContext ctx, int itemCount)
    {
        if (itemCount == 1 && _dragItems.Length > 0)
        {
            var firstId = _dragItems[0].Id;
            return ctx.TrackData.IsNodeId(firstId) ? "Move Track Node" : "Move Platform";
        }

        return base.GetUndoActionName(ctx, itemCount);
    }

    // ========================================================================
    // INPUT HANDLING (rest unchanged)
    // ========================================================================

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                HandleLeftMousePress(ctx, btn);
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } btn:
                if (IsDragging && _hasMoveSincePress)
                {
                    CommitDrag(ctx, btn.Position);
                }

                CleanupDrag();
                UpdateHoveredItem(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

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
                _hasMoveSincePress = true;
                UpdateHoveredItem(ctx, mouseMotion.Position);

                if (IsDragging)
                {
                    LiveDragPreview(ctx, mouseMotion.Position);
                }

                RequestOverlayUpdate();
                break;

            case InputEventKey { Keycode: Key.Delete, Pressed: true }:
                if (ctx.Selected.Any())
                {
                    DeleteSelected(ctx);
                    RequestOverlayUpdate();
                    return true;
                }

                break;
        }

        return false;
    }

    private void HandleLeftMousePress(PluginContext ctx, InputEventMouseButton btn)
    {
        var clickedId = GetClosestId(ctx.Track, btn.Position);
        UpdateHoveredItem(ctx, btn.Position);

        if (!string.IsNullOrEmpty(clickedId) && ctx.IsSelectable(clickedId))
        {
            if (btn.ShiftPressed)
            {
                ctx.ToggleSelect(clickedId);
            }
            else if (!ctx.IsSelected(clickedId))
            {
                ctx.SelectOnly(clickedId);
            }

            if (IsDraggable(clickedId, ctx.TrackData) && ctx.IsSelected(clickedId))
            {
                StartDrag(ctx, btn.Position);
            }
        }
        else if (!btn.ShiftPressed)
        {
            ctx.ClearSelection();
        }
        
        if (!btn.ShiftPressed && !string.IsNullOrEmpty(clickedId))
        {
            var now = Time.GetTicksMsec();
            if (clickedId == _lastClickedId && (now - _lastClickTime) < 400) // double-click
            {
                // Open editor for this item (Godot will show Inspector automatically)
                EditorInterface.Singleton.EditResource(GetResourceForId(ctx, clickedId));
            }
            else
            {
                _lastClickedId = clickedId;
                _lastClickTime = now;
            }
        }
    }
    
    private Resource? GetResourceForId(PluginContext ctx, string id)
    {
        if (ctx.TrackData.IsNodeId(id)) return ctx.TrackData.GetNode(id);
        if (ctx.TrackData.IsPlatformId(id)) return ctx.TrackData.GetPlatform(id);
        if (ctx.TrackData.IsSignalId(id)) return ctx.TrackData.GetSignal(id);
        return null;
    }

    private void DeleteSelected(PluginContext ctx)
    {
        if (ctx.UndoRedo is null) return;

        var selectedSignals = ctx.Selected.Where(ctx.TrackData.IsSignalId).ToList();
        var selectedLinks = ctx.Selected.Where(ctx.TrackData.IsLinkId).ToList();
        var selectedNodes = ctx.Selected.Where(ctx.TrackData.IsNodeId).ToList();
        var selectedPlatforms = ctx.Selected.Where(ctx.TrackData.IsPlatformId).ToList();

        foreach (var id in selectedSignals)
        {
            var signal = ctx.TrackData.GetSignal(id);
            if (signal != null)
                TrackEditorActions.DeleteTrackSignal(ctx.TrackData, signal, ctx.UndoRedo);
        }

        foreach (var id in selectedLinks)
        {
            var link = ctx.TrackData.GetLink(id);
            if (link != null)
                TrackEditorActions.DeleteTrackLink(ctx.TrackData, link, ctx.UndoRedo);
        }

        foreach (var id in selectedNodes)
        {
            var node = ctx.TrackData.GetNode(id);
            if (node != null)
                TrackEditorActions.DeleteTrackNode(ctx.TrackData, node, ctx.UndoRedo);
        }

        foreach (var id in selectedPlatforms)
        {
            var platform = ctx.TrackData.GetPlatform(id);
            if (platform != null)
                TrackEditorActions.DeleteTrackPlatform(ctx.TrackData, platform, ctx.UndoRedo);
        }

        ctx.ClearSelection();
    }

    private void UpdateHoveredItem(PluginContext ctx, Vector2 screenPosition)
    {
        var hoveredId = GetClosestId(ctx.Track, screenPosition);
        if (!string.IsNullOrEmpty(hoveredId) && ctx.IsSelectable(hoveredId))
            ctx.Hover(hoveredId);
        else
            ctx.ClearHovered();
    }
}