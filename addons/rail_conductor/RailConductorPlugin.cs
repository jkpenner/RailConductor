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
    Insert
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
    private int _hoveredNodeId = -1;

    private readonly Dictionary<ToolMode, PluginModeHandler> _modeHandlers = new();

    private EditorUndoRedoManager? _undoRedo;

    public override void _EnterTree()
    {
        _undoRedo = GetUndoRedo();
        _undoRedo.VersionChanged += OnVersionChanged;

        _modeHandlers.Clear();
        _modeHandlers.Add(ToolMode.Select, new SelectTrackNodeMode());
        _modeHandlers.Add(ToolMode.Create, new AddTrackNodeMode());
        _modeHandlers.Add(ToolMode.Insert, new InsertTrackNodeMode());
        _modeHandlers.Add(ToolMode.Move, new MoveTrackNodeMode());
        _modeHandlers.Add(ToolMode.Delete, new DeleteTrackNodeMode());
        _modeHandlers.Add(ToolMode.Link, new LinkTrackNodeMode());
        _modeHandlers.Add(ToolMode.Unlink, new UnlinkTrackNodeMode());

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OnSetup();
        }
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

        _undoRedo = null;

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OnCleanup();
        }
    }

    public override bool _Handles(GodotObject obj)
    {
        return obj is Track;
    }

    public override void _Edit(GodotObject obj)
    {
        _target = obj as Track;
        _dragging = false;
        _hoveredNodeId = -1;
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
            _hoveredNodeId = -1;
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


        _hoveredNodeId = -1;
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
            var hoveredIndex = _target.Data.FindClosestNodeId(localPosition);
            if (hoveredIndex != _hoveredNodeId)
            {
                _hoveredNodeId = hoveredIndex;
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

        if (!handler.OnGuiInput(_target, @event, _undoRedo))
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
        var links = _target.Data.GetNodes()
            .SelectMany(n => n.Links.Select(l => PluginUtility.GetLinkId(n.Id, l)))
            .ToHashSet();

        // Draw the links between all nodes
        foreach (var (node1Id, node2Id) in links)
        {
            var node1 = _target.Data.GetNode(node1Id);
            var node2 = _target.Data.GetNode(node2Id);

            if (node1 is null || node2 is null)
            {
                continue;
            }

            var globalPosition1 = _target.ToGlobal(node1.Position);
            var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);

            var globalPosition2 = _target.ToGlobal(node2.Position);
            var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);

            overlay.DrawLine(screenPosition1, screenPosition2,
                PluginSettings.LinkColor, PluginSettings.LinkWidth * scale);
        }

        // Draw the selected node effect
        if (_modeHandlers.TryGetValue(_currentToolMode, out var handler))
        {
            foreach (var id in handler.SelectedNodeId)
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

            var fillColor = _hoveredNodeId == node.Id ? PluginSettings.NodeHoverColor : PluginSettings.NodeNormalColor;
            overlay.DrawCircle(screenPosition, (PluginSettings.NodeRadius - 2) * zoom, fillColor);

            foreach (var link in node.Links)
            {
                links.Add(PluginUtility.GetLinkId(node.Id, link));
            }
        }

        if (_modeHandlers.TryGetValue(_currentToolMode, out var handler2))
        {
            handler2.OnGuiDraw(_target, overlay);
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