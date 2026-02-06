using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackNodeData : Resource
{
    [Export]
    public int Id { get; set; }
    
    [Export]
    public Vector2 Position { get; set; }
    
    [Export]
    public Godot.Collections.Array<int> Links { get; set; } = [];

    public void AddLink(int otherId)
    {
        Links.Add(otherId);
    }

    public void RemoveLink(int otherId)
    {
        Links.Remove(otherId);
    }

    public void InsertLink(int index, int otherId)
    {
        Links.Insert(index, otherId);
    }
}