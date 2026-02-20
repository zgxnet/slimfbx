using Stride.Core.Mathematics;
namespace SlimFbx;

public partial class Mesh : Geometry
{
    public override EType AttributeType => EType.Mesh;

    public struct PolygonDef //fbx: PolygonDef
    {
        public int Index;
        public int Size;
        //public int Group;

        public override readonly string ToString() => $"{Size}, {Index}";
    }

    public bool IsTriangleMesh;

    public Vector3[] VertexPositions = [];

    public Vector3[]? VertexNormals;

    public int[] Indices = [];

    public PolygonDef[]? Polygons; //only for polygon mesh, may be skipped when IsTriangleMesh==true

    public int VertexCount => VertexPositions.Length;

    public Mesh()
    {
    }

    public override void DoScale(float factor)
    {
        base.DoScale(factor);
        for (int i = 0; i < VertexPositions.Length; i++)
        {
            VertexPositions[i] *= factor;
        }
    }

    public BoundingBox CalcBoundingBox()
    {
        if (VertexPositions.Length == 0)
            return new BoundingBox();
        Vector3 min = VertexPositions[0];
        Vector3 max = VertexPositions[0];
        foreach (var v in VertexPositions)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }
        return new BoundingBox(min, max);
    }

    public BoundingSphere CalcBoundingSphere()
    {
        if (VertexPositions.Length == 0)
            return new BoundingSphere();

        // First, find the center point (centroid of all vertices)
        Vector3 center = Vector3.Zero;
        foreach (var vertex in VertexPositions)
        {
            center += vertex;
        }
        center /= VertexPositions.Length;

        // Then, find the maximum distance from center to any vertex
        float maxDistanceSquared = 0f;
        foreach (var vertex in VertexPositions)
        {
            float distanceSquared = Vector3.DistanceSquared(center, vertex);
            if (distanceSquared > maxDistanceSquared)
            {
                maxDistanceSquared = distanceSquared;
            }
        }

        float radius = (float)Math.Sqrt(maxDistanceSquared);
        return new BoundingSphere(center, radius);
    }

    public Mesh Triangulate(bool cloneData = false)
    {
        if (IsTriangleMesh)
        {
            if (cloneData)
                throw new NotImplementedException("Cloning an already triangulated mesh is not implemented.");
            return this; // 已经是三角形网格，直接返回
        }

        if (Polygons == null)
            throw new InvalidOperationException("Cannot triangulate a non-triangle mesh without polygon definitions.");

        Mesh trimesh = new ()
        {
            IsTriangleMesh = true,
            VertexPositions = cloneData ? (Vector3[])VertexPositions.Clone() : VertexPositions,
            VertexNormals = cloneData ? (Vector3[]?)VertexNormals?.Clone() : VertexNormals,
            //misc properties
            Name = Name,
            Nodes = cloneData ? [..Nodes] : Nodes,
            Deformers = cloneData ? [..Deformers] : Deformers,
        };

        List<int> triangulatedIndices = new List<int>();

        // 遍历每个多边形进行三角化
        foreach (var polygon in Polygons)
        {
            if (polygon.Size < 3)
                continue; // 跳过无效多边形

            if (polygon.Size == 3)
            {
                // 已经是三角形，直接添加
                for (int i = 0; i < 3; i++)
                {
                    triangulatedIndices.Add(Indices[polygon.Index + i]);
                }
            }
            else
            {
                // 使用扇形三角化（Fan Triangulation）
                // 以第一个顶点为中心，连接后续相邻顶点形成三角形
                int firstVertex = Indices[polygon.Index];
                
                for (int i = 1; i < polygon.Size - 1; i++)
                {
                    triangulatedIndices.Add(firstVertex);
                    triangulatedIndices.Add(Indices[polygon.Index + i]);
                    triangulatedIndices.Add(Indices[polygon.Index + i + 1]);
                }
            }
        }

        trimesh.Indices = triangulatedIndices.ToArray();
        
        // 为三角化后的网格创建新的多边形定义（全部都是三角形）
        int triangleCount = triangulatedIndices.Count / 3;
        trimesh.Polygons = new PolygonDef[triangleCount];
        
        for (int i = 0; i < triangleCount; i++)
        {
            trimesh.Polygons[i] = new PolygonDef
            {
                Index = i * 3,
                Size = 3
            };
        }

        return trimesh;
    }
}