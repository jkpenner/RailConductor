using System.Collections.Generic;
using Godot;

namespace RailConductor;

public class TrackGraphNode
{
    public string Id { get; set; } = string.Empty;
    public TrackNodeType NodeType { get; set; }
    public Vector2 Position { get; set; }
    public bool IsIsolator { get; set; }

    public List<TrackGraphEdge> OutgoingEdges { get; } = [];
    
    public TrackGraphNode(string id, Vector2 position, TrackNodeType type, bool isIsolator = false)
    {
        Id = id;
        Position = position;
        NodeType = type;
        IsIsolator = isIsolator;
    }
}