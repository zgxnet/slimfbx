using System.Numerics;
using System.Text.Json.Nodes;

namespace SlimFbx;

public class Scene : FbxObject
{
    public SystemUnit SystemUnit;
    public FbxTime.EMode TimeMode = FbxTime.EMode.DefaultMode;

    public NodeAttribute[] NodeAttributes = [];
    public Node[] Nodes = [];

    public required Node RootNode;

    public AnimStack[] AnimStacks = [];

    public readonly Dictionary<string, Node> Name2NodeMap = [];

    public IEnumerable<Mesh> GetMeshes()
        => NodeAttributes.Where(_ => _ is Mesh).Select(_ => (Mesh)_);

    public IEnumerable<AnimLayer> GetAnimLayers()
        => AnimStacks.SelectMany(stack => stack.Layers);

    public void BuildName2NodeMap()
    {
        Name2NodeMap.Clear();
        foreach (var node in Nodes)
        {
            if (!string.IsNullOrEmpty(node.Name) && !Name2NodeMap.ContainsKey(node.Name))
            {
                Name2NodeMap[node.Name] = node;
            }
        }
    }

    public void DoScale(float factor)
    {
        SystemUnit = new SystemUnit(SystemUnit.ScaleFactor * factor, SystemUnit.Multiplier);

        foreach(var node in Nodes)
            node.DoScale(factor);

        foreach (var attr in NodeAttributes)
            attr.DoScale(factor);

        foreach (var animStack in AnimStacks)
            animStack.DoScale(factor);
    }

    public Node? FindNodeByName(string name)
        => RootNode.FindByName(name);

    public JsonObject ToJson()
    {
        var json = new JsonObject();
        
        // Add scene name
        if (!string.IsNullOrEmpty(Name))
        {
            json["name"] = Name;
        }
        
        // Add system unit
        json["systemUnit"] = new JsonObject
        {
            ["scaleFactor"] = SystemUnit.ScaleFactor,
            ["multiplier"] = SystemUnit.Multiplier
        };

        json["timeMode"] = TimeMode.ToString();

        // Add root node and hierarchy
        json["rootNode"] = NodeToJson(RootNode);
        
        return json;
    }
    
    private JsonObject NodeToJson(Node node)
    {
        var nodeJson = new JsonObject
        {
            ["name"] = node.Name
        };

        // Add transform data
        void SetVector3(string name, Vector3 v, Vector3 def)
        {
            if (v != def)
                nodeJson[name] = new JsonArray(v.X, v.Y, v.Z);
        }
        SetVector3("translation", node.LclTranslation, Vector3.Zero);
        SetVector3("rotation", node.LclRotation, Vector3.Zero);
        SetVector3("scaling", node.LclScaling, Vector3.One);

        SetVector3("preRotation", node.PreRotation, Vector3.Zero);
        SetVector3("postRotation", node.PostRotation, Vector3.Zero);
        SetVector3("rotationOffset", node.RotationOffset, Vector3.Zero);
        SetVector3("rotationPivot", node.RotationPivot, Vector3.Zero);
        SetVector3("scalingOffset", node.ScalingOffset, Vector3.Zero);
        SetVector3("scalingPivot", node.ScalingPivot, Vector3.Zero);

        SetVector3("geometricScaling", node.GeometricScaling, Vector3.One);
        SetVector3("geometricRotation", node.GeometricRotation, Vector3.Zero);
        SetVector3("geometricTranslation", node.GeometricTranslation, Vector3.Zero);

        // Add attributes if any
        if (node.Attributes.Length > 0)
        {
            var attributesArray = new JsonArray();
            foreach (var attr in node.Attributes)
            {
                attributesArray.Add(new JsonObject
                {
                    ["type"] = attr.GetType().Name,
                    ["name"] = attr.Name
                });
            }
            nodeJson["attributes"] = attributesArray;
        }
        
        // Add children recursively
        if (node.Children.Length > 0)
        {
            var childrenArray = new JsonArray();
            foreach (var child in node.Children)
            {
                childrenArray.Add(NodeToJson(child));
            }
            nodeJson["children"] = childrenArray;
        }
        
        return nodeJson;
    }

    public void PrintNodeHierarchy()
    {
        Console.WriteLine("Node Hierarchy:");
        PrintNodeRecursive(RootNode, 0);
    }

    private void PrintNodeRecursive(Node node, int depth)
    {
        // Create indentation based on depth
        string indent = new string(' ', depth * 2);
        
        // Print the node name with indentation
        Console.WriteLine($"{indent}- {node.Name ?? "[Unnamed Node]"}");
        
        // Print node attributes if any
        if (node.Attributes.Length > 0)
        {
            foreach (var attr in node.Attributes)
            {
                Console.WriteLine($"{indent}  * {attr.GetType().Name}: {attr.Name ?? "[Unnamed]"}");
            }
        }
        
        // Recursively print children
        foreach (var child in node.Children)
        {
            PrintNodeRecursive(child, depth + 1);
        }
    }
}
