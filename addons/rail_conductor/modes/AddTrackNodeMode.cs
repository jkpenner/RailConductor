using Godot;

namespace RailConductor.Plugin;

public class AddTrackNodeMode : PluginModeHandler
{
    public override int SelectedIndex => _selectedIndex;
    
    private int _selectedIndex;
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
                    Id = target.Data.GetAvailableId(),
                    Position = localPosition
                };
                _selectedIndex = target.Data.Nodes.Count;
                _originalPosition = localPosition;
                
                undoRedo.CreateAction("Add Track Node");
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddNode), newNode);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveNode), _selectedIndex);
                undoRedo.CommitAction();
                return true;
            }
            else
            {
                var finalPos = target.Data.Nodes[_selectedIndex].Position;
                var nodeRef = target.Data.Nodes[_selectedIndex]; // Reference for closure
                
                undoRedo.CreateAction("Move Track Node");
                undoRedo.AddDoProperty(nodeRef, nameof(TrackNodeData.Position), finalPos);
                undoRedo.AddUndoProperty(nodeRef, nameof(TrackNodeData.Position), _originalPosition);
                undoRedo.CommitAction();
                _selectedIndex = -1;
                return true;
            }
        }

        if (e is InputEventMouseMotion mouseMotion && _selectedIndex >= 0 &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (_selectedIndex >= 0)
            {
                target.Data.Nodes[_selectedIndex].Position = localPosition;
                target.RecalculateGraph();
                target.NotifyPropertyListChanged();
                return true;
            }
        }

        return false;
    }
}