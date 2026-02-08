using System;
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

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (btn.Pressed)
            {
                var newNode = new TrackNodeData
                {
                    Position = localPosition
                };

                MarkAsSelected(newNode.Id);
                _originalPosition = localPosition;

                undoRedo.CreateAction("Add Track Node");
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddNode), newNode.Id, newNode);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveNode), newNode.Id);
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), target.Data);
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
                undoRedo.AddDoMethod(nodeRef, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.CommitAction();
                _selectedNodeId = string.Empty;
                return true;
            }
        }

        if (e is InputEventMouseMotion mouseMotion && !string.IsNullOrEmpty(_selectedNodeId) &&
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