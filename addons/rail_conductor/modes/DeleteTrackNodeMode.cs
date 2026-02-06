using Godot;

namespace RailConductor.Plugin;

public class DeleteTrackNodeMode : PluginModeHandler
{
    public override int SelectedNodeId => -1;

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

        var deletedId = target.Data.FindClosestNodeId(localPosition);
        if (deletedId < 0)
        {
            return false;
        }

        var nodeToDelete = target.Data.GetNode(deletedId);
        if (nodeToDelete is null)
        {
            return false;
        }

        undoRedo.CreateAction("Delete Track Node");

        // Remove the links from any linked node.
        foreach (var linkedNodeId in nodeToDelete.Links)
        {
            var linkedNode = target.Data.GetNode(linkedNodeId);
            if (linkedNode is null)
            {
                continue;
            }
            
            undoRedo.AddDoMethod(linkedNode, nameof(TrackNodeData.RemoveLink), deletedId);
            undoRedo.AddUndoMethod(linkedNode, nameof(TrackNodeData.AddLink), deletedId);
        }

        undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveNode), deletedId);
        undoRedo.AddUndoMethod(target.Data, nameof(TrackData.AddNode), deletedId, nodeToDelete);
        undoRedo.CommitAction();
        return true;
    }
}