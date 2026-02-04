using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackSegment : Interactable, ITrackGraphBuildHandler
{
    public int GraphBuildPhase => BuildPhase.Links;
    
    [Export]
    public float SegmentWidth
    {
        get => _segmentWidth;
        set
        {
            _segmentWidth = value;
            if (Engine.IsEditorHint())
            {
                UpdateTrackSegment();
            }
        }
    }

    [Export]
    public TrackSettings? Settings { get; set; }

    private float _segmentWidth = 20f;

    private Line2D? _line;
    private TrackNode? _endA;
    private TrackNode? _endB;
    private CollisionShape2D? _shape;

    private bool _useOverrideColor = false;
    private Color _overrideColor;

    /// <summary>
    /// Indicates whether the track segment can be used by a train.
    /// </summary>
    public bool IsUsable { get; private set; }

    public TrackNode EndA => _endA ?? throw new NullReferenceException();
    public TrackNode EndB => _endB ?? throw new NullReferenceException();

    /// <summary>
    /// Occurs when the value of the IsUsable property changes.
    /// </summary>
    public event Action? UsabilityChanged;

    public override void _Ready()
    {
        _line = GetNodeOrNull<Line2D>(nameof(Line2D));
        _endA = GetNodeOrNull<TrackNode>("ConnectionA");
        _endB = GetNodeOrNull<TrackNode>("ConnectionB");
        _shape = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));

        if (Engine.IsEditorHint())
        {
            _endA.LocalPositionChanged += OnJunctionChanged;
            _endB.LocalPositionChanged += OnJunctionChanged;
        }
    }

    protected override void OnInteraction()
    {
        // Interaction
    }

    public void SetOverrideColor(Color color)
    {
        _useOverrideColor = true;
        _overrideColor = color;
        UpdateVisuals();
    }

    public void ClearOverrideColor()
    {
        _useOverrideColor = false;
        UpdateVisuals();
    }

    /// <summary>
    /// Updates the track segment visuals based on its current state.
    /// </summary>
    private void UpdateVisuals()
    {
        if (_line is null)
        {
            return;
        }

        if (_useOverrideColor)
        {
            _line.Modulate = _overrideColor;
            return;
        }

        if (Settings is null)
        {
            return;
        }

        _line.Modulate = Settings.SegmentNormalColor;
    }

    public void SetIsUsable(bool isUsable)
    {
        SetVisible(isUsable);
        IsUsable = isUsable;
        UsabilityChanged?.Invoke();
    }
    
    private void OnJunctionChanged()
    {
        if (Engine.IsEditorHint())
        {
            UpdateTrackSegment();
        }
    }

    private void UpdateTrackSegment()
    {
        // Get the current positions of both ends of the segment.
        var endA = _endA?.GlobalPosition ?? GlobalPosition;
        var endB = _endB?.GlobalPosition ?? GlobalPosition;

        // Disable end notifications to prevent infinite loop.
        _endA?.SetNotifyLocalTransform(false);
        _endB?.SetNotifyLocalTransform(false);

        // Move segments position between both ends
        GlobalPosition = endA.Lerp(endB, 0.5f);

        if (_endA is not null)
        {
            _endA.GlobalPosition = endA; // = ToLocal(endA);
        }

        if (_endB is not null)
        {
            _endB.GlobalPosition = endB; //ToLocal(endB);
        }

        // Restore notifications after position updates.
        _endA?.SetNotifyLocalTransform(true);
        _endB?.SetNotifyLocalTransform(true);

        // Update the Line2D based on the junctions
        if (_line is not null)
        {
            _line.Position = Vector2.Zero;
            _line.SetPoints([ToLocal(endA), ToLocal(endB)]);
        }

        // Update collision shape based on new positions
        if (_shape is not null)
        {
            _shape.Position = Vector2.Zero;
            _shape.GlobalRotation = (endB - endA).Angle();

            if (_shape.Shape is not RectangleShape2D shape)
            {
                shape = new RectangleShape2D();
                _shape.Shape = shape;
            }

            shape.Size = new Vector2((endB - endA).Length(), SegmentWidth);
        }
    }
    
    public void OnGraphBuildPhase(TrackGraph graph)
    {
        var keyA = EndA.GetTrackKey();
        var nodeA = graph.GetNode(keyA);
        if (nodeA is null)
        {
            GD.PushWarning($"Track node {keyA} not registered");
            return;
        }

        var keyB = EndB.GetTrackKey();
        var nodeB = graph.GetNode(keyB);
        if (nodeB is null)
        {
            GD.PushWarning($"Track node {keyB} not registered");
            return;
        }

        graph.AddLink(nodeA, nodeB);
    }
}