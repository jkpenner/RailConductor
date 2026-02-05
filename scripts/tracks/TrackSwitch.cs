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
public partial class TrackSwitch : Interactable
{
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
}