using Godot;

namespace RailConductor;

public abstract class TrackGraphBuildPhase
{
    public abstract int GraphBuildPhase { get; }
    public abstract void ExecuteBuildPhase(Track track, TrackGraph graph);
}

public abstract class ProcessNodeBuildPhase<TNode> : TrackGraphBuildPhase where TNode : Node
{
    public override void ExecuteBuildPhase(Track track, TrackGraph graph)
    {
        foreach (var child in track.FindChildren("*", typeof(TNode).Name))
        {
            if (child is not TNode node)
            {
                continue;
            }

            ProcessNode(track, graph, node);
        }
    }
    
    protected abstract void ProcessNode(Track track, TrackGraph graph, TNode node);
}