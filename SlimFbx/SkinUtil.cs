using System;
using System.Collections.Generic;
using System.Text;

namespace SlimFbx;

public static class SkinUtil
{
    /// <summary>
    /// Builds bone indices and weights for each vertex from the FBX skin data.
    /// </summary>
    /// <param name="nVertices">The total number of vertices in the mesh.</param>
    /// <param name="fbxSkin">The FBX skin deformer containing bone clusters and weights.</param>
    /// <param name="nBonesPerVertex">Number of bones that can influence each vertex.
    ///     If a vertex has less than this number, the remaining weights will be zero.
    ///     And if a vertex has more, only the top nBonesPerVertex weights will be kept.
    /// </param>
    /// <returns>
    /// A jagged array where each element represents a vertex, containing tuples of (boneIndex, weight).
    /// The bone indices are limited to the top N most influential bones per vertex based on weight.
    /// </returns>
    public static (ushort boneIndex, float weight)[][] BuildVertexBones(int nVertices, Skin fbxSkin, int nBonesPerVertex)
    {
        // Initialize vertex bones array - each vertex can have multiple bone influences
        var vertexBones = new List<(ushort boneIndex, float weight)>[nVertices];
        for (int i = 0; i < nVertices; i++)
            vertexBones[i] = [];
        if (fbxSkin.Clusters.Length > ushort.MaxValue)
            throw new ArgumentException("Too many bone clusters");

        // Iterate through each cluster (bone) in the skin
        for (int clusterIndex = 0; clusterIndex < fbxSkin.Clusters.Length; clusterIndex++)
        {
            var cluster = fbxSkin.Clusters[clusterIndex];

            // Add bone influence for each control point (vertex) affected by this cluster
            for (int i = 0; i < cluster.ControlPointIndices.Length; i++)
            {
                int vertexIndex = cluster.ControlPointIndices[i];
                float weight = cluster.ControlPointWeights[i];

                // Skip zero weights
                if (weight > 0 && vertexIndex < nVertices)
                    vertexBones[vertexIndex].Add(((ushort)clusterIndex, weight));
            }
        }

        // Convert to array and keep only top N bones per vertex, sorted by weight
        var result = new (ushort boneIndex, float weight)[nVertices][];
        for (int i = 0; i < nVertices; i++)
        {
            var bones = vertexBones[i];

            // Sort by weight descending and take top N
            var topBones = new (ushort boneIndex, float weight)[nBonesPerVertex];
            int count = 0;
            foreach (var bone in bones.OrderByDescending(b => b.weight).Take(nBonesPerVertex))
                topBones[count++] = bone;

            // Normalize weights to sum to 1.0
            float totalWeight = topBones.Sum(b => b.weight);
            if (totalWeight > float.Epsilon)
            {
                for (int j = 0; j < count; j++)
                {
                    topBones[j].weight /= totalWeight;
                }
            }

            result[i] = topBones;
        }

        return result;
    }
}
