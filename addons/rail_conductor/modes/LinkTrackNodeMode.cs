using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Mode for linking track nodes together with easy chaining and live preview line.
/// 
/// Behaviours:
/// • Click first node → becomes the "start" of the chain
/// • Hover any other valid node → shows a dashed preview line
/// • Click the hovered node → link is created + chaining continues
/// • Right-click or Escape → clears the current chain
/// </summary>
public class LinkTrackNodeMode : PluginModeHandler
{
    private string _startNodeId = string.Empty;
    private string _hoveredNodeId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        ctx.RestrictSelectionType(SelectionType.Node);
        ResetChain();
        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ctx.ResetSelectRestrictions();
        ctx.ClearSelection();
        ResetChain();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                UpdateHoverPreview(ctx, motion.Position);
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                HandleLeftClick(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                if (!string.IsNullOrEmpty(_startNodeId))
                {
                    CancelCurrentChain(ctx);
                    RequestOverlayUpdate();
                    return true;
                }
                break;
        }

        return false;
    }

    // ========================================================================
    // HOVER PREVIEW
    // ========================================================================

    private void UpdateHoverPreview(PluginContext ctx, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(_startNodeId))
        {
            _hoveredNodeId = string.Empty;
            return;
        }

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        var candidateId = ctx.TrackData.FindClosestNodeId(localPos);

        if (string.IsNullOrEmpty(candidateId) ||
            candidateId == _startNodeId ||
            ctx.TrackData.IsLinked(_startNodeId, candidateId))
        {
            _hoveredNodeId = string.Empty;
            return;
        }

        _hoveredNodeId = candidateId;
    }

    // ========================================================================
    // LEFT CLICK + CHAINING (unchanged)
    // ========================================================================

    private void HandleLeftClick(PluginContext ctx, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(_startNodeId))
        {
            var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
            var localPos = ctx.Track.ToLocal(globalPos);

            _startNodeId = ctx.TrackData.FindClosestNodeId(localPos);
            if (!string.IsNullOrEmpty(_startNodeId))
                ctx.SelectOnly(_startNodeId);

            _hoveredNodeId = string.Empty;
            return;
        }

        if (string.IsNullOrEmpty(_hoveredNodeId)) return;

        var node1 = ctx.TrackData.GetNode(_startNodeId);
        var node2 = ctx.TrackData.GetNode(_hoveredNodeId);

        if (node1 is null || node2 is null) return;

        CreateLink(ctx, node1, node2);

        _startNodeId = _hoveredNodeId;
        ctx.SelectOnly(_startNodeId);
        _hoveredNodeId = string.Empty;
    }

    private void CreateLink(PluginContext ctx, TrackNodeData node1, TrackNodeData node2)
    {
        if (ctx.UndoRedo is null) return;

        var link = new TrackLinkData { NodeAId = node1.Id, NodeBId = node2.Id };

        ctx.UndoRedo.CreateAction("Link Track Nodes");

        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddLink), link.Id, link);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemoveLink), link.Id);

        ctx.UndoRedo.AddDoMethod(node1, nameof(TrackNodeData.AddLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(node1, nameof(TrackNodeData.RemoveLink), link.Id);
        ctx.UndoRedo.AddDoMethod(node2, nameof(TrackNodeData.AddLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(node2, nameof(TrackNodeData.RemoveLink), link.Id);

        ctx.UndoRedo.AddDoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddDoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);

        ctx.UndoRedo.CommitAction();
    }

    private void CancelCurrentChain(PluginContext ctx)
    {
        _startNodeId = string.Empty;
        _hoveredNodeId = string.Empty;
        ctx.ClearSelection();
    }

    private void ResetChain()
    {
        _startNodeId = string.Empty;
        _hoveredNodeId = string.Empty;
    }

    // ========================================================================
    // DRAW PREVIEW LINE – Godot 4.6 compatible
    // ========================================================================

    public override void Draw(Control overlay, PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_startNodeId) || string.IsNullOrEmpty(_hoveredNodeId))
            return;

        var startNode = ctx.TrackData.GetNode(_startNodeId);
        var hoverNode = ctx.TrackData.GetNode(_hoveredNodeId);

        if (startNode is null || hoverNode is null)
            return;

        var p1 = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(startNode.Position));
        var p2 = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(hoverNode.Position));

        // Godot 4.6: DrawDashedLine has NO "gap" parameter
        var previewColor = new Color(0.4f, 0.85f, 1f, 0.7f);
        overlay.DrawDashedLine(p1, p2, previewColor, width: 4f, dash: 10f);
    }
}