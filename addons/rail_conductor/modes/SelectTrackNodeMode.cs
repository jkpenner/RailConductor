using Godot;

namespace RailConductor.Plugin;

public class SelectTrackNodeMode : PluginModeHandler
{
    private string _selectedNodeId = string.Empty;

    protected override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        if (!string.IsNullOrEmpty(_selectedNodeId))
        {
            MarkAsSelected(_selectedNodeId);
        }
        
        if (target.Data is null)
        {
            return false;
        }

        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            return false;
        }

        var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPosition = target.ToLocal(globalPosition);
        
        var newSelectedNodeId = target.Data.FindClosestNodeId(localPosition);
        if (newSelectedNodeId != _selectedNodeId)
        {
            _selectedNodeId = newSelectedNodeId;
            return true;
        }

        _selectedNodeId = string.Empty;
        return false;
    }
}