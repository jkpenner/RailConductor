using System.Collections.Generic;
using Godot;

namespace RailConductor;

public sealed class TrackVisualBuilder(Track track)
{
    private const string NodeContainerName = "Nodes";
    private const string SignalContainerName = "Signals";
    private const string SegmentContainerName = "Segments";
    private const string PlatformContainerName = "Platforms";

    private Node2D? _nodes;
    private Node2D? _signals;
    private Node2D? _segments;
    private Node2D? _platforms;

    private readonly Dictionary<string, TrackVisual> _visuals = new();

    public void Build()
    {
        CreateContainers();

        BuildNodes();
        BuildSegments();
        BuildSignals();
    }

    private void Clear()
    {
        foreach (var (id, visual) in _visuals)
        {
            visual.OnDetach(track, id);
            visual.QueueFree();
        }
        _visuals.Clear();
    }

    private void BuildNodes()
    {
        if (track.Graph is null)
        {
            return;
        }
        
        foreach (var node in track.Graph.Nodes)
        {
            if (!_visuals.TryGetValue(node.Id, out var visual))
            {
                var packedScene = node.NodeType switch
                {
                    TrackNodeType.Basic => track.Settings.BasicNodeScene,
                    TrackNodeType.Switch => track.Settings.SwitchNodeScene,
                    TrackNodeType.Crossover => track.Settings.CrossoverNodeScene,
                    _ => null
                };

                visual = packedScene?.InstantiateOrNull<TrackVisual>();
                if (visual is null)
                {
                    continue;
                }

                var parent = _nodes ?? track;
                parent.AddChild(visual);
                
                _visuals[node.Id] = visual;
                visual.OnAttach(track, node.Id);
            }

            visual.Sync(track, node.Id);
        }
    }

    private void BuildSegments()
    {
        if (track.Graph is null)
        {
            return;
        }
        
        foreach (var edge in track.Graph.Edges)
        {
            if (!_visuals.TryGetValue(edge.Id, out var visual))
            {
                var packedScene = track.Settings.TrackSegmentScene;
                visual = packedScene?.InstantiateOrNull<TrackVisual>();
                if (visual is null)
                {
                    continue;
                }

                var parent = _segments ?? track;
                parent.AddChild(visual);
                
                _visuals[edge.Id] = visual;
                visual.OnAttach(track, edge.Id);
            }

            visual.Sync(track, edge.Id);
        }
    }
    
    private void BuildSignals()
    {
        if (track.Data is null)
        {
            return;
        }
        
        foreach (var signal in track.Data.GetSignals())
        {
            if (!_visuals.TryGetValue(signal.Id, out var visual))
            {
                var packedScene = track.Settings.SignalScene;
                visual = packedScene?.InstantiateOrNull<TrackVisual>();
                if (visual is null)
                {
                    continue;
                }

                var parent = _signals ?? track;
                parent.AddChild(visual);
                
                _visuals[signal.Id] = visual;
                visual.OnAttach(track, signal.Id);
            }

            visual.Sync(track, signal.Id);
        }
    }

    private void CreateContainers()
    {
        _platforms = GetOrCreateContainer(track.Settings.PlatformsContainerPath, "Platforms");
        _segments = GetOrCreateContainer(track.Settings.SegmentsContainerPath, "Segments");
        _nodes = GetOrCreateContainer(track.Settings.NodesContainerPath, "Nodes");
        _signals = GetOrCreateContainer(track.Settings.SignalsContainerPath, "Signals");
    }

    private Node2D GetOrCreateContainer(NodePath path, string fallbackName)
    {
        var container = track.GetNodeOrNull<Node2D>(path);
        if (container == null)
        {
            container = new Node2D { Name = fallbackName };
            track.AddChild(container);
        }

        return container;
    }
}