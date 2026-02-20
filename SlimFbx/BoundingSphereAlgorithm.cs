using Stride.Core.Mathematics;

namespace SlimFbx;

public static class BoundingSphereAlgorithm
{
    //Ritter's bounding sphere
    public static BoundingSphere CalcRitterBoundingSphere(Vector3[] points)
    {
        if (points == null)
            throw new ArgumentNullException(nameof(points));

        if (points.Length == 0)
            return new BoundingSphere(Vector3.Zero, 0f);

        var first = points[0];
        var p1 = first;
        var maxDistSq = 0f;

        // Find point farthest from an arbitrary start to anchor the diameter search.
        foreach (var point in points)
        {
            var distSq = Vector3.DistanceSquared(first, point);
            if (distSq > maxDistSq)
            {
                maxDistSq = distSq;
                p1 = point;
            }
        }

        var p2 = p1;
        maxDistSq = 0f;
        // Find the point farthest from p1, giving us a good initial diameter.
        foreach (var point in points)
        {
            var distSq = Vector3.DistanceSquared(p1, point);
            if (distSq > maxDistSq)
            {
                maxDistSq = distSq;
                p2 = point;
            }
        }

        var center = 0.5f * (p1 + p2);
        var radius = 0.5f * MathF.Sqrt(Vector3.DistanceSquared(p1, p2));
        var radiusSq = radius * radius;

        // Expand the sphere whenever a point lies outside the current boundary.
        foreach (var point in points)
        {
            var offset = point - center;
            var distSq = offset.LengthSquared();

            if (distSq <= radiusSq)
                continue;

            var dist = MathF.Sqrt(distSq);
            if (dist <= float.Epsilon)
                continue;

            radius = 0.5f * (radius + dist);
            center += offset * ((dist - radius) / dist);
            radiusSq = radius * radius;
        }

        return new BoundingSphere(center, radius);
    }
}
