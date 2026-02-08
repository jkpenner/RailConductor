using Godot;

namespace RailConductor.Plugin;

public class SelectMode : PluginModeHandler
{
    protected override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        if (target.Data is null)
        {
            return false;
        }

        switch (e)
        {
            case InputEventMouseButton mouseButton:
                UpdateHoveredItem(target, mouseButton.Position);
                if (mouseButton is { ButtonIndex: MouseButton.Left, Pressed: true })
                {
                    var newSelectedId = GetClosestId(target, mouseButton.Position);
                    if (!string.IsNullOrEmpty(newSelectedId))
                    {
                        if (!mouseButton.ShiftPressed)
                        {
                            ClearSelection();
                        }
                        
                        Select(newSelectedId);
                        return true;
                    }
                }

                break;
            case InputEventMouseMotion mouseMotion:
                UpdateHoveredItem(target, mouseMotion.Position);
                break;
            case InputEventKey { Keycode: Key.Delete, Pressed: true } key:
                if (Selected.Count == 0)
                {
                    return false;
                }

                HandleDeleteEvent(target.Data, undoRedo);
                return true;
        }

        return false;
    }

    private void HandleDeleteEvent(TrackData track, EditorUndoRedoManager undoRedo)
    {
        foreach (var id in Selected)
        {
            if (track.IsNodeId(id))
            {
                var node = track.GetNode(id);
                if (node is not null)
                {
                    TrackEditorActions.DeleteTrackNode(track, node, undoRedo);
                    continue;
                }
            }

            if (track.IsLinkId(id))
            {
                var link = track.GetLink(id);
                if (link is not null)
                {
                    TrackEditorActions.DeleteTrackLink(track, link, undoRedo);
                    continue;
                }
            }

            if (track.IsSignalId(id))
            {
                var signal = track.GetSignal(id);
                if (signal is not null)
                {
                    TrackEditorActions.DeleteTrackSignal(track, signal, undoRedo);
                    continue;
                }
            }
        }

        ClearSelection();
        RequestOverlayUpdate();
    }

    private void UpdateHoveredItem(Track target, Vector2 screenPosition)
    {
        var newHoveredId = GetClosestId(target, screenPosition);
        if (!string.IsNullOrEmpty(newHoveredId))
        {
            Hover(newHoveredId);
        }
    }
}