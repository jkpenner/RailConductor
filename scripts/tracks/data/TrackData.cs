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
}