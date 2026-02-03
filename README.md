# BVH2D

A high-performance 2D Bounding Volume Hierarchy (BVH) library for C# / .NET, designed for efficient spatial queries and collision detection in 2D space.

This library is a C# port of the [bvh2d](https://github.com/mockersf/bvh2d) Rust crate, adapted to leverage modern C# performance features.

## Features

- **Fast Spatial Queries**: Efficiently find shapes containing a point using hierarchical traversal
- **Optimized Construction**: Uses Surface Area Heuristic (SAH) for optimal tree construction
- **Memory Efficient**: Uses `Span<T>`, `stackalloc`, and `ArrayPool` for minimal allocations
- **Zero-Copy Queries**: Iterator-based API for traversal without intermediate allocations
- **Generic Support**: Works with any type implementing the `IBounded` interface
- **Modern C#**: Uses recent C# features including extension types and aggressive inlining

## Installation

Get BVH2D from NuGet using one of the following methods:

##### .NET CLI
```
dotnet add package BVH2D
```
Use this command in terminal/command prompt. Works on Windows, macOS, and Linux.

##### Package Manager Console
```
Install-Package BVH2D
```
Use this command in Visual Studio's Package Manager Console(Windows only).

##### NuGet Package Manager GUI

Alternatively, you can install via the graphical interface in Visual Studio or your preferred IDE by searching for "BVH2D".

## Quick Start

### 1. Define Your Shape Type

Your shape type must implement the `IBounded` interface:

```csharp
using System.Numerics;
using BVH2D;

public class Circle : IBounded
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; }

    public AABB GetAABB()
    {
        return new AABB(
            Center - new Vector2(Radius, Radius),
            Center + new Vector2(Radius, Radius)
        );
    }
}
```

### 2. Build the BVH

```csharp
// Create your shapes
var circles = new Circle[]
{
    new Circle { Center = new Vector2(10, 10), Radius = 5 },
    new Circle { Center = new Vector2(50, 30), Radius = 8 },
    new Circle { Center = new Vector2(100, 80), Radius = 12 },
    // ... more shapes
};

// Build the BVH
var bvh = BVH2D.Build(circles);
```

### 3. Query for Shapes Containing a Point

```csharp
var point = new Vector2(52, 28);

// Option 1: Get all results as a List
List<int> results = bvh.QueryPoint(point);
foreach (int index in results)
{
    // Check actual containment (BVH only checks bounding boxes)
    if (IsPointInCircle(point, circles[index]))
    {
        Console.WriteLine($"Point is in circle {index}");
    }
}

// Option 2: Use iterator for zero-allocation traversal
foreach (int index in bvh.ContainsIterator(point))
{
    if (IsPointInCircle(point, circles[index]))
    {
        Console.WriteLine($"Point is in circle {index}");
    }
}

// Option 3: Fill a pre-allocated Span
Span<int> resultBuffer = stackalloc int[32];
int count = bvh.QueryPoint(point, resultBuffer);
for (int i = 0; i < count; i++)
{
    int index = resultBuffer[i];
    // Process shape at index
}

// Option 4: Add to an existing List (doesn't clear it)
var existingList = new List<int>();
bvh.QueryPoint(point, existingList);
```

## API Reference

### Core Types

#### `BVH2d`

The main BVH structure.

**Construction:**
```csharp
public static BVH2d Build<T>(T[] shapes) where T : IBounded
```

**Query Methods:**
```csharp
// Returns a new List<int> with results
public List<int> QueryPoint(Vector2 point)

// Fills a Span<int> buffer, returns count written
public int QueryPoint(Vector2 point, Span<int> results)

// Adds results to an existing List<int> (doesn't clear it)
public void QueryPoint(Vector2 point, List<int> results)

// Returns an iterator for zero-allocation traversal
public BVH2DTraverseIterator ContainsIterator(Vector2 point)
```

#### `IBounded`

Interface that your shapes must implement:

```csharp
public interface IBounded
{
    AABB GetAABB();
}
```

#### `AABB`

Axis-Aligned Bounding Box structure:

```csharp
public struct AABB
{
    public Vector2 Min { get; set; }
    public Vector2 Max { get; set; }
    
    // Properties
    public readonly Vector2 Size { get; }
    public readonly Vector2 Center { get; }
    public readonly float SurfaceArea { get; }
    public readonly Axis LargestAxis { get; }
    public readonly bool IsEmpty { get; }
    
    // Methods
    public static AABB WithBounds(Vector2 min, Vector2 max)
    public readonly bool Contains(in Vector2 point)
}
```

## Complete Example

```csharp
using System;
using System.Numerics;
using BVH2D;

public class Rectangle : IBounded
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }

    public AABB GetAABB()
    {
        return new AABB(Position, Position + Size);
    }

    public bool ContainsPoint(Vector2 point)
    {
        return point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }
}

class Program
{
    static void Main()
    {
        // Create a grid of rectangles
        var rectangles = new Rectangle[100];
        int index = 0;
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                rectangles[index++] = new Rectangle
                {
                    Position = new Vector2(x * 20, y * 20),
                    Size = new Vector2(15, 15)
                };
            }
        }

        // Build BVH
        var bvh = BVH2d.Build(rectangles);

        // Query a point
        var testPoint = new Vector2(45, 67);
        
        Console.WriteLine($"Shapes potentially containing {testPoint}:");
        
        foreach (int idx in bvh.ContainsIterator(testPoint))
        {
            // BVH returns candidates - verify with actual shape test
            if (rectangles[idx].ContainsPoint(testPoint))
            {
                Console.WriteLine($"  Rectangle {idx} at {rectangles[idx].Position}");
            }
        }
    }
}
```

## Performance Characteristics

### Build Time
- **Complexity**: O(n log n) average case
- **Method**: Surface Area Heuristic (SAH) with bucket-based splitting
- **Optimization**: Uses `stackalloc` for small datasets, `ArrayPool` for large ones

### Query Time
- **Point Query**: O(log n) average case, O(n) worst case
- **Memory**: Zero allocations when using iterator API
- **Cache Friendly**: Sequential node traversal

4. **Rebuild for Dynamic Scenes**: The BVH is static. If shapes move frequently, rebuild the BVH periodically.

5. **Tight Bounding Boxes**: Ensure `GetAABB()` returns tight-fitting boxes for better query performance.

## Requirements

- **.NET 6.0+**
- **System.Numerics** (for Vector2)
- **System.Buffers** (for ArrayPool)

## Acknowledgments

This library is a C# port of the [bvh2d](https://github.com/mockersf/bvh2d) Rust crate by [@mockersf](https://github.com/mockersf). The original Rust implementation has been adapted to leverage modern C# performance features including:
- Span-based APIs for zero-copy operations
- ArrayPool for efficient memory reuse
- Aggressive inlining for hot paths
- SAH-based splitting for balanced trees
