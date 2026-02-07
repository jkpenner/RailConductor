using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

[Tool]
public partial class TrackNodeOptions : Control
{
    public event Action<ToolMode>? ToolModeSelected;
    
    private readonly Dictionary<ToolMode, Button> _buttons = new();
    private readonly Dictionary<ToolMode, Action> _setActions = new();

    public override void _Ready()
    {
        // Register all buttons
        _buttons.Clear();
        _buttons.Add(ToolMode.Select, GetNode<Button>("%SelectNodeButton"));
        _buttons.Add(ToolMode.Create, GetNode<Button>("%CreateNodeButton"));
        _buttons.Add(ToolMode.Move, GetNode<Button>("%MoveNodeButton"));
        _buttons.Add(ToolMode.Delete, GetNode<Button>("%DeleteNodeButton"));
        _buttons.Add(ToolMode.Link, GetNode<Button>("%LinkNodesButton"));
        _buttons.Add(ToolMode.Unlink, GetNode<Button>("%UnlinkNodesButton"));
        
        // Generate all required callbacks.
        foreach (var mode in Enum.GetValues<ToolMode>())
        {
            if (mode == ToolMode.None)
            {
                continue;
            }
            
            _setActions.Add(mode, () => ToolModeSelected?.Invoke(mode));
        }
        
        // Register all press callbacks
        foreach (var (mode, button) in _buttons)
        {
            button.Pressed += _setActions[mode];
        }
    }

    public override void _ExitTree()
    {
        // Unregister all press callbacks
        foreach (var (mode, button) in _buttons)
        {
            button.Pressed -= _setActions[mode];
        }
    }
    
    public void SetToolMode(ToolMode mode)
    {
        foreach (var (buttonMode, button) in _buttons)
        {
            button.ButtonPressed = buttonMode == mode;
        }
    }
}