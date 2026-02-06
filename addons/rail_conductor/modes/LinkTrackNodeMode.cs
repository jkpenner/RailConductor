using Godot;

namespace RailConductor.Plugin;

public class LinkTrackNodeMode : PluginModeHandler
{
    public override int SelectedIndex => _firstSelection;

    private int _firstSelection = -1;
    private int _secondSelection = -1;

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
        if (_firstSelection == -1)
        {
            _firstSelection = target.Data.FindClosestNode(localPosition);
            return true;
        }
        
        // Select the second node.
        _secondSelection = target.Data.FindClosestNode(localPosition);
        if (_secondSelection == _firstSelection)
        {
            return false;
        }

        GD.Print($"linking nodes {_firstSelection} and {_secondSelection}");
        var node1 = target.Data.Nodes[_firstSelection];
        var node2 = target.Data.Nodes[_secondSelection];
        
        GD.Print($"linking nodes {node1.Id} and {node2.Id}");

        // Link the two nodes
        undoRedo.CreateAction("Link Track Node");
        undoRedo.AddDoMethod(node1, nameof(TrackNodeData.AddLink), node2.Id);
        undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.RemoveLink), node2.Id);
        undoRedo.AddDoMethod(node2, nameof(TrackNodeData.AddLink), node1.Id);
        undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.RemoveLink), node1.Id);
        undoRedo.CommitAction();

        // Clear the selections
        _firstSelection = -1;
        _secondSelection = -1;

        return true;
    }
}