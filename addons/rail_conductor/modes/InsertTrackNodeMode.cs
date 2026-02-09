using Godot;

namespace RailConductor.Plugin;

public class InsertTrackNodeMode : PluginModeHandler
{
    private string _hoveredLinkId = string.Empty;
    private float _hoveredLinkDistance = float.MaxValue;

    protected override bool OnGuiInput(PluginContext ctx, InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(motion.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);
            _hoveredLinkId = ctx.TrackData.FindClosestLink(localPosition);
            _hoveredLinkDistance = ctx.TrackData.GetClosestLinkDistance(localPosition);
        }

        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            if (_hoveredLinkDistance < 2f)
            {
                var link = ctx.TrackData.GetLink(_hoveredLinkId);
                if (link is null)
                {
                    return false;
                }
                
                var nodeA = ctx.TrackData.GetNode(link.NodeAId);
                var nodeB = ctx.TrackData.GetNode(link.NodeBId);

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
                
                ctx.UndoRedo.CreateAction("Insert Track Node");
                
                // Remove the old link from TrackData
                ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), link.Id);
                ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.AddLink), link.Id, link);
                
                // Remove the old link ID from the previous nodes
                ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.RemoveLink), link.Id);
                ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.AddLink), link.Id);
                ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.RemoveLink), link.Id);
                ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.AddLink), link.Id);
                
                // Add the new node and new links to TrackData
                ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddNode), newNode.Id, newNode);
                ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveNode), newNode.Id);
                ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddLink), newLink1.Id, newLink1);
                ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), newLink1.Id);
                ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddLink), newLink2.Id, newLink2);
                ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), newLink2.Id);
                
                // Add the new link IDs to the old nodes
                ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.AddLink), newLink1.Id);
                ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.RemoveLink), newLink1.Id);
                ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.AddLink), newLink2.Id);
                ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.RemoveLink), newLink2.Id);
                
                // Add the new link IDs to the new node
                ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink1.Id);
                ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink1.Id);
                ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink2.Id);
                ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink2.Id);
                
                // Update configurations (do and undo for all affected nodes)
                ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                
                ctx.UndoRedo.CommitAction();
            }
            return true;
        }
        
        return false;
    }

    // public override void OnGuiDraw(Track target, Control overlay)
    // {
    //     if (ctx.TrackData is null)
    //     {
    //         return;
    //     }
    //     
    //     var link = ctx.TrackData.GetLink(_hoveredLinkId);
    //     if (link is null)
    //     {
    //         return;
    //     }
    //     
    //     var nodeA = ctx.TrackData.GetNode(link.NodeAId);
    //     var nodeB = ctx.TrackData.GetNode(link.NodeBId);
    //
    //     if (nodeA is null || nodeB is null)
    //     {
    //         return;
    //     }
    //
    //     var globalPosition1 = ctx.Track.ToGlobal(nodeA.Position);
    //     var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);
    //
    //     var globalPosition2 = ctx.Track.ToGlobal(nodeB.Position);
    //     var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);
    //
    //     overlay.DrawLine(screenPosition1, screenPosition2,
    //         Colors.Yellow, PluginSettings.LinkWidth * PluginUtility.GetZoom());
    // }
}