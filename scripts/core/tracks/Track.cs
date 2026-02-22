using Godot;
using System.Collections.Generic;

namespace RailConductor;

[Tool]
public partial class Track : Node2D
{
    [Export]
    public TrackData Data { get; set; }

    [Export]
    public TrackSettings Settings { get; set; } = new();

    // Runtime containers (auto-created)
    private Node2D _segments;
    private Node2D _nodes;
    private Node2D _signals;
    private Node2D _platforms;

    private TrackGraph? _graph;
    private TrackGraphBuilder? _graphBuilder;

    public override void _Ready()
    {
        if (Data == null || Settings == null)
        {
            if (!Engine.IsEditorHint())
                GD.PushWarning("Track missing Data or Settings");
            return;
        }

        // Runtime only – editor drawing is still handled by the plugin
        if (!Engine.IsEditorHint())
        {
            BuildRuntimeVisuals();
        }
    }

    private void BuildRuntimeVisuals()
    {
        CreateContainers();
        ClearAllContainers();

        BuildLinksAsSegments();
        BuildNodes();
        BuildSignals();
        BuildPlatforms();
        BuildPlatformGroups();
    }

    private void CreateContainers()
    {
        _segments = GetOrCreateContainer(Settings.SegmentsContainerPath, "Segments");
        _nodes     = GetOrCreateContainer(Settings.NodesContainerPath, "Nodes");
        _signals   = GetOrCreateContainer(Settings.SignalsContainerPath, "Signals");
        _platforms = GetOrCreateContainer(Settings.PlatformsContainerPath, "Platforms");
    }

    private Node2D GetOrCreateContainer(NodePath path, string fallbackName)
    {
        var container = GetNodeOrNull<Node2D>(path);
        if (container == null)
        {
            container = new Node2D { Name = fallbackName };
            AddChild(container);
        }
        return container;
    }

    private void ClearAllContainers()
    {
        ClearChildren(_segments);
        ClearChildren(_nodes);
        ClearChildren(_signals);
        ClearChildren(_platforms);
    }

    private static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren()) child.QueueFree();
    }

    // ========================================================================
    // BUILD HELPERS
    // ========================================================================

    private void BuildLinksAsSegments()
    {
        if (Settings.TrackSegmentScene == null) return;

        foreach (var link in Data.GetLinks())
        {
            var nodeA = Data.GetNode(link.NodeAId);
            var nodeB = Data.GetNode(link.NodeBId);
            if (nodeA == null || nodeB == null) continue;

            var instance = Settings.TrackSegmentScene.Instantiate<Node2D>();

            // Position at midpoint, rotate to match link
            var mid = nodeA.Position.Lerp(nodeB.Position, 0.5f);
            var dir = nodeB.Position - nodeA.Position;
            instance.Position = mid;
            instance.Rotation = dir.Angle();

            // Stretch to exact length (most segment scenes use Scale.X)
            float length = dir.Length();
            if (instance.HasMethod("SetLength"))
                instance.Call("SetLength", length);
            else
                instance.Scale = new Vector2(length, Settings.SegmentWidth);

            // Optional Setup call for custom logic / collision
            if (instance.HasMethod("Setup"))
                instance.Call("Setup", link, Data);

            _segments.AddChild(instance);
        }
    }

    private void BuildNodes()
    {
        foreach (var nodeData in Data.GetNodes())
        {
            var scene = nodeData.NodeType switch
            {
                TrackNodeType.Switch    => Settings.SwitchNodeScene,
                TrackNodeType.Crossover => Settings.CrossoverNodeScene,
                _                       => Settings.BasicNodeScene
            };

            if (scene == null) continue;

            var instance = scene.Instantiate<Node2D>();
            instance.Position = nodeData.Position;

            if (instance.HasMethod("Setup"))
                instance.Call("Setup", nodeData, Data);

            _nodes.AddChild(instance);
        }
    }

    private void BuildSignals()
    {
        if (Settings.SignalScene == null) return;

        foreach (var signal in Data.GetSignals())
        {
            var posInfo = Data.GetSignalPosition(signal);
            if (!posInfo.HasValue) continue;

            var instance = Settings.SignalScene.Instantiate<Node2D>();
            instance.Position = posInfo.Value.Position;
            instance.Rotation = posInfo.Value.Angle;

            if (instance.HasMethod("Setup"))
                instance.Call("Setup", signal, Data);

            _signals.AddChild(instance);
        }
    }

    private void BuildPlatforms()
    {
        if (Settings.PlatformScene == null) return;

        foreach (var platform in Data.GetPlatforms())
        {
            var instance = Settings.PlatformScene.Instantiate<Node2D>();
            instance.Position = platform.Position;

            if (instance.HasMethod("Setup"))
                instance.Call("Setup", platform, Data);

            _platforms.AddChild(instance);
        }
    }

    private void BuildPlatformGroups()
    {
        if (Settings.PlatformGroupScene == null) return;

        foreach (var group in Data.GetPlatformGroups())
        {
            var instance = Settings.PlatformGroupScene.Instantiate<Node2D>();
            instance.Position = group.Position;

            if (instance.HasMethod("Setup"))
                instance.Call("Setup", group, Data);

            AddChild(instance);   // groups sit at root level
        }
    }
}