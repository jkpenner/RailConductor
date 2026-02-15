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
    PlaceSignal,
    PlacePlatform
}

[Tool]
public partial class RailConductorPlugin : EditorPlugin, ISerializationListener
{
    private ToolMode _currentToolMode = ToolMode.None;
    private PluginContext? _context;
    private readonly Dictionary<ToolMode, PluginModeHandler> _modeHandlers = new();
    
    private bool _isInitialized;
    private TrackNodeOptions? _options;
    private readonly NodeLocator<Track> _trackLocator = new();

    public override void _EnterTree() => Initialize();
    public override void _ExitTree() => Cleanup();

    public void OnAfterDeserialize() => Initialize();
    public void OnBeforeSerialize() => Cleanup();
    
    public override bool _Handles(GodotObject obj)
    {
        return obj is Track;
    }

    public override void _Edit(GodotObject obj)
    {
        UpdateCurrentContext(obj as Track);
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
    
    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        
        // Set up the current scene being edited
        _trackLocator.SetRoot(EditorInterface.Singleton.GetEditedSceneRoot());
        SceneChanged += _trackLocator.SetRoot;

        // Setup undo/redo callback handling.
        GetUndoRedo().VersionChanged += OnVersionChanged;

        // Initialize Mode Handlers
        _modeHandlers.Add(ToolMode.Select, new SelectMode());
        _modeHandlers.Add(ToolMode.Create, new AddTrackNodeMode());
        _modeHandlers.Add(ToolMode.Insert, new InsertTrackNodeMode());
        _modeHandlers.Add(ToolMode.Link, new LinkTrackNodeMode());
        _modeHandlers.Add(ToolMode.PlaceSignal, new PlaceSignalMode());
        _modeHandlers.Add(ToolMode.PlacePlatform, new PlacePlatformMode());

        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested += OnUpdateOverlayRequested;
        }
        
        SetForceDrawOverForwardingEnabled();
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
        // Setup undo/redo callback handling.
        GetUndoRedo().VersionChanged -= OnVersionChanged;

        // Clean up mode handlers.
        foreach (var handler in _modeHandlers.Values)
        {
            handler.OverlayUpdateRequested -= OnUpdateOverlayRequested;
        }

        _modeHandlers.Clear();
        
        SceneChanged -= _trackLocator.SetRoot;

        _trackLocator.Reset();
    }

    private void OnUpdateOverlayRequested()
    {
        UpdateOverlays();
    }

    private void OnVersionChanged()
    {
        UpdateOverlays();
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

        RemoveControlFromContainer(CustomControlContainer.CanvasEditorSideRight, _options);
        _options.ToolModeSelected -= SetMode;
        _options.QueueFree();
        _options = null;
    }

    private void SetMode(ToolMode toolMode)
    {
        var ctx = GetCurrentContext();
        if (ctx is null)
        {
            return;
        }
        
        if (_modeHandlers.TryGetValue(_currentToolMode, out var prevHandler))
        {
            prevHandler.Disable(ctx);
        }

        _currentToolMode = toolMode;

        if (_modeHandlers.TryGetValue(_currentToolMode, out var nextHandler))
        {
            nextHandler.Enable(ctx);
        }

        _options?.SetToolMode(toolMode);
    }

    public override bool _ForwardCanvasGuiInput(InputEvent input)
    {
        // Ignore input events if invalid state.
        var ctx = GetCurrentContext();
        if (ctx is null || _currentToolMode == ToolMode.None)
        {
            return false;
        }

        // Ignore if no current mode handler.
        return _modeHandlers.TryGetValue(_currentToolMode, out var handler) 
               && handler.HandleGuiInput(ctx, input);
    }
    
    private PluginContext? GetCurrentContext()
    {
        return _context;
    }

    private PluginContext? CreateContext(Track? track)
    {
        if (track?.Data is null)
        {
            return null;
        }

        return new PluginContext
        {
            Track = track,
            TrackData = track.Data,
            UndoRedo = GetUndoRedo(),
        };
    }
    
    private void UpdateCurrentContext(Track? track)
    {
        // Clear context on null or invalid data.
        if (track?.Data is null)
        {
            _context = null;
            return;
        }

        // Ignore updating to the same track.
        if (_context != null && _context.Track == track)
        {
            return;
        }
        
        _context = CreateContext(track);
    }
    
    public override void _ForwardCanvasDrawOverViewport(Control overlay)
    {
        var ctx = GetCurrentContext();
        if (ctx is not null)
        {
            TrackEditorDrawer.DrawTrackPlatform(overlay, ctx, new PlatformData
            {
                Position = new Vector2(300, 200),
                IsVertical = false,
                DisplayName = "Test"
            });
            
            TrackEditorDrawer.DrawTrack(overlay, ctx);

            if (_modeHandlers.TryGetValue(_currentToolMode, out var handler))
            {
                handler.Draw(overlay, ctx);
            }
        }
    }
    
    public override void _ForwardCanvasForceDrawOverViewport(Control overlay)
    {
        var currentContext = GetCurrentContext();
        
        foreach(var track in _trackLocator.Nodes)
        {
            // Ignore the selected track
            if (currentContext?.Track == track)
            {
                continue;
            }
            
            var ctx = CreateContext(track);
            if (ctx is not null)
            {
                TrackEditorDrawer.DrawTrack(overlay, ctx);    
            }
        }
    }
}
#endif