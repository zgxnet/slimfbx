namespace SlimFbx;
using Stride.Core.Mathematics;

public class AnimNode
{
    public required Node Node;

    public required AnimCurve3 LclScaling;

    public required AnimCurve3 LclRotation;

    public required AnimCurve3 LclTranslation;

    public override string ToString()
        => $"AnimNode: {Node?.Name ?? "<unnamed>"}";

    public bool IsDefaultScaling
        => CheckNodeNoPrePost() &&
           IsConstant(LclScaling.X, 1) &&
           IsConstant(LclScaling.Y, 1) &&
           IsConstant(LclScaling.Z, 1);

    public bool IsDefaultRotation
        => CheckNodeNoPrePost() &&
           IsConstant(LclRotation.X, 0) &&
           IsConstant(LclRotation.Y, 0) &&
           IsConstant(LclRotation.Z, 0);

    public bool IsDefaultTranslation
        => CheckNodeNoPrePost() &&
           IsConstant(LclTranslation.X, 0) &&
           IsConstant(LclTranslation.Y, 0) &&
           IsConstant(LclTranslation.Z, 0);

    bool CheckNodeNoPrePost()
        => 
           Node.PreRotation == Vector3.Zero &&
           Node.PostRotation == Vector3.Zero &&
           Node.RotationOffset == Vector3.Zero &&
           Node.RotationPivot == Vector3.Zero &&
           Node.ScalingOffset == Vector3.Zero &&
           Node.ScalingPivot == Vector3.Zero;

    bool IsConstant(AnimCurve curve, float val)
    {
        foreach(var key in curve.Keyframes)
        {
            if (key.Value != val)
                return false;
        }
        return true;
    }

    public LocalTransform EvaluateLocalTransformLinearAt(long time)
        => new()
        {
            LclTranslation = LclTranslation.EvaluateLinear(time),
            LclRotation = LclRotation.EvaluateLinear(time),
            LclScaling = LclScaling.EvaluateLinear(time),
            PreRotation = Node.PreRotation,
            PostRotation = Node.PostRotation,
            RotationOffset = Node.RotationOffset,
            RotationPivot = Node.RotationPivot,
            ScalingOffset = Node.ScalingOffset,
            ScalingPivot = Node.ScalingPivot
        };

    public LocalTransform EvaluateLocalTransformLinearAt(float time)
        => new()
        {
            LclTranslation = LclTranslation.EvaluateLinear(time),
            LclRotation = LclRotation.EvaluateLinear(time),
            LclScaling = LclScaling.EvaluateLinear(time),
            PreRotation = Node.PreRotation,
            PostRotation = Node.PostRotation,
            RotationOffset = Node.RotationOffset,
            RotationPivot = Node.RotationPivot,
            ScalingOffset = Node.ScalingOffset,
            ScalingPivot = Node.ScalingPivot
        };

    public void DoScale(float factor)
    {
        LclTranslation.DoScale(factor);
    }
}
