using static System.MathF;
using Stride.Core.Mathematics;

namespace SlimFbx;

public static class FbxMath
{
    public static Quaternion RotAngleToQuaternion(Vector3 rotation)
    {
        // Convert Euler angles to quaternion (simplified - assumes XYZ order)
        float rx = rotation[0] * PI / 180.0f; // Convert to radians
        float ry = rotation[1] * PI / 180.0f;
        float rz = rotation[2] * PI / 180.0f;

        // Simple quaternion from Euler angles (XYZ order)
        float cx = Cos(rx * 0.5f), sx = Sin(rx * 0.5f);
        float cy = Cos(ry * 0.5f), sy = Sin(ry * 0.5f);
        float cz = Cos(rz * 0.5f), sz = Sin(rz * 0.5f);

        return new Quaternion(
            sx * cy * cz - cx * sy * sz, // x
            cx * sy * cz + sx * cy * sz, // y
            cx * cy * sz - sx * sy * cz, // z
            cx * cy * cz + sx * sy * sz  // w
        );
    }
}
