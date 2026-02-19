using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

[Tool]
public partial class TrackOptions : Control
{
    private Label _modeHint = null!;
    public event Action<ToolMode>? ToolModeSelected;
    
    private readonly Dictionary<ToolMode, Button> _buttons = new();
    private readonly Dictionary<ToolMode, Action> _setActions = new();

    public override void _Ready()
    {
        _modeHint = GetNode<Label>("%ModeHint");
        
        // Register all buttons
        _buttons.Clear();
        //General Modes
        _buttons.Add(ToolMode.Select, GetNode<Button>("%SelectModeButton"));
        // Track Nodes
        _buttons.Add(ToolMode.PlaceNode, GetNode<Button>("%CreateNodeButton"));
        _buttons.Add(ToolMode.Insert, GetNode<Button>("%InsertNodeButton"));
        _buttons.Add(ToolMode.Link, GetNode<Button>("%LinkNodesButton"));
        // Track Signals
        _buttons.Add(ToolMode.PlaceSignal, GetNode<Button>("%PlaceSignalButton"));
        _buttons.Add(ToolMode.PlacePlatform, GetNode<Button>("%PlacePlatformButton"));
        
        _buttons.Add(ToolMode.AttachPlatform, GetNode<Button>("%AttachPlatformButton"));
        
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
            button.Text = mode switch
            {
                ToolMode.Select => "Select (Q)",
                ToolMode.PlaceNode => "Create (W)",
                ToolMode.Insert => "Insert (E)",
                ToolMode.Link => "Link (R)",
                ToolMode.PlaceSignal => "Signal (T)",
                ToolMode.PlacePlatform => "Platform (Y)",
                ToolMode.AttachPlatform => "Attach Platform (U)",
                _ => button.Text
            };
            
            button.TooltipText = mode switch
            {
                ToolMode.Select => "Select and move objects (Q)",
                ToolMode.PlaceNode => "Place new track node (W)",
                ToolMode.Insert => "Insert node on existing link (E)",
                ToolMode.Link => "Link two nodes together (R)",
                ToolMode.PlaceSignal => "Place signal on a link (T)",
                ToolMode.PlacePlatform => "Place platform (Y)",
                ToolMode.AttachPlatform => "Attach Platform (U)",
                _ => ""
            };
            
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
        
        if (_modeHint != null)
        {
            _modeHint.Text = mode switch
            {
                ToolMode.Link => "Click first node, then second node (or chain)",
                ToolMode.Insert => "Click on a link to insert a node",
                ToolMode.PlaceSignal => "Hover a link and click to place signal",
                _ => ""
            };
        }
    }
}