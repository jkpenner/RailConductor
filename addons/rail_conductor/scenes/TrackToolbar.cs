using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

[Tool]
public partial class TrackToolbar : HBoxContainer
{
    public event Action<ToolMode>? ToolModeSelected;
    
    private readonly Dictionary<ToolMode, Button> _buttons = new();
    private readonly Dictionary<ToolMode, Action> _setActions = new();
    
    public override void _EnterTree()
    {
        foreach (var mode in Enum.GetValues<ToolMode>())
        {
            if (mode == ToolMode.None)
            {
                continue;
            }
            
            _setActions.Add(mode, () => ToolModeSelected?.Invoke(mode));
        }
        
        _buttons.Add(ToolMode.Select, CreateModeButton(ToolMode.Select, "res://addons/rail_conductor/icons/select.svg"));
        _buttons.Add(ToolMode.Create, CreateModeButton(ToolMode.Create, "res://addons/rail_conductor/icons/create.svg"));
        _buttons.Add(ToolMode.Move, CreateModeButton(ToolMode.Move, "res://addons/rail_conductor/icons/move.svg"));
        _buttons.Add(ToolMode.Delete, CreateModeButton(ToolMode.Delete, "res://addons/rail_conductor/icons/delete.svg"));
        _buttons.Add(ToolMode.Link, CreateModeButton(ToolMode.Link, "res://addons/rail_conductor/icons/link.svg"));
        _buttons.Add(ToolMode.Unlink, CreateModeButton(ToolMode.Unlink, "res://addons/rail_conductor/icons/unlink.svg"));
        
        foreach (var button in _buttons.Values)
        {
            AddChild(button);
        }
    }

    public override void _ExitTree()
    {
        foreach (var (mode, button) in _buttons)
        {
            button.Pressed -= _setActions[mode];
            button.QueueFree();
        }
        _buttons.Clear();
        _setActions.Clear();
    }
    
    public void SetToolMode(ToolMode mode)
    {
        foreach (var (buttonMode, button) in _buttons)
        {
            button.ButtonPressed = buttonMode == mode;
        }
    }
    
    private Button CreateModeButton(ToolMode mode, string iconPath)
    {
        var button = new Button();
        button.Icon = ResourceLoader.Load<Texture2D>(iconPath);
        button.Pressed += _setActions[mode];
        // button.Flat = true;
        button.ExpandIcon = true;
        button.ToggleMode = true;
        return button;
    }
}