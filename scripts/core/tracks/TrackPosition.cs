using System;
using Godot;

namespace RailConductor;

public record TrackPosition(TrackGraphEdge Edge, TrackGraphNode Face, float NormalizedPosition = 1f)
{
    public TrackGraphNode Other => Edge.GetOtherNode(Face);

    /// <summary>
    /// Gets the node in which the location is facing.
    /// </summary>
    public float DistanceToNextNode()
    {
        var linkLength = Edge.Length;
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
    public TrackPosition FlipDirection()
        => new(Edge, Edge.GetOtherNode(Face), 1f - NormalizedPosition);

    /// <summary>
    /// Creates a new track location that is moved along the track link by the given
    /// distances. The moved location is clamped to the link. This will return any
    /// remaining distance not used by the move.
    /// </summary>
    public TrackPosition Move(float distance, out float remainder)
    {
        var linkLength = Edge.Length;
        if (linkLength <= 0f)
        {
            remainder = distance;
            return this;
        }

        var deltaT = distance / linkLength;
        
        // Clamp the new normalized position
        var newT = Mathf.Clamp(NormalizedPosition + deltaT, 0f, 1f);
        remainder = (NormalizedPosition + deltaT - newT) * linkLength;

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
        => Other.Position.Lerp(Face.Position, NormalizedPosition);

    /// <summary>
    /// Gets the forward direction (toward Face).
    /// </summary>
    public Vector2 GetForward()
    {
        var vec = Face.Position - GetGlobalPosition();
        return vec == Vector2.Zero
            ? (Face.Position - Other.Position).Normalized()
            : vec.Normalized();
    }

    /// <summary>
    /// Gets the right direction (perpendicular to forward).
    /// </summary>
    public Vector2 GetRight()
        => GetForward().Rotated(Mathf.Pi * 0.5f);
}