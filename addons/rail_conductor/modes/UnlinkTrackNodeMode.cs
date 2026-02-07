using Godot;

namespace RailConductor.Plugin;

public class UnlinkTrackNodeMode : PluginModeHandler
{
    public override int[] SelectedNodeId => [_selectedNodeId1];

    private int _selectedNodeId1 = -1;
    private int _selectedNodeId2 = -1;

    public override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
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

        // Select the first node 
        if (_selectedNodeId1 == -1)
        {
            _selectedNodeId1 = target.Data.FindClosestNodeId(localPosition);
            return true;
        }
        
        // Select the second node.
        _selectedNodeId2 = target.Data.FindClosestNodeId(localPosition);
        if (_selectedNodeId2 == _selectedNodeId1)
        {
            return false;
        }
        
        var node1 = target.Data.GetNode(_selectedNodeId1);
        var node2 = target.Data.GetNode(_selectedNodeId2);

        if (node1 is null || node2 is null || !node1.Links.Contains(_selectedNodeId2) || !node2.Links.Contains(_selectedNodeId1))
        {
            _selectedNodeId1 = -1;
            _selectedNodeId2 = -1;
            return false;
        }

        // Link the two nodes
        undoRedo.CreateAction("Link Track Node");
        undoRedo.AddDoMethod(node1, nameof(TrackNodeData.RemoveLink), _selectedNodeId2);
        undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.AddLink), _selectedNodeId2);
        undoRedo.AddDoMethod(node2, nameof(TrackNodeData.RemoveLink), _selectedNodeId1);
        undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.AddLink), _selectedNodeId1);
        undoRedo.CommitAction();

        // Clear the selections
        _selectedNodeId1 = -1;
        _selectedNodeId2 = -1;

        return true;
    }
}