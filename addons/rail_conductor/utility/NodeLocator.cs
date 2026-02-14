using System.Collections.Generic;
using Godot;

public class NodeLocator<T> where T : Node
{
    public IReadOnlySet<T> Nodes => _nodes;
    private readonly HashSet<T> _nodes = [];

    private Node? _root;

    public void SetRoot(Node? newRoot)
    {
        if (_root == newRoot) return;

        DisconnectAll();
        _root = newRoot;
        _nodes.Clear();

        if (_root is null) return;

        _root.ChildEnteredTree += OnDirectChildEntered;
        _root.ChildExitingTree += OnDirectChildExited;

        // One-time full registration
        RegisterSubtree(_root);
    }

    public void Reset() => SetRoot(null);

    private void DisconnectAll()
    {
        if (_root is null)
        {
            return;
        }

        _root.ChildEnteredTree -= OnDirectChildEntered;
        _root.ChildExitingTree -= OnDirectChildExited;

        // Clean up EVERY node we ever touched
        UnregisterSubtree(_root);
    }

    private void OnDirectChildEntered(Node node)
    {
        if (node is T t)
        {
            _nodes.Add(t);
        }

        node.ChildEnteredTree += OnDeepChildEntered;
        node.ChildExitingTree += OnDeepChildExited;

        RegisterSubtree(node);
    }

    private void OnDirectChildExited(Node node)
    {
        if (node is T t)
        {
            _nodes.Remove(t);
        }

        // Critical: clean up this whole branch
        UnregisterSubtree(node);

        node.ChildEnteredTree -= OnDeepChildEntered;
        node.ChildExitingTree -= OnDeepChildExited;
    }

    private void OnDeepChildEntered(Node node)
    {
        if (node is T t)
        {
            _nodes.Add(t);
        }
    }

    private void OnDeepChildExited(Node node)
    {
        if (node is T t)
        {
            _nodes.Remove(t);
        }
    }

    // Register all T nodes + add deep listeners to EVERY node in subtree
    private void RegisterSubtree(Node node)
    {
        if (node is T t)
        {
            _nodes.Add(t);
        }

        // Subscribe to this node's children changes (so we catch future deep adds)
        node.ChildEnteredTree += OnDeepChildEntered;
        node.ChildExitingTree += OnDeepChildExited;

        foreach (var child in node.GetChildren())
        {
            RegisterSubtree(child);
        }
    }

    // Remove all T nodes + unsubscribe from EVERY node in this subtree
    private void UnregisterSubtree(Node node)
    {
        if (node is T t)
        {
            _nodes.Remove(t);
        }

        // Unsubscribe from this node's child signals
        node.ChildEnteredTree -= OnDeepChildEntered;
        node.ChildExitingTree -= OnDeepChildExited;

        foreach (var child in node.GetChildren())
        {
            UnregisterSubtree(child);
        }
    }
}