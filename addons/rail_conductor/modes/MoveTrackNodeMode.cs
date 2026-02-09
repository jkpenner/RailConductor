using Godot;

namespace RailConductor.Plugin;

public class MoveTrackNodeMode : PluginModeHandler
{
    private string _selectedNodeId = string.Empty;
    private Vector2 _originalPosition = Vector2.Zero;

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        if (!string.IsNullOrEmpty(_selectedNodeId))
        {
            ctx.Select(_selectedNodeId);
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            if (btn.Pressed)
            {
                _selectedNodeId = ctx.TrackData.FindClosestNodeId(localPosition);
                var selectedNode = ctx.TrackData.GetNode(_selectedNodeId);
                if (selectedNode is not null)
                {
                    _originalPosition = selectedNode.Position;
                }
            }
            else // Released
            {
                var selectedNode = ctx.TrackData.GetNode(_selectedNodeId);
                if (selectedNode is not null)
                {
                    var finalPos = selectedNode.Position;

                    ctx.UndoRedo.CreateAction("Move Track Node");
                    ctx.UndoRedo.AddDoProperty(selectedNode, nameof(TrackNodeData.Position), finalPos);
                    ctx.UndoRedo.AddUndoProperty(selectedNode, nameof(TrackNodeData.Position), _originalPosition);
                    
                    ctx.UndoRedo.AddDoMethod(selectedNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                    ctx.UndoRedo.AddUndoMethod(selectedNode, nameof(TrackNodeData.UpdateConfiguration), ctx.TrackData);
                    
                    ctx.UndoRedo.CommitAction();
                    _selectedNodeId = string.Empty;
                }
            }

            // ctx.Track.Update();
            ctx.Track.RecalculateGraph();
            ctx.Track.NotifyPropertyListChanged();
            return true;
        }

        if (e is InputEventMouseMotion mouseMotion && !string.IsNullOrEmpty(_selectedNodeId) &&
            Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(mouseMotion.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            var selectedNode = ctx.TrackData.GetNode(_selectedNodeId);
            if (selectedNode is not null)
            {
                selectedNode.Position = localPosition;

                // ctx.Track.Update();
                ctx.Track.RecalculateGraph();
                ctx.Track.NotifyPropertyListChanged();
                return true;
            }
        }

        return false;
    }
}