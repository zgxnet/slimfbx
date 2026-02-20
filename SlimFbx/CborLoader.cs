using PeterO.Cbor;
using SharpFont;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static SlimFbx.CborConverter;
using static SlimFbx.LayerElement;

namespace SlimFbx;

public static class CborLoader
{
    enum CArrayElementType
    {
        Int32,
        Single,
        Double,
        Single3,
        Double3,
        Polygon
    }

    //for CBOR
    class CSystemUnit
    {
        public float scaleFactor;
        public float multiplier;

        public static CSystemUnit Default = new CSystemUnit
        {
            scaleFactor = 1,
            multiplier = 1
        };
    }

    class CLayerElement<T>
    {
        public EMappingMode mappingMode;
        public EReferenceMode referenceMode;

        [CompactDataArray]
        public T[] data = [];
    }

    class CLayer
    {
        public CLayerElement<Vector3>? normals;
    }

    class CObject
    {
        public string? name;

        public override string ToString()
            => GetType().Name[1..] + (!string.IsNullOrEmpty(name) ? " - " + name : "");
    }

    class CNodeAttribute : CObject
    {
        public NodeAttribute.EType attributeType;
        public static Type GetActualType(CBORObject obj)
        {
            var t = obj["attributeType"].AsString();
            return t switch
            {
                "Mesh" => typeof(CMesh),
                "Skeleton" => typeof(CSkeleton),
                "Null" => typeof(CFbxNull),
                _ => typeof(CNodeAttribute)
            };
        }
    }

    class CLayerContainer : CNodeAttribute
    {
        public CLayer[] layers = [];
    }

    class CGeometry : CLayerContainer
    {
        public CDeformer[] deformers = [];
    }

    class CMesh : CGeometry
    {
        public bool isTriangleMesh;

        [CompactDataArray]
        public Vector3[]? vertices;

        public struct PolygonData
        {
            public byte[] sizes;

            [CompactDataArray]
            public int[] indices;
        }

        public PolygonData polygons = new() { sizes = [], indices = [] };
    }

    class CDeformer : CObject
    {
        public Deformer.EType deformerType;
        public bool multiLayer;

        public void Assign(Deformer deformer)
        {
            deformer.MultiLayer = multiLayer;
        }

        public static Type GetActualType(CBORObject obj)
        {
            var t = obj["deformerType"].AsString();
            return t switch
            {
                "Skin" => typeof(CSkin),
                _ => typeof(CDeformer)
            };
        }
    }

    class CSkin : CDeformer
    {
        public float deformAccuracy;

        public Skin.EType skinningType;

        public CCluster[] clusters = [];
    }

    class CSubDeformer : CObject
    {
        public bool multiLayer;
    }

    class CCluster : CSubDeformer
    {
        public Cluster.ELinkMode linkMode;
        public int? linkNode;

        public Matrix transformLinkMatrix;
        public Matrix transformMatrix;

        [CompactDataArray]
        public int[] controlPointIndices = [];

        [CompactDataArray]
        public float[] controlPointWeights = [];
    }

    class CSkeleton : CNodeAttribute
    {
        public float size;
        public float limbLength;
    }

    class CFbxNull : CNodeAttribute
    {
        public float size;
        public FbxNull.ELook look;
    }

    class CNode : CObject
    {
        public int defaultAttributeIndex = -1;

        public Vector3 lclTranslation = Vector3.Zero;
        public Vector3 lclRotation = Vector3.Zero; //Euler XYZ in degrees
        public Vector3 lclScaling = Vector3.One;

        public Vector3 preRotation = Vector3.Zero;
        public Vector3 postRotation = Vector3.Zero;
        public Vector3 rotationOffset = Vector3.Zero;
        public Vector3 rotationPivot = Vector3.Zero;

        public Vector3 scalingOffset = Vector3.Zero;
        public Vector3 scalingPivot = Vector3.Zero;

        public Vector4 geometricScaling = Vector4.One;
        public Vector4 geometricRotation = new (Vector3.Zero, 1);
        public Vector4 geometricTranslation = new (Vector3.Zero, 1);

        public Euler.EOrder rotationOrder = Euler.EOrder.YXZ;

        public int[] attributes = [];
        public int[] children = [];
    }

    class CScene : CObject
    {
        public CSystemUnit systemUnit = CSystemUnit.Default;
        public FbxTime.EMode timeMode = FbxTime.EMode.DefaultMode;
        public CNodeAttribute[] nodeAttributes = [];
        public CNode[] nodes = [];
        public CAnimStack[] animStacks = [];
        public int? defaultMeshIndex;
    }

    class CAnimCurve : CObject
    {
        public List<AnimCurveKey> keyframes = [];
    }

    class CNodeAnimation
    {
        public int nodeIndex;
        public CAnimCurve[] lclScaling = [];
        public CAnimCurve[] lclRotation = [];
        public CAnimCurve[] lclTranslation = [];
    }

    class CAnimLayer : CObject
    {
        public CNodeAnimation[] nodeAnimations = [];
    }

    struct CFbxTimeSpan
    {
        public long start;
        public long stop;
    }

    class CAnimStack : CObject
    {
        public CAnimLayer[] animLayers = [];
        public CFbxTimeSpan localTimeSpan;
    }

    static CborConverter cborConverter = new CborConverter();

    static CborLoader()
    {
        cborConverter.Register<CSkeleton>();
        cborConverter.Register<CMesh>();
        cborConverter.Register<CFbxNull>();
        cborConverter.Register<CMesh.PolygonData>();
        cborConverter.Register<CLayer>();
        cborConverter.Register<CNode>();
        cborConverter.Register<CScene>();
        cborConverter.Register<CSkin>();
    }

    //public static Mesh LoadMeshFile(string fname)
    //{
    //    CborObject meshVal = Cbor.Deserialize<CborObject>(File.ReadAllBytes(fname));
    //    if (meshVal is not CborObject meshObj)
    //        throw new Exception("Invalid mesh data in cbor file");
    //    return LoadMesh(meshObj);
    //}

    //public static Mesh LoadMesh(CborObject cborObject)
    //{
    //    CMesh cborMesh = cborObject.ToObject<CMesh>();
    //    return LoadMesh(cborMesh);
    //}

    public static Scene LoadScene(byte[] bytes)
        => LoadScene(CBORObject.DecodeFromBytes(bytes));

    public static Scene LoadSceneFile(string fname)
        => LoadScene(File.ReadAllBytes(fname));

    public static Scene LoadScene(CBORObject jscene)
    {
        LoadContext ctx = new LoadContext();
        return ctx.LoadScene(jscene);
    }

    class LoadContext
    {
        Node[] nodes = [];

        static void LoadNode(Node node, CNode cnode)
        {
            node.Name = cnode.name;

            node.LclTranslation = cnode.lclTranslation;
            node.LclRotation = cnode.lclRotation;
            node.LclScaling = cnode.lclScaling;

            node.PreRotation = cnode.preRotation;
            node.PostRotation = cnode.postRotation;
            node.RotationOffset = cnode.rotationOffset;
            node.RotationPivot = cnode.rotationPivot;

            node.ScalingOffset = cnode.scalingOffset;
            node.ScalingPivot = cnode.scalingPivot;

            node.GeometricScaling = cnode.geometricScaling.XYZ();
            node.GeometricRotation = cnode.geometricRotation.XYZ();
            node.GeometricTranslation = cnode.geometricTranslation.XYZ();

            node.RotationOrder = cnode.rotationOrder;
        }

        Mesh LoadMesh(CMesh cborMesh)
        {
            Mesh mesh = new()
            {
                Name = cborMesh.name,
                IsTriangleMesh = cborMesh.isTriangleMesh
            };

            // Load vertices
            if (cborMesh.vertices?.Length > 0)
            {
                mesh.VertexPositions = cborMesh.vertices;
            }

            //load polygons
            if (cborMesh.polygons.sizes.Length > 0)
            {
                byte[] sizes = cborMesh.polygons.sizes;
                List<Mesh.PolygonDef> polygons = [];
                int p = 0;
                for (int i = 0; i < sizes.Length; i++)
                {
                    polygons.Add(new Mesh.PolygonDef { Index = p, Size = sizes[i] });
                    p += sizes[i];
                }
                if (p != cborMesh.polygons.indices.Length)
                    throw new Exception($"Invalid polygon data, size mismatch, total size: {p}, total indices: {cborMesh.polygons.indices.Length}");
                mesh.Indices = cborMesh.polygons.indices;
                mesh.Polygons = polygons.ToArray();
            }

            //layers
            mesh.Layers = new Layer[cborMesh.layers.Length];
            for (int layerIndex = 0; layerIndex < cborMesh.layers.Length; layerIndex++)
            {
                var rawLayer = cborMesh.layers[layerIndex];
                var layer = new Layer();
                mesh.Layers[layerIndex] = layer;
                if (rawLayer != null)
                {
                    if (rawLayer.normals != null)
                    {
                        if (rawLayer.normals.mappingMode != EMappingMode.ByPolygonVertex)
                            throw new NotImplementedException($"mappingMode {rawLayer.normals.mappingMode} not implemented");
                        if (rawLayer.normals.referenceMode != EReferenceMode.Direct)
                            throw new NotImplementedException($"referenceMode {rawLayer.normals.referenceMode} not implemented");
                        layer.Normals = new LayerElement<Vector3>
                        {
                            MappingMode = rawLayer.normals.mappingMode,
                            ReferenceMode = rawLayer.normals.referenceMode,
                            Data = rawLayer.normals.data
                        };
                    }
                }
            }

            //process vertex normals
            if (mesh.Layers.Length >= 1 && mesh.Layers[0].Normals?.MappingMode == EMappingMode.ByPolygonVertex)
            {
                var normals = mesh.Layers[0].Normals!.Data;
                Debug.Assert(normals.Length == mesh.Indices.Length);
                int vertexCount = mesh.VertexCount;
                Vector3[] vnormals = new Vector3[vertexCount];
                int[] vcnts = new int[vertexCount];
                for (int i = 0; i < normals.Length; i++)
                {
                    int v = mesh.Indices[i];
                    vnormals[v] += normals[i];
                    vcnts[v]++;
                }
                for (int i = 0; i < vertexCount; i++)
                {
                    if (vcnts[i] > 0)
                        vnormals[i] /= vcnts[i];
                    else
                        vnormals[i] = Vector3.UnitY;
                }
                mesh.VertexNormals = vnormals;
            }

            //deformers
            mesh.Deformers = new Deformer[cborMesh.deformers.Length];
            for (int i = 0; i < cborMesh.deformers.Length; i++)
            {
                var cdeformer = cborMesh.deformers[i];
                mesh.Deformers[i] = LoadDeformer(cdeformer);
            }
            return mesh;
        }

        FbxNull LoadNull(CFbxNull fbxNull)
            => new ()
            {
                Name = fbxNull.name,
                Size = fbxNull.size,
                Look = fbxNull.look
            };

        Deformer LoadDeformer(CDeformer cdeformer)
        {
            Deformer deformer;
            if (cdeformer is CSkin cskin)
            {
                var skin = new Skin();
                skin.DeformAccuracy = cskin.deformAccuracy;
                skin.SkinningType = cskin.skinningType;
                skin.Clusters = new Cluster[cskin.clusters.Length];                
                for (int i = 0; i < cskin.clusters.Length; i++)
                {
                    var ccluster = cskin.clusters[i];
                    var cluster = new Cluster();
                    cluster.LinkMode = ccluster.linkMode;
                    cluster.ControlPointIndices = ccluster.controlPointIndices;
                    cluster.ControlPointWeights = ccluster.controlPointWeights;
                    cluster.TransformMatrix = ccluster.transformMatrix;
                    cluster.TransformLinkMatrix = ccluster.transformLinkMatrix;
                    if (ccluster.linkNode.HasValue)
                        cluster.Link = nodes[ccluster.linkNode.Value];
                    skin.Clusters[i] = cluster;
                }
                deformer = skin;
            }
            else
            {
                deformer = new UnsupportedDeformer(cdeformer.deformerType);
            }
            cdeformer.Assign(deformer);
            return deformer;
        }

        static Skeleton LoadSkeleton(CSkeleton cskeleton)
        {
            var skeleton = new Skeleton();
            skeleton.Name = cskeleton.name;
            skeleton.Size = cskeleton.size;
            skeleton.LimbLength = cskeleton.limbLength;
            return skeleton;
        }

        NodeAttribute LoadAttr(CNodeAttribute cattr)
            => cattr.attributeType switch
            {
                NodeAttribute.EType.Skeleton => LoadSkeleton((CSkeleton)cattr),
                NodeAttribute.EType.Mesh => LoadMesh((CMesh)cattr),
                NodeAttribute.EType.Null => LoadNull((CFbxNull)cattr),
                _ => throw new NotImplementedException($"Attribute {cattr.attributeType} not implemented")
            };

        AnimCurve LoadAnimCurve(CAnimCurve cAnimCurve)
            => new ()
            {
                Keyframes = cAnimCurve.keyframes
            };

        AnimCurve3 LoadAnimCurve3(CAnimCurve[] cAnimCurves)
        {
            if (cAnimCurves.Length != 3)
                throw new Exception("Invalid AnimCurve3 data, length != 3");
            return new AnimCurve3
            {
                X = LoadAnimCurve(cAnimCurves[0]),
                Y = LoadAnimCurve(cAnimCurves[1]),
                Z = LoadAnimCurve(cAnimCurves[2])
            };
        }

        AnimNode LoadNodeAnimation(CNodeAnimation cnodeAnim)
            => new()
            {
                Node = nodes[cnodeAnim.nodeIndex],
                LclScaling = LoadAnimCurve3(cnodeAnim.lclScaling),
                LclRotation = LoadAnimCurve3(cnodeAnim.lclRotation),
                LclTranslation = LoadAnimCurve3(cnodeAnim.lclTranslation)
            };

        AnimLayer LoadAnimLayer(CAnimLayer cLayer)
            => new ()
            {
                Name = cLayer.name,
                NodeAnimations = [.. cLayer.nodeAnimations.Select(LoadNodeAnimation)]
            };

        AnimStack LoadAnimStack(CAnimStack cAnimStack)
        {
            var animStack = new AnimStack
            {
                Name = cAnimStack.name,
                Layers = [.. cAnimStack.animLayers.Select(LoadAnimLayer)],
                LocalTimeSpan = new FbxTimeSpan
                {
                    Start = cAnimStack.localTimeSpan.start,
                    Stop = cAnimStack.localTimeSpan.stop
                }
            };
            foreach(var layer in animStack.Layers)
                layer.AnimStack = animStack;
            return animStack;
        }

        public Scene LoadScene(CBORObject jscene)
        {
            var cscene = cborConverter.Convert<CScene>(jscene) ?? throw new Exception("Invalid scene");
            string? sceneName = cscene.name;
            int attrCount = cscene.nodeAttributes.Length;
            int nodeCount = cscene.nodes.Length;
            nodes = new Node[nodeCount];
            for (int i = 0; i < nodeCount; i++)
                nodes[i] = new () { Index = i };
            Scene scene = new ()
            {
                NodeAttributes = new NodeAttribute[attrCount],
                Nodes = this.nodes,
                RootNode = new Node(),
                Name = sceneName,
                SystemUnit = new ()
                {
                    ScaleFactor = cscene.systemUnit.scaleFactor,
                    Multiplier = cscene.systemUnit.multiplier,
                },
                TimeMode = cscene.timeMode
            };

            //attributes
            for (int i = 0; i < attrCount; i++)
            {
                var cattr = cscene.nodeAttributes[i];
                scene.NodeAttributes[i] = LoadAttr(cattr);
            }

            //nodes
            for (int i = 0; i < nodeCount; i++)
            {
                var cnode = cscene.nodes[i];
                LoadNode(nodes[i], cnode);
            }
            for (int i = 0; i < nodeCount; i++)
            {
                var cnode = cscene.nodes[i];
                var node = nodes[i];
                //attributes
                int attrIdx = cnode.defaultAttributeIndex;
                if (attrIdx >= 0 && attrIdx < attrCount)
                    node.DefaultAttributeIndex = attrIdx;
                if (cnode.attributes?.Length > 0)
                {
                    node.Attributes = new NodeAttribute[cnode.attributes.Length];
                    for (int j = 0; j < cnode.attributes.Length; j++)
                    {
                        int aidx = cnode.attributes[j];
                        if (aidx < 0 || aidx >= attrCount)
                            throw new Exception($"Invalid attribute index {aidx} in node (attribute count {attrCount})");
                        var attr = scene.NodeAttributes[aidx];
                        node.Attributes[j] = attr;
                        attr.Nodes.Add(node);
                    }
                }
                //children
                if (cnode.children?.Length > 0)
                {
                    node.Children = new Node[cnode.children.Length];
                    for (int j = 0; j < cnode.children.Length; j++)
                    {
                        int nidx = cnode.children[j];
                        if (nidx >= 0 && nidx < nodeCount)
                        {
                            node.Children[j] = nodes[nidx];
                            if (nodes[nidx].Parent != null)
                                throw new Exception($"Node {nodes[nidx].Name} has multiple parents!");
                            nodes[nidx].Parent = node;
                        }
                    }
                }
            }
            foreach(var node in nodes)
                node.Scene = scene;
            scene.RootNode = nodes[0];

            //animation
            scene.AnimStacks = [.. cscene.animStacks.Select(LoadAnimStack)];
            foreach(var animStack in scene.AnimStacks)
                animStack.Scene = scene;

            //build name map
            scene.BuildName2NodeMap();

            return scene;
        }
    }
}
