using System.Collections.Generic;

namespace RailConductor;

public interface ITrackObject
{
    IEnumerable<TrackKey> GetConnections();
}