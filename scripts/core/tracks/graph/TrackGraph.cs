using System.Collections.Generic;

namespace RailConductor;

public sealed class TrackGraph
{
    public IReadOnlyList<TrackGraphNode> Nodes => _nodes;
    public IReadOnlyList<TrackGraphEdge> Edges => _edges;

    private readonly List<TrackGraphNode> _nodes = [];
    private readonly List<TrackGraphEdge> _edges = [];

    private readonly Dictionary<string, TrackGraphNode> _nodeLookup = new();
    private readonly Dictionary<string, TrackGraphEdge> _edgeLookup = new();
    
    public TrackGraphNode? GetNode(string id) => _nodeLookup.GetValueOrDefault(id);
    public TrackGraphEdge? GetEdge(string id) => _edgeLookup.GetValueOrDefault(id);

    public void AddNode(TrackGraphNode node)
    {
        _nodes.Add(node);
        _nodeLookup[node.Id] = node;
    }

    public void AddEdge(TrackGraphEdge edge)
    {
        _edges.Add(edge);
        _edgeLookup[edge.Id] = edge;

        edge.NodeA.OutgoingEdges.Add(edge);
        edge.NodeB.OutgoingEdges.Add(edge);
    }
}