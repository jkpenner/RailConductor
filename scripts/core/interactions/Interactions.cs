using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

public partial class Interactions : Node
{
    public static Interactions? Instance { get; private set; }
    
    private readonly HashSet<Interactable> _objects = [];

    
    public Action<Interactable>? OnInteract;

    public override void _EnterTree()
    {
        if (Instance is not null && Instance != this)
        {
            QueueFree();
            return;
        }
        
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void _Process(double delta)
    {
        if (_objects.Count == 0)
        {
            return;
        }
        
        // Get the interactable with the highest priority.
        var target = _objects
            .OrderBy(i => i.InteractPriority)
            .FirstOrDefault(i => i.IsInteractEnabled);
        
        _objects.Clear();
        target?.Interact();
    }

    public void NotifyInteraction(Interactable interactable)
    {
        _objects.Add(interactable);
    }
}