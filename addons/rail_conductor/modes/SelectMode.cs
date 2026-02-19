using System.Linq;
using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Handles selection, multi-object dragging (Nodes + Platforms), deletion, and hover.
/// 
/// New dragging features:
/// • Click any draggable item (node or platform) → immediately selects it + starts dragging
/// • All currently selected movable items (nodes + platforms) move together
/// • Mixed selections are fully supported (click a node or platform to drag the whole group)
/// • Single batch undo/redo action for any number of moved objects
/// • Right-click or Escape key cancels the drag (original positions restored, no undo created)
/// • Shift + Click still toggles selection
/// • All previous behaviours (empty-space deselect, hover, safe deletion order) preserved
/// </summary>
public class SelectMode : PluginModeHandler
{
    // ========================================================================
    // DRAG STATE
    // ========================================================================

    private bool _isDraggable;
    private bool _hasMoveSincePress;
    private Vector2 _initialPressPosition;

    /// <summary>
    /// All currently selected movable items (TrackNodeData or PlatformData) with their drag deltas.
    /// </summary>
    private (string Id, Vector2 Delta)[] _movableDeltas = [];

    // ========================================================================
    // INPUT HANDLING
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
                if (_isDraggable && _hasMoveSincePress)
                {
                    CommitDrag(ctx, btn.Position);
                }

                CleanupAfterRelease();
                UpdateHoveredItem(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

            // Cancel active drag
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (_isDraggable)
                {
                    CancelDrag(ctx);
                    RequestOverlayUpdate();
                    return true;
                }

                break;

            case InputEventMouseMotion mouseMotion:
                _hasMoveSincePress = true;
                UpdateHoveredItem(ctx, mouseMotion.Position);

                if (_isDraggable)
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

    // ========================================================================
    // LEFT MOUSE PRESS — Selection + Drag Setup
    // ========================================================================

    private void HandleLeftMousePress(PluginContext ctx, InputEventMouseButton btn)
    {
        var clickedId = GetClosestId(ctx.Track, btn.Position);
        UpdateHoveredItem(ctx, btn.Position);

        // Reset drag state for this press
        _hasMoveSincePress = false;
        _isDraggable = false;
        _movableDeltas = [];

        var btnGlobalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
        _initialPressPosition = ctx.Track.ToLocal(btnGlobalPosition);

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

            // Enable dragging only if the clicked item is a movable type (Node or Platform)
            if (IsDraggable(ctx.TrackData, clickedId) && ctx.IsSelected(clickedId))
            {
                _isDraggable = true;
                _movableDeltas = BuildMovableDeltas(ctx);
            }
        }
        else if (!btn.ShiftPressed)
        {
            ctx.ClearSelection();
        }
    }

    private static bool IsDraggable(TrackData data, string id)
    {
        return data.IsNodeId(id) || data.IsPlatformId(id);
    }

    private (string Id, Vector2 Delta)[] BuildMovableDeltas(PluginContext ctx)
    {
        return ctx.Selected
            .Where(id => IsDraggable(ctx.TrackData, id))
            .Select(id =>
            {
                Vector2 pos = Vector2.Zero;
                if (ctx.TrackData.IsNodeId(id))
                    pos = ctx.TrackData.GetNode(id)?.Position ?? Vector2.Zero;
                else if (ctx.TrackData.IsPlatformId(id))
                    pos = ctx.TrackData.GetPlatform(id)?.Position ?? Vector2.Zero;

                return (id, pos - _initialPressPosition);
            })
            .ToArray();
    }

    // ========================================================================
    // DRAG OPERATIONS
    // ========================================================================

    private void LiveDragPreview(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPosition);

        foreach (var (id, delta) in _movableDeltas)
        {
            var newPos = newOrigin + delta;

            if (ctx.TrackData.IsNodeId(id))
            {
                var node = ctx.TrackData.GetNode(id);
                if (node != null) node.Position = newPos;
            }
            else if (ctx.TrackData.IsPlatformId(id))
            {
                var platform = ctx.TrackData.GetPlatform(id);
                if (platform != null) platform.Position = newPos;
            }
        }
    }

    private void CommitDrag(PluginContext ctx, Vector2 screenPosition)
    {
        if (_movableDeltas.Length == 0 || ctx.UndoRedo is null) return;

        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPosition);

        // Nice action name in undo history
        string actionName = _movableDeltas.Length == 1
            ? (ctx.TrackData.IsNodeId(_movableDeltas[0].Id) ? "Move Track Node" : "Move Platform")
            : $"Move {_movableDeltas.Length} Track Objects";

        ctx.UndoRedo.CreateAction(actionName);

        foreach (var (id, delta) in _movableDeltas)
        {
            var finalPosition = newOrigin + delta;
            var originalPosition = _initialPressPosition + delta;

            if (ctx.TrackData.IsNodeId(id))
            {
                var node = ctx.TrackData.GetNode(id);
                if (node is null) continue;

                ctx.UndoRedo.AddDoProperty(node, nameof(TrackNodeData.Position), finalPosition);
                ctx.UndoRedo.AddUndoProperty(node, nameof(TrackNodeData.Position), originalPosition);
                ctx.UndoRedo.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
            }
            else if (ctx.TrackData.IsPlatformId(id))
            {
                var platform = ctx.TrackData.GetPlatform(id);
                if (platform is null) continue;

                ctx.UndoRedo.AddDoProperty(platform, nameof(PlatformData.Position), finalPosition);
                ctx.UndoRedo.AddUndoProperty(platform, nameof(PlatformData.Position), originalPosition);
                // Platforms have no UpdateConfiguration
            }
        }

        ctx.UndoRedo.CommitAction();
    }

    private void CancelDrag(PluginContext ctx)
    {
        foreach (var (id, delta) in _movableDeltas)
        {
            var originalPos = _initialPressPosition + delta;

            if (ctx.TrackData.IsNodeId(id))
            {
                var node = ctx.TrackData.GetNode(id);
                if (node != null) node.Position = originalPos;
            }
            else if (ctx.TrackData.IsPlatformId(id))
            {
                var platform = ctx.TrackData.GetPlatform(id);
                if (platform != null) platform.Position = originalPos;
            }
        }

        CleanupAfterRelease();
    }

    private void CleanupAfterRelease()
    {
        _isDraggable = false;
        _hasMoveSincePress = false;
        _movableDeltas = [];
    }

    // ========================================================================
    // DELETION & HOVER (unchanged)
    // ========================================================================

    private void DeleteSelected(PluginContext ctx)
    {
        if (ctx.UndoRedo is null) return;

        var selectedPlatforms = ctx.Selected.Where(ctx.TrackData.IsPlatformId).ToList(); 
        var selectedSignals = ctx.Selected.Where(ctx.TrackData.IsSignalId).ToList();
        var selectedLinks = ctx.Selected.Where(ctx.TrackData.IsLinkId).ToList();
        var selectedNodes = ctx.Selected.Where(ctx.TrackData.IsNodeId).ToList();

        foreach (var id in selectedPlatforms)
        {
            var platform = ctx.TrackData.GetPlatform(id);
            if (platform != null)
                TrackEditorActions.DeleteTrackPlatform(ctx.TrackData, platform, ctx.UndoRedo);
        }
        
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

        ctx.ClearSelection();
    }

    private void UpdateHoveredItem(PluginContext ctx, Vector2 screenPosition)
    {
        var hoveredId = GetClosestId(ctx.Track, screenPosition);

        if (!string.IsNullOrEmpty(hoveredId) && ctx.IsSelectable(hoveredId))
        {
            ctx.Hover(hoveredId);
        }
        else
        {
            ctx.ClearHovered();
        }
    }
}