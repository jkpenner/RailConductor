using System;
using System.Collections.Generic;
using Godot;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackSegment : Interactable, ITrackObject
{
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
    private TrackConnection? _endA;
    private TrackConnection? _endB;
    private CollisionShape2D? _shape;

    private bool _useOverrideColor = false;
    private Color _overrideColor;

    /// <summary>
    /// Indicates whether the track segment can be used by a train.
    /// </summary>
    public bool IsUsable { get; private set; }

    public TrackConnection EndA => _endA ?? throw new NullReferenceException();
    public TrackConnection EndB => _endB ?? throw new NullReferenceException();

    /// <summary>
    /// Occurs when the value of the IsUsable property changes.
    /// </summary>
    public event Action? UsabilityChanged;

    public override void _Ready()
    {
        _line = GetNodeOrNull<Line2D>(nameof(Line2D));
        _endA = GetNodeOrNull<TrackConnection>("ConnectionA");
        _endB = GetNodeOrNull<TrackConnection>("ConnectionB");
        _shape = GetNodeOrNull<CollisionShape2D>(nameof(CollisionShape2D));

        if (Engine.IsEditorHint())
        {
            _endA.LocalPositionChanged += OnJunctionChanged;
            _endB.LocalPositionChanged += OnJunctionChanged;
        }
    }

    public IEnumerable<TrackKey> GetConnections()
    {
        if (Engine.IsEditorHint())
        {
            throw new InvalidOperationException($"{nameof(GetConnections)} not supported while in editor.");
        }

        return
        [
            TrackKey.From(EndA.GlobalPosition),
            TrackKey.From(EndB.GlobalPosition)
        ];
    }

    protected override void OnInteraction()
    {
        // Interaction
    }
    
    /// <summary>
    /// Retrieves the position of the junction at the A end of the track segment.
    /// </summary>
    public Vector2 GetEndAPosition() => EndA.GlobalPosition;

    public TrackKey GetEndAKey() => TrackKey.From(GetEndAPosition());

    /// <summary>
    /// Retrieves the position of the junction at the B end of the track segment.
    /// </summary>
    public Vector2 GetEndBPosition() => EndB.GlobalPosition;

    public TrackKey GetEndBKey() => TrackKey.From(GetEndBPosition());
    
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

    public Vector2 FindClosestPoint(Vector2 point)
    {
        var pointA = GetEndAPosition();
        var pointB = GetEndBPosition();

        var lineSegment = pointB - pointA;
        var pointAP = point - pointA;

        var t = pointAP.Dot(lineSegment) / lineSegment.Length();

        return t switch
        {
            < 0.0f => pointA,
            > 1.0f => pointB,
            _ => pointA + t * lineSegment
        };
    }

    public Vector2 FindPointAtExactDistance(Vector2 point, float distance)
    {
        var a = GetEndAPosition();
        var b = GetEndBPosition();
        var ab = b - a;
        var lenSq = ab.LengthSquared();

        if (lenSq < 0.00001f)
        {
            return point.DistanceTo(a) <= distance ? a : b;
        }

        var ap = point - a;
        var proj = ap.Dot(ab) / lenSq; // t in [0..1] range would be clamped

        var closest = Geometry2D.GetClosestPointToSegment(point, a, b);
        var distClosest = point.DistanceTo(closest);

        if (Mathf.Abs(distClosest - distance) < 0.001f)
            return closest;

        // Quadratic: |a + t·ab - point|² = distance²
        var A = lenSq;
        var B = 2.0f * ap.Dot(ab);
        var C = ap.LengthSquared() - distance * distance;

        var discriminant = B * B - 4.0f * A * C;
        if (discriminant < 0)
        {
            // No real intersection → return closer endpoint
            return point.DistanceTo(a) < point.DistanceTo(b) ? a : b;
        }

        var sqrtD = Mathf.Sqrt(discriminant);
        var t1 = (-B - sqrtD) / (2.0f * A);
        var t2 = (-B + sqrtD) / (2.0f * A);

        // Return first valid point on segment (prefer smaller t = closer to A)
        if (t1 >= 0f && t1 <= 1f)
            return a + ab * t1;

        if (t2 >= 0f && t2 <= 1f)
            return a + ab * t2;

        // No intersection on segment → fallback to closest endpoint
        return point.DistanceTo(a) < point.DistanceTo(b) ? a : b;
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
}