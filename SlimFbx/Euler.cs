using Stride.Core.Mathematics;

namespace SlimFbx;

//see FbxEuler
public static class Euler
{
    public enum EAxis { AxisX = 0, AxisY = 1, AxisZ = 2 }

    public enum EOrder
    {
        XYZ,
        XZY,
        YZX,
        YXZ,
        ZXY,
        ZYX,
        SphericXYZ
    }

    static bool[] parityOdd = [false, true, false, true, false, true, false];

    public static bool IsParityOdd(EOrder pOrder) => parityOdd[(int)pOrder];

    public static bool IsRepeat(EOrder pOrder) => false;

    public static IReadOnlyList<Int3> AxisTable = [
        new(0, 1, 2),        
        new(0, 2, 1),
        new(1, 2, 0),
        new(1, 0, 2),
        new(2, 0, 1),
        new(2, 1, 0)
    ];

    public static float DegenerateThreshold
    {
        get => degenerateThreshold;
        set => degenerateThreshold = value;
    }

    static float degenerateThreshold = 16.0f * float.Epsilon;
}
