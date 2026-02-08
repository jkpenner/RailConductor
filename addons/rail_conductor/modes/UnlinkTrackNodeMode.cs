using Godot;

namespace RailConductor.Plugin;

public class UnlinkTrackNodeMode : PluginModeHandler
{
    public override string[] SelectedNodeId => [_selectedNodeId1];

    private string _selectedNodeId1 = string.Empty;
    private string _selectedNodeId2 = string.Empty;

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
        if (string.IsNullOrEmpty(_selectedNodeId1))
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
        var link = target.Data.GetConnectingLink(_selectedNodeId1, _selectedNodeId2);

        if (node1 is null || node2 is null || link is null)
        {
            _selectedNodeId1 = string.Empty;
            _selectedNodeId2 = string.Empty;
            return false;
        }

        // Link the two nodes
        undoRedo.CreateAction("Link Track Node");
        
        undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveLink), link.Id);
        undoRedo.AddUndoMethod(target.Data, nameof(TrackData.AddLink), link.Id, link);
        
        undoRedo.AddDoMethod(node1, nameof(TrackNodeData.RemoveLink), link.Id);
        undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.AddLink), link.Id);
        
        undoRedo.AddDoMethod(node2, nameof(TrackNodeData.RemoveLink), link.Id);
        undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.AddLink), link.Id);
        
        undoRedo.AddDoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        undoRedo.AddDoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        undoRedo.CommitAction();

        // Clear the selections
        _selectedNodeId1 = string.Empty;
        _selectedNodeId2 = string.Empty;

        return true;
    }
}