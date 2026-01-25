using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RailConductor;

public partial class Main : Node
{
    public static Main Instance { get; private set; }

    public readonly List<Interactable> Interactables = [];

    public override void _Ready()
    {
        if (Instance is not null && Instance != this)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
    }

    public override void _Process(double delta)
    {
        if (Interactables.Count > 0)
        {
            var target = Interactables
                .OrderBy(i => i.InteractOrder)
                .FirstOrDefault(i => i.IsInteractable);

            GD.Print($"Interacted with {Interactables.Count} objects.");
            if (target is not null)
            {
                GD.Print($"Interacting with {target.Name}");
            }

            Interactables.Clear();
        }
    }
}