using Stride.Core.Mathematics;

namespace SlimFbx;

//For the FBX local transformation, see https://claude.ai/share/e4ccccfc-2995-475e-b53f-9f1c56b5417c
//https://help.autodesk.com/view/FBX/2020/ENU/?guid=FBX_Developer_Help_nodes_and_scene_graph_fbx_nodes_computing_transformation_matrix_html
//LocalTransform = T × Roff × Rp × Rpre × R × Rpost⁻¹ × Rp⁻¹ × Soff × Sp × S × Sp⁻¹
//V' = LocalTransform*V
//V' = Q*(S*(V-Sp)+Sp+Soff-Rp)+Rp+Roff+T = Q*S*V + Q*(Sp-S*Sp+Soff-Rp)+Rp+Roff+T
/*各组件说明：
T: Translation（平移）
Roff: Rotation Offset（旋转偏移）
Rp: Rotation Pivot（旋转轴心点）
Rpre: Pre-Rotation（预旋转）
R: Rotation（旋转）
Rpost: Post-Rotation（后旋转）
Soff: Scaling Offset（缩放偏移）
Sp: Scaling Pivot（缩放轴心点）
S: Scaling（缩放）
*/

//FbxAMatrix.Get似乎应该是(col, row)
public struct LocalTransform
{
    public Vector3 LclTranslation = Vector3.Zero;

    public Vector3 LclRotation = Vector3.Zero; //Euler XYZ in degrees

    public Vector3 LclScaling = new(1, 1, 1);

    //related rotation
    public Vector3 PreRotation;
    public Vector3 PostRotation;
    public Vector3 RotationOffset;
    public Vector3 RotationPivot;

    //related scaling
    public Vector3 ScalingOffset;
    public Vector3 ScalingPivot;

    public LocalTransform()
    { }

    public TRSTransform ToTRSTransform()
    {
        // Combine rotations in FBX order (v -> post⁻¹ -> local -> pre).
        var qPre = FbxMath.RotAngleToQuaternion(PreRotation);
        var qLocal = FbxMath.RotAngleToQuaternion(LclRotation);
        var qPost = FbxMath.RotAngleToQuaternion(PostRotation);
        qPost.Invert();

        var combinedRotation = Quaternion.Multiply(qPost, Quaternion.Multiply(qLocal, qPre));
        combinedRotation = Quaternion.Normalize(combinedRotation);

        //Q*(Sp-S*Sp+Soff-Rp)+Rp+Roff+T
        var T1 = ScalingPivot - Vector3.Modulate(LclScaling, ScalingPivot) + ScalingOffset - RotationPivot;
        var T2 = RotationPivot + RotationOffset + LclTranslation;
        var T3 = T1;
        combinedRotation.Rotate(ref T3);
        T3 += T2;

        return new TRSTransform
        {
            Translation = T3,
            Rotation = combinedRotation,
            Scaling = LclScaling
        };
    }

    public Matrix ToMatrix()
        => ToTRSTransform().ToMatrix();
}
