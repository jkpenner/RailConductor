using Godot;

namespace RailConductor.Plugin;

public class InsertTrackNodeMode : PluginModeHandler
{
    public override int[] SelectedNodeId => [];

    private (int, int) _hoveredLink = (-1, -1);
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
            _hoveredLink = target.Data.FindClosestLink(localPosition);
            _hoveredLinkDistance = target.Data.GetClosestLinkDistance(localPosition);
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = target.ToLocal(globalPosition);

            if (_hoveredLinkDistance < 2f)
            {
                var (nodeId1, nodeId2) = _hoveredLink;
                var node1 = target.Data.GetNode(nodeId1);
                var node2 = target.Data.GetNode(nodeId2);

                if (node1 is null || node2 is null)
                {
                    return false;
                }
                
                var newNode = new TrackNodeData
                {
                    Id = target.Data.GetAvailableNodeId(),
                    Position = PluginUtility.SnapPosition(node1.Position.Lerp(node2.Position, 0.5f)),
                    Links = [_hoveredLink.Item1, _hoveredLink.Item2]
                };
                
                undoRedo.CreateAction("Insert Track Node");
                
                // Add the New Node
                undoRedo.AddDoMethod(target.Data, nameof(TrackData.AddNode), newNode.Id, newNode);
                undoRedo.AddUndoMethod(target.Data, nameof(TrackData.RemoveNode), newNode.Id);
                
                // Remove link between the last two nodes
                undoRedo.AddDoMethod(node1, nameof(TrackNodeData.RemoveLink), nodeId2);
                undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.AddLink), nodeId2);
                undoRedo.AddDoMethod(node2, nameof(TrackNodeData.RemoveLink), nodeId1);
                undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.AddLink), nodeId1);
                
                // Link the old nodes to the new node
                undoRedo.AddDoMethod(node1, nameof(TrackNodeData.AddLink), newNode.Id);
                undoRedo.AddUndoMethod(node1, nameof(TrackNodeData.RemoveLink), newNode.Id);
                undoRedo.AddDoMethod(node2, nameof(TrackNodeData.AddLink), newNode.Id);
                undoRedo.AddUndoMethod(node2, nameof(TrackNodeData.RemoveLink), newNode.Id);
                
                // Link the new node to the old nodes
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), nodeId1);
                undoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), nodeId1);
                undoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), nodeId2);
                undoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), nodeId2);
                
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
        
        var (node1Id, node2Id) = _hoveredLink;
        var node1 = target.Data.GetNode(node1Id);
        var node2 = target.Data.GetNode(node2Id);

        if (node1 is null || node2 is null)
        {
            return;
        }

        var globalPosition1 = target.ToGlobal(node1.Position);
        var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);

        var globalPosition2 = target.ToGlobal(node2.Position);
        var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);

        overlay.DrawLine(screenPosition1, screenPosition2,
            Colors.Yellow, PluginSettings.LinkWidth * PluginUtility.GetZoom());
    }
}