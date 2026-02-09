using System.Linq;
using Godot;

namespace RailConductor.Plugin;

public class SelectMode : PluginModeHandler
{
    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton mouseButton:
                UpdateHoveredItem(ctx, mouseButton.Position);
                if (mouseButton is { ButtonIndex: MouseButton.Left, Pressed: true })
                {
                    var newSelectedId = GetClosestId(ctx.Track, mouseButton.Position);
                    if (!string.IsNullOrEmpty(newSelectedId))
                    {
                        if (!mouseButton.ShiftPressed)
                        {
                            ctx.ClearSelection();
                        }
                        
                        ctx.Select(newSelectedId);
                        return true;
                    }
                }

                break;
            case InputEventMouseMotion mouseMotion:
                UpdateHoveredItem(ctx, mouseMotion.Position);
                break;
            case InputEventKey { Keycode: Key.Delete, Pressed: true } key:
                if (ctx.Selected.Any())
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
                    continue;
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