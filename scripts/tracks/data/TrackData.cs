using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackData : Resource
{
    [Export] private Godot.Collections.Dictionary<int, TrackNodeData> _nodes = new();

    public IEnumerable<TrackNodeData> GetNodes() => _nodes.Values;

    public TrackNodeData? GetNode(int id) => _nodes.GetValueOrDefault(id);

    public void AddNode(int id, TrackNodeData newNode) => _nodes.Add(id, newNode);

    public void RemoveNode(int id) => _nodes.Remove(id);

    public bool LinkNodes(int nodeId1, int nodeId2)
    {
        var node1 = GetNode(nodeId1);
        var node2 = GetNode(nodeId2);

        if (node1 is null || node2 is null)
        {
            return false;
        }

        if (!node1.Links.Contains(nodeId2))
        {
            node1.Links.Add(nodeId2);
        }

        if (!node2.Links.Contains(nodeId1))
        {
            node2.Links.Add(nodeId1);
        }

        return true;
    }

    public int GetAvailableNodeId()
    {
        var id = 0;

        foreach (var key in _nodes.Keys)
        {
            if (id <= key)
            {
                id = key + 1;
            }
        }

        return id;
    }

    public int FindClosestNodeId(Vector2 position)
    {
        if (_nodes.Count == 0)
        {
            return -1;
        }

        var minDist = float.MaxValue;
        var closest = -1;

        foreach (var (id, node) in _nodes)
        {
            var dist = node.Position.DistanceTo(position);
            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = id;
        }

        return minDist < 20f ? closest : -1;
    }

    public (int, int) FindClosestLink(Vector2 position)
    {
        if (_nodes.Count < 2)
        {
            return (-1, -1);
        }

        var links = GetUniqueLinks();

        if (links.Count == 0)
        {
            return (-1, -1);
        }

        var minDist = float.MaxValue;
        var closestLink = (-1, -1);

        foreach (var link in links)
        {
            var node1 = GetNode(link.Item1);
            var node2 = GetNode(link.Item2);

            if (node1 == null || node2 == null)
            {
                continue;
            }

            var dist = DistanceToSegment(position, node1.Position, node2.Position);

            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closestLink = link;
        }

        return minDist < 20f ? closestLink : (-1, -1);
    }

    public float GetClosestLinkDistance(Vector2 position)
    {
        if (_nodes.Count < 2)
        {
            return float.MaxValue;
        }

        var links = GetUniqueLinks();

        if (links.Count == 0)
        {
            return float.MaxValue;
        }

        var minDist = float.MaxValue;

        foreach (var link in links)
        {
            var node1 = GetNode(link.Item1);
            var node2 = GetNode(link.Item2);

            if (node1 == null || node2 == null)
            {
                continue;
            }

            var dist = DistanceToSegment(position, node1.Position, node2.Position);

            if (dist < minDist)
            {
                minDist = dist;
            }
        }

        return minDist;
    }

    private HashSet<(int, int)> GetUniqueLinks()
    {
        var links = new HashSet<(int, int)>();

        foreach (var node in _nodes.Values)
        {
            foreach (var linkId in node.Links)
            {
                var id1 = Mathf.Min(node.Id, linkId);
                var id2 = Mathf.Max(node.Id, linkId);
                links.Add((id1, id2));
            }
        }

        return links;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = p - a;
        
        var len2 = ab.LengthSquared();
        if (len2 == 0)
        {
            return p.DistanceTo(a);
        }

        var proj = ap.Dot(ab);
        var t = Mathf.Clamp(proj / len2, 0f, 1f);
        var projection = a + t * ab;
        return p.DistanceTo(projection);
    }
}