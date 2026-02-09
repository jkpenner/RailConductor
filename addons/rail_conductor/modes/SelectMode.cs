using System.Linq;
using Godot;

namespace RailConductor.Plugin;

public class SelectMode : PluginModeHandler
{
    private bool _isDraggable;
    private bool _hasMoveSincePress;

    private Vector2 _initialPressPosition;
    private (string, Vector2)[] _nodeDeltaPositions = [];

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                UpdateHoveredItem(ctx, btn.Position);

                _hasMoveSincePress = false;
                _initialPressPosition = btn.Position;

                var btnGlobalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
                _initialPressPosition = ctx.Track.ToLocal(btnGlobalPosition);

                // If initial click is on an existing selected node, setup possible move
                var selectedNodeId = GetClosestId(ctx.Track, btn.Position);
                if (!string.IsNullOrEmpty(selectedNodeId) && ctx.IsSelected(selectedNodeId))
                {
                    _isDraggable = true;
                    _nodeDeltaPositions = ctx.Selected
                        .Select(ctx.TrackData.GetNode)
                        .OfType<TrackNodeData>()
                        // Delta from the initial press location.
                        .Select(n => (n.Id, n.Position - _initialPressPosition))
                        .ToArray();
                }

                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } btn:
                UpdateHoveredItem(ctx, btn.Position);

                if (_isDraggable && _hasMoveSincePress)
                {
                    _isDraggable = false;
                    // Update all nodes based on their original positions.
                    var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
                    var newOrigin = ctx.Track.ToLocal(globalPosition);

                    foreach (var (id, delta) in _nodeDeltaPositions)
                    {
                        var node = ctx.TrackData.GetNode(id);
                        if (node is null)
                        {
                            continue;
                        }

                        TrackEditorActions.MoveTrackNode(ctx.TrackData, node,
                            newOrigin + delta, _initialPressPosition + delta, ctx.UndoRedo);
                    }


                    return true;
                }

                var newSelectedId = GetClosestId(ctx.Track, btn.Position);
                if (!string.IsNullOrEmpty(newSelectedId))
                {
                    if (!btn.ShiftPressed)
                    {
                        ctx.ClearSelection();
                    }

                    ctx.Select(newSelectedId);
                    return true;
                }

                break;
            case InputEventMouseMotion mouseMotion:
                _hasMoveSincePress = true;
                UpdateHoveredItem(ctx, mouseMotion.Position);

                if (_isDraggable)
                {
                    // Update all nodes based on their original positions.
                    var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
                    var newOrigin = ctx.Track.ToLocal(globalPosition);

                    foreach (var (id, delta) in _nodeDeltaPositions)
                    {
                        var node = ctx.TrackData.GetNode(id);
                        if (node is null)
                        {
                            continue;
                        }

                        node.Position = newOrigin + delta;
                    }
                }

                break;
            case InputEventKey { Keycode: Key.Delete, Pressed: true }:
                if (!ctx.Selected.Any())
                {
                    return false;
                }

                DeleteSelected(ctx);
                return true;
        }

        return false;
    }

    private void DeleteSelected(PluginContext ctx)
    {
        foreach (var id in ctx.Selected)
        {
            if (ctx.TrackData.IsNodeId(id))
            {
                var node = ctx.TrackData.GetNode(id);
                if (node is not null)
                {
                    TrackEditorActions.DeleteTrackNode(ctx.TrackData, node, ctx.UndoRedo);
                    continue;
                }
            }

            if (ctx.TrackData.IsLinkId(id))
            {
                var link = ctx.TrackData.GetLink(id);
                if (link is not null)
                {
                    TrackEditorActions.DeleteTrackLink(ctx.TrackData, link, ctx.UndoRedo);
                    continue;
                }
            }

            if (ctx.TrackData.IsSignalId(id))
            {
                var signal = ctx.TrackData.GetSignal(id);
                if (signal is not null)
                {
                    TrackEditorActions.DeleteTrackSignal(ctx.TrackData, signal, ctx.UndoRedo);
                }
            }
        }

        ctx.ClearSelection();
        RequestOverlayUpdate();
    }

    private void UpdateHoveredItem(PluginContext ctx, Vector2 screenPosition)
    {
        var newHoveredId = GetClosestId(ctx.Track, screenPosition);
        if (!string.IsNullOrEmpty(newHoveredId))
        {
            ctx.Hover(newHoveredId);
        }
    }
}