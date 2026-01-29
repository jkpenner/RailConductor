using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Track : Node2D
{
    private readonly Dictionary<TrackKey, List<TrackSegment>> _segments = new();
    private readonly HashSet<TrackKey> _isolators = [];

    public override void _Ready()
    {
        GenerateTrack();
    }

    private void GenerateTrack()
    {
        foreach (var child in GetChildren())
        {
            switch (child)
            {
                case TrackSegment segment:
                    AddTrackSegment(segment);
                    break;
                case TrackIsolator isolator:
                    AddTrackIsolator(isolator);
                    break;
            }
        }
    }

    private void AddTrackIsolator(TrackIsolator isolator)
    {
        foreach (var connection in isolator.GetConnections())
        {
            _isolators.Add(connection);
        }
    }

    private void AddTrackSegment(TrackSegment segment)
    {
        foreach (var location in segment.GetConnections())
        {
            if (_segments.TryGetValue(location, out var segments))
            {
                segments.Add(segment);
            }
            else
            {
                _segments.Add(location, [segment]);
            }
        }
    }

    public List<TrackSegment> GetSegmentsInCircuit(TrackSegment segment)
    {
        var connected = new HashSet<TrackSegment>();
        var closedKeys = new HashSet<TrackKey>();
        
        var openKeys = segment.GetConnections().ToList();

        while (openKeys.Count > 0)
        {
            var current = openKeys[0];
            openKeys.RemoveAt(0);
            closedKeys.Add(current);

            // The key is an isolator, the circuit does not extend.
            if (_isolators.Contains(current))
            {
                continue;
            }
            
            if (!_segments.TryGetValue(current, out var connections))
            {
                continue;
            }

            foreach (var connection in connections)
            {
                if (connected.Contains(connection))
                {
                    continue;
                }

                if (!connection.IsUsable)
                {
                    continue;
                }
                
                connected.Add(connection);

                foreach (var key in connection.GetConnections())
                {
                    if (closedKeys.Contains(key) || openKeys.Contains(key))
                    {
                        continue;
                    }
                    
                    openKeys.Add(key);
                }
            }
        }
        
        return connected.ToList();
    }
}