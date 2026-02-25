using System;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class SegmentVisual : TrackVisual
{
    private Line2D? _line;
    private CollisionShape2D? _shape;

    private bool _useOverrideColor = false;
    private Color _overrideColor;

    /// <summary>
    /// Indicates whether the track segment can be used by a train.
    /// </summary>
    public bool IsUsable { get; private set; }

    /// <summary>
    /// Occurs when the value of the IsUsable property changes.
    /// </summary>
    public event Action? UsabilityChanged;

    public override void _Ready()
    {
        _line = GetNodeOrNull<Line2D>(nameof(Line2D));
        _shape = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));

        if (Engine.IsEditorHint())
        {
            // _endA.LocalPositionChanged += OnJunctionChanged;
            // _endB.LocalPositionChanged += OnJunctionChanged;
        }
    }
    
    public override void OnAttach(Track track, string id)
    {
        var state = track.State?.GetSegmentState(id);
        if (state is null)
        {
            return;
        }

        state.Changed += OnStateChanged;
        OnStateChanged(state);
    }
    
    public override void OnDetach(Track track, string id)
    {
        var state = track.State?.GetSegmentState(id);
        if (state is null)
        {
            return;
        }

        state.Changed -= OnStateChanged;
    }

    private void OnStateChanged(SegmentState state)
    {
        this.Visible = state.IsVisible;
    }
    
    public override void Sync(Track track, string id)
    {
        var edge = track.Graph?.GetEdge(id);
        if (edge is null)
        {
            return;
        }

        var nodeA = edge.NodeA;
        var nodeB = edge.NodeB;
        
        UpdateTrackSegment(nodeA, nodeB, track.Settings.SegmentWidth);
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
        
        // _line.Modulate = Settings.SegmentNormalColor;
    }

    public void SetIsUsable(bool isUsable)
    {
        SetVisible(isUsable);
        IsUsable = isUsable;
        UsabilityChanged?.Invoke();
    }
    

    private void UpdateTrackSegment(TrackGraphNode nodeA, TrackGraphNode nodeB, float width)
    {
        // Get the current positions of both ends of the segment.
        var endA = nodeA.Position;
        var endB = nodeB.Position;

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

            shape.Size = new Vector2((endB - endA).Length(), width);
        }
    }
}