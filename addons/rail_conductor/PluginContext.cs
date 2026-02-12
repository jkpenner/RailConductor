using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor.Plugin;

[Flags]
public enum SelectionType
{
    None = 0x0,
    Node = 0x1,
    Link = 0x2,
    Signal = 0x4,
    All = Node | Link | Signal
}

public class PluginContext
{
    private readonly HashSet<string> _selected = new();
    private readonly HashSet<string> _hovered = new();
    
    private SelectionType _allowedFlags = ~SelectionType.None;
    private readonly HashSet<string> _allowed = new();
    
    public Track Track { get; set; }
    public TrackData TrackData { get; set; }
    public EditorUndoRedoManager? UndoRedo { get; set; }
    
    public IEnumerable<string> Selected => _selected;
    public IEnumerable<string> Hovered => _hovered;


    public bool IsSelectable(string id)
    {
        if (TrackData.IsNodeId(id) && !_allowedFlags.HasFlag(SelectionType.Node))
        {
            return false;
        }
        
        if (TrackData.IsLinkId(id) && !_allowedFlags.HasFlag(SelectionType.Link))
        {
            return false;
        }
        
        if (TrackData.IsSignalId(id) && !_allowedFlags.HasFlag(SelectionType.Signal))
        {
            return false;
        }
        
        return _allowed.Count == 0 || _allowed.Contains(id);
    }
    
    public void ResetSelectRestrictions()
    {
        _allowed.Clear();
        _allowedFlags = SelectionType.All;
    }
    
    public void RestrictSelectionType(SelectionType allowed)
    {
        _allowedFlags = allowed;
    }

    public void AddSelectableObject(string id)
    {
        _allowed.Add(id);
    }
    

    public void ClearSelection() => _selected.Clear();
    public void Select(string id) => _selected.Add(id);
    
    public void Deselect(string id) => _selected.Remove(id);

    public void ToggleSelect(string id)
    {
        if (!_selected.Add(id))
        {
            _selected.Remove(id);
        }
    }

    public void SelectOnly(string id)
    {
        ClearSelection();
        Select(id);
    }

    public void SetSelection(IEnumerable<string> ids)
    {
        ClearSelection();
        AddToSelection(ids);
    }

    public void AddToSelection(IEnumerable<string> ids) => _selected.UnionWith(ids);
    public void RemoveFromSelection(IEnumerable<string> ids) => _selected.ExceptWith(ids);

    public void ClearHovered() => _hovered.Clear();
    public void Hover(string id) => _hovered.Add(id);

    public void SetHovered(IEnumerable<string> ids)
    {
        _hovered.Clear();
        _hovered.UnionWith(ids);
    }

    public bool IsHovered(string id) =>  _hovered.Contains(id);
    public bool IsSelected(string id) => _selected.Contains(id);
}