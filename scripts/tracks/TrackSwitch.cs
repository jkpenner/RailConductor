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
    public TrackGraph? Graph { get; set; }
    public TrackGraphNode? Node { get; set; }

    private TrackSegment _inSegment = null!;
    private TrackSegment _outSegmentA = null!;
    private TrackSegment _outSegmentB = null!;

    public TrackSegment InSegment => _inSegment;
    public TrackSegment OutSegmentA => _outSegmentA;
    public TrackSegment OutSegmentB => _outSegmentB;

    [Export]
    public SwitchRoute Route { get; set; } = SwitchRoute.ARoute;

    [Export]
    public Label IdText { get; set; } = null!;

    [Export]
    public Label StateText { get; set; } = null!;

    [Export]
    public Sprite2D Indicator { get; set; } = null!;

    public override void _Ready()
    {
        _inSegment = GetNode<TrackSegment>("InSegment");
        _outSegmentA = GetNode<TrackSegment>("OutSegmentA");
        _outSegmentB = GetNode<TrackSegment>("OutSegmentB");
        
        SetRoute(Route);
    }

    protected override void OnInteraction()
    {
        ToggleRoute();
        GD.Print($"Toggling Switch {Node?.Id ?? 0} to {Route}");
    }

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

        if (Node is not null)
        {
            // Toggle to the next link
            Node.ActiveIncomingLink = (Node.ActiveIncomingLink + 1) % Node.IncomingLinks.Length;
            Node.ActiveOutgoingLink = (Node.ActiveOutgoingLink + 1) % Node.OutgoingLinks.Length;
            Node.RebuildConnections();
        }

        if (route == SwitchRoute.ARoute)
        {
            Indicator.SelfModulate = Colors.Aqua; // settings.SwitchNormalRouteColor;
            StateText.Text = "N";
        }
        else
        {
            Indicator.SelfModulate = Colors.Orange; // settings.SwitchNormalRouteColor;
            StateText.Text = "D";
        }
    }
}