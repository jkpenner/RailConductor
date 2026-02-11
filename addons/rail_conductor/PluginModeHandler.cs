using System;
using Godot;

namespace RailConductor.Plugin;

public abstract class PluginModeHandler
{
    public bool IsEnabled { get; private set; }
    public event Action? OverlayUpdateRequested;

    public void Enable(PluginContext ctx)
    {
        if (IsEnabled)
        {
            return;
        }
        IsEnabled = true;
        OnEnable(ctx);
    }

    public void Disable(PluginContext ctx)
    {
        if (!IsEnabled)
        {
            return;
        }
        IsEnabled = false;
        OnDisable(ctx);
    }
    
    protected virtual void OnEnable(PluginContext ctx) { }
    protected virtual void OnDisable(PluginContext ctx) { }

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