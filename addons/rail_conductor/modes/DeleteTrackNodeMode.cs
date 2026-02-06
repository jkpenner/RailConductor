using Godot;

namespace RailConductor.Plugin;

public class DeleteTrackNodeMode : PluginModeHandler
{
    public override int SelectedIndex => -1;
    
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

        var deleteIdx = target.Data.FindClosestNode(localPosition);
        if (deleteIdx < 0)
        {
            return false;
        }

        var nodeToDelete = target.Data.Nodes[deleteIdx];
        undoRedo.CreateAction("Delete Track Node");

        foreach (var link in nodeToDelete.Links)
        {
            // Todo: Remove all links connected to this node.
        }
        
        undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveNode), deleteIdx);
        undoRedo.AddUndoMethod(target.Data, nameof(TrackData.InsertNode), deleteIdx,
            nodeToDelete);
        undoRedo.CommitAction();
        return true;
    }
}