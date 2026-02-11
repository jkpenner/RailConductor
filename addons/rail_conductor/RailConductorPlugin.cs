#if TOOLS
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

public enum ToolMode
{
    None,
    Select,
    Create,
    Link,
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
    private Font _font;

    private PluginContext _context = new();
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
        _modeHandlers.Add(ToolMode.Link, new LinkTrackNodeMode());
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
    }

    public override void _MakeVisible(bool visible)
    {
        if (visible)
        {
            SetupMenus();
            SetMode(ToolMode.Select);
        }
        else
        {
            ClearMenus();
            _currentToolMode = ToolMode.None;
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
        if (_modeHandlers.TryGetValue(_currentToolMode, out var prevHandler))
        {
            prevHandler.Disable(_context);
        }
        
        _currentToolMode = toolMode;
        
        if (_modeHandlers.TryGetValue(_currentToolMode, out var nextHandler))
        {
            nextHandler.Enable(_context);
        }
        
        // _toolbar?.SetToolMode(toolMode);
        _options?.SetToolMode(toolMode);
    }

    public override bool _ForwardCanvasGuiInput(InputEvent input)
    {
        if (_target?.Data is null || _currentToolMode == ToolMode.None || _undoRedo is null)
        {
            return false;
        }

        if (!_modeHandlers.TryGetValue(_currentToolMode, out var handler))
        {
            return false;
        }
        
        _context.Track = _target;
        _context.TrackData = _target.Data;
        _context.UndoRedo = GetUndoRedo();

        if (!handler.HandleGuiInput(_context, input))
        {
            return false;
        }

        UpdateOverlays();
        return true;
    }

    private bool IsHovered(string id) => _context.IsHovered(id);

    private bool IsSelected(string id) => _context.IsSelected(id);


    public override void _ForwardCanvasDrawOverViewport(Control overlay)
    {
        if (_target?.Data is null)
        {
            return;
        }

        _context.Track = _target;
        _context.TrackData = _target.Data;
        _context.UndoRedo = GetUndoRedo();

        // Draw the links between all nodes
        foreach (var link in _target.Data.GetLinks())
        {
            TrackEditorDrawer.DrawTrackLink(overlay, _context, link);
        }

        // Draw all nodes
        foreach (var node in _target.Data.GetNodes())
        {
            TrackEditorDrawer.DrawTrackNode(overlay, _context, node);
        }

        foreach (var signal in _target.Data.GetSignals())
        {
            TrackEditorDrawer.DrawTrackSignal(overlay, _context, signal);
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