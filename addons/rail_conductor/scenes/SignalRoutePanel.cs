using System;
using Godot;

namespace RailConductor.Plugin;

[GlobalClass, Tool]
public partial class SignalRoutePanel : Control
{
    public event Action ActiveSelectionChanged;

    private SignalData _currentSignal;
    private PluginContext _ctx;

    private int _activeDefIndex = -1;
    private int _activeRouteIndex = -1;

    // Lists
    private Control _routeDefinitions = null!;
    private Control _routeDefinitionEntryParent = null!;
    private Button _newRouteDefinitionButton = null!;

    private Control _routes = null!;
    private Control _routeEntryParent = null!;
    private Button _newRouteButton = null!;

    // Shared options panel (now contains Priority + Min/Max for the active RD)
    private Control _routeOptions = null!;
    private SpinBox _prioritySpinBox = null!;
    private SpinBox _minRouteCodeSpinBox = null!;
    private SpinBox _maxRouteCodeSpinBox = null!;

    public override void _Ready()
    {
        // Lists
        _routeDefinitions = GetNode<Control>("%RouteDefinitions");
        _routeDefinitionEntryParent = _routeDefinitions.GetNode<Control>("Entries");
        _newRouteDefinitionButton = _routeDefinitions.GetNode<Button>("HeaderRow/NewButton");
        _newRouteDefinitionButton.Pressed += OnNewRouteDefinitionPressed;

        _routes = GetNode<Control>("%Routes");
        _routeEntryParent = _routes.GetNode<Control>("Entries");
        _newRouteButton = _routes.GetNode<Button>("HeaderRow/NewButton");
        _newRouteButton.Pressed += OnNewRouteButtonPressed;

        // Route options panel
        _routeOptions = GetNode<Control>("%RouteOptions");

        _prioritySpinBox = _routeOptions.GetNode<SpinBox>("%PrioritySpinBox");
        _minRouteCodeSpinBox = _routeOptions.GetNode<SpinBox>("%MinRouteCode");
        _maxRouteCodeSpinBox = _routeOptions.GetNode<SpinBox>("%MaxRouteCode");

        _prioritySpinBox.ValueChanged += OnPriorityValueChanged;
        _minRouteCodeSpinBox.ValueChanged += OnMinRouteCodeChanged;
        _maxRouteCodeSpinBox.ValueChanged += OnMaxRouteCodeChanged;

        _routeOptions.Visible = false;
    }

    public void SetSignal(SignalData signal, PluginContext ctx)
    {
        _currentSignal = signal;
        _ctx = ctx;
        _activeDefIndex = 0;
        _activeRouteIndex = 0;

        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    public void Clear()
    {
        _currentSignal = null;
        _ctx = null;
        _activeDefIndex = -1;
        _activeRouteIndex = -1;

        ClearChildren(_routeDefinitionEntryParent);
        ClearChildren(_routeEntryParent);
        _routeOptions.Visible = false;
    }

    public Route? GetActiveRoute()
    {
        if (_currentSignal == null || _activeDefIndex < 0 || _activeRouteIndex < 0) return null;
        if (_activeDefIndex >= _currentSignal.RouteDefinitions.Count) return null;
        var def = _currentSignal.RouteDefinitions[_activeDefIndex];
        return _activeRouteIndex < def.Routes.Count ? def.Routes[_activeRouteIndex] : null;
    }

    private RouteDefinition? GetActiveRouteDefinition()
    {
        if (_currentSignal == null || _activeDefIndex < 0 || _activeDefIndex >= _currentSignal.RouteDefinitions.Count)
            return null;
        return _currentSignal.RouteDefinitions[_activeDefIndex];
    }

    private void RefreshAll()
    {
        RefreshRouteDefinitions();
        RefreshRoutes();
        RefreshRouteOptions();
    }

    private void RefreshRouteDefinitions()
    {
        ClearChildren(_routeDefinitionEntryParent);
        if (_currentSignal == null) return;

        for (int i = 0; i < _currentSignal.RouteDefinitions.Count; i++)
        {
            var def = _currentSignal.RouteDefinitions[i];
            var entry = ResourceLoader.Load<PackedScene>("res://addons/rail_conductor/scenes/SignalRouteEntry.tscn")
                .Instantiate<SignalRouteEntry>();

            _routeDefinitionEntryParent.AddChild(entry);
            entry.Setup($"RD{i+1}: {def.GetDisplayCode()}", i, true);
            entry.Selected += OnEntrySelected;
            entry.Deleted += OnEntryDeleted;
            entry.SetSelected(i == _activeDefIndex);
        }
    }

    private void RefreshRoutes()
    {
        ClearChildren(_routeEntryParent);
        if (_currentSignal == null || _activeDefIndex < 0 || _activeDefIndex >= _currentSignal.RouteDefinitions.Count) return;

        var def = _currentSignal.RouteDefinitions[_activeDefIndex];
        for (int i = 0; i < def.Routes.Count; i++)
        {
            var entry = ResourceLoader.Load<PackedScene>("res://addons/rail_conductor/scenes/SignalRouteEntry.tscn")
                .Instantiate<SignalRouteEntry>();
            _routeEntryParent.AddChild(entry);
            entry.Setup($"Route {i+1}", i, false);
            entry.Selected += OnEntrySelected;
            entry.Deleted += OnEntryDeleted;
            entry.SetSelected(i == _activeRouteIndex);
        }
    }

    private void RefreshRouteOptions()
    {
        var activeDef = GetActiveRouteDefinition();
        var activeRoute = GetActiveRoute();

        _routeOptions.Visible = activeDef != null;
        if (activeDef == null) return;

        // Min / Max (on RouteDefinition)
        _minRouteCodeSpinBox.SetMeta("ignore_change", true);
        _minRouteCodeSpinBox.Value = activeDef.MinRouteCode;
        _minRouteCodeSpinBox.SetMeta("ignore_change", false);

        _maxRouteCodeSpinBox.SetMeta("ignore_change", true);
        _maxRouteCodeSpinBox.Value = activeDef.MaxRouteCode;
        _maxRouteCodeSpinBox.SetMeta("ignore_change", false);

        // Priority (on individual Route)
        if (activeRoute != null)
        {
            _prioritySpinBox.SetMeta("ignore_change", true);
            _prioritySpinBox.Value = activeRoute.Priority;
            _prioritySpinBox.SetMeta("ignore_change", false);
            _prioritySpinBox.Visible = true;
        }
        else
        {
            _prioritySpinBox.Visible = false;
        }
    }

    private void OnEntrySelected(int index, bool isRouteDefinition)
    {
        if (isRouteDefinition)
        {
            _activeDefIndex = index;
            _activeRouteIndex = 0;
        }
        else
        {
            _activeRouteIndex = index;
        }
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnEntryDeleted()
    {
        if (_currentSignal == null || _ctx == null) return;

        if (_activeRouteIndex >= 0)   // deleting a route
        {
            if (_activeDefIndex >= 0 && _activeDefIndex < _currentSignal.RouteDefinitions.Count)
            {
                var def = _currentSignal.RouteDefinitions[_activeDefIndex];
                TrackEditorActions.RemoveRouteFromDefinition(_ctx, def, _activeRouteIndex);
                if (_activeRouteIndex >= def.Routes.Count)
                    _activeRouteIndex = Mathf.Max(0, def.Routes.Count - 1);
            }
        }
        else   // deleting a route definition
        {
            TrackEditorActions.RemoveRouteDefinition(_ctx, _currentSignal, _activeDefIndex);
            if (_activeDefIndex >= _currentSignal.RouteDefinitions.Count)
                _activeDefIndex = Mathf.Max(0, _currentSignal.RouteDefinitions.Count - 1);
            _activeRouteIndex = 0;
        }

        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnNewRouteDefinitionPressed()
    {
        if (_currentSignal == null || _ctx == null) return;

        int nextCode = _currentSignal.RouteDefinitions.Count + 1;
        var newDef = new RouteDefinition 
        { 
            MinRouteCode = nextCode, 
            MaxRouteCode = nextCode 
        };

        TrackEditorActions.AddRouteDefinition(_ctx, _currentSignal, newDef);
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnNewRouteButtonPressed()
    {
        if (_currentSignal == null || _ctx == null || _activeDefIndex < 0) return;
        var def = _currentSignal.RouteDefinitions[_activeDefIndex];
        var newRoute = new Route { TargetLinkId = "", Priority = def.Routes.Count };
        TrackEditorActions.AddRouteToDefinition(_ctx, def, newRoute);
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnMinRouteCodeChanged(double value)
    {
        if (_minRouteCodeSpinBox.GetMeta("ignore_change").AsBool()) return;
        var def = GetActiveRouteDefinition();
        if (def == null || _ctx == null) return;

        TrackEditorActions.SetRouteDefinitionMinCode(_ctx, def, (int)value);
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnMaxRouteCodeChanged(double value)
    {
        if (_maxRouteCodeSpinBox.GetMeta("ignore_change").AsBool()) return;
        var def = GetActiveRouteDefinition();
        if (def == null || _ctx == null) return;

        TrackEditorActions.SetRouteDefinitionMaxCode(_ctx, def, (int)value);
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private void OnPriorityValueChanged(double value)
    {
        if (_prioritySpinBox.GetMeta("ignore_change").AsBool()) return;
        var route = GetActiveRoute();
        if (route == null || _ctx == null) return;

        TrackEditorActions.SetRoutePriority(_ctx, route, (int)value);
        RefreshAll();
        ActiveSelectionChanged?.Invoke();
    }

    private static void ClearChildren(Control parent)
    {
        foreach (var child in parent.GetChildren())
            child.QueueFree();
    }
}