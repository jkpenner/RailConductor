using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Uses the real SignalRoutePanel.tscn scene attached to the editor overlay.
/// The panel is fixed, zoom-independent, and always above the world.
/// World highlights (switch N/R labels + dashed target link) are still drawn.
///
/// FIXED: Panel is now created ONCE and reused across mode entries/exits.
///        Only fully destroyed on plugin unload (RailConductorPlugin.Cleanup).
///        This eliminates the re-entry bug where second+ signal selection
///        was treated as "nothing selected".
/// </summary>
public class EditSignalRoutesMode : PluginModeHandler
{
    private SignalRoutePanel _panel;
    private Control _currentOverlay;
    private string _activeSignalId = string.Empty;
    private string _hoveredLinkId = string.Empty;

    protected override void OnEnable(PluginContext ctx)
    {
        RestrictTo(SelectionType.Signal, ctx);
        Reset();                    // clear state only – do NOT destroy panel

        EnsurePanelCreated();

        _panel.ActiveSelectionChanged += OnPanelSelectionChanged;

        // Make sure panel is parented to current overlay
        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ResetRestrictions(ctx);
        Reset();                    // clear state + hide panel

        if (_panel != null)
        {
            _panel.ActiveSelectionChanged -= OnPanelSelectionChanged;
        }
        // DO NOT call Cleanup() here – panel survives mode switches
    }

    /// <summary>
    /// Called ONLY by RailConductorPlugin.Cleanup() on full plugin unload/recompile.
    /// </summary>
    public void Cleanup()
    {
        if (_panel != null)
        {
            _panel.ActiveSelectionChanged -= OnPanelSelectionChanged;

            if (_panel.GetParent() != null)
            {
                _panel.GetParent().RemoveChild(_panel);
            }

            _panel.QueueFree();
            _panel = null;
        }
    }
    
    /// <summary>
    /// Called by RailConductorPlugin when switching scenes or losing the current Track.
    /// Ensures the panel never sticks around after scene change.
    /// </summary>
    public void HidePanel()
    {
        if (_panel != null)
        {
            _panel.Clear();
            _panel.Visible = false;

            if (_panel.GetParent() != null)
                _panel.GetParent().RemoveChild(_panel);
        }
        Reset();
    }

    private void EnsurePanelCreated()
    {
        if (_panel != null)
            return;

        var packed = ResourceLoader.Load<PackedScene>("res://addons/rail_conductor/scenes/SignalRoutePanel.tscn");
        _panel = packed.Instantiate<SignalRoutePanel>();
        _panel.Position = new Vector2(30, 30);
        _panel.Visible = false;
    }

    private void OnPanelSelectionChanged()
    {
        RequestOverlayUpdate();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                UpdateHover(ctx, motion.Position);
                RequestOverlayUpdate();
                return false;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn:
                HandleLeftClick(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } btn:
                HandleRightClick(ctx, btn.Position);
                RequestOverlayUpdate();
                return true;

            case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                Reset(ctx);
                RequestOverlayUpdate();
                return true;
        }
        return false;
    }

    private void UpdateHover(PluginContext ctx, Vector2 screenPos)
    {
        if (string.IsNullOrEmpty(_activeSignalId)) return;
        var global = PluginUtility.ScreenToWorldSnapped(screenPos);
        var local = ctx.Track.ToLocal(global);
        _hoveredLinkId = ctx.TrackData.FindClosestLink(local);
    }

    private void HandleLeftClick(PluginContext ctx, Vector2 screenPos)
    {
        var id = GetClosestId(ctx.Track, screenPos);

        if (ctx.TrackData.IsSignalId(id))
        {
            _activeSignalId = id;
            ctx.SelectOnly(id);
            _panel.SetSignal(ctx.TrackData.GetSignal(id), ctx);
            _panel.Visible = true;
            return;
        }

        if (_activeSignalId != string.Empty)
        {
            var route = _panel.GetActiveRoute();
            if (route == null) return;

            if (ctx.TrackData.IsNodeId(id))
            {
                var node = ctx.TrackData.GetNode(id);
                if (node?.NodeType == TrackNodeType.Switch)
                    TrackEditorActions.SetRouteSwitchAlignment(ctx, route, id, SwitchAlignment.Normal);
            }
            else if (ctx.TrackData.IsLinkId(id))
            {
                TrackEditorActions.SetRouteTargetLink(ctx, route, id);
            }
        }
    }

    private void HandleRightClick(PluginContext ctx, Vector2 screenPos)
    {
        if (_activeSignalId == string.Empty) return;

        var id = GetClosestId(ctx.Track, screenPos);
        var route = _panel.GetActiveRoute();
        if (route == null) return;

        if (ctx.TrackData.IsNodeId(id))
        {
            var node = ctx.TrackData.GetNode(id);
            if (node?.NodeType == TrackNodeType.Switch)
                TrackEditorActions.SetRouteSwitchAlignment(ctx, route, id, SwitchAlignment.Reverse);
        }
    }

    private void Reset(PluginContext ctx = null)
    {
        _activeSignalId = string.Empty;
        _hoveredLinkId = string.Empty;
        ctx?.ClearSelection();

        if (_panel is not null)
        {
            _panel.Clear();
            _panel.Visible = false;
        }
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        // Re-parent panel if overlay changed (safe on every draw)
        if (_currentOverlay != overlay)
        {
            if (_currentOverlay != null && _panel != null && _panel.GetParent() == _currentOverlay)
                _currentOverlay.RemoveChild(_panel);

            _currentOverlay = overlay;

            if (_panel != null && _panel.GetParent() == null)
                _currentOverlay.AddChild(_panel);
        }

        if (string.IsNullOrEmpty(_activeSignalId)) return;

        var signal = ctx.TrackData.GetSignal(_activeSignalId);
        if (signal == null) return;

        var sigScreen = GetSignalScreenPos(ctx, signal);
        var scale = PluginUtility.GetZoom();
        overlay.DrawCircle(sigScreen, 16f * scale, new Color(0.2f, 0.8f, 1f, 0.6f));

        var activeRoute = _panel.GetActiveRoute();
        if (activeRoute != null)
        {
            foreach (var (swId, align) in activeRoute.SwitchAlignments)
            {
                var node = ctx.TrackData.GetNode(swId);
                if (node == null) continue;
                var nScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(node.Position));
                var col = align == SwitchAlignment.Normal ? Colors.Lime : Colors.OrangeRed;
                overlay.DrawCircle(nScreen, 8f * scale, col);
                overlay.DrawString(TrackEditorDrawer.GetFont(), nScreen + new Vector2(12, -8),
                    align == SwitchAlignment.Normal ? "N" : "R", fontSize: (int)(14 * scale), modulate: Colors.White);
            }

            if (!string.IsNullOrEmpty(activeRoute.TargetLinkId) || !string.IsNullOrEmpty(_hoveredLinkId))
            {
                var linkId = !string.IsNullOrEmpty(activeRoute.TargetLinkId) ? activeRoute.TargetLinkId : _hoveredLinkId;
                var link = ctx.TrackData.GetLink(linkId);
                if (link != null)
                {
                    var mid = ctx.TrackData.GetNode(link.NodeAId)?.Position.Lerp(
                        ctx.TrackData.GetNode(link.NodeBId)?.Position ?? Vector2.Zero, 0.5f) ?? Vector2.Zero;
                    var targetScreen = PluginUtility.WorldToScreen(ctx.Track.ToGlobal(mid));
                    var color = !string.IsNullOrEmpty(activeRoute.TargetLinkId) ? Colors.Yellow : new Color(1f, 1f, 0.3f, 0.6f);
                    overlay.DrawDashedLine(sigScreen, targetScreen, color, 3f, 12f);
                }
            }
        }
    }

    private Vector2 GetSignalScreenPos(PluginContext ctx, SignalData signal)
    {
        var pos = ctx.TrackData.GetSignalPosition(signal);
        return pos.HasValue ? PluginUtility.WorldToScreen(ctx.Track.ToGlobal(pos.Value.Position)) : Vector2.Zero;
    }
}