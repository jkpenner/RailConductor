using Godot;

namespace RailConductor.Plugin;

public class DeleteTrackNodeMode : PluginModeHandler
{
    public override string[] SelectedNodeId => [];

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
        if (string.IsNullOrEmpty(deletedId))
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
        foreach (var linkId in nodeToDelete.Links)
        {
            var link = target.Data.GetLink(linkId);
            if (link is null)
            {
                continue;
            }
            
            // Remove the connected link
            undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveLink), linkId);
            undoRedo.AddUndoMethod(target.Data, nameof(TrackData.AddLink), linkId, link);
            
            var linkedNode = target.Data.GetNode(link.GetOtherNode(deletedId));
            if (linkedNode is null)
            {
                continue;
            }
            
            // Remove the link from the connected node
            undoRedo.AddDoMethod(linkedNode, nameof(TrackNodeData.RemoveLink), linkId);
            undoRedo.AddDoMethod(linkedNode, nameof(TrackNodeData.UpdateConfiguration), target.Data);
            
            undoRedo.AddUndoMethod(linkedNode, nameof(TrackNodeData.AddLink), linkId);
            undoRedo.AddUndoMethod(linkedNode, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        }

        undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveNode), deletedId);
        undoRedo.AddUndoMethod(target.Data, nameof(TrackData.AddNode), deletedId, nodeToDelete);
        undoRedo.AddUndoMethod(nodeToDelete, nameof(TrackNodeData.UpdateConfiguration), target.Data);
        undoRedo.CommitAction();
        return true;
    }
}