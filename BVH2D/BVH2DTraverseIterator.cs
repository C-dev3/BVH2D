using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BVH2D;

/// <summary>
/// Iterator for traversing a BVH2D tree to find shapes containing a point
/// </summary>
public sealed class BVH2DTraverseIterator : IEnumerator<int>, IEnumerable<int>
{
    private readonly BVH2d _bvh2d;
    private readonly Vector2 _point;
    private readonly int[] _stack;
    private int _nodeIndex;
    private int _stackSize;
    private bool _hasNode;
    private int _current;

    internal BVH2DTraverseIterator(BVH2d bvh2d, Vector2 point)
    {
        _bvh2d = bvh2d;
        _point = point;
        _stack = new int[64];
        _nodeIndex = 0;
        _stackSize = 0;
        _hasNode = bvh2d.NodeCount > 0;
        _current = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsStackEmpty() => _stackSize == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StackPush(int node) => _stack[_stackSize++] = node;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int StackPop() => _stack[--_stackSize];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveLeft()
    {
        var node = _bvh2d.Nodes[_nodeIndex];

        if (!node.IsLeaf)
        {
            if (_bvh2d.Nodes[_nodeIndex].ChildLAABB.Contains(in _point))
            {
                _nodeIndex = node.ChildLIndex;
                _hasNode = true;
            }
            else
            {
                _hasNode = false;
            }
        }
        else
        {
            _hasNode = false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveRight()
    {
        var node = _bvh2d.Nodes[_nodeIndex];

        if (!node.IsLeaf)
        {
            if (_bvh2d.Nodes[_nodeIndex].ChildRAABB.Contains(in _point))
            {
                _nodeIndex = node.ChildRIndex;
                _hasNode = true;
            }
            else
            {
                _hasNode = false;
            }
        }
        else
        {
            _hasNode = false;
        }
    }

    /// <summary>
/// Advances the iterator to the next shape whose bounding volume contains the query point.
/// </summary>
/// <returns>
/// <see langword="true"/> if the iterator successfully advanced to the next element;
/// <see langword="false"/> if the traversal has completed.
/// </returns>
    bool IEnumerator.MoveNext()
    {
        while (true)
        {
            if (IsStackEmpty() && !_hasNode)
            {
                // Completed traversal
                return false;
            }

            if (_hasNode)
            {
                // If we have any node, save it and attempt to move to its left child
                StackPush(_nodeIndex);
                MoveLeft();
            }
            else
            {
                // Go back up the stack and see if a node or leaf was pushed
                _nodeIndex = StackPop();
                var node = _bvh2d.Nodes[_nodeIndex];

                if (!node.IsLeaf)
                {
                    // If a node was pushed, now attempt to move to its right child
                    MoveRight();
                }
                else
                {
                    // We previously pushed a leaf node. This is the "visit" of the in-order traverse.
                    // Next time we call MoveNext() we try to pop the stack again.
                    _hasNode = false;
                    _current = node.ShapeIndex;
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// Resets the iterator to its initial state, restarting the BVH traversal from the root.
    /// </summary>
    public void Reset()
    {
        _nodeIndex = 0;
        _stackSize = 0;
        _hasNode = _bvh2d.NodeCount > 0;
        _current = -1;
    }

    /// <summary>
    /// Gets the index of the current shape found during the traversal.
    /// </summary>
    public int Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _current;
    }

    object IEnumerator.Current => Current;

    void IDisposable.Dispose() { }

    /// <summary>
    /// Returns an enumerator that iterates through the shape indices produced by this traversal.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> that iterates over shape indices.
    /// </returns>
    public IEnumerator<int> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}