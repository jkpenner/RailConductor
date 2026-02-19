using System.Collections.Generic;
using Godot;

public partial class NodeLocator<T> : GodotObject where T : Node
{
    public IReadOnlySet<T> Nodes => _nodes;
    private readonly HashSet<T> _nodes = new();

    private Node? _root;
    private SceneTree? _sceneTree;

    public void SetRoot(Node? newRoot)
    {
        if (_root == newRoot) return;

        DisconnectAll();
        _root = newRoot;
        _nodes.Clear();

        if (_root is null) return;

        _sceneTree = _root.GetTree();

        if (_sceneTree is not null)
        {
            _sceneTree.NodeAdded += OnNodeAdded;
            _sceneTree.NodeRemoved += OnNodeRemoved;
        }

        RegisterSubtree(_root);
    }

    public void Reset() => SetRoot(null);

    private void DisconnectAll()
    {
        if (_sceneTree is not null)
        {
            _sceneTree.NodeAdded -= OnNodeAdded;
            _sceneTree.NodeRemoved -= OnNodeRemoved;
            _sceneTree = null;
        }

        _nodes.Clear();
    }

    private void OnNodeAdded(Node node)
    {
        if (node is T t)
            _nodes.Add(t);
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is T t)
            _nodes.Remove(t);
    }

    private void RegisterSubtree(Node node)
    {
        if (!GodotObject.IsInstanceValid(node)) return;

        if (node is T t)
            _nodes.Add(t);

        foreach (var child in node.GetChildren())
            RegisterSubtree(child);
    }
}