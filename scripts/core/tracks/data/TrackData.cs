using System.Collections.Generic;
using System.Linq;
using Godot;
using RailConductor.Plugin;

namespace RailConductor;

[GlobalClass, Tool]
public partial class TrackData : Resource
{
    [Export] private Godot.Collections.Dictionary<string, TrackNodeData> _nodes = new();
    [Export] private Godot.Collections.Dictionary<string, TrackLinkData> _links = new();
    [Export] private Godot.Collections.Dictionary<string, SignalData> _signals = new();
    [Export] private Godot.Collections.Dictionary<string, PlatformData> _platforms = new();
    [Export] private Godot.Collections.Dictionary<string, InterlockingGroupData> _interlockingGroups = new();
    [Export] private Godot.Collections.Dictionary<string, PlatformGroupData> _platformGroups = new();

    private readonly Dictionary<string, string> _linkToPlatformId = new();
    
    public bool IsValidId(string id) => IsNodeId(id) || IsLinkId(id) || IsSignalId(id) || IsPlatformId(id) || IsInterlockingGroupId(id) || IsPlatformGroupId(id);
    public bool IsNodeId(string id) => _nodes.ContainsKey(id);
    public bool IsLinkId(string id) => _links.ContainsKey(id);
    public bool IsSignalId(string id) => _signals.ContainsKey(id);
    public bool IsPlatformId(string id) => _platforms.ContainsKey(id);
    public bool IsPlatformGroupId(string id) => _platformGroups.ContainsKey(id);
    public bool IsInterlockingGroupId(string id) => _interlockingGroups.ContainsKey(id);

    public IEnumerable<TrackNodeData> GetNodes() => _nodes.Values;
    public IEnumerable<TrackLinkData> GetLinks() => _links.Values;
    public IEnumerable<SignalData> GetSignals() => _signals.Values;
    public IEnumerable<PlatformData> GetPlatforms() => _platforms.Values;
    public IEnumerable<PlatformGroupData> GetPlatformGroups() => _platformGroups.Values;
    public IEnumerable<InterlockingGroupData> GetInterlockingGroups() => _interlockingGroups.Values;

    public TrackNodeData? GetNode(string id) => _nodes.GetValueOrDefault(id);
    public TrackLinkData? GetLink(string id) => _links.GetValueOrDefault(id);
    public SignalData? GetSignal(string id) => _signals.GetValueOrDefault(id);
    public PlatformData? GetPlatform(string id) => _platforms.GetValueOrDefault(id);
    public PlatformGroupData? GetPlatformGroup(string id) => _platformGroups.GetValueOrDefault(id);
    public InterlockingGroupData? GetInterlockingGroup(string id) => _interlockingGroups.GetValueOrDefault(id);

    public void AddNode(string id, TrackNodeData newNode) => _nodes.Add(id, newNode);
    public void AddLink(string id, TrackLinkData newLink) => _links.Add(id, newLink);
    public void AddSignal(string id, SignalData newSignal) => _signals.Add(id, newSignal);
    public void AddPlatform(string id, PlatformData newPlatform) => _platforms.Add(id, newPlatform);
    public void AddPlatformGroup(string id, PlatformGroupData newPlatform) => _platformGroups.Add(id, newPlatform);

    public void RemoveNode(string id) => _nodes.Remove(id);
    public void RemoveLink(string id) => _links.Remove(id);
    public void RemoveSignal(string id) => _signals.Remove(id);
    public void RemovePlatform(string id) => _platforms.Remove(id);
    public void RemovePlatformGroup(string id) => _platformGroups.Remove(id);

    /// <summary>
    /// Rebuilds the fast reverse lookup: which platform is attached to each link.
    /// Called automatically by undo/redo actions after any platform link change.
    /// </summary>
    public void RefreshPlatformLinkCache()
    {
        _linkToPlatformId.Clear();

        foreach (var platform in GetPlatforms())
        {
            foreach (var linkId in platform.LinkedLinkIds)
            {
                if (!string.IsNullOrEmpty(linkId))
                {
                    _linkToPlatformId[linkId] = platform.Id;   // One link can only belong to one platform
                }
            }
        }
    }
    
    
    
    /// <summary>
    /// Returns the platform attached to a specific link (null if none).
    /// Very fast thanks to the cache.
    /// </summary>
    public PlatformData? GetPlatformOnLink(string linkId)
    {
        return _linkToPlatformId.TryGetValue(linkId, out var platformId)
            ? GetPlatform(platformId)
            : null;
    }

    public IEnumerable<PlatformData> GetPlatformsInGroup(string groupId)
    {
        var group = GetPlatformGroup(groupId);
        if (group == null) return Enumerable.Empty<PlatformData>();
        return group.PlatformIds.Select(GetPlatform).OfType<PlatformData>();
    }
    
    public bool IsLinked(string nodeAId, string nodeBId)
    {
        var nodeA = GetNode(nodeAId);
        var nodeB = GetNode(nodeBId);

        if (nodeA is null || nodeB is null)
        {
            return false;
        }

        // Check for a link from node A.
        if (nodeA.Links.Select(GetLink).OfType<TrackLinkData>()
            .Any(link => link.NodeAId == nodeBId || link.NodeBId == nodeBId))
        {
            return true;
        }

        // Check for a link from node B.
        return nodeB.Links.Select(GetLink).OfType<TrackLinkData>()
            .Any(link => link.NodeAId == nodeAId || link.NodeBId == nodeAId);
    }

    public TrackLinkData? GetConnectingLink(string nodeAId, string nodeBId)
    {
        var nodeA = GetNode(nodeAId);
        var nodeB = GetNode(nodeBId);

        if (nodeA is null || nodeB is null)
        {
            return null;
        }

        var linkId = nodeA.Links.Intersect(nodeB.Links).FirstOrDefault();
        return string.IsNullOrEmpty(linkId) ? null : GetLink(linkId);
    }


    public bool LinkNodes(string nodeId1, string nodeId2)
    {
        var node1 = GetNode(nodeId1);
        var node2 = GetNode(nodeId2);

        if (node1 is null || node2 is null)
        {
            return false;
        }

        if (!node1.Links.Contains(nodeId2))
        {
            node1.Links.Add(nodeId2);
        }

        if (!node2.Links.Contains(nodeId1))
        {
            node2.Links.Add(nodeId1);
        }

        return true;
    }

    public string FindClosestId(Vector2 position)
    {
        var nodeId = FindClosestNodeId(position);
        if (!string.IsNullOrEmpty(nodeId))
        {
            return nodeId;
        }

        var signalId = FindClosestSignalId(position);
        if (!string.IsNullOrEmpty(signalId))
        {
            return signalId;
        }

        var linkId = FindClosestLink(position);
        if (!string.IsNullOrEmpty(linkId))
        {
            return linkId;
        }

        var platformId = FindClosestPlatform(position);
        if (!string.IsNullOrEmpty(platformId))
        {
            return platformId;
        }

        return string.Empty;
    }

    public string FindClosestNodeId(Vector2 position)
    {
        if (_nodes.Count == 0)
        {
            return string.Empty;
        }

        var minDist = float.MaxValue;
        var closest = string.Empty;

        foreach (var (id, node) in _nodes)
        {
            var dist = node.Position.DistanceTo(position);
            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = id;
        }

        return minDist < PluginSettings.MaxSelectDistance ? closest : string.Empty;
    }

    public string FindClosestSignalId(Vector2 position)
    {
        if (_signals.Count == 0)
        {
            return string.Empty;
        }

        var minDist = float.MaxValue;
        var closest = string.Empty;

        foreach (var (id, signal) in _signals)
        {
            var orientation = GetSignalPosition(signal);
            if (!orientation.HasValue)
            {
                continue;
            }

            var dist = orientation!.Value.Position.DistanceTo(position);
            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = id;
        }

        return minDist < PluginSettings.MaxSelectDistance ? closest : string.Empty;
    }

    public string FindClosestPlatform(Vector2 position)
    {
        if (_platforms.Count == 0)
        {
            return string.Empty;
        }
        
        var minDist = float.MaxValue;
        var closest = string.Empty;

        foreach (var platform in GetPlatforms())
        {
            var size = platform.IsVertical
                ? PluginSettings.PlatformVerticalSize
                : PluginSettings.PlatformHorizontalSize;
            var rect = new Rect2(platform.Position, size);
            var dist = PluginUtility.DistanceToRect(position, rect);

            // If the position is in the rect just return it.
            if (rect.HasPoint(position))
            {
                return platform.Id;
            }
            
            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = platform.Id;
        }
        
        return minDist < PluginSettings.MaxSelectDistance ? closest : string.Empty;
    }

    public string FindClosestLink(Vector2 position)
    {
        if (_nodes.Count < 2)
        {
            return string.Empty;
        }

        var minDist = float.MaxValue;
        var closest = string.Empty;

        foreach (var link in GetLinks())
        {
            var node1 = GetNode(link.NodeAId);
            var node2 = GetNode(link.NodeBId);

            if (node1 == null || node2 == null)
            {
                continue;
            }

            var dist = DistanceToSegment(position, node1.Position, node2.Position);

            if (dist >= minDist)
            {
                continue;
            }

            minDist = dist;
            closest = link.Id;
        }

        return minDist < PluginSettings.MaxSelectDistance ? closest : string.Empty;
    }

    public float GetClosestLinkDistance(Vector2 position)
    {
        if (_nodes.Count < 2)
        {
            return float.MaxValue;
        }

        var minDist = float.MaxValue;

        foreach (var link in GetLinks())
        {
            var node1 = GetNode(link.NodeAId);
            var node2 = GetNode(link.NodeBId);

            if (node1 == null || node2 == null)
            {
                continue;
            }

            var dist = DistanceToSegment(position, node1.Position, node2.Position);

            if (dist < minDist)
            {
                minDist = dist;
            }
        }

        return minDist;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var ap = p - a;

        var len2 = ab.LengthSquared();
        if (len2 == 0)
        {
            return p.DistanceTo(a);
        }

        var proj = ap.Dot(ab);
        var t = Mathf.Clamp(proj / len2, 0f, 1f);
        var projection = a + t * ab;
        return p.DistanceTo(projection);
    }
    
    public (Vector2 Position, float Angle)? GetSignalPosition(string signalId)
    {
        var signal = GetSignal(signalId);
        if (signal is null)
        {
            return null;
        }

        return GetSignalPosition(signal);
    }

    public (Vector2 Position, float Angle)? GetSignalPosition(SignalData signal)
    {
        var link = GetLink(signal.LinkId);
        if (link is null)
        {
            GD.Print("Failed to find signal's link");
            return null;
        }

        var nodeA = GetNode(signal.DirectionNodeId);
        var nodeB = GetNode(link.GetOtherNode(signal.DirectionNodeId));

        if (nodeA is null || nodeB is null)
        {
            GD.Print("Failed to find signal's nodes");
            return null;
        }

        var direction = (nodeA.Position - nodeB.Position).Normalized();
        var angle = direction.Angle();
        var rotated = direction.Rotated(Mathf.DegToRad(90f));
        var position = nodeA.Position + rotated * 12;
        return (position, angle);
    }
}