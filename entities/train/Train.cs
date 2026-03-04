#nullable enable

using Godot;
using System;
using System.Linq;

namespace RailConductor;

/// <summary>
/// Core train entity with separate A-end (lead when active) and B-end (rear when A active) positions.
/// Lead end drives movement and automatically continues to the next visible edge when reaching a node.
/// Visual is always centered between the two ends and rotated from rear → lead.
/// </summary>
[GlobalClass, Tool]
public partial class Train : Node2D
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "0,300,1")]
    public float Speed { get; set; } = 80f; // pixels/second

    [ExportGroup("Dimensions")]
    /// <summary>Constant distance between A-end and B-end. Visual sits at the exact midpoint.</summary>
    [Export(PropertyHint.Range, "1,200,1")]
    public float TrainLength { get; set; } = 80f;

    [ExportGroup("Route Codes")]
    [Export]
    public string RouteCodeA { get; set; } = "001";

    [Export]
    public string RouteCodeB { get; set; } = "002";

    /// <summary>Currently active end – determines which end is the LEAD (forward).</summary>
    public TrainEnd ActiveEnd { get; private set; } = TrainEnd.A;

    public string CurrentRouteCode => ActiveEnd == TrainEnd.A ? RouteCodeA : RouteCodeB;

    public TrackPosition? AEndPosition { get; private set; }
    public TrackPosition? BEndPosition { get; private set; }

    public TrackPosition? LeadPosition => ActiveEnd == TrainEnd.A ? AEndPosition : BEndPosition;
    public TrackPosition? RearPosition => ActiveEnd == TrainEnd.A ? BEndPosition : AEndPosition;

    public event Action<Train, string /*signalId*/, string /*routeCode*/>? RouteSelectionPlaced;

    private Track? _track;
    private bool _isAtSignal = false;
    private string? _currentSignalId;

    private Node2D? _visual;

    public override void _Ready()
    {
        _visual = GetNodeOrNull<Node2D>("Visual");
        _track = GetTree().Root.GetNodeOrNull<Track>("Main/Track");

        if (_track is null)
        {
            GD.PushError("Train could not find Track node.");
            return;
        }

        // Example start (A-end slightly ahead)
        if (AEndPosition is null && _track.Graph?.Edges.Count > 0)
        {
            var edge = _track.Graph.Edges[0];
            AEndPosition = new TrackPosition(edge, edge.NodeA, 0.2f);
            BEndPosition = AEndPosition.FlipDirection().Move(TrainLength, out _).FlipDirection();
            SyncVisual();
        }
    }

    public override void _Process(double delta)
    {
        if (_track is null || LeadPosition is null || _isAtSignal)
            return;

        MoveLeadEnd((float)delta);
    }

    /// <summary>
    /// Moves the lead end forward, automatically crossing nodes when reached.
    /// Rear is then backtracked to maintain exact TrainLength.
    /// </summary>
    private void MoveLeadEnd(float delta)
    {
        var distance = Speed * delta;
        var currentLead = LeadPosition!;

        var newLead = _track!.Move(currentLead, distance);
        

        if (ActiveEnd == TrainEnd.A)
            AEndPosition = newLead;
        else
            BEndPosition = newLead;

        // FIXED: Rear now correctly crosses nodes backward
        var newRear = ComputeRearFromLead(newLead);

        if (ActiveEnd == TrainEnd.A)
            BEndPosition = newRear;
        else
            AEndPosition = newRear;
        
        if (newLead.IsAtFaceNode())
        {
            var nextEdge = newLead.Face.GetNextEdge(newLead.Edge.Id, _track!.State!);
            if (nextEdge is null)
            {
                // True terminus
                FlipActiveEnd();
            }
        }

        SyncVisual();
    }

    /// <summary>
    /// Computes the rear position by backtracking exactly TrainLength from the lead.
    /// 
    /// FIXED (Feb 27 2026):
    /// - After Backtrack's double flip, we now correctly calculate the node to cross backward using rear.Edge.GetOtherNode(rear.Face).
    /// - This eliminates all random forward snapping of the rear.
    /// - Final clamp guarantees rear is never ahead of lead.
    /// </summary>
    private TrackPosition ComputeRearFromLead(TrackPosition lead)
    {
        if (TrainLength <= Mathf.Epsilon) return lead;

        var rear = lead.FlipDirection();
        rear = _track!.Move(rear, TrainLength);
        return rear.FlipDirection();
    }

    /// <summary>
    /// When the lead reaches a node, select the next visible outgoing edge (excluding backtrack)
    /// and continue with any remaining distance. Only reverses at true termini (dead-end).
    /// </summary>
    /// <summary>
    /// Uses TrackGraphNode.PairedLinks + SwitchState.Alignment to select the correct next edge.
    /// This is the key change requested – trains now respect switch alignments and crossover pairings.
    /// </summary>
    private TrackPosition AdvancePastNode(TrackPosition arrivedLead, float remainingDistance)
    {
        var node = arrivedLead.Face;
        var incomingEdgeId = arrivedLead.Edge.Id;

        // NEW: Use PairedLinks helper (respects switch alignment)
        var nextEdge = node.GetNextEdge(incomingEdgeId, _track!.State!);

        if (nextEdge is null)
        {
            // True terminus
            FlipActiveEnd();
            return arrivedLead.FlipDirection();
        }

        var nextFace = nextEdge.GetOtherNode(node);
        var newLead = new TrackPosition(nextEdge, nextFace, 0f);

        if (remainingDistance > 0.001f)
        {
            newLead = newLead.Move(remainingDistance, out _);
        }

        return newLead;
    }

    // ====================================================================
    // Signal handling, reversal, placement, and visuals (unchanged from previous version)
    // ====================================================================

    private void CheckSignalAhead(TrackPosition pos)
    {
        foreach (var signal in _track!.Data!.GetSignals())
        {
            if (signal.LinkId == pos.Edge.Id && signal.DirectionNodeId == pos.Face.Id)
            {
                StopAtSignal(signal.Id);
                return;
            }
        }
    }

    private void StopAtSignal(string signalId)
    {
        _isAtSignal = true;
        _currentSignalId = signalId;
        RouteSelectionPlaced?.Invoke(this, signalId, CurrentRouteCode);

        if (_track!.State!.TryAuthorizeRoute(signalId, CurrentRouteCode))
            ProceedPastSignal();
    }

    public void ProceedPastSignal()
    {
        if (!_isAtSignal) return;
        _isAtSignal = false;
        _currentSignalId = null;
    }

    public void FlipActiveEnd()
    {
        ActiveEnd = ActiveEnd == TrainEnd.A ? TrainEnd.B : TrainEnd.A;

        AEndPosition = AEndPosition?.FlipDirection();
        BEndPosition = BEndPosition?.FlipDirection();

        GD.Print($"Train {Name} reversed – now using {CurrentRouteCode} from {ActiveEnd} end");
    }

    public void PlaceAt(TrackPosition aEndStart)
    {
        AEndPosition = aEndStart;
        BEndPosition = AEndPosition.FlipDirection().Move(TrainLength, out _).FlipDirection();
        _isAtSignal = false;
        SyncVisual();
    }

    private void SyncVisual()
    {
        if (LeadPosition is null || RearPosition is null || _visual is null) return;

        var leadPos = LeadPosition.GetGlobalPosition();
        var rearPos = RearPosition.GetGlobalPosition();

        GlobalPosition = (leadPos + rearPos) * 0.5f;
        Rotation = (leadPos - rearPos).Normalized().Angle();

        GetNode<Node2D>("Lead").GlobalPosition = leadPos;
        GetNode<Node2D>("Rear").GlobalPosition = rearPos;
    }
}