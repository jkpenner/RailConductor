namespace RailConductor;

public static class TrackBuildPhase
{
    /// <summary>
    /// Create Nodes
    /// </summary>
    public const int Nodes = 0x10;
    /// <summary>
    /// Create Links
    /// </summary>
    public const int Links = 0x20;
    /// <summary>
    /// Setup Switches / Junctions
    /// </summary>
    public const int Junctions = 0x30;
    /// <summary>
    /// Add Signals / Isolators / Speed Restrictions
    /// </summary>
    public const int Restrictions = 0x40;
    /// <summary>
    /// Add Track Circuits
    /// </summary>
    public const int Circuits = 0x50;
    /// <summary>
    /// Validation and Post processing of tracks
    /// </summary>
    public const int Validation = 0x60;
}