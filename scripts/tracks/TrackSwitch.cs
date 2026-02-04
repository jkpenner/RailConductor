using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor;

public enum SwitchRoute
{
    ARoute,
    BRoute,
}

[GlobalClass]
public partial class TrackSwitch : Interactable, ITrackGraphBuildHandler
{
    public int GraphBuildPhase => BuildPhase.Junctions;

    private TrackSegment _inSegment = null!;
    private TrackSegment _outSegmentA = null!;
    private TrackSegment _outSegmentB = null!;


    public TrackNode Node { get; private set; } = null!;
    public TrackSegment InSegment => _inSegment;
    public TrackSegment OutSegmentA => _outSegmentA;
    public TrackSegment OutSegmentB => _outSegmentB;

    [Export]
    public SwitchRoute Route { get; set; } = SwitchRoute.ARoute;

    public override void _Ready()
    {
        _inSegment = GetNode<TrackSegment>("InSegment");
        _outSegmentA = GetNode<TrackSegment>("OutSegmentA");
        _outSegmentB = GetNode<TrackSegment>("OutSegmentB");
    }

    protected override void OnInteraction() { }

    public void ToggleRoute()
    {
        SetRoute(Route switch
        {
            SwitchRoute.ARoute => SwitchRoute.BRoute,
            SwitchRoute.BRoute => SwitchRoute.ARoute,
            _ => throw new NotImplementedException()
        });
    }

    public void SetRoute(SwitchRoute route)
    {
        Route = route;
        // _inSegment?.SetIsUsable(GetActiveSegmentA() == segmentA1);
        // _outSegmentA?.SetIsUsable(GetActiveSegmentA() == segmentA2);
        // _outSegmentB?.SetIsUsable(GetActiveSegmentB() == segmentB1);
        //
        // if (route == SwitchRoute.ARoute)
        // {
        //     indicator.color = settings.SwitchNormalRouteColor;
        //     stateText.text = "N";
        // }
        // else
        // {
        //     indicator.color = settings.SwitchDivergingRouteColor;
        //     stateText.text = "D";
        // }
    }

    public void OnGraphBuildPhase(TrackGraph graph)
    {
        var key = Node.GetTrackKey();
        var node = graph.GetNode(key);
        if (node is null)
        {
            GD.PushWarning($"Track node {key} not registered");
            return;
        }

        var inSegment = graph.GetLink(
            InSegment.EndA.GetTrackKey(),
            InSegment.EndB.GetTrackKey()
        );

        var outSegmentA = graph.GetLink(
            OutSegmentA.EndA.GetTrackKey(),
            OutSegmentA.EndB.GetTrackKey()
        );

        var outSegmentB = graph.GetLink(
            OutSegmentB.EndA.GetTrackKey(),
            OutSegmentB.EndB.GetTrackKey()
        );
        
        if (inSegment is null || outSegmentA is null || outSegmentB is null)
        {
            GD.PushWarning("Failed to get associated links for switch.");
            return;
        }

        node.IsSwitch = true;

        node.ActiveIncomingLink = 0;
        node.IncomingLinks = [inSegment];

        node.ActiveOutgoingLink = 0;
        node.OutgoingLinks = [outSegmentA, outSegmentB];
    }
}