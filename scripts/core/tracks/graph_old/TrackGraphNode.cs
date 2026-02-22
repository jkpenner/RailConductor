using System.Collections.Generic;
using Godot;

namespace RailConductor.GraphOld;

public class TrackGraphNode
{
    public required int Id { get; init; }
    public required Vector2 GlobalPosition { get; init; }

    public bool IsSwitch { get; set; }
    public int ActiveIncomingLink { get; set; }
    public int ActiveOutgoingLink { get; set; }

    public TrackGraphLink[] IncomingLinks { get; set; } = [];
    public TrackGraphLink[] OutgoingLinks { get; set; } = [];

    public bool IsCircuitIsolator { get; set; }

    private Dictionary<TrackGraphLink, TrackGraphLink> _connections = [];

    public void RebuildConnections()
    {
        _connections = new Dictionary<TrackGraphLink, TrackGraphLink>();

        if (IsSwitch)
        {
            foreach (var incoming in IncomingLinks)
            {
                _connections.Add(incoming, OutgoingLinks[ActiveOutgoingLink]);
            }

            foreach (var outgoing in OutgoingLinks)
            {
                _connections.Add(outgoing, IncomingLinks[ActiveIncomingLink]);
            }
        }
        else
        {
            // We assume a one-to-one relationship, otherwise the link terminates
            var minLength = Mathf.Min(IncomingLinks.Length, OutgoingLinks.Length);
            for (var i = 0; i < minLength; i++)
            {
                _connections.Add(IncomingLinks[i], OutgoingLinks[i]);
                _connections.Add(OutgoingLinks[i], IncomingLinks[i]);
            }
        }
    }

    public TrackGraphLink? GetConnectedLink(TrackGraphLink link)
        => _connections.GetValueOrDefault(link);
}