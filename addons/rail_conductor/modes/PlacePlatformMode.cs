using System;
using Godot;

namespace RailConductor.Plugin;

public class PlacePlatformMode : PluginModeHandler
{
    private bool _placeVertical = false;
    private Vector2 _placePosition;

    protected override void OnEnable(PluginContext ctx)
    {
        ctx.ClearSelection();
        RequestOverlayUpdate();
    }

    protected override bool OnGuiInput(PluginContext ctx, InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(motion.Position);
            _placePosition = ctx.Track.ToLocal(globalPosition);
            RequestOverlayUpdate();
            return false;
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            _placeVertical = !_placeVertical;
            RequestOverlayUpdate();
            return true;
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } btn)
        {
            var globalPosition = PluginUtility.ScreenToWorldSnapped(btn.Position);
            var localPosition = ctx.Track.ToLocal(globalPosition);

            var platform = new PlatformData
            {
                Position = localPosition,
                IsVertical = _placeVertical
            };

            ctx.SelectOnly(platform.Id);
            TrackEditorActions.AddTrackPlatform(ctx, platform);
            RequestOverlayUpdate();
            return true;
        }

        return false;
    }

    public override void Draw(Control overlay, PluginContext ctx)
    {
        TrackEditorDrawer.DrawTrackPlatform(overlay, ctx, new PlatformData
        {
            Position = _placePosition,
            IsVertical = _placeVertical
        });
    }
}