using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RailConductor;

public enum TrackNodeType
{
    Invalid,
    Basic,
    Switch,
    Crossover,
}

[GlobalClass, Tool]
public partial class TrackNodeData : Resource
{
    [Export]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Export]
    public Vector2 Position { get; set; }

    [Export]
    public bool IsIsolator { get; set; } = false;
    
    [Export]
    public Godot.Collections.Array<string> Links { get; set; } = [];

    [Export]
    public TrackNodeType NodeType { get; private set; } = TrackNodeType.Invalid;

    [Export]
    public Godot.Collections.Array<TrackLinkPairData> PairedLinks { get; private set; } = [];

    public void AddLink(string linkedNodeId)
    {
        if (!Links.Contains(linkedNodeId))
        {
            Links.Add(linkedNodeId);
        }
    }

    public void RemoveLink(string linkedNodeId)
    {
        Links.Remove(linkedNodeId);
    }

    public void UpdateConfiguration(TrackData data)
    {
        NodeType = GetNodeType() switch
        {
            TrackNodeType.Basic => UpdateBasicConfiguration(data),
            TrackNodeType.Switch => UpdateSwitchConfiguration(data),
            TrackNodeType.Crossover => UpdateCrossoverConfiguration(data),
            _ => TrackNodeType.Invalid,
        };
    }

    private TrackNodeType UpdateBasicConfiguration(TrackData data)
    {
        if (Links.Count == 1)
        {
            PairedLinks = [];
            return TrackNodeType.Basic;    
        }

        if (Links.Count == 2)
        {
            PairedLinks =
            [
                new TrackLinkPairData
                {
                    LinkAId = Links[0],
                    LinkBId = Links[1],
                }
            ];
            return TrackNodeType.Basic;
        }

        return TrackNodeType.Invalid;
    }
    
    private TrackNodeType UpdateSwitchConfiguration(TrackData data)
    {
        var sortedAngles = GetLinkAngles(data);
        var linkCount = sortedAngles.Count;
        if (linkCount != 3)
        {
            return TrackNodeType.Invalid;
        }

        var minAngle = float.MaxValue;
        var branch1 = string.Empty;
        var branch2 = string.Empty;

        // Find the pair of links with the smallest angle between.
        for (var i = 0; i < linkCount; i++)
        {
            for (var j = i + 1; j < linkCount; j++)
            {
                var angle = Mathf.Abs(Mathf.AngleDifference(sortedAngles[i].Angle, sortedAngles[j].Angle));
                if (angle >= minAngle)
                {
                    continue;
                }

                minAngle = angle;
                branch1 = sortedAngles[i].LinkId;
                branch2 = sortedAngles[j].LinkId;
            }
        }

        var stem = Links.First(id => id != branch1 && id != branch2);
        PairedLinks =
        [
            new TrackLinkPairData
            {
                LinkAId = stem,
                LinkBId = branch1,
            },
            new TrackLinkPairData
            {
                LinkAId = stem,
                LinkBId = branch2,
            }
        ];

        return TrackNodeType.Switch;
    }
    
    private TrackNodeType UpdateCrossoverConfiguration(TrackData data)
    {
        var sortedAngles = GetLinkAngles(data);
        var linkCount = sortedAngles.Count;
        if (linkCount != 4)
        {
            return TrackNodeType.Invalid;
        }
        
        var maxAngle = float.MinValue;
        var pair1A = string.Empty;
        var pair1B = string.Empty;

        // Find the pair of links with the smallest angle between.
        for (var i = 0; i < linkCount; i++)
        {
            for (var j = i + 1; j < linkCount; j++)
            {
                var angle = Mathf.Abs(Mathf.AngleDifference(sortedAngles[i].Angle, sortedAngles[j].Angle));
                if (angle <= maxAngle)
                {
                    continue;
                }

                maxAngle = angle;
                pair1A = sortedAngles[i].LinkId;
                pair1B = sortedAngles[j].LinkId;
            }
        }
        
        var pair2A = Links.First(id => id != pair1A && id != pair1B);
        var pair2B = Links.First(id => id != pair1B && id != pair1A & id != pair2A);
        
        PairedLinks =
        [
            new TrackLinkPairData
            {
                LinkAId = pair1A,
                LinkBId = pair1B,
            },
            new TrackLinkPairData
            {
                LinkAId = pair2A,
                LinkBId = pair2B,
            }
        ];
        
        return TrackNodeType.Crossover;
    }

    private TrackNodeType GetNodeType()
    {
        return Links.Count switch
        {
            1 or 2 => TrackNodeType.Basic,
            3 => TrackNodeType.Switch,
            4 => TrackNodeType.Crossover,
            _ => TrackNodeType.Invalid
        };
    }

    private List<(string LinkId, float Angle)> GetLinkAngles(TrackData data)
    {
        var angles = new List<(string, float)>();

        foreach (var linkId in Links)
        {
            var link = data.GetLink(linkId);
            if (link is null)
            {
                continue;
            }

            var linkedNode = data.GetNode(link.GetOtherNode(Id));
            if (linkedNode is null)
            {
                continue;
            }

            var direction = (linkedNode.Position - Position).Normalized();
            if (direction == Vector2.Zero)
            {
                continue;
            }

            angles.Add((linkId, direction.Angle()));
        }

        return angles;
    }
}