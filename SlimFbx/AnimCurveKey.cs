using System.Runtime.CompilerServices;
namespace SlimFbx;
using static AnimCurve;

public class AnimCurveKey
{
    public FbxTime Time; // The time unit in FBX(FbxTime) is 1/46186158000 of one second.

    public float Value;

    public uint Flags;

    public Float6 FloatData;

    public EInterpolationType Interpolation
    {
        get { return (EInterpolationType)(Flags & Mask_InterpolationType); }
        set { Flags = (Flags & ~Mask_InterpolationType) | (uint)value; }
    }

    public ETangentMode TangentMode
    {
        get { return (ETangentMode)(Flags & Mask_TangentMode); }
        set { Flags = (Flags & ~Mask_TangentMode) | (uint)value; }
    }

    public EWeightedMode TagentWeightedMode
    {
        get { return (EWeightedMode)(Flags & Mask_WeightedMode); }
        set { Flags = (Flags & ~Mask_WeightedMode) | (uint)value; }
    }

    public EVelocityMode TangentVelocityMode
    {
        get { return (EVelocityMode)(Flags & Mask_VelocityMode); }
        set { Flags = (Flags & ~Mask_VelocityMode) | (uint)value; }
    }

    public EConstantMode ConstantMode
    {
        get { return (EConstantMode)(Flags & Mask_ConstantMode); }
        set { Flags = (Flags & ~Mask_ConstantMode) | (uint)value; }
    }

    public ETangentVisibility TangentVisibility
    {
        get { return (ETangentVisibility)(Flags & Mask_TangentVisibility); }
        set { Flags = (Flags & ~Mask_TangentVisibility) | (uint)value; }
    }

    public float GetDataFloat(EDataIndex pIndex) => FloatData[(int)pIndex];

    public void SetDataFloat(EDataIndex pIndex, float pValue) => FloatData[(int)pIndex] = pValue;

    public float FloatTime => Time.FloatTime;

    public AnimCurveKey(long time, float value, uint flags)
    {
        Time = time;
        Value = value;
        Flags = flags;
    }

    public AnimCurveKey(long time, float value, uint flags, float d0, float d1, float d2, float d3, float d4, float d5)
    {
        Time = time;
        Value = value;
        Flags = flags;
        FloatData[0] = d0;
        FloatData[1] = d1;
        FloatData[2] = d2;
        FloatData[3] = d3;
        FloatData[4] = d4;
        FloatData[5] = d5;
    }

    public override string ToString()
        => $"AnimCurveKey: Time={FloatTime}, Value={Value}, Interpolation={Interpolation}";

    //todo: support cubic scale
    public void DoScale(float factor)
    {
        Value *= factor;
    }
}
