using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
namespace SlimFbx;

public class MeshDeformLab
{
    Scene scene;
    Mesh mesh;
    AnimLayer? animLayer;

    struct VertexInfo
    {
        public VertexInfo() { }
        public List<(int boneIndex, float weight)> boneWeights = [];
        public float weightSum = 0;

        public override readonly string ToString()
        {
            return $"Bones: {boneWeights.Count}, Sum: {weightSum}";
        }
    }
    VertexInfo[] vertexInfos;

    struct BoneInfo
    {
        public Cluster cluster;
        public Node linkNode;
        public Matrix linkMatrix;
        public Matrix totalTransform;
        public readonly int LinkNodeIndex => linkNode.Index;

        public override readonly string ToString() => linkNode.Name ?? "<unnamed>";
    }
    
    struct NodeInfo
    {
        public Node node;
        public Matrix globalTransform;

        public override readonly string ToString() => node.Name ?? "<unnamed>";
    }

    BoneInfo[] boneInfos;
    NodeInfo[] nodeInfos;

    public MeshDeformLab(Scene scene, Mesh mesh, AnimLayer? animLayer = null)
    {
        this.scene = scene;
        this.mesh = mesh;
        this.animLayer = animLayer;
        Init();
    }

    IEnumerable<Vector3> GetBonePositions()
    {
        foreach (var v in boneInfos)
        {
            var nodeIndex = v.LinkNodeIndex;
            yield return nodeInfos[nodeIndex].globalTransform.TranslationVector;
        }
    }

    public BoundingBox CalcSkeletonBoundingBox()
    {   
        Vector3 min = new (float.MaxValue);
        Vector3 max = new (float.MinValue);
        foreach (var pos in GetBonePositions())
        {
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        return new BoundingBox(min, max);
    }

    public BoundingSphere CalcSkeletonBoundingSphere()
        => BoundingSphereAlgorithm.CalcRitterBoundingSphere([.. GetBonePositions()]);

    public void TransformNodesAt(float time)
    {
        if (animLayer == null)
            throw new InvalidOperationException("No animation layer specified");
        Dictionary<Node, AnimNode> animNodes = animLayer.NodeAnimations.Select(_ => KeyValuePair.Create(_.Node, _)).ToDictionary();
        Matrix EvalNode(Node node)
            => animNodes.TryGetValue(node, out var anim) ? anim.EvaluateLocalTransformLinearAt(time).ToTRSTransform().ToMatrix()
            : node.LocalTransform.ToTRSTransform().ToMatrix();
        void CalcGlobalTransform(Node root)
        {
            ref NodeInfo info = ref nodeInfos[root.Index];
            if (root.Parent == null)
            {
                info.globalTransform = EvalNode(root);
            }
            else
            {
                info.globalTransform = EvalNode(root) * nodeInfos[root.Parent.Index].globalTransform;
            }
            foreach (var child in root.Children)
                CalcGlobalTransform(child);
        }
        CalcGlobalTransform(scene.RootNode);
        UpdateBoneTransform();
    }

    public (Vector3[] positions, Vector3[]? normals) Deform(Vector3[]? positionBuffer = null, Vector3[]? normalBuffer = null)
    {
        if (positionBuffer != null)
        {
            if (positionBuffer.Length != mesh.VertexCount)
                throw new ArgumentException("newPositions length mismatch mesh vertex count");
        }
        else
            positionBuffer = new Vector3[mesh.VertexCount];

        if (mesh.VertexNormals != null)
        {
            if (normalBuffer != null)
            {
                if (normalBuffer.Length != mesh.VertexCount)
                    throw new ArgumentException("newNormals length mismatch mesh vertex count");
            }
            else
                normalBuffer = new Vector3[mesh.VertexCount];
        }
        else
            normalBuffer = null; // no normals

        for (int i = 0; i < mesh.VertexCount; i++)
        {
            ref var vtxInfo = ref vertexInfos[i];
            if (vtxInfo.boneWeights.Count == 0)
            {
                positionBuffer[i] = mesh.VertexPositions[i];
                if (normalBuffer != null)
                    normalBuffer[i] = mesh.VertexNormals![i];
                continue;
            }
            foreach ((int boneIndex, float weight) in vtxInfo.boneWeights)
            {
                ref var boneInfo = ref boneInfos[boneIndex];
                Vector3 origPos = mesh.VertexPositions[i];
                Vector3 transformedPos = Vector3.Transform(origPos, boneInfo.totalTransform).XYZ();
                positionBuffer[i] += transformedPos * (weight / vtxInfo.weightSum);
                if(normalBuffer != null)
                {
                    Vector3 origNormal = mesh.VertexNormals![i];
                    Vector3 transformedNormal = Vector3.TransformNormal(origNormal, boneInfo.totalTransform);
                    transformedNormal.Normalize();
                    normalBuffer[i] += transformedNormal * (weight / vtxInfo.weightSum);
                }
            }
        }
        return (positionBuffer, normalBuffer);
    }
   
    [MemberNotNull(nameof(vertexInfos), nameof(boneInfos), nameof(nodeInfos))]
    void Init()
    {
        Skin skinDeformer = mesh.SkinDeformer ?? throw new InvalidOperationException("No skin deformer");
        Debug.Assert(skinDeformer.SkinningType == Skin.EType.Rigid);
        vertexInfos = new VertexInfo[mesh.VertexCount];
        for (int i = 0; i < mesh.VertexCount; i++)
        {
            vertexInfos[i] = new VertexInfo();
        }

        boneInfos = new BoneInfo[skinDeformer.Clusters.Length];
        for (int i = 0; i < skinDeformer.Clusters.Length; i++)
        {
            var cluster = skinDeformer.Clusters[i];
            var linkNode = cluster.Link ?? throw new InvalidOperationException("Cluster has no link node");
            boneInfos[i] = new BoneInfo()
            {
                cluster = cluster,
                linkNode = linkNode,
                linkMatrix = cluster.EvaluateLinkToMeshMatrix(mesh)
            };
            Debug.Assert(cluster.LinkMode == Cluster.ELinkMode.Normalize);
            if (cluster.ControlPointIndices.Length != cluster.ControlPointWeights.Length)
                throw new InvalidOperationException("Cluster control point indices count mismatch weights count");
            for (int j = 0; j < cluster.ControlPointIndices.Length; j++)
            {
                int vtxIndex = cluster.ControlPointIndices[j];
                float weight = cluster.ControlPointWeights[j];
                ref var vtxInfo = ref vertexInfos[vtxIndex];
                vtxInfo.boneWeights.Add((i, weight));
                vtxInfo.weightSum += weight;
            }
        }

        nodeInfos = new NodeInfo[scene.Nodes.Length];
        for (int i = 0; i < scene.Nodes.Length; i++)
        {
            nodeInfos[i] = new NodeInfo()
            {
                node = scene.Nodes[i],
                globalTransform = Matrix.Identity
            };
        }
        void CalcGlobalTransform(Node root)
        {
            ref NodeInfo info = ref nodeInfos[root.Index];
            if (root.Parent == null)
            {
                info.globalTransform = root.LocalTransform.ToMatrix();
            }
            else
            {
                info.globalTransform = root.LocalTransform.ToMatrix() * nodeInfos[root.Parent.Index].globalTransform;
            }
            foreach (var child in root.Children)
                CalcGlobalTransform(child);
        }
        CalcGlobalTransform(scene.RootNode);
        UpdateBoneTransform();
    }

    private void UpdateBoneTransform()
    {
        //update bone total transform
        for (int i = 0; i < boneInfos.Length; i++)
        {
            ref var boneInfo = ref boneInfos[i];
            ref var nodeInfo = ref nodeInfos[boneInfo.LinkNodeIndex];
            boneInfo.totalTransform = boneInfo.linkMatrix * nodeInfo.globalTransform;
        }
    }
}
