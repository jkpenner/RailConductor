using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Uses the real SignalRoutePanel.tscn scene attached to the editor overlay.
/// The panel is fixed, zoom-independent, and always above the world.
/// World highlights (switch N/R labels + dashed target link) are still drawn.
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
        Reset();

        if (_panel == null)
        {
            var packed = ResourceLoader.Load<PackedScene>("res://addons/rail_conductor/scenes/SignalRoutePanel.tscn");
            _panel = packed.Instantiate<SignalRoutePanel>();
            _panel.Position = new Vector2(30, 30);
        }

        _panel.ActiveSelectionChanged += OnPanelSelectionChanged;   // ← connect

        RequestOverlayUpdate();
    }

    protected override void OnDisable(PluginContext ctx)
    {
        ResetRestrictions(ctx);
        Reset();
        Cleanup();

        if (_panel != null)
        {
            _panel.ActiveSelectionChanged -= OnPanelSelectionChanged;   // ← disconnect
            if (_panel.GetParent() != null)
                _panel.GetParent().RemoveChild(_panel);
        }
    }
    
    private void OnPanelSelectionChanged()
    {
        RequestOverlayUpdate();   // ← this is the key line that refreshes world drawing
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
    
    /// <summary>
    /// Called by main plugin during full cleanup/reload to ensure the panel is destroyed.
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
        _panel?.Clear();
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
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
            // Switch highlights + target link preview for the CURRENTLY SELECTED route
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