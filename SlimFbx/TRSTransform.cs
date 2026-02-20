using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimFbx;

public struct TRSTransform
{
    public Vector3 Translation = Vector3.Zero;

    public Quaternion Rotation = Quaternion.Identity;

    public Vector3 Scaling = new(1, 1, 1);

    public TRSTransform()
    { }

    public readonly bool IsIdentity
        => Translation == Vector3.Zero
        && Rotation == Quaternion.Identity
        && Scaling == new Vector3(1, 1, 1);

    public readonly Matrix ToMatrix()
    {
        Matrix.RotationQuaternion(in Rotation, out var m);
        Matrix.Scaling(in Scaling, out var sm);
        m = sm * m;
        m.TranslationVector = Translation;
        return m;
    }

    public readonly Vector3 Multiply(Vector3 v)
    {
        // v' = T * R * S * v
        Vector3 scaled = v * Scaling;
        Vector3 rotated = Vector3.Transform(scaled, Rotation);
        Vector3 translated = rotated + Translation;
        return translated;
    }

    //https://claude.ai/chat/77fec753-8d7b-4de3-ba5e-8842f83d92db, for non-uniform case
    public readonly Vector3 MultiplyNormal(Vector3 v)
    {
        Vector3 scaled;
        if (Scaling == Vector3.One)
            scaled = v;
        else
        {
            // v' = R * M(S) * v
            Vector3 MS = new(Scaling.Y * Scaling.Z, Scaling.X * Scaling.Z, Scaling.X * Scaling.Y);
            scaled = Vector3.Normalize(MS * Scaling);
        }
        Vector3 rotated = Rotation == Quaternion.Identity ? scaled : Vector3.Transform(scaled, Rotation);
        return rotated;
    }
}
