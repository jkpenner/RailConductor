using Godot;

namespace RailConductor.Plugin;

public static class TrackEditorActions
{
    public static void AddTrackNode(
        TrackData track,
        TrackNodeData node,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Add Track Node");
        undoRedo.AddDoMethod(track, nameof(TrackData.AddNode), node.Id, node);
        undoRedo.AddUndoMethod(track, nameof(TrackData.RemoveNode), node.Id);
        undoRedo.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), track);
        undoRedo.CommitAction();
    }

    public static void AddTrackSignal(
        TrackData track,
        SignalData signal,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Place Signal");

        undoRedo.AddDoMethod(track, nameof(TrackData.AddSignal), signal.Id, signal);
        undoRedo.AddUndoMethod(track, nameof(TrackData.RemoveSignal), signal.Id);

        undoRedo.CommitAction();
    }

    public static void MoveTrackNode(
        TrackData track,
        TrackNodeData node,
        Vector2 position,
        Vector2 originalPosition,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Move Track Node");
        undoRedo.AddDoProperty(node, nameof(TrackNodeData.Position), position);
        undoRedo.AddUndoProperty(node, nameof(TrackNodeData.Position), originalPosition);
        undoRedo.AddDoMethod(node, nameof(TrackNodeData.UpdateConfiguration), track);
        undoRedo.CommitAction();
    }
    
    public static void AddTrackPlatform(
        PluginContext ctx,
        PlatformData platform)
    {
        if (ctx.UndoRedo is null)
        {
            return;
        }
        
        ctx.UndoRedo.CreateAction("Place Platform");
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.AddPlatform), platform.Id, platform);
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RemovePlatform), platform.Id);
        ctx.UndoRedo.CommitAction();
    }

    public static void DeleteTrackNode(
        TrackData track,
        TrackNodeData node,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Delete Track Node");
        DeleteTrackNodeActions(track, node, undoRedo);
        undoRedo.CommitAction();
    }


    private static void DeleteTrackNodeActions(
        TrackData track,
        TrackNodeData node,
        EditorUndoRedoManager undoRedo)
    {
        // Remove the links from any linked node.
        foreach (var linkId in node.Links)
        {
            var link = track.GetLink(linkId);
            if (link is null)
            {
                continue;
            }

            DeleteTrackLinkActions(track, link, undoRedo);
        }

        undoRedo.AddDoMethod(track, nameof(TrackData.RemoveNode), node.Id);
        undoRedo.AddUndoMethod(track, nameof(TrackData.AddNode), node.Id, node);
        undoRedo.AddUndoMethod(node, nameof(TrackNodeData.UpdateConfiguration), track);
    }


    public static void DeleteTrackLink(
        TrackData track,
        TrackLinkData link,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Delete Track Link");
        DeleteTrackLinkActions(track, link, undoRedo);
        undoRedo.CommitAction();
    }

    private static void DeleteTrackLinkActions(
    TrackData track,
    TrackLinkData link,
    EditorUndoRedoManager undoRedo)
{
    // Delete all signals placed on the track link
    foreach (var signal in track.GetSignals())
    {
        if (signal.LinkId == link.Id)
        {
            DeleteTrackSignalActions(track, signal, undoRedo);
        }
    }

    // Clean up any platforms that reference this link
    foreach (var platform in track.GetPlatforms())
    {
        if (platform.IsLinkedTo(link.Id))
        {
            undoRedo.AddDoMethod(platform, nameof(PlatformData.RemoveLink), link.Id);
            undoRedo.AddUndoMethod(platform, nameof(PlatformData.AddLink), link.Id);
        }
    }

    // Remove the link from the connected node A
    var nodeA = track.GetNode(link.NodeAId);
    if (nodeA is not null)
    {
        undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.RemoveLink), link.Id);
        undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), track);

        undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.AddLink), link.Id);
        undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), track);
    }

    // Remove the link from the connected node B
    var nodeB = track.GetNode(link.NodeBId);
    if (nodeB is not null)
    {
        undoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.RemoveLink), link.Id);
        undoRedo.AddDoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), track);

        undoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.AddLink), link.Id);
        undoRedo.AddUndoMethod(nodeB, nameof(TrackNodeData.UpdateConfiguration), track);
    }

    // Remove the connected link
    undoRedo.AddDoMethod(track, nameof(TrackData.RemoveLink), link.Id);
    undoRedo.AddUndoMethod(track, nameof(TrackData.AddLink), link.Id, link);

    // Refresh platform link cache after deletion
    undoRedo.AddDoMethod(track, nameof(TrackData.RefreshPlatformLinkCache));
    undoRedo.AddUndoMethod(track, nameof(TrackData.RefreshPlatformLinkCache));
}
    
    /// <summary>
    /// Attaches a single track link to a platform (supports multiple links per platform).
    /// Fully undoable with proper snapshot of the entire list.
    /// </summary>
    public static void LinkPlatformToTrackLink(PluginContext ctx, PlatformData platform, string linkId)
    {
        if (ctx.UndoRedo is null || platform == null || string.IsNullOrEmpty(linkId))
            return;

        // Prevent duplicate links
        if (platform.IsLinkedTo(linkId))
            return;

        // Snapshot the current list for reliable undo
        var oldLinks = new Godot.Collections.Array<string>(platform.LinkedLinkIds);

        ctx.UndoRedo.CreateAction("Attach Link to Platform");

        // Do: Add the link
        ctx.UndoRedo.AddDoMethod(platform, nameof(PlatformData.AddLink), linkId);

        // Undo: Restore the previous list exactly
        ctx.UndoRedo.AddUndoMethod(platform, nameof(PlatformData.ClearLinks));
        foreach (var oldId in oldLinks)
        {
            ctx.UndoRedo.AddUndoMethod(platform, nameof(PlatformData.AddLink), oldId);
        }

        // Refresh the reverse lookup cache in TrackData
        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));

        ctx.UndoRedo.CommitAction();
    }
    
    public static void UnlinkPlatformFromTrackLink(PluginContext ctx, PlatformData platform, string linkId)
    {
        if (ctx.UndoRedo is null || platform == null || string.IsNullOrEmpty(linkId))
            return;

        if (!platform.IsLinkedTo(linkId))
            return;

        var oldLinks = new Godot.Collections.Array<string>(platform.LinkedLinkIds);

        ctx.UndoRedo.CreateAction("Detach Link from Platform");

        ctx.UndoRedo.AddDoMethod(platform, nameof(PlatformData.RemoveLink), linkId);

        ctx.UndoRedo.AddUndoMethod(platform, nameof(PlatformData.ClearLinks));
        foreach (var oldId in oldLinks)
        {
            ctx.UndoRedo.AddUndoMethod(platform, nameof(PlatformData.AddLink), oldId);
        }

        ctx.UndoRedo.AddDoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));
        ctx.UndoRedo.AddUndoMethod(ctx.TrackData, nameof(TrackData.RefreshPlatformLinkCache));

        ctx.UndoRedo.CommitAction();
    }

    public static void AddPlatformGroup(TrackData track, PlatformGroupData group, EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Add Platform Group");
        undoRedo.AddDoMethod(track, nameof(TrackData.AddPlatformGroup), group.Id, group);
        undoRedo.AddUndoMethod(track, nameof(TrackData.RemovePlatformGroup), group.Id);
        undoRedo.CommitAction();
    }

    /// <summary>
    /// Deletes a PlatformGroup and clears the GroupId reference from ALL platforms that belonged to it.
    /// Platforms themselves are NOT deleted — they simply become ungrouped.
    /// Fully undoable.
    /// </summary>
    public static void DeletePlatformGroup(TrackData track, PlatformGroupData group, EditorUndoRedoManager undoRedo)
    {
        if (track == null || group == null || undoRedo == null)
            return;

        undoRedo.CreateAction("Delete Platform Group");

        // 1. Clear GroupId on every platform that was in this group
        foreach (var platform in track.GetPlatformsInGroup(group.Id))
        {
            if (platform == null) continue;

            undoRedo.AddDoProperty(platform, nameof(PlatformData.GroupId), string.Empty);
            undoRedo.AddUndoProperty(platform, nameof(PlatformData.GroupId), group.Id);
        }

        // 2. Remove the group itself
        undoRedo.AddDoMethod(track, nameof(TrackData.RemovePlatformGroup), group.Id);
        undoRedo.AddUndoMethod(track, nameof(TrackData.AddPlatformGroup), group.Id, group);

        undoRedo.CommitAction();
    }
    
    public static void DeleteTrackSignal(
        TrackData track,
        SignalData signal,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Delete Track Signal");
        DeleteTrackSignalActions(track, signal, undoRedo);
        undoRedo.CommitAction();
    }

    private static void DeleteTrackSignalActions(
        TrackData track,
        SignalData signal,
        EditorUndoRedoManager undoRedo)
    {
        undoRedo.AddDoMethod(track, nameof(TrackData.RemoveSignal), signal.Id);
        undoRedo.AddUndoMethod(track, nameof(TrackData.AddSignal), signal.Id, signal);
    }
    
    /// <summary>
    /// Deletes a platform and also cleans up its membership in any PlatformGroup (prevents orphaned IDs in the group).
    /// </summary>
    public static void DeleteTrackPlatform(TrackData track, PlatformData platform, EditorUndoRedoManager undoRedo)
    {
        undoRedo.CreateAction("Delete Platform");

        // Clean up group membership (if any)
        if (!string.IsNullOrEmpty(platform.GroupId))
        {
            var group = track.GetPlatformGroup(platform.GroupId);
            if (group != null)
            {
                undoRedo.AddDoMethod(group, nameof(PlatformGroupData.RemovePlatform), platform.Id);
                undoRedo.AddUndoMethod(group, nameof(PlatformGroupData.AddPlatform), platform.Id);
            }
        }

        undoRedo.AddDoMethod(track, nameof(TrackData.RemovePlatform), platform.Id);
        undoRedo.AddUndoMethod(track, nameof(TrackData.AddPlatform), platform.Id, platform);

        undoRedo.CommitAction();
    }
    
    public static void AddPlatformToGroup(PluginContext ctx, PlatformData platform, PlatformGroupData group)
    {
        if (ctx.UndoRedo is null || platform == null || group == null) return;

        var oldGroupId = platform.GroupId;
        var oldPlatformIds = new Godot.Collections.Array<string>(group.PlatformIds);

        ctx.UndoRedo.CreateAction("Add Platform to Group");

        // Platform side
        ctx.UndoRedo.AddDoProperty(platform, nameof(PlatformData.GroupId), group.Id);
        ctx.UndoRedo.AddUndoProperty(platform, nameof(PlatformData.GroupId), oldGroupId);

        // Group side (snapshot for perfect undo)
        ctx.UndoRedo.AddDoMethod(group, nameof(PlatformGroupData.AddPlatform), platform.Id);
        ctx.UndoRedo.AddUndoMethod(group, nameof(PlatformGroupData.ClearPlatforms));
        foreach (var id in oldPlatformIds)
            ctx.UndoRedo.AddUndoMethod(group, nameof(PlatformGroupData.AddPlatform), id);

        ctx.UndoRedo.CommitAction();
    }

    public static void RemovePlatformFromGroup(PluginContext ctx, PlatformData platform, PlatformGroupData group)
    {
        if (ctx.UndoRedo is null || platform == null || group == null) return;
        if (platform.GroupId != group.Id) return;

        var oldPlatformIds = new Godot.Collections.Array<string>(group.PlatformIds);

        ctx.UndoRedo.CreateAction("Remove Platform from Group");

        ctx.UndoRedo.AddDoProperty(platform, nameof(PlatformData.GroupId), string.Empty);
        ctx.UndoRedo.AddDoMethod(group, nameof(PlatformGroupData.RemovePlatform), platform.Id);

        ctx.UndoRedo.AddUndoProperty(platform, nameof(PlatformData.GroupId), group.Id);
        ctx.UndoRedo.AddUndoMethod(group, nameof(PlatformGroupData.ClearPlatforms));
        foreach (var id in oldPlatformIds)
            ctx.UndoRedo.AddUndoMethod(group, nameof(PlatformGroupData.AddPlatform), id);

        ctx.UndoRedo.CommitAction();
    }
}