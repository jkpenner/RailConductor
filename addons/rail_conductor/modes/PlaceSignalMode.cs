using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Placement mode for Signals (stamp / multi-placement style).
/// 
/// Behaviour:
/// • Hover any link → live ghost preview (auto-faces the closer node)
/// • Left-click → places the signal instantly + automatically selects it
/// • You can immediately hover and click again to place another signal (restrictions stay Link-only)
/// • Right-click or Escape → exits the mode and returns to normal selection
/// 
/// Fixes the "restrictions are cleared" bug by re-applying the Link restriction after every placement.
/// </summary>
public class PlaceSignalMode : PluginModeHandler
{
    private string _hoveredLinkId = string.Empty;
    private string _previewDirectionNodeId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        ctx.RestrictSelectionType(SelectionType.Link);
        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ctx.ResetSelectRestrictions();
        ctx.ClearSelection();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                UpdatePreview(ctx, motion.Position);
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                PlaceSignal(ctx);
                RequestOverlayUpdate();
                return true;

            // Exit placement mode
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                return true; // mode switcher will handle the actual mode change
        }

        return false;
    }

    private void UpdatePreview(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPos = ctx.Track.ToLocal(globalPos);

        _hoveredLinkId = ctx.TrackData.FindClosestLink(localPos);
        _previewDirectionNodeId = string.Empty;

        if (string.IsNullOrEmpty(_hoveredLinkId))
            return;

        var link = ctx.TrackData.GetLink(_hoveredLinkId);
        if (link is null) return;

        var nodeA = ctx.TrackData.GetNode(link.NodeAId);
        var nodeB = ctx.TrackData.GetNode(link.NodeBId);
        if (nodeA is null || nodeB is null) return;

        // Choose the node that is closer to the mouse
        var closestPoint = Geometry2D.GetClosestPointToSegment(localPos, nodeA.Position, nodeB.Position);
        var distA = closestPoint.DistanceTo(nodeA.Position);
        var distB = closestPoint.DistanceTo(nodeB.Position);

        _previewDirectionNodeId = distA <= distB ? link.NodeAId : link.NodeBId;
    }

    private void PlaceSignal(PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_hoveredLinkId) ||
            string.IsNullOrEmpty(_previewDirectionNodeId) ||
            ctx.UndoRedo is null)
            return;

        var newSignal = new SignalData
        {
            LinkId = _hoveredLinkId,
            DirectionNodeId = _previewDirectionNodeId
        };

        TrackEditorActions.AddTrackSignal(ctx.TrackData, newSignal, ctx.UndoRedo);

        ctx.SelectOnly(newSignal.Id);

        // CRITICAL FIX: Reset then immediately re-restrict so we can keep placing more signals
        ctx.ResetSelectRestrictions();
        ctx.RestrictSelectionType(SelectionType.Link);
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        if (string.IsNullOrEmpty(_hoveredLinkId) || string.IsNullOrEmpty(_previewDirectionNodeId))
            return;

        var previewSignal = new SignalData
        {
            LinkId = _hoveredLinkId,
            DirectionNodeId = _previewDirectionNodeId
        };

        TrackEditorDrawer.DrawTrackSignal(overlay, ctx, previewSignal);
    }
}