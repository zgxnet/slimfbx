namespace SlimFbx;

public class AnimStack : FbxObject
{
    public List<AnimLayer> Layers = [];

    public FbxTimeSpan LocalTimeSpan;

    public Scene? Scene;

    public void DoScale(float factor)
    {
        foreach (var layer in Layers)
            layer.DoScale(factor);
    }
}
