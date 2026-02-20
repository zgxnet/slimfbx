using Stride.Core.Mathematics;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Matrix = Stride.Core.Mathematics.Matrix;
namespace SlimFbx;

public class GlExporter
{
    // glTF component type constants
    enum ComponentType
    {
        Byte = 5120,
        UnsignedByte = 5121,
        Short = 5122,
        UnsignedShort = 5123,
        UnsignedInt = 5125,
        Float = 5126
    }

    // glTF buffer view target constants
    enum BufferTarget
    {
        ArrayBuffer = 34962,
        ElementArrayBuffer = 34963
    }

    // glTF primitive mode constants
    enum PrimitiveMode
    {
        Points = 0,
        Lines = 1,
        LineLoop = 2,
        LineStrip = 3,
        Triangles = 4,
        TriangleStrip = 5,
        TriangleFan = 6
    }

    readonly Scene scene;

    // If rootNode does not contain transform, we do not need it
    bool exportRootnode = false;

    // glTF bufferView definitions
    JsonArray bufferViews = new JsonArray();
    // glTF accessor definitions 
    JsonArray accessors = new JsonArray();
    // glTF binary data
    MemoryWriter binaryData = new MemoryWriter();
    // glTF meshes
    JsonArray meshes = new JsonArray();
    // glTF skins
    JsonArray skins = new JsonArray();
    // glTF nodes
    JsonArray nodes = new JsonArray();
    JsonArray sceneRootNodes = new JsonArray();
    // glTF animations
    JsonArray animations = new JsonArray();

    public GlExporter(Scene scene)
    {
        this.scene = scene ?? throw new ArgumentNullException(nameof(scene));
        exportRootnode = scene.RootNode != null && scene.RootNode.HasTransform;
        AddAllNodes();
    }

    int GetNodeIndex(Node node)
        => exportRootnode ? node.Index : node.Index - 1;

    void AddAllNodes()
    {
        foreach (var node in exportRootnode ? scene.Nodes : scene.Nodes.Skip(1))
        {
            var nodeObject = new JsonObject();

            // Add node name if available
            if (!string.IsNullOrEmpty(node.Name))
            {
                nodeObject["name"] = node.Name;
            }

            // Build local transform matrix from node properties
            var trs = node.LocalTransform.ToTRSTransform();

            // Add translation if not zero
            if (trs.Translation != Vector3.Zero)
            {
                nodeObject["translation"] = new JsonArray(trs.Translation.X, trs.Translation.Y, trs.Translation.Z);
            }

            // Add rotation if not identity
            if (trs.Rotation != Quaternion.Identity)
            {
                nodeObject["rotation"] = new JsonArray(trs.Rotation.X, trs.Rotation.Y, trs.Rotation.Z, trs.Rotation.W);
            }

            // Add scale if not one
            if (trs.Scaling != Vector3.One)
            {
                nodeObject["scale"] = new JsonArray(trs.Scaling.X, trs.Scaling.Y, trs.Scaling.Z);
            }

            // Add children indices if node has children
            if (node.Children.Length > 0)
            {
                var children = new JsonArray();
                foreach (var child in node.Children)
                {
                    children.Add(GetNodeIndex(child));
                }
                nodeObject["children"] = children;
            }

            nodes.Add(nodeObject);
        }
        if(exportRootnode)
            sceneRootNodes.Add(0);
        else
        {
            foreach (var node in scene.RootNode.Children)
                sceneRootNodes.Add(GetNodeIndex(node));
        }
    }

    public void AddAllMeshes()
    {
        foreach (var mesh in scene.GetMeshes())
        {
            // Create the mesh and get its index
            int meshIndex = AddMesh(mesh);
            
            // Create skin if mesh has skin deformer
            int skinIndex = AddSkin(mesh);
            
            // Attach mesh and skin to all nodes that use this mesh
            foreach (var node in mesh.Nodes)
            {
                int nodeIndex = GetNodeIndex(node);
                var nodeObject = (JsonObject)nodes[nodeIndex]!;
                
                nodeObject["mesh"] = meshIndex;
                
                if (skinIndex >= 0)
                {
                    nodeObject["skin"] = skinIndex;
                }
            }
        }
    }

    /// <summary>
    /// Converts a mesh from the FBX scene into a glTF mesh JSON object.
    /// It populates the meshes array as well as the bufferViews and accessors arrays, and binary data used in the glTF structure.
    /// </summary>
    /// <param name="mesh">The mesh to convert.</param>
    /// <returns>The index of the mesh in the meshes array.</returns>
    int AddMesh(Mesh mesh)
    {
        if(!mesh.IsTriangleMesh)
            mesh = mesh.Triangulate(false);
        bool hasNormal = mesh.VertexNormals?.Length > 0;
        bool hasSkin = mesh.SkinDeformer != null;
        int vertexCount = mesh.VertexCount;
        int indexCount = mesh.Indices.Length;

        // Calculate interleaved vertex layout and accessor byte offsets
        int vertexStride = 0;
        int positionOffset = vertexStride;
        vertexStride += Unsafe.SizeOf<Vector3>(); // Position
        
        int normalOffset = -1;
        if (hasNormal)
        {
            normalOffset = vertexStride;
            vertexStride += Unsafe.SizeOf<Vector3>(); // Normal
        }
        
        int jointsOffset = -1;
        int weightsOffset = -1;
        if (hasSkin)
        {
            if (mesh.SkinDeformer!.Clusters.Length > 255)
                throw new InvalidOperationException("Too many joints in skin for byte joint indices, max number is 255.");
            jointsOffset = vertexStride;
            vertexStride += 4 * sizeof(byte); // Joints (4 x byte)
            weightsOffset = vertexStride;
            vertexStride += Unsafe.SizeOf<Vector4>(); // Weights
        }

        // Build vertex bone data if skin exists
        (ushort jointIndex, float weight)[][]? vertexBones = hasSkin ?
            SkinUtil.BuildVertexBones(vertexCount, mesh.SkinDeformer!, 4) : null;

        // Write interleaved vertex data to binary buffer
        int vertexBufferOffset = binaryData.Length;
        int vertexBufferLength = vertexCount * vertexStride;
        
        var posMin = new Vector3(float.MaxValue);
        var posMax = new Vector3(float.MinValue);
        
        for (int i = 0; i < vertexCount; i++)
        {
            var pos = mesh.VertexPositions[i];
            binaryData.Write(pos);
            posMin = Vector3.Min(posMin, pos);
            posMax = Vector3.Max(posMax, pos);

            if (hasNormal)
            {
                var v = mesh.VertexNormals![i];
                v.Normalize();
                binaryData.Write(v);
            }

            if (hasSkin)
            {
                var bones = vertexBones![i];
                // Write joint indices (as 4 bytes)
                for (int j = 0; j < 4; j++)
                {
                    binaryData.Write(j < bones.Length ? (byte)bones[j].jointIndex : (byte)0);
                }
                
                // Write joint weights (as 4 floats)
                for (int j = 0; j < 4; j++)
                {
                    binaryData.Write(j < bones.Length ? bones[j].weight : 0f);
                }
            }
        }

        // Create single buffer view for interleaved vertex data
        int vertexBufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = vertexBufferOffset,
            ["byteLength"] = vertexBufferLength,
            ["byteStride"] = vertexStride,
            ["target"] = (int)BufferTarget.ArrayBuffer
        });

        // Create accessor for positions
        int positionAccessorIndex = accessors.Count;
        accessors.Add(new JsonObject
        {
            ["bufferView"] = vertexBufferViewIndex,
            ["byteOffset"] = positionOffset,
            ["componentType"] = (int)ComponentType.Float,
            ["count"] = vertexCount,
            ["type"] = "VEC3",
            ["min"] = new JsonArray(posMin.X, posMin.Y, posMin.Z),
            ["max"] = new JsonArray(posMax.X, posMax.Y, posMax.Z)
        });

        // Create accessor for normals if available
        int normalAccessorIndex = -1;
        if (hasNormal)
        {
            normalAccessorIndex = accessors.Count;
            accessors.Add(new JsonObject
            {
                ["bufferView"] = vertexBufferViewIndex,
                ["byteOffset"] = normalOffset,
                ["componentType"] = (int)ComponentType.Float,
                ["count"] = vertexCount,
                ["type"] = "VEC3"
            });
        }

        // Create accessors for joints and weights if skin exists
        int jointsAccessorIndex = -1;
        int weightsAccessorIndex = -1;
        if (hasSkin)
        {
            jointsAccessorIndex = accessors.Count;
            accessors.Add(new JsonObject
            {
                ["bufferView"] = vertexBufferViewIndex,
                ["byteOffset"] = jointsOffset,
                ["componentType"] = (int)ComponentType.UnsignedByte,
                ["count"] = vertexCount,
                ["type"] = "VEC4"
            });

            weightsAccessorIndex = accessors.Count;
            accessors.Add(new JsonObject
            {
                ["bufferView"] = vertexBufferViewIndex,
                ["byteOffset"] = weightsOffset,
                ["componentType"] = (int)ComponentType.Float,
                ["count"] = vertexCount,
                ["type"] = "VEC4"
            });
        }

        // Write indices data to binary buffer
        int indicesBufferOffset = binaryData.Length;
        int indicesByteLength = indexCount * sizeof(int);
        binaryData.WriteSpan(mesh.Indices);

        // Create buffer view and accessor for indices
        int indicesBufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = indicesBufferOffset,
            ["byteLength"] = indicesByteLength,
            ["target"] = (int)BufferTarget.ElementArrayBuffer
        });

        int indicesAccessorIndex = accessors.Count;
        accessors.Add(new JsonObject
        {
            ["bufferView"] = indicesBufferViewIndex,
            ["byteOffset"] = 0,
            ["componentType"] = (int)ComponentType.UnsignedInt,
            ["count"] = indexCount,
            ["type"] = "SCALAR"
        });

        // Create mesh primitive
        var attributes = new JsonObject
        {
            ["POSITION"] = positionAccessorIndex
        };

        if (hasNormal)
        {
            attributes["NORMAL"] = normalAccessorIndex;
        }

        if (hasSkin)
        {
            attributes["JOINTS_0"] = jointsAccessorIndex;
            attributes["WEIGHTS_0"] = weightsAccessorIndex;
        }

        var primitive = new JsonObject
        {
            ["attributes"] = attributes,
            ["indices"] = indicesAccessorIndex,
            ["mode"] = (int)PrimitiveMode.Triangles
        };

        // Create and add the mesh object
        var meshObject = new JsonObject
        {
            ["primitives"] = new JsonArray { primitive }
        };

        if (!string.IsNullOrEmpty(mesh.Name))
        {
            meshObject["name"] = mesh.Name;
        }

        int meshIndex = meshes.Count;
        meshes.Add(meshObject);
        return meshIndex;
    }

    /// <summary>
    /// Creates a glTF skin object for the mesh.
    /// Returns the skin index, or -1 if the mesh has no skin.
    /// </summary>
    int AddSkin(Mesh mesh)
    {
        var skinDeformer = mesh.SkinDeformer;
        if (skinDeformer == null)
            return -1;

        var joints = new JsonArray();
        var inverseBindMatrices = new List<Matrix>();

        foreach (var cluster in skinDeformer.Clusters)
        {
            var linkNode = cluster.Link ?? throw new InvalidOperationException("Cluster has no link node");
            int nodeIndex = GetNodeIndex(linkNode);

            joints.Add(nodeIndex);
            inverseBindMatrices.Add(cluster.EvaluateLinkToMeshMatrix(mesh));
        }

        // Write inverse bind matrices to binary buffer
        int ibmBufferOffset = binaryData.Length;
        foreach (var matrix in inverseBindMatrices)
        {
            // glTF uses column-major matrices, right mult
            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    float val = (col, row) switch 
                    {
                        (3, 3) => 1.0f,
                        (_, 3) => 0.0f,
                        _ => matrix[col, row]
                    };
                    binaryData.Write(val);
                }
            }
        }
        int ibmByteLength = inverseBindMatrices.Count * 16 * sizeof(float);

        // Create buffer view for inverse bind matrices
        int ibmBufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = ibmBufferOffset,
            ["byteLength"] = ibmByteLength
        });

        // Create accessor for inverse bind matrices
        int ibmAccessorIndex = accessors.Count;
        accessors.Add(new JsonObject
        {
            ["bufferView"] = ibmBufferViewIndex,
            ["byteOffset"] = 0,
            ["componentType"] = (int)ComponentType.Float,
            ["count"] = inverseBindMatrices.Count,
            ["type"] = "MAT4"
        });

        // Create skin object
        var skin = new JsonObject
        {
            ["joints"] = joints,
            ["inverseBindMatrices"] = ibmAccessorIndex
        };

        int skinIndex = skins.Count;
        if (!string.IsNullOrEmpty(mesh.Name))
        {
            skin["name"] = skinDeformer.Name ?? (mesh.Name != null ? $"{mesh.Name}_Skin" : $"Skin_{skinIndex}");
        }

        skins.Add(skin);
        return skinIndex;
    }

    //mesh must be a triangle mesh and is from the scene
    public int AddMeshNode(Mesh mesh)
    {
        // Create the mesh and get its index
        int meshIndex = AddMesh(mesh);
        
        // Create skin if mesh has skin deformer
        int skinIndex = AddSkin(mesh);
        
        // Create a node that references this mesh
        var meshNode = new JsonObject
        {
            ["mesh"] = meshIndex
        };
        
        // Add skin reference if skin exists
        if (skinIndex >= 0)
        {
            meshNode["skin"] = skinIndex;
        }
        
        // Add mesh name to node if available
        if (!string.IsNullOrEmpty(mesh.Name))
        {
            meshNode["name"] = $"{mesh.Name}_Node";
        }
        
        int meshNodeIndex = nodes.Count;
        nodes.Add(meshNode);
        sceneRootNodes.Add(meshNodeIndex);
        return meshNodeIndex;
    }

    /// <summary>
    /// Adds an animation stack to the glTF export. Currently only supports single-layer animation stacks.
    /// </summary>
    public int AddAnimStack(AnimStack animStack)
    {
        if (animStack.Layers.Count != 1)
            throw new NotSupportedException("Only single-layer animation stacks are supported.");
        return AddAnimLayer(animStack.Layers[0]);
    }

    /// <summary>
    /// Converts an animation layer from the FBX scene into a glTF animation.
    /// Creates animation samplers and channels for translation, rotation, and scaling of animated nodes.
    /// Populates the animations array as well as the bufferViews and accessors arrays for keyframe data.
    /// </summary>
    /// <param name="animLayer">The animation layer to convert. Must contain node animations with keyframes.</param>
    /// <returns>The index of the animation in the animations array.</returns>
    public int AddAnimLayer(AnimLayer animLayer)
    {
        var samplers = new JsonArray();
        var channels = new JsonArray();
        FbxTimeSpan timeSpan = animLayer.AnimStack?.LocalTimeSpan ?? throw new ArgumentException("AnimLayer has no AnimStack or TimeSpan");
        long frameTimeStep = FbxTime.GetOneFrameValue(animLayer.AnimStack?.Scene?.TimeMode ??
            throw new ArgumentException("AnimLayer has no AnimStack or Scene"));
        List<long> times = [];
        for (long time = timeSpan.Start; time <= timeSpan.Stop; time += frameTimeStep)
            times.Add(time);

        if (times.Count == 0)
            return -1;

        int sharedTimesAccessorIndex = CreateTimeAccessor(times);

        foreach (var animNode in animLayer.NodeAnimations)
        {
            int nodeIndex = GetNodeIndex(animNode.Node);
            bool hasTranslation = !animNode.IsDefaultTranslation;
            bool hasRotation = !animNode.IsDefaultRotation;
            bool hasScale = !animNode.IsDefaultScaling;

            if (!hasTranslation && !hasRotation && !hasScale)
                continue;

            List<Vector3> translations = new(times.Count), scales = new(times.Count);
            List<Quaternion> rotations = new(times.Count);
            
            foreach (var time in times)
            {
                var trs = animNode.EvaluateLocalTransformLinearAt(time).ToTRSTransform();
                if (hasTranslation) translations.Add(trs.Translation);
                if (hasRotation) rotations.Add(trs.Rotation);
                if (hasScale) scales.Add(trs.Scaling);
            }

            if (hasTranslation)
            {
                int accessorIndex = CreateVector3Accessor(translations);
                int samplerIndex = samplers.Count;
                samplers.Add(new JsonObject
                {
                    ["input"] = sharedTimesAccessorIndex,
                    ["output"] = accessorIndex,
                    ["interpolation"] = "LINEAR"
                });
                channels.Add(new JsonObject
                {
                    ["sampler"] = samplerIndex,
                    ["target"] = new JsonObject { ["node"] = nodeIndex, ["path"] = "translation" }
                });
            }

            if (hasRotation)
            {
                int accessorIndex = CreateQuaternionAccessor(rotations);
                int samplerIndex = samplers.Count;
                samplers.Add(new JsonObject
                {
                    ["input"] = sharedTimesAccessorIndex,
                    ["output"] = accessorIndex,
                    ["interpolation"] = "LINEAR"
                });
                channels.Add(new JsonObject
                {
                    ["sampler"] = samplerIndex,
                    ["target"] = new JsonObject { ["node"] = nodeIndex, ["path"] = "rotation" }
                });
            }

            if (hasScale)
            {
                int accessorIndex = CreateVector3Accessor(scales);
                int samplerIndex = samplers.Count;
                samplers.Add(new JsonObject
                {
                    ["input"] = sharedTimesAccessorIndex,
                    ["output"] = accessorIndex,
                    ["interpolation"] = "LINEAR"
                });
                channels.Add(new JsonObject
                {
                    ["sampler"] = samplerIndex,
                    ["target"] = new JsonObject { ["node"] = nodeIndex, ["path"] = "scale" }
                });
            }
        }

        var animation = new JsonObject
        {
            ["samplers"] = samplers,
            ["channels"] = channels
        };

        string? name = animLayer.AnimStack?.Name ?? animLayer.Name;
        if (!string.IsNullOrEmpty(name))
            animation["name"] = name;

        int animationIndex = animations.Count;
        animations.Add(animation);
        return animationIndex;
    }

    int CreateTimeAccessor(List<long> times)
    {
        int bufferOffset = binaryData.Length;
        foreach (var time in times)
            binaryData.Write((float)(time * FbxTime.TimeUnit));

        int bufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = bufferOffset,
            ["byteLength"] = times.Count * sizeof(float)
        });

        int accessorIndex = accessors.Count;
        float minTime = (float)(times[0] * FbxTime.TimeUnit);
        float maxTime = (float)(times[^1] * FbxTime.TimeUnit);
        accessors.Add(new JsonObject
        {
            ["bufferView"] = bufferViewIndex,
            ["byteOffset"] = 0,
            ["componentType"] = (int)ComponentType.Float,
            ["count"] = times.Count,
            ["type"] = "SCALAR",
            ["min"] = new JsonArray(minTime),
            ["max"] = new JsonArray(maxTime)
        });

        return accessorIndex;
    }

    int CreateVector3Accessor(List<Vector3> vectors)
    {
        int bufferOffset = binaryData.Length;
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var v in vectors)
        {
            binaryData.Write(v);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        int bufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = bufferOffset,
            ["byteLength"] = vectors.Count * Unsafe.SizeOf<Vector3>()
        });

        int accessorIndex = accessors.Count;
        accessors.Add(new JsonObject
        {
            ["bufferView"] = bufferViewIndex,
            ["byteOffset"] = 0,
            ["componentType"] = (int)ComponentType.Float,
            ["count"] = vectors.Count,
            ["type"] = "VEC3",
            ["min"] = new JsonArray(min.X, min.Y, min.Z),
            ["max"] = new JsonArray(max.X, max.Y, max.Z)
        });

        return accessorIndex;
    }

    int CreateQuaternionAccessor(List<Quaternion> quaternions)
    {
        int bufferOffset = binaryData.Length;
        foreach (var q in quaternions)
            binaryData.Write(q);

        int bufferViewIndex = bufferViews.Count;
        bufferViews.Add(new JsonObject
        {
            ["buffer"] = 0,
            ["byteOffset"] = bufferOffset,
            ["byteLength"] = quaternions.Count * Unsafe.SizeOf<Quaternion>()
        });

        int accessorIndex = accessors.Count;
        accessors.Add(new JsonObject
        {
            ["bufferView"] = bufferViewIndex,
            ["byteOffset"] = 0,
            ["componentType"] = (int)ComponentType.Float,
            ["count"] = quaternions.Count,
            ["type"] = "VEC4"
        });

        return accessorIndex;
    }

    /// <summary>
    /// Generates the glTF JSON structure
    /// </summary>
    /// <param name="includeBinaryUri">If true, includes a URI reference to external .bin file. If false, assumes embedded binary (GLB format).</param>
    /// <param name="binaryFileName">The name of the binary file (used only when includeBinaryUri is true).</param>
    /// <returns>The glTF JSON object.</returns>
    JsonObject GenerateGltfJson(bool includeBinaryUri = false, string? binaryFileName = null)
    {
        // Create the glTF JSON structure
        var gltf = new JsonObject
        {
            ["asset"] = new JsonObject
            {
                ["version"] = "2.0",
                ["generator"] = "SlimFbx GlExporter"
            }
        };

        // Add nodes (already built in constructor)
        if (nodes.Count > 0)
        {
            gltf["nodes"] = nodes.DeepClone();
        }

        // Create a scene that references mesh nodes
        gltf["scenes"] = new JsonArray
            {
                new JsonObject
                {
                    ["nodes"] = sceneRootNodes.DeepClone()
                }
            };

        // Set the default scene to display
        gltf["scene"] = 0;

        // Add buffers
        var bufferObject = new JsonObject
        {
            ["byteLength"] = binaryData.Length
        };
        
        if (includeBinaryUri && !string.IsNullOrEmpty(binaryFileName))
        {
            bufferObject["uri"] = binaryFileName;
        }
        
        gltf["buffers"] = new JsonArray { bufferObject };

        // Add buffer views if any
        if (bufferViews.Count > 0)
        {
            gltf["bufferViews"] = bufferViews.DeepClone();
        }

        // Add accessors if any
        if (accessors.Count > 0)
        {
            gltf["accessors"] = accessors.DeepClone();
        }

        // Add meshes if any
        if (meshes.Count > 0)
        {
            gltf["meshes"] = meshes.DeepClone();
        }

        // Add skins if any
        if (skins.Count > 0)
        {
            gltf["skins"] = skins.DeepClone();
        }

        // Add animations if any
        if (animations.Count > 0)
        {
            gltf["animations"] = animations.DeepClone();
        }

        return gltf;
    }

    public void SaveAuto(string fname)
    {
        if (fname.EndsWith(".gltf"))
            SaveGltf(fname, true);
        else if (fname.EndsWith(".glb"))
            SaveGlb(fname);
        else
            throw new ArgumentException("File extension must be either .gltf or .glb", nameof(fname));
    }

    /// <summary>
    /// Saves the glTF data as separate .gltf (JSON) and .bin (binary) files.
    /// </summary>
    /// <param name="fname">The path for the .gltf file. The .bin file will have the same name with .bin extension.</param>
    public void SaveGltf(string fname, bool writeIndented = true)
    {
        // Determine binary file name
        string binFileName = Path.ChangeExtension(fname, ".bin");
        string binFileNameOnly = Path.GetFileName(binFileName);

        // Generate glTF JSON with URI reference to binary file
        var gltf = GenerateGltfJson(includeBinaryUri: true, binaryFileName: binFileNameOnly);

        // Save JSON file
        string jsonString = gltf.ToJsonString(new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = writeIndented
        });
        File.WriteAllText(fname, jsonString, System.Text.Encoding.UTF8);

        // Save binary file
        File.WriteAllBytes(binFileName, binaryData.AsSpan());
    }

    /// <summary>
    /// Saves the glTF data as a single .glb (binary glTF) file.
    /// </summary>
    /// <param name="fname">The path for the .glb file.</param>
    public void SaveGlb(string fname)
    {
        // Generate glTF JSON (no URI, for embedded binary)
        var gltf = GenerateGltfJson(includeBinaryUri: false);

        // Serialize JSON to string
        string jsonString = gltf.ToJsonString();
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        // Pad JSON to 4-byte alignment
        int jsonPadding = (4 - (jsonBytes.Length % 4)) % 4;
        int jsonLength = jsonBytes.Length + jsonPadding;

        // Prepare binary data
        var binaryBytes = binaryData.AsSpan();
        int binaryPadding = (4 - (binaryBytes.Length % 4)) % 4;
        int binaryLength = binaryBytes.Length + binaryPadding;

        // Calculate total file size
        const int headerSize = 12;
        const int chunkHeaderSize = 8;
        int totalLength = headerSize + 
                         chunkHeaderSize + jsonLength + 
                         chunkHeaderSize + binaryLength;

        // Write GLB file
        using var stream = new FileStream(fname, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write GLB header
        writer.Write(0x46546C67u); // magic: "glTF"
        writer.Write(2u);           // version: 2
        writer.Write((uint)totalLength);

        // Write JSON chunk header
        writer.Write((uint)jsonLength);
        writer.Write(0x4E4F534Au);  // chunkType: "JSON"

        // Write JSON chunk data
        writer.Write(jsonBytes);
        for (int i = 0; i < jsonPadding; i++)
            writer.Write((byte)0x20); // Space padding

        // Write BIN chunk header
        writer.Write((uint)binaryLength);
        writer.Write(0x004E4942u);   // chunkType: "BIN\0"

        // Write BIN chunk data
        writer.Write(binaryBytes);
        for (int i = 0; i < binaryPadding; i++)
            writer.Write((byte)0); // Zero padding
    }
}
