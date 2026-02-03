using System.Numerics;
using System.Runtime.CompilerServices;

namespace BVH2D;

/// <summary>
/// Axis-Aligned Bounding Box (AABB) in 2D space
/// </summary>
public struct AABB
{
    /// <summary>
    /// The minimum corner point of the AABB (bottom-left in 2D space)
    /// </summary>
    public Vector2 Min { get; set; }

    /// <summary>
    /// The maximum corner point of the AABB (top-right in 2D space)
    /// </summary>
    public Vector2 Max { get; set; }

    /// <summary>
    /// Represents an empty/invalid AABB that contains no area. Used as a sentinel value for initialization.
    /// </summary>
    public static readonly AABB Empty = new(
        new Vector2(float.PositiveInfinity, float.PositiveInfinity),
        new Vector2(float.NegativeInfinity, float.NegativeInfinity)
    );

    /// <summary>
    /// Initializes a new instance of the AABB structure with the specified minimum and maximum points
    /// </summary>
    /// <param name="min">The minimum corner point (bottom-left)</param>
    /// <param name="max">The maximum corner point (top-right)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates an AABB with specified bounds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB WithBounds(Vector2 min, Vector2 max) => new(min, max);

    /// <summary>
    /// Joins this AABB with another, creating a new AABB that contains both
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly AABB Join(in AABB other) => new(
            Vector2.Min(Min, other.Min),
            Vector2.Max(Max, other.Max)
        );

    /// <summary>
    /// Joins this AABB with another in-place
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void JoinMut(in AABB other)
    {
        Min = Vector2.Min(Min, other.Min);
        Max = Vector2.Max(Max, other.Max);
    }

    /// <summary>
    /// Grows the AABB to include a point in-place
    /// </summary>
    /// <param name="point"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void GrowMut(in Vector2 point)
    {
        Min = Vector2.Min(Min, point);
        Max = Vector2.Max(Max, point);
    }

    /// <summary>
    /// Gets the size of the AABB
    /// </summary>
    public readonly Vector2 Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Max - Min;
    }

    /// <summary>
    /// Gets the center point of the AABB
    /// </summary>
    public readonly Vector2 Center
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Min + (Size / 2.0f);
    }

    /// <summary>
    /// Checks if the AABB is empty (invalid)
    /// </summary>
    internal readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Min.X > Max.X || Min.Y > Max.Y;
    }

    /// <summary>
    /// Calculates the surface area (in 2D, this is the area)
    /// </summary>
    internal readonly float SurfaceArea
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var size = Size;
            return size.X * size.Y;
        }
    }

    /// <summary>
    /// Gets the largest axis of the AABB
    /// </summary>
    internal readonly Axis LargestAxis
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var size = Size;
            return size.X > size.Y ? Axis.X : Axis.Y;
        }
    }

    /// <summary>
    /// Checks if a point is contained within this AABB
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(in Vector2 point) => point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y;
}