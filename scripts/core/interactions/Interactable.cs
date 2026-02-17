using Godot;

namespace RailConductor;

public abstract partial class Interactable : Area2D
{
    [Export]
    public int InteractPriority { get; set; } = 0;

    [Export]
    public bool IsInteractEnabled { get; set; } = true;
    
    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            Interactions.Instance?.NotifyInteraction(this);
        }
    }

    public void Interact()
    {
        if (!IsInteractEnabled)
        {
            GD.PushWarning("Attempted to interact with disabled interactable.");
            return;
        }
        
        OnInteraction();
    }

    protected abstract void OnInteraction();
}