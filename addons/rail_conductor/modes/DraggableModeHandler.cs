using Godot;

namespace RailConductor.Plugin;

/// <summary>
/// Abstract base class that provides reusable drag logic for nodes and platforms.
/// Now enforces consistent snapping for both TrackNodes and Platforms.
/// </summary>
public abstract class DraggableModeHandler : PluginModeHandler
{
    protected bool _isDraggable;
    protected bool _hasMoveSincePress;
    protected Vector2 _initialPressPosition;

    protected (string Id, Vector2 Delta)[] _dragItems = [];

    // ========================================================================
    // ABSTRACT METHODS
    // ========================================================================

    protected abstract bool IsDraggable(string id, TrackData data);
    protected abstract (string Id, Vector2 Delta)[] BuildDragItems(PluginContext ctx, Vector2 initialLocalPos);
    protected abstract void ApplyPosition(PluginContext ctx, string id, Vector2 newLocalPos);
    protected abstract void CommitItem(PluginContext ctx, string id, Vector2 finalLocalPos, Vector2 originalLocalPos);

    protected virtual string GetUndoActionName(PluginContext ctx, int itemCount)
    {
        return itemCount == 1 ? "Move Track Object" : $"Move {itemCount} Track Objects";
    }

    protected virtual void OnCancelDrag(PluginContext ctx)
    {
        foreach (var (id, delta) in _dragItems)
        {
            ApplyPosition(ctx, id, _initialPressPosition + delta);
        }
    }

    // ========================================================================
    // COMMON DRAG METHODS
    // ========================================================================

    protected void StartDrag(PluginContext ctx, Vector2 screenPosition)
    {
        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        _initialPressPosition = ctx.Track.ToLocal(globalPos);

        _hasMoveSincePress = false;
        _isDraggable = true;
        _dragItems = BuildDragItems(ctx, _initialPressPosition);
    }

    protected void LiveDragPreview(PluginContext ctx, Vector2 screenPosition)
    {
        _hasMoveSincePress = true;

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPos);

        foreach (var (id, delta) in _dragItems)
        {
            ApplyPosition(ctx, id, newOrigin + delta);
        }
    }

    protected void CommitDrag(PluginContext ctx, Vector2 screenPosition)
    {
        if (!_hasMoveSincePress || _dragItems.Length == 0 || ctx.UndoRedo is null) return;

        var globalPos = PluginUtility.ScreenToWorldSnapped(screenPosition);
        var newOrigin = ctx.Track.ToLocal(globalPos);

        ctx.UndoRedo.CreateAction(GetUndoActionName(ctx, _dragItems.Length));

        foreach (var (id, delta) in _dragItems)
        {
            // FIXED: Snap each object's final position individually
            var finalPos = PluginUtility.SnapPosition(newOrigin + delta);
            var originalPos = _initialPressPosition + delta;
            CommitItem(ctx, id, finalPos, originalPos);
        }

        ctx.UndoRedo.CommitAction();
    }

    protected void CancelDrag(PluginContext ctx)
    {
        OnCancelDrag(ctx);
        CleanupDrag();
    }

    protected void CleanupDrag()
    {
        _isDraggable = false;
        _hasMoveSincePress = false;
        _dragItems = [];
    }

    protected bool IsDragging => _isDraggable;
}