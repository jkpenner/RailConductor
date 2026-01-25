using Godot;

namespace RailConductor;

public partial class Interactable : Area2D
{
    [Export]
    public int InteractOrder { get; set; } = 0;

    [Export]
    public bool IsInteractable { get; set; } = true;
    
    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            Main.Instance?.Interactables.Add(this);
            
            // GD.Print($"Hello from {Name}");
        }
    }
}