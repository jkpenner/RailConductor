using System;
using Godot;

namespace RailConductor;

public readonly struct TrackKey: IEquatable<TrackKey>
{
    public required long X { get; init; }
    public required long Y { get; init; }

    /// <summary>
    /// Create a TrackKey based on a global position.
    /// </summary>
    public static TrackKey From(Vector2 globalPosition)
    {
        return new TrackKey
        {
            X = Mathf.RoundToInt(globalPosition.X),
            Y = Mathf.RoundToInt(globalPosition.Y)
        };
    }

    public bool Equals(TrackKey other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is TrackKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    public static bool operator ==(TrackKey left, TrackKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrackKey left, TrackKey right)
    {
        return !(left == right);
    }
}