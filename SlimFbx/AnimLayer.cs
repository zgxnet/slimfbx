namespace SlimFbx;

public class AnimLayer : FbxObject
{
    public List<AnimNode> NodeAnimations = [];

    public AnimStack? AnimStack;

    public void DoScale(float factor)
    {
        foreach (var animNode in NodeAnimations)
        {
            animNode.DoScale(factor);
        }
    }
}
