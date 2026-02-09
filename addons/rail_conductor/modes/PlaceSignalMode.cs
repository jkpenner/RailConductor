using Godot;

namespace RailConductor.Plugin;

public class PlaceSignalMode : PluginModeHandler
{
    private string _hoveredLinkId = string.Empty;
    private float _hoveredLinkDistance = float.MaxValue;
    private string _selectedLinkId = string.Empty;

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            if (string.IsNullOrEmpty(_selectedLinkId))
            {
                var globalPosition = PluginUtility.ScreenToWorldSnapped(motion.Position);
                var localPosition = ctx.Track.ToLocal(globalPosition);
                _hoveredLinkId = ctx.TrackData.FindClosestLink(localPosition);
                _hoveredLinkDistance = ctx.TrackData.GetClosestLinkDistance(localPosition);
            }
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            if (string.IsNullOrEmpty(_selectedLinkId))
            {
                if (_hoveredLinkDistance < 20f && !string.IsNullOrEmpty(_hoveredLinkId))
                {
                    _selectedLinkId = _hoveredLinkId;
                    return true;
                }
            }
            else
            {
                var closestNodeId = ctx.TrackData.FindClosestNodeId(localPosition);
                if (string.IsNullOrEmpty(closestNodeId))
                {
                    _selectedLinkId = string.Empty;
                    return false;
                }

                var link = ctx.TrackData.GetLink(_selectedLinkId);
                if (link is null)
                {
                    _selectedLinkId = string.Empty;
                    return false;
                }

                if (closestNodeId != link.NodeAId && closestNodeId != link.NodeBId)
                {
                    _selectedLinkId = string.Empty;
                    return false;
                }

                var newSignal = new TrackSignalData
                {
                    LinkId = _selectedLinkId,
                    DirectionNodeId = closestNodeId
                };

                ctx.UndoRedo.CreateAction("Place Signal");

                ctx.UndoRedo.AddDoMethod(ctx.Track.Data, nameof(TrackData.AddSignal), newSignal.Id, newSignal);
                ctx.UndoRedo.AddUndoMethod(ctx.Track.Data, nameof(TrackData.RemoveSignal), newSignal.Id);

                ctx.UndoRedo.CommitAction();

                _selectedLinkId = string.Empty;
                return true;
            }
        }

        return false;
    }
    //
    // protected override void OnGuiDraw(Track target, Control overlay)
    // {
    //     if (ctx.Track.Data is null)
    //     {
    //         return;
    //     }
    //
    //     var drawLinkId = string.IsNullOrEmpty(_selectedLinkId) ? _hoveredLinkId : _selectedLinkId;
    //     var drawColor = string.IsNullOrEmpty(_selectedLinkId) ? Colors.Yellow : Colors.Cyan;
    //
    //     if (string.IsNullOrEmpty(drawLinkId))
    //     {
    //         return;
    //     }
    //
    //     var link = ctx.TrackData.GetLink(drawLinkId);
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
    //         drawColor, PluginSettings.LinkWidth * PluginUtility.GetZoom());
    // }
}