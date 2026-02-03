using System.Numerics;

namespace BVH2D;

/// <summary>
/// Extension methods for Vector2 to support axis indexing
/// </summary>
internal static class Vector2Extensions
{
    public static float GetAxis(this Vector2 vector, Axis axis)
        => axis == Axis.X ? vector.X : vector.Y;
}