using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public class Cluster : SubDeformer
{
    public override EType SubDeformerType => EType.Cluster;

    /**
* \name Link Mode, Link Node, Associate Model
*/
    //@{
    /** Link modes.
      * The link mode sets how the link influences the position of a control
      * point and the relationship between the weights assigned to a control
      * point. The weights assigned to a control point are distributed among
      * the set of links associated with an instance of class FbxGeometry.
      */
    public enum ELinkMode
    {
        Normalize,       /*!<	  In mode eNormalize, the sum of the weights assigned to a control point
		                          is normalized to 1.0. Setting the associate model in this mode is not
		                          relevant. The influence of the link is a function of the displacement of the
		                          link node relative to the node containing the control points.*/
        Additive,
        /*!<    In mode eAdditive, the sum of the weights assigned to a control point
                is kept as is. It is the only mode where setting the associate model is
                relevant. The influence of the link is a function of the displacement of
                the link node relative to the node containing the control points or,
                if set, the associate model. The weight gives the proportional displacement
                of a control point. For example, if the weight of a link over a control
                point is set to 2.0, a displacement of the link node of 1 unit in the X
                direction relative to the node containing the control points or, if set,
                the associate model, triggers a displacement of the control point of 2
                units in the same direction.*/
        TotalOne
        /*!<    Mode eTotalOne is identical to mode eNormalize except that the sum of the
                weights assigned to a control point is not normalized and must equal 1.0.*/
    }

    public ELinkMode LinkMode;

    public Node? Link;

    public int[] ControlPointIndices = [];
    
    public float[] ControlPointWeights = [];

    public Matrix TransformLinkMatrix = Matrix.Identity;
    public Matrix TransformMatrix = Matrix.Identity;

    //utility

    /// <summary>
    /// The matrix to transform from mesh space to local space of this bone.
    /// see fbx samples: void ComputeClusterDeformation(FbxAMatrix& pGlobalPosition, 
    //                                                  FbxMesh* pMesh,
    //                                                  FbxCluster* pCluster, 
	//						                            FbxAMatrix& pVertexTransformMatrix,
	//						                            FbxTime pTime,
    //                                                  FbxPose* pPose)
    /// </summary>
    public Matrix EvaluateLinkToMeshMatrix(Node meshNode)
    {
        Matrix lReferenceGeometry = meshNode.GeometricTransform.ToMatrix();
        Matrix lReferenceGlobalInitPosition = lReferenceGeometry * TransformMatrix; //mesh init position

        Matrix lClusterGlobalInitPositionInvert = TransformLinkMatrix;
        lClusterGlobalInitPositionInvert.Invert();

        return lReferenceGlobalInitPosition * lClusterGlobalInitPositionInvert;
    }

    public Matrix EvaluateLinkToMeshMatrix(Mesh mesh)
        => EvaluateLinkToMeshMatrix(mesh.SingleNode);

    public void DoScale(float factor)
    {
        TransformLinkMatrix.TranslationVector *= factor;
        TransformMatrix.TranslationVector *= factor;
    }
}
