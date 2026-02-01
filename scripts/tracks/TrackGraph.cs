using System.Collections.Generic;
using Godot;

namespace RailConductor;

public class TrackGraph
{
    private readonly Dictionary<TrackKey, TrackGraphNode> _nodesByPos = [];

    private readonly Dictionary<int, TrackGraphNode> _nodesById = new();

    private readonly Dictionary<int, TrackGraphLink> _linksById = new();
    private readonly Dictionary<(int, int), TrackGraphLink> _linksByNodePair = new();
    private readonly Dictionary<int, List<TrackGraphLink>> _linksByNodeId = new();

    private readonly List<TrackGraphLink> _links = [];

    private int _nextId = 1;

    public TrackGraphNode CreateOrGetNode(Vector2 position)
    {
        var key = TrackKey.From(position);
        if (_nodesByPos.TryGetValue(key, out var node))
        {
            return node;
        }

        node = new TrackGraphNode
        {
            Id = _nextId++,
            GlobalPosition = position,
        };

        _nodesById[node.Id] = node;
        _nodesByPos[key] = node;

        return node;
    }

    public IEnumerable<TrackGraphNode> GetNodes()
        => _nodesByPos.Values;

    public IEnumerable<TrackGraphLink> GetLinks(int nodeId)
        => _linksByNodeId.GetValueOrDefault(nodeId) ?? [];

    public TrackGraphNode? GetNode(TrackKey key)
        => _nodesByPos.GetValueOrDefault(key);

    public TrackGraphLink? GetLink(TrackGraphNode nodeA, TrackGraphNode nodeB)
        => GetLink(nodeA.Id, nodeB.Id);

    public TrackGraphLink? GetLink(int nodeAId, int nodeBId)
        => _linksByNodePair.GetValueOrDefault((nodeAId, nodeBId));

    public TrackGraphLink AddLink(TrackGraphNode nodeA, TrackGraphNode nodeB)
    {
        var link = new TrackGraphLink
        {
            Id = _nextId++,
            NodeA = nodeA,
            NodeB = nodeB,
        };

        _linksById[link.Id] = link;

        // Register link for both directions
        _linksByNodePair[(nodeA.Id, nodeB.Id)] = link;
        _linksByNodePair[(nodeB.Id, nodeA.Id)] = link;

        if (!_linksByNodeId.TryGetValue(nodeA.Id, out var linksA))
        {
            linksA = [];
            _linksByNodeId[nodeA.Id] = linksA;
        }

        linksA.Add(link);

        if (!_linksByNodeId.TryGetValue(nodeA.Id, out var linksB))
        {
            linksB = [];
            _linksByNodeId[nodeA.Id] = linksB;
        }

        linksB.Add(link);


        return link;
    }
}

public class TrackPosition
{
    public required TrackGraph Graph { get; init; }
    public required TrackGraphLink Link { get; init; }
    public required TrackGraphNode ForwardNode { get; init; }

    public float T { get; set; }

    public Vector2 GetGlobalPosition()
    {
        var other = Link.GetOtherNode(ForwardNode);
        return other.GlobalPosition.Lerp(ForwardNode.GlobalPosition, T);
    }

    /// <summary>
    /// Moves the position along the current link based on the ccurrent ForwardNode.
    /// </summary>
    /// <param name="amount">A positive or negative value</param>
    /// <returns>Any remaining movement</returns>
    public float Move(float amount)
    {
        var linkLength = Link.GetLength();
        if (linkLength == 0f)
        {
            return amount;
        }

        var target = T + (amount / linkLength);
        T = Mathf.Clamp(target, 0f, 1f);
        return target - T;
    }

    /// <summary>
    /// Returns true if the position is approximately at a node. The comparison is done using
    /// a tolerance calculation with Epsilon.
    /// </summary>
    /// <returns>A bool for whether or not the position is approximately at a node.</returns>
    public bool IsApproxAtNode()
        => Mathf.IsEqualApprox(T, 1f) || Mathf.IsZeroApprox(T);
}