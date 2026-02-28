namespace RailConductor;

/// <summary>
/// Represents one end of a train (A-end or B-end). 
/// Each end carries its own route code for terminating services.
/// </summary>
public enum TrainEnd
{
    /// <summary>A-end (usually the "front" when departing from origin)</summary>
    A,
    
    /// <summary>B-end (the opposite end – becomes active at termini)</summary>
    B
}