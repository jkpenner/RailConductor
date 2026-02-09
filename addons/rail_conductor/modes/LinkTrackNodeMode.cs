using Godot;

namespace RailConductor.Plugin;

public class LinkTrackNodeMode : PluginModeHandler
{
    private string _selectedNodeId1 = string.Empty;
    private string _selectedNodeId2 = string.Empty;

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        if (!string.IsNullOrEmpty(_selectedNodeId1))
        {
            ctx.Select(_selectedNodeId1);
        }
        
        if (!string.IsNullOrEmpty(_selectedNodeId2))
        {
            ctx.Select(_selectedNodeId2);
        }

        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            return false;
        }

        var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
        var localPosition = ctx.Track.ToLocal(globalPosition);

        // Select the first node 
        if (string.IsNullOrEmpty(_selectedNodeId1))
        {
            _selectedNodeId1 = ctx.TrackData.FindClosestNodeId(localPosition);
            ctx.Select(_selectedNodeId1);
            return true;
        }
        
        // Select the second node.
        _selectedNodeId2 = ctx.TrackData.FindClosestNodeId(localPosition);
        if (_selectedNodeId2 == _selectedNodeId1)
        {
            return false;
        }
        
        var node1 = ctx.TrackData.GetNode(_selectedNodeId1);
        var node2 = ctx.TrackData.GetNode(_selectedNodeId2);

        if (node1 is null || node2 is null || ctx.TrackData.IsLinked(_selectedNodeId1, _selectedNodeId2))
        {
            _selectedNodeId1 = string.Empty;
            _selectedNodeId2 = string.Empty;
            return false;
        }

        var link = new TrackLinkData
        {
            NodeAId = _selectedNodeId1,
            NodeBId = _selectedNodeId2,
        };

        // Link the two nodes
        ctx.UndoRedo.CreateAction("Link Track Node");
        
        // Add the new link
        ctx.UndoRedo.AddDoMethod(ctx.Track.Data, nameof(TrackData.AddLink), link.Id, link);
        ctx.UndoRedo.AddUndoMethod(ctx.Track.Data, nameof(TrackData.AddLink), link.Id);
        
        // Add the link to each node
        ctx.UndoRedo.AddDoMethod(node1, nameof(TrackNodeData.AddLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(node1, nameof(TrackNodeData.RemoveLink), link.Id);
        ctx.UndoRedo.AddDoMethod(node2, nameof(TrackNodeData.AddLink), link.Id);
        ctx.UndoRedo.AddUndoMethod(node2, nameof(TrackNodeData.RemoveLink), link.Id);
        
        ctx.UndoRedo.AddDoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddDoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(node1, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        ctx.UndoRedo.AddUndoMethod(node2, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
        
        ctx.UndoRedo.CommitAction();

        // Clear the selections
        _selectedNodeId1 = string.Empty;
        _selectedNodeId2 = string.Empty;

        return true;
    }
}