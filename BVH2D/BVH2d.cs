using System.Numerics;
using System.Runtime.CompilerServices;

namespace BVH2D;

/// <summary>
/// 2D Bounding Volume Hierarchy (BVH) for efficient spatial queries
/// </summary>
public class BVH2d
{
    internal BVH2dNode[] Nodes { get; set; }
    internal int NodeCount { get; set; }

    /// <summary>
    /// Creates an empty BVH2D
    /// </summary>
    public BVH2d()
    {
        Nodes = Array.Empty<BVH2dNode>();
        NodeCount = 0;
    }

    /// <summary>
    /// Builds a BVH from a collection of shapes
    /// </summary>
    /// <typeparam name="T">Type of shape that implements IBounded</typeparam>
    /// <param name="shapes">Array of shapes to build the BVH from</param>
    /// <returns>A new BVH2D containing the shapes</returns>
    public static BVH2d Build<T>(T[] shapes) where T : IBounded
    {
        ArgumentNullException.ThrowIfNull(shapes);

        if (shapes.Length == 0)
            return new BVH2d();

        int expectedNodeCount = shapes.Length * 2 - 1;
        var nodes = new BVH2dNode[expectedNodeCount];
        int nodeCount = 0;

        if (shapes.Length <= 1024)
        {
            // Use stackalloc for indices buffer
            Span<int> indices = stackalloc int[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
                indices[i] = i;

            BVH2dNode.Build(shapes, indices, nodes, ref nodeCount);
        }
        else
        {
            // For very large datasets, fall back to array pool
            var indices = new int[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
                indices[i] = i;

            BVH2dNode.Build(shapes, indices.AsSpan(), nodes, ref nodeCount);
        }

        var bvh = new BVH2d
        {
            Nodes = nodes,
            NodeCount = nodeCount,
        };

        return bvh;
    }

    /// <summary>
    /// Creates an iterator that traverses shapes containing the given point
    /// </summary>
    /// <param name="point">The point to query</param>
    /// <returns>An iterator over shape indices that may contain the point</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BVH2DTraverseIterator ContainsIterator(Vector2 point) => new(this, point);

    /// <summary>
    /// Gets all shape indices that may contain the given point
    /// </summary>
    /// <param name="point">The point to query</param>
    /// <returns>List of shape indices</returns>
    public List<int> QueryPoint(Vector2 point)
    {
        List<int> results = new(16); // Pre-allocate capacity
        foreach (var index in ContainsIterator(point))
        {
            results.Add(index);
        }
        return results;
    }

    /// <summary>
    /// Gets all shape indices that may contain the given point
    /// </summary>
    /// <param name="point">The point to query</param>
    /// <param name="results">Span to write results to</param>
    /// <returns>Number of results written</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int QueryPoint(Vector2 point, Span<int> results)
    {
        int count = 0;
        foreach (var index in ContainsIterator(point))
        {
            if (count < results.Length)
            {
                results[count++] = index;
            }
            else
            {
                break; // Buffer full
            }
        }
        return count;
    }

    /// <summary>
    /// Gets all shape indices that may contain the given point
    /// </summary>
    /// <param name="point">The point to query</param>
    /// <param name="results">List to add results to (not cleared)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueryPoint(Vector2 point, ref List<int> results)
    {
        foreach (var index in ContainsIterator(point))
        {
            results.Add(index);
        }
    }
}