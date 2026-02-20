using PeterO.Cbor;
using System.Text;

namespace SlimFbx;

partial class CborUtil
{
    public static bool CompareNodeStructures(CBORObject nodes1, CBORObject nodes2, StringBuilder sb)
    {
        if(nodes1.Count != nodes2.Count)
        {
            sb.AppendLine($"Node count differ: {nodes1.Count} != {nodes2.Count}");
            return false;
        }
        int count = nodes1.Count;
        for(int i = 0; i < count; i++)
        {
            var n1 = nodes1[i];
            var n2 = nodes2[i];
            string[] fields = ["name", "children", "attributes",
                "preRotation", "postRotation", "rotationOffset", "rotationPivot",
                "scalingOffset", "scalingPivot",
                "geometricScaling", "geometricRotation", "geometricTranslation"];
            foreach(var f in fields)
            {
                if (!Equals(n1[f], n2[f]))
                {
                    sb.AppendLine($"Node {i} differ: {f}");
                    return false;
                }
            }
        }
        return true;
    }

    public static void AppendAnimations(CBORObject mainScene, IEnumerable<CBORObject> scenes)
    {
        foreach (var scene in scenes)
        {
            var animStacks = scene["animStacks"];
            if (animStacks != null)
            {
                var mainStacks = mainScene["animStacks"];
                if (mainStacks == null)
                    scene["animStacks"] = mainStacks = CBORObject.NewArray();
                for (int i = 0; i < animStacks.Count; i++)
                {
                    mainStacks.Add(animStacks[i]); //todo: deep clone
                }
            }
        }
    }
}
