namespace SlimFbx;
using static FbxTime;

public struct CurveEvalContext
{
    public List<AnimCurveKey> keyframes;
    public long time;
    public int index0;

    public CurveEvalContext(AnimCurve curve, long time)
    {
        keyframes = curve.Keyframes;
        this.time = time;
        if (this.time <= keyframes[0].Time)
            index0 = -1;
        else if (this.time >= keyframes[keyframes.Count - 1].Time)
            index0 = keyframes.Count;
        else
        {
            index0 = -1;
            // locate
            int left = 0;
            int right = keyframes.Count - 1;
            while (left < right - 1)
            {
                int mid = (left + right) / 2;
                if (keyframes[mid].Time == this.time)
                {
                    index0 = mid;
                    break;
                }
                else if (keyframes[mid].Time < this.time)
                {
                    left = mid;
                }
                else
                {
                    right = mid;
                }
            }
            if (index0 == -1)
                index0 = right;
        }
    }

    public CurveEvalContext(AnimCurve curve, float time)
        : this(curve, (long)Math.Round((double)time * TC_SECOND))
    {}

    public float EvalLinear()
    {
        if (index0 < 0)
            return keyframes[0].Value;
        else if (index0 >= keyframes.Count - 1)
            return keyframes[keyframes.Count - 1].Value;
        else
        {
            var k0 = keyframes[index0];
            var k1 = keyframes[index0 + 1];
            double t = (time - k0.Time) / (double)(k1.Time - k0.Time);
            return (float)(k0.Value * (1 - t) + k1.Value * t);
        }
    }

    //public static float LocateTime(IList<AnimCurveKey> keyframes, float time)
    //{

    //}
}
