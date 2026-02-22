using System;
using Godot;

namespace RailConductor.Plugin;

[GlobalClass, Tool]
public partial class SignalRouteEntry : Control
{
    public event Action<int, bool> Selected;   // row clicked (for selection)
    public event Action Deleted;               // × button clicked

    private Label _label = null!;
    private Button _delete = null!;

    private int _index;
    private bool _isRouteDefinition;

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>("%NameLabel");
        _delete = GetNodeOrNull<Button>("%DeleteButton");

        if (_delete != null)
            _delete.Pressed += OnDeletePressed;

        GuiInput += OnGuiInput;
    }

    public void Setup(string text, int index, bool isRouteDefinition)
    {
        _index = index;
        _isRouteDefinition = isRouteDefinition;

        if (_label != null)
            _label.Text = text;
    }

    public void SetSelected(bool selected)
    {
        Modulate = selected ? new Color(0.8f, 1f, 0.8f) : Colors.White;
    }

    private void OnGuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            if (_delete != null && _delete.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
                return;

            Selected?.Invoke(_index, _isRouteDefinition);
        }
    }

    private void OnDeletePressed()
    {
        Deleted?.Invoke();
    }
}