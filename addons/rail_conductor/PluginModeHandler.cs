using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

public abstract class PluginModeHandler
{
    private readonly HashSet<string> _selected = [];
    private readonly HashSet<string> _hovered = [];

    public IReadOnlyCollection<string> Selected => _selected;
    public IReadOnlyCollection<string> Hovered => _hovered;

    public event Action? OverlayUpdateRequested;

    public bool HandleGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        _selected.Clear();
        _hovered.Clear();

        return OnGuiInput(target, e, undoRedo);
    }

    protected virtual bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        return false;
    }

    protected void MarkAsHovered(string id) => _hovered.Add(id);
    protected void MarkAsSelected(string id) => _selected.Add(id);
    protected void RequestOverlayUpdate() => OverlayUpdateRequested?.Invoke();
}