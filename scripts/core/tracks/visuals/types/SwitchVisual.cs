using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class SwitchVisual : TrackVisual
{
    [Export]
    public Label IdText { get; set; } = null!;

    [Export]
    public Label StateText { get; set; } = null!;

    [Export]
    public Sprite2D Indicator { get; set; } = null!;

    [Export]
    public Interactable? Interactable { get; set; }

    private string _id;
    private Track? _track;
    private TrackSettings? _settings;

    public override void OnAttach(Track track, string id)
    {
        _id = id;

        if (Interactable is not null)
        {
            Interactable.Interacted += OnInteracted;
        }

        _track = track;
        _settings = track.Settings;

        var state = track.State?.GetSwitchState(id);
        if (state is null)
        {
            return;
        }

        IdText.Text = id[..3];
        state.Changed += OnStateChanged;

        OnStateChanged(state);
    }

    public override void OnDetach(Track track, string id)
    {
        if (Interactable is not null)
        {
            Interactable.Interacted -= OnInteracted;
        }

        _id = string.Empty;
        _track = null;
        _settings = null;

        var state = track.State?.GetSwitchState(id);
        if (state is null)
        {
            return;
        }

        state.Changed -= OnStateChanged;
        _settings = null;
    }

    public override void Sync(Track track, string id)
    {
        var node = track.Graph?.GetNode(id);
        if (node is null)
        {
            return;
        }

        Position = node.Position;
        SetRoute(SwitchAlignment.Normal);
    }

    private void OnInteracted()
    {
        var state = _track?.State?.GetSwitchState(_id);
        if (state is null)
        {
            return;
        }

        _track?.State?.SetSwitchAlignment(_id, state.Alignment switch
        {
            SwitchAlignment.Diverging => SwitchAlignment.Normal,
            SwitchAlignment.Normal => SwitchAlignment.Diverging,
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private void OnStateChanged(SwitchState state)
    {
        SetRoute(state.Alignment);
    }

    public void SetRoute(SwitchAlignment alignment)
    {
        if (alignment == SwitchAlignment.Normal)
        {
            Indicator.SelfModulate
                = _settings?.SwitchNormalRouteColor ?? Colors.Aqua;
            StateText.Text = "N";
        }
        else
        {
            Indicator.SelfModulate =
                _settings?.SwitchDivergingRouteColor ?? Colors.Orange;
            StateText.Text = "D";
        }
    }
}