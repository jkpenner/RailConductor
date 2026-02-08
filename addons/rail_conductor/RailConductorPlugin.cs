#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor.Plugin;

public enum ToolMode
{
    None,
    Select,
    Create,
    Move,
    Delete,
    Link,
    Unlink,
    Insert,
    PlaceSignal
}

[Tool]
public partial class RailConductorPlugin : EditorPlugin
{
    private Track? _target;
    private TrackNodeOptions? _options;
    // private TrackToolbar? _toolbar;


    private ToolMode _currentToolMode = ToolMode.None;
    private bool _dragging = false;
    private Vector2 _originalPosition;
    private string _hoveredId = string.Empty;
    private Font _font;

    private readonly Dictionary<ToolMode, PluginModeHandler> _modeHandlers = new();

    private EditorUndoRedoManager? _undoRedo;

    public override void _EnterTree()
    {
        _undoRedo = GetUndoRedo();
        _undoRedo.VersionChanged += OnVersionChanged;

        _modeHandlers.Clear();
        _modeHandlers.Add(ToolMode.Select, new SelectMode());
        _modeHandlers.Add(ToolMode.Create, new AddTrackNodeMode());
        _modeHandlers.Add(ToolMode.Insert, new InsertTrackNodeMode());
        _modeHandlers.Add(ToolMode.Move, new MoveTrackNodeMode());
        _modeHandlers.Add(ToolMode.Delete, new DeleteTrackNodeMode());
        _modeHandlers.Add(ToolMode.Link, new LinkTrackNodeMode());
        _modeHandlers.Add(ToolMode.Unlink, new UnlinkTrackNodeMode());
        _modeHandlers.Add(ToolMode.PlaceSignal, new PlaceSignalMode());

        _font = ResourceLoader.Load<Font>("res://addons/rail_conductor/fonts/default.tres");

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested += OnUpdateOverlayRequested;
        }
    }

    private void OnUpdateOverlayRequested()
    {
        UpdateOverlays();
    }

    private void OnVersionChanged()
    {
        UpdateOverlays();
    }

    public override void _ExitTree()
    {
        ClearMenus();
        _target = null;

        if (_undoRedo is not null)
        {
            _undoRedo.VersionChanged -= OnVersionChanged;
        }

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested -= OnUpdateOverlayRequested;
        }

        _undoRedo = null;
    }

    public override bool _Handles(GodotObject obj)
    {
        return obj is Track;
    }

    public override void _Edit(GodotObject obj)
    {
        _target = obj as Track;
        _dragging = false;
        _hoveredId = string.Empty;
    }

    public override void _MakeVisible(bool visible)
    {
        if (visible)
        {
            SetupMenus();
            _options?.SetToolMode(_currentToolMode);
        }
        else
        {
            ClearMenus();
            _currentToolMode = ToolMode.None;
            _dragging = false;
            _hoveredId = string.Empty;
        }
    }

    private void SetupMenus()
    {
        // if (_toolbar is null)
        // {
        //     var toolbarScene = ResourceLoader.Load<PackedScene>(
        //         "res://addons/rail_conductor/scenes/track_toolbar.tscn");
        //     _toolbar = toolbarScene.Instantiate<TrackToolbar>();
        //     if (_toolbar is not null)
        //     {
        //         GD.Print("Adding tool bar to CanvasEditorMenu");
        //         _toolbar.ToolModeSelected += SetMode;
        //         AddControlToContainer(CustomControlContainer.CanvasEditorMenu, _toolbar);
        //     }
        // }

        if (_options is null)
        {
            var optionsScene = ResourceLoader.Load<PackedScene>(
                "res://addons/rail_conductor/scenes/track_node_options.tscn");
            _options = optionsScene.InstantiateOrNull<TrackNodeOptions>();
            if (_options is not null)
            {
                _options.ToolModeSelected += SetMode;
                AddControlToContainer(CustomControlContainer.CanvasEditorSideRight, _options);
            }
        }
    }

    private void ClearMenus()
    {
        // if (_toolbar is not null)
        // {
        //     RemoveControlFromContainer(CustomControlContainer.CanvasEditorMenu, _toolbar);
        //     _toolbar.ToolModeSelected -= SetMode;
        //     _toolbar.QueueFree();
        //     _toolbar = null;
        // }

        if (_options is not null)
        {
            RemoveControlFromContainer(CustomControlContainer.CanvasEditorMenu, _options);
            _options.ToolModeSelected -= SetMode;
            _options.QueueFree();
            _options = null;
        }
    }

    private void SetMode(ToolMode toolMode)
    {
        _currentToolMode = toolMode;
        // _toolbar?.SetToolMode(toolMode);
        _options?.SetToolMode(toolMode);


        _hoveredId = string.Empty;
        _dragging = false;
    }

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        _undoRedo = GetUndoRedo();

        if (_target?.Data is not null && @event is InputEventMouseMotion mouse)
        {
            UpdateOverlays();

            var globalPosition = PluginUtility.ScreenToWorld(mouse.Position);
            var localPosition = _target.ToLocal(globalPosition);
            var hoveredIndex = _target.Data.FindClosestId(localPosition);
            if (hoveredIndex != _hoveredId)
            {
                _hoveredId = hoveredIndex;
                UpdateOverlays();
            }
        }

        if (_target?.Data is null || _currentToolMode == ToolMode.None || _undoRedo is null)
        {
            return false;
        }

        if (!_modeHandlers.TryGetValue(_currentToolMode, out var handler))
        {
            return false;
        }

        if (!handler.HandleGuiInput(_target, @event, _undoRedo))
        {
            return false;
        }

        UpdateOverlays();
        return true;
    }


    public override void _ForwardCanvasDrawOverViewport(Control overlay)
    {
        if (_target?.Data is null)
        {
            return;
        }


        var scale = PluginUtility.GetZoom();

        // Draw the links between all nodes
        foreach (var link in _target.Data.GetLinks())
        {
            var nodeA = _target.Data.GetNode(link.NodeAId);
            var nodeB = _target.Data.GetNode(link.NodeBId);

            if (nodeA is null || nodeB is null)
            {
                continue;
            }

            var globalPosition1 = _target.ToGlobal(nodeA.Position);
            var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);

            var globalPosition2 = _target.ToGlobal(nodeB.Position);
            var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);

            overlay.DrawLine(screenPosition1, screenPosition2,
                PluginSettings.LinkColor, PluginSettings.LinkWidth * scale);

            if (_font is not null)
            {
                var center = screenPosition1.Lerp(screenPosition2, 0.5f);
                overlay.DrawString(_font, center + new Vector2(-50f, (1.5f * PluginUtility.GetZoom())),
                    link.Id[0..3].ToUpper(),
                    alignment: HorizontalAlignment.Center,
                    fontSize: (int)(4f * PluginUtility.GetZoom()),
                    modulate: Colors.Black,
                    width: 100);
            }
        }

        // Draw the selected node effect
        if (_modeHandlers.TryGetValue(_currentToolMode, out var handler))
        {
            foreach (var id in handler.Selected)
            {
                var selectedNode = _target.Data.GetNode(id);
                if (selectedNode is null)
                {
                    continue;
                }

                var globalPosition = _target.ToGlobal(selectedNode.Position);
                var screenPosition = PluginUtility.WorldToScreen(globalPosition);

                overlay.DrawCircle(screenPosition, (PluginSettings.NodeRadius + 2) * scale,
                    PluginSettings.SelectedColor);
            }
        }

        // Draw all nodes
        foreach (var node in _target.Data.GetNodes())
        {
            var globalPosition = _target.ToGlobal(node.Position);
            var screenPosition = PluginUtility.WorldToScreen(globalPosition);

            var zoom = PluginUtility.GetZoom();
            overlay.DrawCircle(screenPosition, PluginSettings.NodeRadius * zoom, PluginSettings.NodePrimaryColor);

            var fillColor = _hoveredId == node.Id ? PluginSettings.NodeHoverColor : PluginSettings.NodeNormalColor;
            overlay.DrawCircle(screenPosition, (PluginSettings.NodeRadius - 2) * zoom, fillColor);

            if (_font is not null)
            {
                overlay.DrawString(_font, screenPosition + new Vector2(-50f, (1.5f * PluginUtility.GetZoom())),
                    node.Id[0..3].ToUpper(),
                    alignment: HorizontalAlignment.Center,
                    fontSize: (int)(4f * PluginUtility.GetZoom()),
                    modulate: Colors.Black,
                    width: 100);
            }

            if (node.NodeType != TrackNodeType.Switch && node.NodeType != TrackNodeType.Crossover)
            {
                continue;
            }

            HashSet<string> drawn = [];
            for (var i = 0; i < node.PairedLinks.Count; i++)
            {
                var pair = node.PairedLinks[i];
                var linkA = _target.Data.GetLink(pair.LinkAId);
                var linkB = _target.Data.GetLink(pair.LinkBId);
                if (linkA is null || linkB is null)
                {
                    continue;
                }

                var color = i == 0 ? PluginSettings.SwitchPrimaryColor : PluginSettings.SwitchSecondaryColor;

                var linkANode = _target.Data.GetNode(linkA.GetOtherNode(node.Id));
                if (linkANode is not null && drawn.Add(linkA.Id))
                {
                    var direction = (linkANode.Position - node.Position).Normalized();
                    var position = node.Position + direction * (PluginSettings.NodeRadius + 3);

                    var linkGlobalPosition = _target.ToGlobal(position);
                    var linkScreenPosition = PluginUtility.WorldToScreen(linkGlobalPosition);

                    overlay.DrawCircle(linkScreenPosition, 2 * zoom, color);
                }

                var linkBNode = _target.Data.GetNode(linkB.GetOtherNode(node.Id));
                if (linkBNode is not null && drawn.Add(linkB.Id))
                {
                    var direction = (linkBNode.Position - node.Position).Normalized();
                    var position = node.Position + direction * (PluginSettings.NodeRadius + 3);

                    var linkGlobalPosition = _target.ToGlobal(position);
                    var linkScreenPosition = PluginUtility.WorldToScreen(linkGlobalPosition);

                    overlay.DrawCircle(linkScreenPosition, 2 * zoom, color);
                }
            }

            // foreach (var linkId in node.Links)
            // {
            //     var link = _target.Data.GetLink(linkId);
            //     if (link is null)
            //     {
            //         continue;
            //     }
            //
            //     var linkedNode = _target.Data.GetNode(link.GetOtherNode(node.Id));
            //     if (linkedNode is null)
            //     {
            //         continue;
            //     }
            //
            //     var direction = (linkedNode.Position - node.Position).Normalized();
            //     var position = node.Position + direction * (PluginSettings.NodeRadius + 3);
            //
            //     var linkGlobalPosition = _target.ToGlobal(position);
            //     var linkScreenPosition = PluginUtility.WorldToScreen(linkGlobalPosition);
            //
            //     if (node.NodeType == TrackNodeType.Crossover)
            //     {
            //         node.PairedLinks.FindIndex()
            //     }
            //     overlay.DrawCircle(linkScreenPosition, 2 * zoom, Colors.Red);
            // }
        }

        foreach (var signal in _target.Data.GetSignals())
        {
            var orientation = _target.Data.GetSignalPosition(signal);
            if (orientation is null)
            {
                continue;
            }

            var (position, angle) = orientation.Value;
            var signalGlobalPosition = _target.ToGlobal(position);
            var signalScreenPosition = PluginUtility.WorldToScreen(signalGlobalPosition);

            if (_hoveredId == signal.Id)
            {
                overlay.DrawCircle(signalScreenPosition, 4f * scale, PluginSettings.SelectedColor);
                overlay.DrawArc(signalScreenPosition,
                    5 * scale,
                    angle + Mathf.DegToRad(140f),
                    angle + Mathf.DegToRad(220f),
                    6,
                    PluginSettings.SelectedColor,
                    2 * scale);
            }


            overlay.DrawCircle(signalScreenPosition, 3f * scale, PluginSettings.SignalColor);
            overlay.DrawArc(signalScreenPosition,
                4 * scale,
                angle + Mathf.DegToRad(150f),
                angle + Mathf.DegToRad(210f),
                6,
                PluginSettings.SignalColor,
                2 * scale);
        }
    }
}

public static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}
#endif