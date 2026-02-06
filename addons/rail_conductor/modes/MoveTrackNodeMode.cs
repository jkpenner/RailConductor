using Godot;

namespace RailConductor.Plugin.modes;

public class MoveTrackNodeMode : PluginModeHandler
{
    public override int SelectedNodeId => _selectedNodeId;
    
    private int _selectedNodeId = -1;
    private Vector2 _originalPosition = Vector2.Zero;

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
                _selectedNodeId = target.Data.FindClosestNodeId(localPosition);
                var selectedNode = target.Data.GetNode(_selectedNodeId);
                if (selectedNode is not null)
                {
                    _originalPosition = selectedNode.Position;
                }
            }
            else // Released
            {
                var selectedNode = target.Data.GetNode(_selectedNodeId);
                if (selectedNode is not null)
                {
                    var finalPos = selectedNode.Position;

                    undoRedo.CreateAction("Move Track Node");
                    undoRedo.AddDoProperty(selectedNode, nameof(TrackNodeData.Position), finalPos);
                    undoRedo.AddUndoProperty(selectedNode, nameof(TrackNodeData.Position), _originalPosition);
                    undoRedo.CommitAction();
                    _selectedNodeId = -1;
                }
            }

            // target.Update();
            target.RecalculateGraph();
            target.NotifyPropertyListChanged();
            return true;
        }

        if (e is InputEventMouseMotion mouseMotion && _selectedNodeId >= 0 &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
            var localPosition = target.ToLocal(globalPosition);

            var selectedNode = target.Data.GetNode(_selectedNodeId);
            if (selectedNode is not null)
            {
                selectedNode.Position = localPosition;

                // target.Update();
                target.RecalculateGraph();
                target.NotifyPropertyListChanged();
                return true;
            }
        }

        return false;
    }
}