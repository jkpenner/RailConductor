using Godot;

namespace RailConductor.Plugin;

public class PlaceNodeMode : PluginModeHandler
{
    private string _selectedNodeId = string.Empty;
    private Vector2 _originalPosition;

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion mouseMotion:
                ProcessMouseMotion(ctx, mouseMotion);
                break;
                
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
                return ProcessLeftMouseButton(ctx, mouseButton);
        }

        return false;
    }

    private void ProcessMouseMotion(PluginContext ctx, InputEventMouseMotion mouseMotion)
    {
        if (string.IsNullOrEmpty(_selectedNodeId))
        {
            return;
        }

        var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
        var localPosition = ctx.Track.ToLocal(globalPosition);

        var node = ctx.TrackData?.GetNode(_selectedNodeId);
        if (node is null)
        {
            return;
        }

        node.Position = localPosition;
    }
    
    private bool ProcessLeftMouseButton(PluginContext ctx, InputEventMouseButton mouseButton)
    {
        var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseButton.Position);
        var localPosition = ctx.Track.ToLocal(globalPosition);

        if (mouseButton.Pressed)
        {
            var newNode = new TrackNodeData
            {
                Position = localPosition
            };

            ctx.SelectOnly(newNode.Id);
            _selectedNodeId = newNode.Id;
            
            _originalPosition = localPosition;

            TrackEditorActions.AddTrackNode(ctx.TrackData, newNode, ctx.UndoRedo);
        }
        else
        {
            var node = ctx.TrackData.GetNode(_selectedNodeId);
            if (node is null)
            {
                return false;
            }

            ctx.Deselect(_selectedNodeId);
            _selectedNodeId = string.Empty;
            
            TrackEditorActions.MoveTrackNode(ctx.TrackData, node, node.Position, _originalPosition, ctx.UndoRedo);
        }

        return true;
    }

    
}