namespace RailConductor;

public abstract class TrackGraphBuildPhase
{
    public abstract int PhaseOrder { get; }
    public abstract void Process(TrackGraph graph, TrackData data, TrackSettings settings);
}