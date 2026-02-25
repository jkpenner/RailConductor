using System;

namespace RailConductor;

public class SignalState
{
    private bool _isPermissive = false;
    
    public bool IsPermissive
    {
        get => _isPermissive;
        set
        {
            if (_isPermissive == value)
            {
                return;
            }
            
            _isPermissive = value;
            Changed?.Invoke(this);
        }
    }
    
    public event Action<SignalState>? Changed;
}