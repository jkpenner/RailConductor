using Godot;

namespace RailConductor.Plugin.modes;

public class MoveTrackNodeMode : PluginModeHandler
{
    public override int SelectedIndex => _selectedIndex;
    
    private int _selectedIndex = -1;
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
                _selectedIndex = target.Data.FindClosestNode(localPosition);
                if (_selectedIndex >= 0)
                {
                    _originalPosition = target.Data.Nodes[_selectedIndex].Position;
                }
            }
            else // Released
            {
                if (_selectedIndex >= 0)
                {
                    var finalPos = target.Data.Nodes[_selectedIndex].Position;
                    var nodeRef = target.Data.Nodes[_selectedIndex]; // Reference for closure

                    undoRedo.CreateAction("Move Track Node");
                    undoRedo.AddDoProperty(nodeRef, nameof(TrackNodeData.Position), finalPos);
                    undoRedo.AddUndoProperty(nodeRef, nameof(TrackNodeData.Position), _originalPosition);
                    undoRedo.CommitAction();
                    _selectedIndex = -1;
                }
            }

            // target.Update();
            target.RecalculateGraph();
            target.NotifyPropertyListChanged();
            return true;
        }

        if (e is InputEventMouseMotion mouseMotion && _selectedIndex >= 0 &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (_selectedIndex >= 0)
            {
                target.Data.Nodes[_selectedIndex].Position = localPosition;

                // target.Update();
                target.RecalculateGraph();
                target.NotifyPropertyListChanged();
                return true;
            }
        }

        return false;
    }
}