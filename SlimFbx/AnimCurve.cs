using System.Numerics;
using System.Runtime.CompilerServices;

namespace SlimFbx;

public class AnimCurve : FbxObject //FbxAnimCurve
{
    //! Key tangent mode for cubic interpolation.
    public enum ETangentMode : uint
    {
        Auto = 0x00000100,                                                  //!< Auto key (spline cardinal).
        TCB = 0x00000200,                                                   //!< Spline TCB (Tension, Continuity, Bias)
        User = 0x00000400,                                                  //!< Next slope at the left equal to slope at the right.
        GenericBreak = 0x00000800,                                          //!< Independent left and right slopes.
        Break = GenericBreak | User,                            //!< Independent left and right slopes, with next slope at the left equal to slope at the right.
        AutoBreak = GenericBreak | Auto,                        //!< Independent left and right slopes, with auto key.
        GenericClamp = 0x00001000,                                          //!< Clamp: key should be flat if next or previous key has the same value (overrides tangent mode).
        GenericTimeIndependent = 0x00002000,                                //!< Time independent tangent (overrides tangent mode).
        GenericClampProgressive = 0x00004000 | GenericTimeIndependent   //!< Clamp progressive: key should be flat if tangent control point is outside [next-previous key] range (overrides tangent mode).
    }

    //! Key interpolation type.
    public enum EInterpolationType : uint
    {
        Constant = 0x00000002,  //!< Constant value until next key.
        Linear = 0x00000004,        //!< Linear progression to next key.
        Cubic = 0x00000008      //!< Cubic progression to next key.
    }

    //! Weighted mode.
    public enum EWeightedMode : uint
    {
        None = 0x00000000,                      //!< Tangent has default weights of 0.333; we define this state as not weighted.
        Right = 0x01000000,                     //!< Right tangent is weighted.
        NextLeft = 0x02000000,                  //!< Left tangent is weighted.
        All = Right | NextLeft                  //!< Both left and right tangents are weighted.
    }

    //! Key constant mode.
    public enum EConstantMode : uint
    {
        Standard = 0x00000000,  //!< Curve value is constant between this key and the next
        Next = 0x00000100       //!< Curve value is constant, with next key's value
    }

    //! Velocity mode. Velocity settings speed up or slow down animation on either side of a key without changing the trajectory of the animation. Unlike Auto and Weight settings, Velocity changes the animation in time, but not in space.
    public enum EVelocityMode : uint
    {
        None = 0x00000000,                      //!< No velocity (default).
        Right = 0x10000000,                     //!< Right tangent has velocity.
        NextLeft = 0x20000000,                  //!< Left tangent has velocity.
        All = Right | NextLeft                  //!< Both left and right tangents have velocity.
    }

    //! Tangent visibility.
    public enum ETangentVisibility : uint
    {
        None = 0x00000000,                          //!< No tangent is visible.
        Left = 0x00100000,                          //!< Left tangent is visible.
        Right = 0x00200000,                         //!< Right tangent is visible.
        Both = Left | Right //!< Both left and right tangents are visible.
    }

    //! FbxAnimCurveKey data indices for cubic interpolation tangent information.
    public enum EDataIndex : uint
    {
        RightSlope = 0,     //!< Index of the right derivative, User and Break tangent mode (data are float).
        NextLeftSlope = 1,      //!< Index of the left derivative for the next key, User and Break tangent mode.
        Weights = 2,            //!< Start index of weight values, User and Break tangent break mode (data are FbxInt16 tokens from weight and converted to float).
        RightWeight = 2,        //!< Index of weight on right tangent, User and Break tangent break mode.
        NextLeftWeight = 3, //!< Index of weight on next key's left tangent, User and Break tangent break mode.
        Velocity = 4,           //!< Start index of velocity values, Velocity mode
        RightVelocity = 4,      //!< Index of velocity on right tangent, Velocity mode
        NextLeftVelocity = 5,   //!< Index of velocity on next key's left tangent, Velocity mode
        TCBTension = 0,     //!< Index of Tension, TCB tangent mode (data are floats).
        TCBContinuity = 1,      //!< Index of Continuity, TCB tangent mode.
        TCBBias = 2         //!< Index of Bias, TCB tangent mode.
    }

    public const uint Mask_TangentMode = 0x0000FF00;

    public const uint Mask_InterpolationType = 0x0000000F;

    public const uint Mask_WeightedMode = 0x0F000000;

    public const uint Mask_VelocityMode = 0xF0000000;

    public const uint Mask_ConstantMode = 0x00000100;

    public const uint Mask_TangentVisibility = 0x00F00000;

    public const float DEFAULT_WEIGHT = 1.0f / 3;

    public const float MIN_WEIGHT = 1e-4f;

    public const float MAX_WEIGHT = 0.99f;

    public const float DEFAULT_VELOCITY = 0;

    [InlineArray(6)]
    public struct Float6
    {
        public float Data0;

        public override readonly string ToString()
            => $"[{this[0]}, {this[1]}, {this[2]}, {this[3]}, {this[4]}, {this[5]}]";
    }

    public List<AnimCurveKey> Keyframes = [];

    public float EvalLinear(float t) => new CurveEvalContext(this, t).EvalLinear();

    public float EvalLinear(long t) => new CurveEvalContext(this, t).EvalLinear();

    public void DoScale(float factor)
    {
        foreach (var key in Keyframes)
            key.DoScale(factor);
    }
}

public class AnimCurve3
{
    public required AnimCurve X;
    public required AnimCurve Y;
    public required AnimCurve Z;

    public Vector3 EvaluateLinear(float t)
        => new(X.EvalLinear(t), Y.EvalLinear(t), Z.EvalLinear(t));

    public Vector3 EvaluateLinear(long t)
        => new(X.EvalLinear(t), Y.EvalLinear(t), Z.EvalLinear(t));

    public void DoScale(float factor)
    {
        X.DoScale(factor);
        Y.DoScale(factor);
        Z.DoScale(factor);
    }
}
