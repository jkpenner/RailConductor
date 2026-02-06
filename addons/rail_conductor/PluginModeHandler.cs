using Godot;

namespace RailConductor.Plugin;

public abstract class PluginModeHandler
{
    public abstract int SelectedIndex { get; }
    
    public virtual void OnSetup() {}
    public virtual void OnCleanup() {}
    
    public virtual bool OnGuiInput(Track target, InputEvent e, EditorUndoRedoManager undoRedo)
    {
        return false;
    }

    public virtual void OnGuiDraw(Control overlay)
    {

    }
}