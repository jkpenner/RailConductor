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
    Link,
    Insert,
    PlaceSignal
}

[Tool]
public partial class RailConductorPlugin : EditorPlugin, ISerializationListener
{
    private ToolMode _currentToolMode = ToolMode.None;
    private readonly PluginContext _context = new();
    private readonly Dictionary<ToolMode, PluginModeHandler> _modeHandlers = new();

    private bool _isInitialized;
    private Track? _target;
    private TrackNodeOptions? _options;
    private EditorUndoRedoManager? _undoRedo;

    public override void _EnterTree() => Initialize();
    public override void _ExitTree() => Cleanup();

    public void OnAfterDeserialize() => Initialize();
    public void OnBeforeSerialize() => Cleanup();

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        // Setup undo/redo callback handling.
        _undoRedo = GetUndoRedo();
        _undoRedo.VersionChanged += OnVersionChanged;

        // Initialize Mode Handlers
        _modeHandlers.Add(ToolMode.Select, new SelectMode());
        _modeHandlers.Add(ToolMode.Create, new AddTrackNodeMode());
        _modeHandlers.Add(ToolMode.Insert, new InsertTrackNodeMode());
        _modeHandlers.Add(ToolMode.Link, new LinkTrackNodeMode());
        _modeHandlers.Add(ToolMode.PlaceSignal, new PlaceSignalMode());

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested += OnUpdateOverlayRequested;
        }
    }

    private void Cleanup()
    {
        if (!_isInitialized)
        {
            return;
        }

        _isInitialized = false;
        ClearMenus();

        // Clean up undo/redo callbacks.
        if (_undoRedo is not null)
        {
            _undoRedo.VersionChanged -= OnVersionChanged;
        }

        _undoRedo = null;

        // Clean up mode handlers.
        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested -= OnUpdateOverlayRequested;
        }

        _modeHandlers.Clear();

        _target = null;
    }

    private void OnUpdateOverlayRequested()
    {
        UpdateOverlays();
    }

    private void OnVersionChanged()
    {
        UpdateOverlays();
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
        if (_options is not null)
        {
            return;
        }

        var optionsScene = ResourceLoader.Load<PackedScene>(
            "res://addons/rail_conductor/scenes/track_node_options.tscn");
        _options = optionsScene.InstantiateOrNull<TrackNodeOptions>();

        if (_options is null)
        {
            GD.PushError($"Failed to instantiate {nameof(TrackNodeOptions)} scene.");
            return;
        }

        _options.ToolModeSelected += SetMode;
        AddControlToContainer(CustomControlContainer.CanvasEditorSideRight, _options);
    }

    private void ClearMenus()
    {
        if (_options is null)
        {
            return;
        }

        RemoveControlFromContainer(CustomControlContainer.CanvasEditorMenu, _options);
        _options.ToolModeSelected -= SetMode;
        _options.QueueFree();
        _options = null;
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

        // Update plugin context with current values
        _context.Track = _target;
        _context.TrackData = _target.Data;
        _context.UndoRedo = _undoRedo;

        if (!handler.HandleGuiInput(_context, input))
        {
            return false;
        }
        
        UpdateOverlays();
        return true;
    }

    public override void _ForwardCanvasDrawOverViewport(Control overlay)
    {
        var drawTarget = _target;
        if (drawTarget is null)
        {
            return;
        }
        
        if (drawTarget.Data is null)
        {
            return;
        }
        
        _context.Track = drawTarget;
        _context.TrackData = drawTarget.Data;
        _context.UndoRedo = _undoRedo;

        foreach (var link in drawTarget.Data.GetLinks())
        {
            TrackEditorDrawer.DrawTrackLink(overlay, _context, link);
        }

        foreach (var node in drawTarget.Data.GetNodes())
        {
            TrackEditorDrawer.DrawTrackNode(overlay, _context, node);
        }

        foreach (var signal in drawTarget.Data.GetSignals())
        {
            TrackEditorDrawer.DrawTrackSignal(overlay, _context, signal);
        }
    }
}
#endif