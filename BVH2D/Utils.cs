using System.Runtime.CompilerServices;

namespace BVH2D;

internal static class Utils
{
    /// <summary>
    /// Computes the joint AABB of shapes at specified indices
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB JointAABBOfShapes<T>(ReadOnlySpan<int> indices, T[] shapes) where T : IBounded
    {
        var aabb = AABB.Empty;
        foreach (int index in indices)
        {
            var shapeAabb = shapes[index].GetAABB();
            aabb.JoinMut(in shapeAabb);
        }
        return aabb;
    }
}

/// <summary>
/// Bucket structure used for BVH construction
/// </summary>
internal struct Bucket
{
    public int Size;
    public AABB Aabb;

    public static readonly Bucket Empty = new()
    {
        Size = 0,
        Aabb = AABB.Empty
    };

    /// <summary>
    /// Adds an AABB to this bucket
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddAABB(in AABB aabb)
    {
        Size++;
        Aabb = Aabb.Join(in aabb);
    }

    /// <summary>
    /// Joins two buckets together
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bucket JoinBucket(in Bucket a, in Bucket b) => new()
    {
        Size = a.Size + b.Size,
        Aabb = a.Aabb.Join(in b.Aabb)
    };
}