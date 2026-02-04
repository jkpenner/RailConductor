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

    public TrackLocation Move(TrackLocation location, float distance)
    {
        var current = location;
        var remaining = distance;
        var safetyCounter = 1000;

        while (!Mathf.IsZeroApprox(remaining))
        {
            if (--safetyCounter <= 0)
            {
                GD.PushError("Move exceeded safety limit; possible infinite loop.");
                break;
            }

            var newCurrent = current.Move(remaining, out var overflow);
            if (Mathf.IsZeroApprox(overflow))
            {
                return current;
            }

            TrackGraphNode node;
            TrackGraphLink? nextLink;
            TrackLocation nextLocation;

            // Positive overflow: Moving forwards arrived at Face node.
            if (overflow > 0f)
            {
                node = newCurrent.Face;
                nextLink = node.GetConnectedLink(newCurrent.Link);
                
                // Check for dead end
                if (nextLink is null)
                {
                    return newCurrent;
                }

                var farEnd = nextLink.GetOtherNode(node);
                nextLocation = new TrackLocation(nextLink, farEnd, 0f);
            }
            // Negative overflow: Moving backwards arrived at Other node.
            else
            {
                node = newCurrent.Other;
                nextLink = node.GetConnectedLink(newCurrent.Link);
                
                // Check for dead end
                if (nextLink is null)
                {
                    return newCurrent;
                }
                
                // Start at end (t=1) for backward continuation
                nextLocation = new TrackLocation(nextLink, node, 1f);
            }
            
            current = nextLocation;
            remaining = overflow;
        }

        return current;
    }

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

    public TrackGraphNode? GetNode(int id)
        => _nodesById.GetValueOrDefault(id);

    public TrackGraphLink? GetLink(int linkId)
        => _linksById.GetValueOrDefault(linkId);

    public TrackGraphLink? GetLink(TrackKey keyA, TrackKey keyB)
    {
        var nodeA = GetNode(keyA);
        var nodeB = GetNode(keyB);

        if (nodeA is null || nodeB is null)
        {
            return null;
        }
        
        return GetLink(nodeA.Id, nodeB.Id);
    }

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