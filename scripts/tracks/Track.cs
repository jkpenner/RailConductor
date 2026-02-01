using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace RailConductor;

[GlobalClass]
public partial class Track : Node2D
{
    private TrackGraph _graph = null!;

    public override void _Ready()
    {
        _graph = TrackGraphBuilder.Build(this);
    }
}