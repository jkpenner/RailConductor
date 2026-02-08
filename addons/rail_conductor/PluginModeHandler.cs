using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

public abstract class PluginModeHandler
{
    private readonly HashSet<string> _selected = new();
    private readonly HashSet<string> _hovered = new();

    public IReadOnlySet<string> Selected => _selected;
    public IReadOnlySet<string> Hovered => _hovered;

    public event Action? OverlayUpdateRequested;

    public bool HandleGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        _hovered.Clear();

        bool handled = OnGuiInput(target, e, undoRedo);

        if (handled)
            OverlayUpdateRequested?.Invoke();

        return handled;
    }

    // Selection control API
    protected void ClearSelection() => _selected.Clear();
    protected void Select(string id) => _selected.Add(id);
    protected void Deselect(string id) => _selected.Remove(id);

    protected void ToggleSelect(string id)
    {
        if (!_selected.Add(id))
        {
            _selected.Remove(id);
        }
    }

    protected void SelectOnly(string id)
    {
        ClearSelection();
        Select(id);
    }

    protected void SetSelection(IEnumerable<string> ids)
    {
        ClearSelection();
        AddToSelection(ids);
    }

    protected void AddToSelection(IEnumerable<string> ids) => _selected.UnionWith(ids);
    protected void RemoveFromSelection(IEnumerable<string> ids) => _selected.ExceptWith(ids);

    protected void Hover(string id) => _hovered.Add(id);

    protected void SetHovered(IEnumerable<string> ids)
    {
        _hovered.Clear();
        _hovered.UnionWith(ids);
    }

    protected virtual bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        return false;
    }

    protected void RequestOverlayUpdate() => OverlayUpdateRequested?.Invoke();


    protected string GetClosestId(Track target, Vector2 screenPosition)
    {
        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPosition = target.ToLocal(globalPosition);
        return target.Data?.FindClosestId(localPosition) ?? string.Empty;
    }
}