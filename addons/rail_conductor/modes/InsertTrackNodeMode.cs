using Godot;

namespace RailConductor.Plugin;

public class InsertTrackNodeMode : PluginModeHandler
{
    public override string[] SelectedNodeId => [];

    private string _hoveredLinkId = string.Empty;
    private float _hoveredLinkDistance = float.MaxValue;

    public override bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        if (target.Data is null)
        {
            return false;
        }

        if (e is InputEventMouseMotion motion)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(motion.Position);
            var localPosition = target.ToLocal(globalPosition);
            _hoveredLinkId = target.Data.FindClosestLink(localPosition);
            _hoveredLinkDistance = target.Data.GetClosestLinkDistance(localPosition);
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (_hoveredLinkDistance < 2f)
            {
                var link = target.Data.GetLink(_hoveredLinkId);
                if (link is null)
                {
                    return false;
                }
                
                var nodeA = target.Data.GetNode(link.NodeAId);
                var nodeB = target.Data.GetNode(link.NodeBId);

                if (nodeA is null || nodeB is null)
                {
                    return false;
                }
                
                var newNode = new TrackNodeData
                {
                    Position = PluginUtility.SnapPosition(nodeA.Position.Lerp(nodeB.Position, 0.5f)),
                };

                var newLink1 = new TrackLinkData
                {
                    NodeAId = nodeA.Id,
                    NodeBId = newNode.Id,
                };

                var newLink2 = new TrackLinkData
                {
                    NodeAId = nodeB.Id,
                    NodeBId = newNode.Id,
                };
                
                undoRedo.CreateAction("Insert Track Node");
                
                // Remove the old link from TrackData
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.RemoveLink), link.Id);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.AddLink), link.Id, link);
                
                // Remove the old link ID from the previous nodes
                undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.RemoveLink), link.Id);
                undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.AddLink), link.Id);
                undoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.RemoveLink), link.Id);
                undoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.AddLink), link.Id);
                
                // Add the new node and new links to TrackData
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddNode), newNode.Id, newNode);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveNode), newNode.Id);
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddLink), newLink1.Id, newLink1);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveLink), newLink1.Id);
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddLink), newLink2.Id, newLink2);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveLink), newLink2.Id);
                
                // Add the new link IDs to the old nodes
                undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.AddLink), newLink1.Id);
                undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.RemoveLink), newLink1.Id);
                undoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.AddLink), newLink2.Id);
                undoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.RemoveLink), newLink2.Id);
                
                // Add the new link IDs to the new node
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink1.Id);
                undoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink1.Id);
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink2.Id);
                undoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink2.Id);
                
                // Update configurations (do and undo for all affected nodes)
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                undoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), target.Data);
                
                undoRedo.CommitAction();
            }
            return true;
        }
        
        return false;
    }

    public override void OnGuiDraw(Track target, Control overlay)
    {
        if (target.Data is null)
        {
            return;
        }
        
        var link = target.Data.GetLink(_hoveredLinkId);
        if (link is null)
        {
            return;
        }
        
        var nodeA = target.Data.GetNode(link.NodeAId);
        var nodeB = target.Data.GetNode(link.NodeBId);

        if (nodeA is null || nodeB is null)
        {
            return;
        }

        var globalPosition1 = target.ToGlobal(nodeA.Position);
        var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);

        var globalPosition2 = target.ToGlobal(nodeB.Position);
        var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);

        overlay.DrawLine(screenPosition1, screenPosition2,
            Colors.Yellow, PluginSettings.LinkWidth * PluginUtility.GetZoom());
    }
}