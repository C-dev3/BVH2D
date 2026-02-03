using System.Buffers;

namespace BVH2D;

/// <summary>
/// BVH2D node - can be either a leaf or an internal node
/// </summary>
internal struct BVH2dNode
{
    // Leaf properties
    public int ShapeIndex { get; set; }

    // Node properties
    public int ChildLIndex { get; set; }

    public AABB ChildLAABB { get; set; }

    public int ChildRIndex { get; set; }

    public AABB ChildRAABB { get; set; }

    public bool IsLeaf { get; set; }

    private const float Epsilon = 0.00001f;

    /// <summary>
    /// Creates a dummy leaf node
    /// </summary>
    public static readonly BVH2dNode Dummy = new()
    {
        IsLeaf = true,
        ShapeIndex = 0
    };

    /// <summary>
    /// Creates a leaf node
    /// </summary>
    public static BVH2dNode CreateLeaf(int shapeIndex) => new()
    {
        IsLeaf = true,
        ShapeIndex = shapeIndex
    };

    /// <summary>
    /// Creates an internal node
    /// </summary>
    public static BVH2dNode CreateNode(int childLIndex, AABB childLAABB, int childRIndex, AABB childRAABB) => new()
    {
        IsLeaf = false,
        ChildLIndex = childLIndex,
        ChildLAABB = childLAABB,
        ChildRIndex = childRIndex,
        ChildRAABB = childRAABB
    };

    /// <summary>
    /// Builds a BVH tree recursively
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="shapes"></param>
    /// <param name="indices"></param>
    /// <param name="nodes"></param>
    /// <param name="nodeCount"></param>
    /// <returns></returns>
    public static int Build<T>(T[] shapes, ReadOnlySpan<int> indices, BVH2dNode[] nodes, ref int nodeCount) where T : IBounded
    {
        // If there is only one element left, don't split anymore
        if (indices.Length == 1)
        {
            int shapeIndex = indices[0];
            int leafNodeIndex = nodeCount++;
            nodes[leafNodeIndex] = CreateLeaf(shapeIndex);
            return leafNodeIndex;
        }

        // Compute convex hull without intermediate allocations
        var aabbBounds = AABB.Empty;
        var centroidBounds = AABB.Empty;

        foreach (int index in indices)
        {
            var aabb = shapes[index].GetAABB();
            var center = aabb.Center;
            aabbBounds.JoinMut(in aabb);
            centroidBounds.GrowMut(in center);
        }

        // Create a dummy node that will be replaced later
        int nodeIndex = nodeCount++;
        nodes[nodeIndex] = Dummy;

        // Find the axis along which the shapes are spread the most
        Axis splitAxis = centroidBounds.LargestAxis;
        float splitAxisSize = centroidBounds.Max.GetAxis(splitAxis) - centroidBounds.Min.GetAxis(splitAxis);

        int childLIndex, childRIndex;
        AABB childLAABB, childRAABB;

        if (splitAxisSize < Epsilon)
        {
            // Shapes lie too close together, split the list in half using ArrayPool
            int mid = indices.Length / 2;

            var childLIndices = ArrayPool<int>.Shared.Rent(mid);
            var childRIndices = ArrayPool<int>.Shared.Rent(indices.Length - mid);

            try
            {
                indices[..mid].CopyTo(childLIndices);
                indices[mid..].CopyTo(childRIndices);

                childLAABB = Utils.JointAABBOfShapes(childLIndices.AsSpan(0, mid), shapes);
                childRAABB = Utils.JointAABBOfShapes(childRIndices.AsSpan(0, indices.Length - mid), shapes);

                childLIndex = Build(shapes, childLIndices.AsSpan(0, mid), nodes, ref nodeCount);
                childRIndex = Build(shapes, childRIndices.AsSpan(0, indices.Length - mid), nodes, ref nodeCount);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(childLIndices);
                ArrayPool<int>.Shared.Return(childRIndices);
            }
        }
        else
        {
            const int NumBuckets = 4;

            // Use stackalloc for buckets and bucket size tracking
            Span<Bucket> buckets = stackalloc Bucket[NumBuckets];
            Span<int> bucketSizes = stackalloc int[NumBuckets];

            for (int i = 0; i < NumBuckets; i++)
            {
                buckets[i] = Bucket.Empty;
                bucketSizes[i] = 0;
            }

            // First pass - count bucket sizes to allocate exact memory
            Span<int> bucketAssignments = stackalloc int[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                int idx = indices[i];
                var shape = shapes[idx];
                var shapeAabb = shape.GetAABB();
                var shapeCenter = shapeAabb.Center;

                // Get the relative position of the shape centroid [0.0..1.0]
                float bucketNumRelative = (shapeCenter.GetAxis(splitAxis) - centroidBounds.Min.GetAxis(splitAxis)) / splitAxisSize;

                // Convert to actual bucket number
                int bucketNum = (int)(bucketNumRelative * (NumBuckets - 0.01f));

                // Store assignment and update bucket
                bucketAssignments[i] = bucketNum;
                buckets[bucketNum].AddAABB(in shapeAabb);
                bucketSizes[bucketNum]++;
            }

            // Compute costs and select best configuration
            int minBucket = 0;
            float minCost = float.PositiveInfinity;
            childLAABB = AABB.Empty;
            childRAABB = AABB.Empty;

            for (int i = 0; i < NumBuckets - 1; i++)
            {
                // Split buckets at position i
                var childL = Bucket.Empty;
                for (int j = 0; j <= i; j++)
                {
                    childL = Bucket.JoinBucket(in childL, in buckets[j]);
                }

                var childR = Bucket.Empty;
                for (int j = i + 1; j < NumBuckets; j++)
                {
                    childR = Bucket.JoinBucket(in childR, in buckets[j]);
                }

                float cost = (childL.Size * childL.Aabb.SurfaceArea +
                              childR.Size * childR.Aabb.SurfaceArea) /
                             aabbBounds.SurfaceArea;

                if (cost < minCost)
                {
                    minBucket = i;
                    minCost = cost;
                    childLAABB = childL.Aabb;
                    childRAABB = childR.Aabb;
                }
            }

            // Calculate exact sizes for left and right children
            int leftCount = 0;
            for (int i = 0; i <= minBucket; i++)
            {
                leftCount += bucketSizes[i];
            }

            int rightCount = indices.Length - leftCount;

            // Use ArrayPool for temporary index storage
            var childLIndicesArray = ArrayPool<int>.Shared.Rent(leftCount);
            var childRIndicesArray = ArrayPool<int>.Shared.Rent(rightCount);

            try
            {
                int leftPos = 0;
                int rightPos = 0;

                // Single pass to separate indices into left/right
                for (int i = 0; i < indices.Length; i++)
                {
                    if (bucketAssignments[i] <= minBucket)
                    {
                        childLIndicesArray[leftPos++] = indices[i];
                    }
                    else
                    {
                        childRIndicesArray[rightPos++] = indices[i];
                    }
                }

                childLIndex = Build(shapes, childLIndicesArray.AsSpan(0, leftCount), nodes, ref nodeCount);
                childRIndex = Build(shapes, childRIndicesArray.AsSpan(0, rightCount), nodes, ref nodeCount);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(childLIndicesArray);
                ArrayPool<int>.Shared.Return(childRIndicesArray);
            }
        }

        // Replace the dummy node with the actual node
        System.Diagnostics.Debug.Assert(!childLAABB.IsEmpty);
        System.Diagnostics.Debug.Assert(!childRAABB.IsEmpty);

        nodes[nodeIndex] = CreateNode(childLIndex, childLAABB, childRIndex, childRAABB);

        return nodeIndex;
    }
}