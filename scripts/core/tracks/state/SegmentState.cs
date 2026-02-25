using System;

namespace RailConductor;

public class SegmentState
{
    private bool _isVisible = true;
    private bool _isOccupied = false;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            Changed?.Invoke(this);
        }
    }

    public bool IsOccupied
    {
        get => _isOccupied;
        set
        {
            if (_isOccupied == value)
            {
                return;
            }

            _isOccupied = value;
            Changed?.Invoke(this);
        }
    }
    
    public event Action<SegmentState>? Changed;
}