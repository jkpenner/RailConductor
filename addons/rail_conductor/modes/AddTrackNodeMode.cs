using Godot;

namespace RailConductor.Plugin;

public class AddTrackNodeMode : PluginModeHandler
{
    private string _selectedNodeId = string.Empty;
    private Vector2 _originalPosition;

    protected override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        if (target.Data is null)
        {
            return false;
        }

        switch (e)
        {
            case InputEventMouseMotion mouseMotion:
                ProcessMouseMotion(target, mouseMotion);
                break;
                
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
                return ProcessLeftMouseButton(target, mouseButton, undoRedo);
        }

        return false;
    }

    private void ProcessMouseMotion(Track target, InputEventMouseMotion mouseMotion)
    {
        if (string.IsNullOrEmpty(_selectedNodeId))
        {
            return;
        }

        var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
        var localPosition = target.ToLocal(globalPosition);

        var node = target.Data?.GetNode(_selectedNodeId);
        if (node is null)
        {
            return;
        }

        node.Position = localPosition;
    }
    
    private bool ProcessLeftMouseButton(Track target, InputEventMouseButton mouseButton, EditorUndoRedoManager undoRedo)
    {
        if (target.Data is null)
        {
            return false;
        }
        
        var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseButton.Position);
        var localPosition = target.ToLocal(globalPosition);

        if (mouseButton.Pressed)
        {
            var newNode = new TrackNodeData
            {
                Position = localPosition
            };

            ClearSelection();
            Select(newNode.Id);
            _selectedNodeId = newNode.Id;
            
            _originalPosition = localPosition;

            TrackEditorActions.AddTrackNode(target.Data, newNode, undoRedo);
        }
        else
        {
            var node = target.Data.GetNode(_selectedNodeId);
            if (node is null)
            {
                return false;
            }

            Deselect(_selectedNodeId);
            _selectedNodeId = string.Empty;
            
            TrackEditorActions.MoveTrackNode(target.Data, node, node.Position, _originalPosition, undoRedo);
        }

        return true;
    }

    
}