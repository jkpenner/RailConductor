using Godot;

/// <summary>
/// Smooth 2D camera controller supporting:
/// - Zoom in/out at mouse cursor (via input actions, e.g., mouse wheel).
/// - Mouse drag panning.
/// - WASD/arrow key panning (constant screen-space speed).
/// - Exponential lerp smoothing for responsive feel without overshoot.
/// 
/// Setup:
/// 1. Create Node2D, attach this script.
/// 2. Add child Camera2D named "Camera2D" (set Current = true in Inspector).
/// 3. Configure Input Map actions (see below).
/// </summary>
public partial class CameraController : Node2D
{
    /// <summary>
    /// Minimum zoom scale (zoomed out limit; smaller values show more world).
    /// </summary>
    [Export]
    public float MinZoom { get; set; } = 0.1f;

    /// <summary>
    /// Maximum zoom scale (zoomed in limit; larger values? No, smaller shows less/magnified.
    /// </summary>
    [Export]
    public float MaxZoom { get; set; } = 10.0f;

    /// <summary>
    /// Zoom step multiplier (e.g., 1.2 = ~17% steps).
    /// </summary>
    [Export]
    public float ZoomFactor { get; set; } = 1.2f;

    /// <summary>
    /// Mouse drag pan sensitivity (screen pixels multiplier).
    /// </summary>
    [Export]
    public float PanSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Keyboard pan speed (screen pixels/second, constant feel across zooms).
    /// </summary>
    [Export]
    public float KeyboardPanSpeed { get; set; } = 512f;

    /// <summary>
    /// Lerp speed factor (higher = snappier zoom/pan response).
    /// </summary>
    [Export]
    public float LerpSpeed { get; set; } = 12.0f;

    private Camera2D _camera = null!;

    // Smoothing goals and pivot state
    private Vector2 _zoomGoal;
    private Vector2 _positionGoal;
    private Vector2 _pivotScreenPos;
    private Vector2 _pivotWorldPos;

    // Drag state
    private bool _dragging;

    private const float Epsilon = 0.001f;

    public override void _Ready()
    {
        _camera = GetNode<Camera2D>(nameof(Camera2D));
        _camera.MakeCurrent();

        _zoomGoal = _camera.Zoom;
        _positionGoal = Position;
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        var viewportRect = GetViewportRect();
        var screenCenter = viewportRect.Size / 2f;
        var mouseScreenPos = GetViewport().GetMousePosition();

        // Zoom out (see more: increase scale)
        if (ev.IsAction("zoom_out"))
        {
            _pivotScreenPos = mouseScreenPos;
            _pivotWorldPos = Position + (mouseScreenPos - screenCenter) / _camera.Zoom;
            var targetScale = Mathf.Clamp(_camera.Zoom.X / ZoomFactor, MinZoom, MaxZoom);
            _zoomGoal = new Vector2(targetScale, targetScale);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Zoom in (magnify: decrease scale)
        if (ev.IsAction("zoom_in"))
        {
            _pivotScreenPos = mouseScreenPos;
            _pivotWorldPos = Position + (mouseScreenPos - screenCenter) / _camera.Zoom;
            var targetScale = Mathf.Clamp(_camera.Zoom.X * ZoomFactor, MinZoom, MaxZoom);
            _zoomGoal = new Vector2(targetScale, targetScale);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Drag pan start/stop
        if (ev.IsAction("camera_drag") && ev is InputEventMouseButton mb)
        {
            _dragging = mb.Pressed;
            GetViewport().SetInputAsHandled();
            return;
        }

        // Drag pan motion (world-space delta)
        if (_dragging && ev is InputEventMouseMotion mm)
        {
            var screenDelta = mm.Relative * PanSensitivity;
            _positionGoal -= screenDelta / _camera.Zoom;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var t = 1f - Mathf.Exp(-LerpSpeed * dt);

        // Smoothly lerp zoom
        _camera.Zoom = _camera.Zoom.Lerp(_zoomGoal, t);

        // Compute desired rig position (parent Node2D.Position = camera center)
        var isZooming = _camera.Zoom.DistanceTo(_zoomGoal) > Epsilon;
        Vector2 desiredPos;
        if (isZooming)
        {
            // Exact pivot: keep world point under cursor fixed during zoom
            var viewportRect = GetViewportRect();
            var screenCenter = viewportRect.Size / 2f;
            var pivotOffset = _pivotScreenPos - screenCenter;
            desiredPos = _pivotWorldPos - pivotOffset / _camera.Zoom;
        }
        else
        {
            desiredPos = _positionGoal;
        }

        // Smoothly lerp position
        Position = Position.Lerp(desiredPos, t);

        // Keyboard panning (always update goal; applies immediately or post-zoom)
        var moveInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        _positionGoal += moveInput * (KeyboardPanSpeed * dt / _camera.Zoom.X);
    }
}