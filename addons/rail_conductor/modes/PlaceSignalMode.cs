using System;
using Godot;

namespace RailConductor.Plugin;

public class PlaceSignalMode : PluginModeHandler
{
    private string _hoveredLinkId = string.Empty;
    private float _hoveredLinkDistance = float.MaxValue;
    private string _selectedLinkId = string.Empty;
    private Phase _currentPhase = Phase.LinkSelect;

    private enum Phase
    {
        LinkSelect,
        NodeSelect,
    }

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

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            // Return back to the previous phase on right click
            if (_currentPhase == Phase.NodeSelect)
            {
                _currentPhase = Phase.LinkSelect;
                _selectedLinkId = string.Empty;
                ctx.ClearSelection();
                ctx.ResetSelectRestrictions();
                ctx.RestrictSelectionType(SelectionType.Link);
                return true;
            }
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            switch (_currentPhase)
            {
                case Phase.LinkSelect:
                    HandleLinkSelectPhase(ctx);
                    break;
                case Phase.NodeSelect:
                    HandleNodeSelectPhase(ctx, localPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        return false;
    }

    private void HandleLinkSelectPhase(PluginContext ctx)
    {
        if (_hoveredLinkDistance >= 20f || string.IsNullOrEmpty(_hoveredLinkId))
        {
            return;
        }

        var link = ctx.TrackData.GetLink(_hoveredLinkId);
        if (link is null)
        {
            return;
        }

        _currentPhase = Phase.NodeSelect;
        _selectedLinkId = _hoveredLinkId;
        ctx.SelectOnly(_selectedLinkId);
        ctx.RestrictSelectionType(SelectionType.Node);
        ctx.AddSelectableObject(link.NodeAId);
        ctx.AddSelectableObject(link.NodeBId);
        RequestOverlayUpdate();
    }

    private void HandleNodeSelectPhase(PluginContext ctx, Vector2 localPosition)
    {
        var closestNodeId = ctx.TrackData.FindClosestNodeId(localPosition);
        if (string.IsNullOrEmpty(closestNodeId))
        {
            _selectedLinkId = string.Empty;
            return;
        }

        var link = ctx.TrackData.GetLink(_selectedLinkId);
        if (link is null)
        {
            _selectedLinkId = string.Empty;
            return;
        }

        if (closestNodeId != link.NodeAId && closestNodeId != link.NodeBId)
        {
            _selectedLinkId = string.Empty;
            return;
        }

        var newSignal = new TrackSignalData
        {
            LinkId = _selectedLinkId,
            DirectionNodeId = closestNodeId
        };

        TrackEditorActions.AddTrackSignal(ctx.TrackData, newSignal, ctx.UndoRedo);

        _currentPhase = Phase.LinkSelect;
        _selectedLinkId = string.Empty;
        ctx.SelectOnly(newSignal.Id);
        ctx.ResetSelectRestrictions();
        ctx.RestrictSelectionType(SelectionType.Link);
        RequestOverlayUpdate();
    }
}