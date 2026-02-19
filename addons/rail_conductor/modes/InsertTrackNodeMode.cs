using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Insert mode: click on a link to insert a new node in the middle and split the link.
/// 
/// Features:
/// • Hover any link → live yellow dashed preview + ghost node at midpoint
/// • Left-click → inserts node + creates two new links (undoable in one action)
/// • Right-click or Escape → exits mode
/// • Consistent with all other modes (OnEnable/OnDisable, restrictions, preview drawing)
/// </summary>
public class InsertTrackNodeMode : PluginModeHandler
{
    private string _hoveredLinkId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        RestrictTo(SelectionType.Link, ctx);
        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ResetRestrictions(ctx);             
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                UpdateHover(ctx, motion.Position);
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                TryInsertNode(ctx);
                RequestOverlayUpdate();
                return true;

            // Exit mode with Right-click or Escape
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                return true; // mode switcher will handle exit
        }

        return false;
    }

    private void UpdateHover(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);
        _hoveredLinkId = ctx.TrackData.FindClosestLink(localPos);
    }

    private void TryInsertNode(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_hoveredLinkId) || ctx.UndoRedo is null)
            return;

        var link = ctx.TrackData.GetLink(_hoveredLinkId);
        if (link is null) return;

        var nodeA = ctx.TrackData.GetNode(link.NodeAId);
        var nodeB = ctx.TrackData.GetNode(link.NodeBId);
        if (nodeA is null || nodeB is null) return;

        var newNode = new TrackNodeData
        {
            Position = PluginUtility.SnapPosition(nodeA.Position.Lerp(nodeB.Position, 0.5f))
        };

        var newLink1 = new TrackLinkData { NodeAId = nodeA.Id, NodeBId = newNode.Id };
        var newLink2 = new TrackLinkData { NodeAId = nodeB.Id, NodeBId = newNode.Id };

        ctx.UndoRedo.CreateAction("Insert Track Node");

        // Remove old link
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.AddLink), link.Id, link);

        // Remove old link ID from nodes
        ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.RemoveLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.AddLink), link.Id);
        ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.RemoveLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.AddLink), link.Id);

        // Add new node + links
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddNode), newNode.Id, newNode);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveNode), newNode.Id);
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddLink), newLink1.Id, newLink1);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), newLink1.Id);
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddLink), newLink2.Id, newLink2);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), newLink2.Id);

        // Add new link IDs to nodes
        ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.AddLink), newLink1.Id);
        ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.RemoveLink), newLink1.Id);
        ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.AddLink), newLink2.Id);
        ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.RemoveLink), newLink2.Id);
        ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink1.Id);
        ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink1.Id);
        ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.AddLink), newLink2.Id);
        ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.RemoveLink), newLink2.Id);

        // Update configurations
        ctx.UndoRedo.AddDoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(newNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);

        ctx.UndoRedo.CommitAction();
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_hoveredLinkId)) return;

        var link = ctx.TrackData.GetLink(_hoveredLinkId);
        if (link is null) return;

        var nodeA = ctx.TrackData.GetNode(link.NodeAId);
        var nodeB = ctx.TrackData.GetNode(link.NodeBId);
        if (nodeA is null || nodeB is null) return;

        var p1 = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(nodeA.Position));
        var p2 = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(nodeB.Position));

        // Live preview: yellow dashed line + ghost node
        overlay.DrawDashedLine(p1, p2, Colors.Yellow, width: 6f, dash: 8f);

        var mid = nodeA.Position.Lerp(nodeB.Position, 0.5f);
        var ghostScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(mid));
        overlay.DrawCircle(ghostScreen, PluginSettings.NodeRadius * PluginUtility.GetZoom(), Colors.Yellow);
    }
}