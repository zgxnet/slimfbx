using Stride.Core.Mathematics;

namespace SlimFbx;

public class Node : FbxObject
{
    public Scene? Scene;

    public int Index = -1;

    //std local transform
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

    //geometric transform
    public Vector3 GeometricScaling;
    public Vector3 GeometricRotation;
    public Vector3 GeometricTranslation;

    public Euler.EOrder RotationOrder = Euler.EOrder.YXZ;

    public int DefaultAttributeIndex = -1;

    public NodeAttribute[] Attributes = [];

    public Node[] Children = [];

    public Node? Parent;

    public override string? ToString() => Name;

    public void DoScale(float factor)
    {
        LclTranslation *= factor;
        RotationOffset *= factor;
        RotationPivot *= factor;
        ScalingOffset *= factor;
        ScalingPivot *= factor;
        GeometricTranslation *= factor;
    }

    public bool HasTransform
        => !(LclTranslation == Vector3.Zero &&
        LclRotation == Vector3.Zero &&
        LclScaling == Vector3.One &&
        PreRotation == Vector3.Zero &&
        PostRotation == Vector3.Zero &&
        RotationOffset == Vector3.Zero &&
        RotationPivot == Vector3.Zero &&
        ScalingOffset == Vector3.Zero &&
        ScalingPivot == Vector3.Zero);

    public Node? FindByName(string name)
    {
        if (Name == name) return this;
        foreach(var child in Children)
        {
            var node = child.FindByName(name);
            if (node != null)
                return node;
        }
        return null;
    }

    public LocalTransform LocalTransform
        => new()
        {
            LclTranslation = LclTranslation,
            LclRotation = LclRotation,
            LclScaling = LclScaling,
            PreRotation = PreRotation,
            PostRotation = PostRotation,
            RotationOffset = RotationOffset,
            RotationPivot = RotationPivot,
            ScalingOffset = ScalingOffset,
            ScalingPivot = ScalingPivot
        };

    public TRSTransform GeometricTransform
        => new()
        {
            Scaling = GeometricScaling,
            Rotation = FbxMath.RotAngleToQuaternion(GeometricRotation),
            Translation = GeometricTranslation
        };

    public Matrix EvaluateGlobalTransform()
    {
        Matrix result = LocalTransform.ToMatrix();
        Node? node = Parent;
        while(node != null)
        {
            result *= node.LocalTransform.ToMatrix();
            node = node.Parent;
        }
        return result;
    }
}
