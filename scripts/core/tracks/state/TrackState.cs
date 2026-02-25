using System.Collections.Generic;
using System.Linq;

namespace RailConductor;

public class TrackState
{
    private readonly Dictionary<string, SwitchState> _switches = new();
    private readonly Dictionary<string, SegmentState> _segments = new();
    private readonly Dictionary<string, SignalState> _signals = new();

    public IReadOnlyDictionary<string, SwitchState> Switches => _switches;
    public IReadOnlyDictionary<string, SegmentState> Segments => _segments;
    public IReadOnlyDictionary<string, SignalState> Signals => _signals;
    
    private readonly TrackGraph _graph;
    private readonly TrackData _data;

    public TrackState(TrackGraph graph, TrackData data)
    {
        _graph = graph;
        _data = data;
        InitializeDefaultState();
    }

    private void InitializeDefaultState()
    {
        foreach (var node in _graph.Nodes)
        {
            if (node.NodeType != TrackNodeType.Switch)
            {
                continue;
            }

            var state = new SwitchState();
            _switches[node.Id] = state;
        }
        
        foreach (var edge in _graph.Edges)
        {
            var state = new SegmentState();
            _segments[edge.Id] = state;
        }
        
        foreach (var signal in _data.GetSignals())
        {
            var state = new SignalState();
            _signals[signal.Id] = state;
        }

        foreach (var (id, state) in _switches)
        {
            SetSwitchAlignment(id, state.Alignment);
        }
    }
    
    public SwitchState? GetSwitchState(string nodeId)
        => _switches.GetValueOrDefault(nodeId);

    public SegmentState? GetSegmentState(string edgeId)
        => _segments.GetValueOrDefault(edgeId);

    public SignalState? GetSignalState(string signalId)
        => _signals.GetValueOrDefault(signalId);

    /// <summary>
    /// Sets the alignment of a target switch, along with correctly updates
    /// the related switch track segments.
    /// </summary>
    public void SetSwitchAlignment(string switchId, SwitchAlignment alignment)
    {
        var state = GetSwitchState(switchId);
        if (state is null)
        {
            return;
        }
        
        state.Alignment = alignment;
        
        var data = _data.GetNode(switchId);
        var node = _graph.GetNode(switchId);
        
        if (data is null || node is null)
        {
            return;
        }

        // Get segments currently active for the switch's alignment
        var pair = data.PairedLinks[(int)state.Alignment];

        // Update each assigned edge's visible based on if they
        // are active for the switch alignment.
        foreach (var edge in node.OutgoingEdges)
        {
            var edgeState = GetSegmentState(edge.Id);
            if (edgeState is null)
            {
                continue;
            }

            edgeState.IsVisible = pair.Contains(edge.Id) || pair.Contains(edge.AltId);
        }
    }
}