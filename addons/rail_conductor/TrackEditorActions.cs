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

        // Remove the link from the connected node
        var nodeA = track.GetNode(link.NodeAId);
        if (nodeA is not null)
        {
            undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.RemoveLink), link.Id);
            undoRedo.AddDoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), track);

            undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.AddLink), link.Id);
            undoRedo.AddUndoMethod(nodeA, nameof(TrackNodeData.UpdateConfiguration), track);
        }

        // Remove the link from the connected node
        var nodeB = track.GetNode(link.NodeAId);
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
}