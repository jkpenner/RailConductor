using System;
using Godot;

namespace RailConductor.Plugin;

public abstract class PluginModeHandler
{
    public event Action? OverlayUpdateRequested;

    public bool HandleGuiInput(PluginContext ctx, InputEvent input)
    {
        ctx.ClearHovered();
        
        bool handled = OnGuiInput(ctx, input);

        if (handled)
            OverlayUpdateRequested?.Invoke();

        return handled;
    }

    protected virtual bool OnGuiInput(PluginContext ctx, InputEvent input)
    {
        return false;
    }

    protected void RequestOverlayUpdate() => OverlayUpdateRequested?.Invoke();


    protected string GetClosestId(Track target, Vector2 screenPosition)
    {
        var globalPosition = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var localPosition = target.ToLocal(globalPosition);
        return target.Data?.FindClosestId(localPosition) ?? string.Empty;
    }
}