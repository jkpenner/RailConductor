using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackData : Resource
{
    [Export]
    public Godot.Collections.Array<TrackNodeData> Nodes { get; set; } = [];

    public void AddNode(TrackNodeData newNode)
    {
        Nodes.Add(newNode);
    }

    public void InsertNode(int index, TrackNodeData newNode)
    {
        Nodes.Insert(index, newNode);
    }

    public void RemoveNode(int index)
    {
        Nodes.RemoveAt(index);
    }

    public void LinkNodes(int node1, int node2) { }

    public int GetAvailableId()
    {
        var id = 0;

        foreach (var node in Nodes)
        {
            if (id <= node.Id)
            {
                id = node.Id + 1;
            }
        }

        return id;
    }

    public int FindClosestNode(Vector2 position)
    {
        if (Nodes.Count == 0)
        {
            return -1;
        }

        var minDist = float.MaxValue;
        var closest = -1;

        for (var i = 0; i < Nodes.Count; i++)
        {
            var dist = Nodes[i].Position.DistanceTo(position);
            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = i;
        }

        return minDist < 20f ? closest : -1;
    }
}