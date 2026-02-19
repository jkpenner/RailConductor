using System.Linq;
using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Handles selection, multi-node dragging, deletion, and hover for track nodes, links, and signals.
/// 
/// Updated to fully leverage the new PluginContext API (ToggleSelect, SelectOnly, ClearHovered, IsSelectable, etc.)
/// and nullable reference types (project-wide #nullable enable assumed).
/// 
/// Features:
/// • Click on item → immediately selects (or keeps multi-selection) + starts drag in one gesture
/// • Shift + Click → toggle selection (add/remove the clicked item)
/// • Normal click on already-selected node in a multi-selection → keeps the whole group (standard editor behaviour)
/// • Escape key or Right Mouse Button → cancels current drag (restores original positions, no undo entry)
/// • Multi-node drag → single batch undo action in history
/// • Deletion respects dependency order: signals → links → nodes
/// • Hover is cleared properly when over empty space
/// • All operations respect IsSelectable restrictions
/// </summary>
public class SelectMode : PluginModeHandler
{
    // ========================================================================
    // DRAG STATE
    // ========================================================================

    private bool _isDraggable;
    private bool _hasMoveSincePress;
    private Vector2 _initialPressPosition;
    private (string Id, Vector2 Delta)[] _nodeDeltaPositions = [];

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

            // Cancel active drag with Right-click or Escape
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
        _nodeDeltaPositions = [];

        var btnGlobalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
        _initialPressPosition = ctx.Track.ToLocal(btnGlobalPosition);

        if (!string.IsNullOrEmpty(clickedId) && ctx.IsSelectable(clickedId))
        {
            if (btn.ShiftPressed)
            {
                // Shift + Click = Toggle (add or remove the item)
                ctx.ToggleSelect(clickedId);
            }
            else
            {
                // Normal click: replace selection unless clicking inside an existing multi-selection
                if (!ctx.IsSelected(clickedId))
                {
                    ctx.SelectOnly(clickedId);
                }
                // else: already selected → keep the whole group (allows dragging multi-selections)
            }

            // Enable drag only if the clicked item is a node and it is now selected
            if (ctx.TrackData.IsNodeId(clickedId) && ctx.IsSelected(clickedId))
            {
                _isDraggable = true;
                _nodeDeltaPositions = ctx.Selected
                    .Select(ctx.TrackData.GetNode)
                    .OfType<TrackNodeData>()
                    .Select(n => (n.Id, n.Position - _initialPressPosition))
                    .ToArray();
            }
        }
        else
        {
            // Clicked empty space
            if (!btn.ShiftPressed)
            {
                ctx.ClearSelection();
            }
        }
    }

    // ========================================================================
    // DRAG OPERATIONS
    // ========================================================================

    private void LiveDragPreview(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPosition);

        foreach (var (id, delta) in _nodeDeltaPositions)
        {
            var node = ctx.TrackData.GetNode(id);
            if (node is null) continue;

            node.Position = newOrigin + delta;
        }
    }

    private void CommitDrag(PluginContext ctx, Vector2 screenPosition)
    {
        if (_nodeDeltaPositions.Length == 0 || ctx.UndoRedo is null) return;

        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPosition);

        string actionName = _nodeDeltaPositions.Length == 1
            ? "Move Track Node"
            : $"Move {_nodeDeltaPositions.Length} Track Nodes";

        ctx.UndoRedo.CreateAction(actionName);

        foreach (var (id, delta) in _nodeDeltaPositions)
        {
            var node = ctx.TrackData.GetNode(id);
            if (node is null) continue;

            var finalPosition = newOrigin + delta;
            var originalPosition = _initialPressPosition + delta;

            ctx.UndoRedo.AddDoProperty(node, nameof(TrackNodeData.Position), finalPosition);
            ctx.UndoRedo.AddUndoProperty(node, nameof(TrackNodeData.Position), originalPosition);
            ctx.UndoRedo.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        }

        ctx.UndoRedo.CommitAction();
    }

    private void CancelDrag(PluginContext ctx)
    {
        foreach (var (id, delta) in _nodeDeltaPositions)
        {
            var node = ctx.TrackData.GetNode(id);
            if (node is null) continue;

            node.Position = _initialPressPosition + delta;
        }

        CleanupAfterRelease();
    }

    private void CleanupAfterRelease()
    {
        _isDraggable = false;
        _hasMoveSincePress = false;
        _nodeDeltaPositions = [];
    }

    // ========================================================================
    // DELETION (safe dependency order)
    // ========================================================================

    private void DeleteSelected(PluginContext ctx)
    {
        if (ctx.UndoRedo is null) return;

        var selectedSignals = ctx.Selected.Where(ctx.TrackData.IsSignalId).ToList();
        var selectedLinks   = ctx.Selected.Where(ctx.TrackData.IsLinkId).ToList();
        var selectedNodes   = ctx.Selected.Where(ctx.TrackData.IsNodeId).ToList();

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

    // ========================================================================
    // HOVER
    // ========================================================================

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