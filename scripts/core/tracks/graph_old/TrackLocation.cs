using System;
using Godot;

namespace RailConductor.GraphOld;

public record TrackLocation(TrackGraphLink Link, TrackGraphNode Face, float NormalizedPosition = 1f)
{
    public TrackGraphNode Other => Link.GetOtherNode(Face);

    /// <summary>
    /// Gets the node in which the location is facing.
    /// </summary>
    public float DistanceToNextNode()
    {
        var linkLength = Link.GetLength();
        if (linkLength <= 0f)
        {
            return 0f;
        }

        return linkLength * (1f - NormalizedPosition);
    }

    /// <summary>
    /// Creates a new track location that if facing the opposite direction.
    /// </summary>
    /// <returns></returns>
    public TrackLocation FlipDirection()
        => new(Link, Link.GetOtherNode(Face), 1f - NormalizedPosition);

    /// <summary>
    /// Creates a new track location that is moved along the track link by the given
    /// distances. The moved location is clamped to the link. This will return any
    /// remaining distance not used by the move.
    /// </summary>
    public TrackLocation Move(float distance, out float remainder)
    {
        var linkLength = Link.GetLength();
        if (linkLength == 0f)
        {
            remainder = distance;
            return this;
        }

        var deltaT = distance / linkLength;
        var target = NormalizedPosition + deltaT;
        var newT = Mathf.Clamp(target, 0f, 1f);

        remainder = (target - newT) * linkLength;
        return this with { NormalizedPosition = newT };
    }

    /// <summary>
    /// Returns true if the position is approximately at a node (using float epsilon).
    /// </summary>
    public bool IsApproxAtNode()
        => Mathf.IsEqualApprox(NormalizedPosition, 1f) || Mathf.IsZeroApprox(NormalizedPosition);

    /// <summary>
    /// Calculates the global position along the track segment.
    /// </summary>
    public Vector2 GetGlobalPosition()
        => Other.GlobalPosition.Lerp(Face.GlobalPosition, NormalizedPosition);

    /// <summary>
    /// Gets the forward direction (toward Face).
    /// </summary>
    public Vector2 GetForward()
    {
        var vec = Face.GlobalPosition - GetGlobalPosition();
        return vec == Vector2.Zero
            ? (Face.GlobalPosition - Other.GlobalPosition).Normalized()
            : vec.Normalized();
    }

    /// <summary>
    /// Gets the right direction (perpendicular to forward).
    /// </summary>
    public Vector2 GetRight()
        => GetForward().Rotated(Mathf.Pi * 0.5f);
}