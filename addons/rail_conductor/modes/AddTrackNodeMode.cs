using System;
using Godot;

namespace RailConductor.Plugin;

public class AddTrackNodeMode : PluginModeHandler
{
    public override int SelectedNodeId => _selectedNodeId;

    private int _selectedNodeId;
    private Vector2 _originalPosition;

    public override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        if (target.Data is null)
        {
            return false;
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (btn.Pressed)
            {
                var newNode = new TrackNodeData
                {
                    Id = target.Data.GetAvailableNodeId(),
                    Position = localPosition
                };

                _selectedNodeId = newNode.Id;
                _originalPosition = localPosition;

                undoRedo.CreateAction("Add Track Node");
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddNode), _selectedNodeId, newNode);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveNode), _selectedNodeId);
                undoRedo.CommitAction();
                return true;
            }
            else
            {
                var finalPos = target.Data.GetNode(_selectedNodeId)?.Position ?? throw new NullReferenceException();
                var nodeRef = target.Data.GetNode(_selectedNodeId) ?? throw new NullReferenceException(); // Reference for closure

                undoRedo.CreateAction("Move Track Node");
                undoRedo.AddDoProperty(nodeRef, nameof(TrackNodeData.Position), finalPos);
                undoRedo.AddUndoProperty(nodeRef, nameof(TrackNodeData.Position), _originalPosition);
                undoRedo.CommitAction();
                _selectedNodeId = -1;
                return true;
            }
        }

        if (e is InputEventMouseMotion mouseMotion && _selectedNodeId >= 0 &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
            var localPosition = target.ToLocal(globalPosition);

            var node = target.Data.GetNode(_selectedNodeId);
            if (node is not null)
            {
                node.Position = localPosition;
            }
                
            target.RecalculateGraph();
            target.NotifyPropertyListChanged();
            return true;
        }

        return false;
    }
}