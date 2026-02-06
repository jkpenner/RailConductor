#if TOOLS
using System.Collections.Generic;
using Godot;
using RailConductor.Plugin.modes;

namespace RailConductor.Plugin;

[Tool]
public partial class RailConductorPlugin : EditorPlugin
{
    public enum Mode
    {
        None,
        Add,
        Move,
        Delete,
        Link,
    }

    private Track? _target;
    private HBoxContainer? _toolbar;
    private Button? _addButton;
    private Button? _moveButton;
    private Button? _deleteButton;
    private Button? _linkButton;

    private Mode _currentMode = Mode.None;
    private bool _dragging = false;
    private Vector2 _originalPosition;
    private int _hoveredIndex = -1;

    private readonly Dictionary<Mode, PluginModeHandler> _modeHandlers = new();

    private EditorUndoRedoManager? _undoRedo;

    public override void _EnterTree()
    {
        _undoRedo = GetUndoRedo();
        _undoRedo.VersionChanged += OnVersionChanged;

        _modeHandlers.Clear();
        _modeHandlers.Add(Mode.Add, new AddTrackNodeMode());
        _modeHandlers.Add(Mode.Move, new MoveTrackNodeMode());
        _modeHandlers.Add(Mode.Delete, new DeleteTrackNodeMode());
        _modeHandlers.Add(Mode.Link, new LinkTrackNodeMode());

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
        ClearToolbar();
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
        _hoveredIndex = -1;
    }

    public override void _MakeVisible(bool visible)
    {
        if (visible)
        {
            SetupToolbar();
        }
        else
        {
            ClearToolbar();
            _currentMode = Mode.None;
            _dragging = false;
            _hoveredIndex = -1;
        }
    }

    private void SetupToolbar()
    {
        if (_toolbar is not null)
        {
            return;
        }

        _toolbar = new HBoxContainer();


        _addButton = new Button();
        _addButton.Icon = ResourceLoader.Load<Texture2D>("res://addons/rail_conductor/icons/create.svg");
        _addButton.Pressed += () => SetMode(Mode.Add);
        _addButton.ToggleMode = true;
        _toolbar.AddChild(_addButton);

        _moveButton = new Button();
        _moveButton.Icon = ResourceLoader.Load<Texture2D>("res://addons/rail_conductor/icons/move.svg");
        _moveButton.Pressed += () => SetMode(Mode.Move);
        _moveButton.ToggleMode = true;
        _toolbar.AddChild(_moveButton);

        _deleteButton = new Button();
        _deleteButton.Icon = ResourceLoader.Load<Texture2D>("res://addons/rail_conductor/icons/delete.svg");
        _deleteButton.Pressed += () => SetMode(Mode.Delete);
        _deleteButton.ToggleMode = true;
        _toolbar.AddChild(_deleteButton);
        
        _linkButton = new Button();
        _linkButton.Icon = ResourceLoader.Load<Texture2D>("res://addons/rail_conductor/icons/link.svg");
        _linkButton.Pressed += () => SetMode(Mode.Link);
        _linkButton.ToggleMode = true;
        _toolbar.AddChild(_linkButton);

        AddControlToContainer(CustomControlContainer.CanvasEditorMenu, _toolbar);
    }

    private void ClearToolbar()
    {
        if (_toolbar is null)
        {
            return;
        }

        RemoveControlFromContainer(CustomControlContainer.CanvasEditorMenu, _toolbar);
        _toolbar.QueueFree();

        _toolbar = null;
        _addButton = null;
        _moveButton = null;
        _deleteButton = null;
    }

    private void SetMode(Mode mode)
    {
        _currentMode = mode;

        if (_addButton is not null)
        {
            _addButton.ButtonPressed = mode == Mode.Add;
        }

        if (_moveButton is not null)
        {
            _moveButton.ButtonPressed = mode == Mode.Move;
        }

        if (_deleteButton is not null)
        {
            _deleteButton.ButtonPressed = mode == Mode.Delete;
        }

        _hoveredIndex = -1;
        _dragging = false;
    }

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        _undoRedo = GetUndoRedo();

        if (_target?.Data is not null && @event is InputEventMouseMotion mouse)
        {
            // While in node modes display a hover effect on the hovered node.
            if (_currentMode is Mode.Add or Mode.Move or Mode.Delete)
            {
                var globalPosition = PluginUtility.ScreenToWorld(mouse.Position);
                var localPosition = _target.ToLocal(globalPosition);
                var hoveredIndex = _target.Data.FindClosestNode(localPosition);
                if (hoveredIndex != _hoveredIndex)
                {
                    _hoveredIndex = hoveredIndex;
                    UpdateOverlays();
                }
            }
        }

        if (_target?.Data is null || _currentMode == Mode.None || _undoRedo is null)
        {
            return false;
        }

        if (!_modeHandlers.TryGetValue(_currentMode, out var handler))
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

        var selectedIndex = -1;

        // While dragging a node display the selection effect.
        if (_modeHandlers.TryGetValue(_currentMode, out var handler))
        {
            selectedIndex = handler.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _target.Data.Nodes.Count)
            {
                var node = _target.Data.Nodes[selectedIndex];
                var globalPosition = _target.ToGlobal(node.Position);
                var screenPosition = PluginUtility.WorldToScreen(globalPosition);

                var size = PluginUtility.GetZoom() * 14f;
                overlay.DrawCircle(screenPosition, size, Colors.YellowGreen.WithAlpha(0.7f));
            }
        }

        // While in node modes display a hover effect on the hovered node.
        if (_currentMode is Mode.Add or Mode.Move or Mode.Delete)
        {
            if (_hoveredIndex != selectedIndex && _hoveredIndex >= 0 && _hoveredIndex < _target.Data.Nodes.Count)
            {
                var node = _target.Data.Nodes[_hoveredIndex];
                var globalPosition = _target.ToGlobal(node.Position);
                var screenPosition = PluginUtility.WorldToScreen(globalPosition);

                var size = PluginUtility.GetZoom() * 14f;
                overlay.DrawCircle(screenPosition, size, Colors.Yellow.WithAlpha(0.7f));
            }
        }

        var links = new HashSet<(int, int)>();
        var nodeByIds = new Dictionary<int, TrackNodeData>();
        
        // Draw all nodes
        foreach (var node in _target.Data.Nodes)
        {
            var globalPosition = _target.ToGlobal(node.Position);
            var screenPosition = PluginUtility.WorldToScreen(globalPosition);
            var size = PluginUtility.GetZoom() * 8f;
            overlay.DrawCircle(screenPosition, size, Colors.Green.WithAlpha(0.7f));

            nodeByIds.Add(node.Id, node);
            
            foreach (var link in node.Links)
            {
                links.Add(PluginUtility.GetLinkId(node.Id, link));
            }
        }

        foreach (var (node1Id, node2Id) in links)
        {
            var node1 = nodeByIds[node1Id];
            var node2 = nodeByIds[node2Id];
            
            var globalPosition1 = _target.ToGlobal(node1.Position);
            var screenPosition1 = PluginUtility.WorldToScreen(globalPosition1);
            
            var globalPosition2 = _target.ToGlobal(node2.Position);
            var screenPosition2 = PluginUtility.WorldToScreen(globalPosition2);
            
            overlay.DrawLine(screenPosition1, screenPosition2, Colors.Green.WithAlpha(0.7f));
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