using System;

namespace RailConductor;

public class SwitchState
{
    private SwitchAlignment _alignment = SwitchAlignment.Normal;
    
    public SwitchAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment == value)
            {
                return;
            }
            
            _alignment = value;
            Changed?.Invoke(this);
        }
    }
    
    public event Action<SwitchState>? Changed;
}