using PeterO.Cbor;
using Stride.Engine;
using System.Text;

namespace SlimFbx;

public static class FileOps
{
    public static CBORObject LoadCbor(string fname)
    {
        string ext = Path.GetExtension(fname).ToLower();
        byte[] content = ext switch
        {
            ".cbor" or ".slimfbx" => File.ReadAllBytes(fname),
            ".fbx" => ExternalFbxSdk.LoadAndConvertToSlimFbx(fname),
            _ => throw new NotSupportedException($"Unsupported file extension: {ext}"),
        };
        return CBORObject.DecodeFromBytes(content);
    }

    public static Scene LoadScene(CBORObject cborScene)
    {
        var scene = CborLoader.LoadScene(cborScene);
        if (scene.SystemUnit == SystemUnit.CM) //todo: support more units
        {
            scene.SystemUnit = SystemUnit.M;
            scene.DoScale(0.01f); // convert cm to m
        }
        else if (scene.SystemUnit != SystemUnit.M)
        {
            throw new NotSupportedException($"Unsupported system unit: {scene.SystemUnit}");
        }
        return scene;
    }

    public static Scene LoadScene(string fname)
        => LoadScene(LoadCbor(fname));

    public static void MergeAnimationFiles(string fmergedScene, IEnumerable<string> fscenes)
    {
        if (!fscenes.Any()) return;
        var mainScene = LoadCbor(fscenes.First());
        CborUtil.AppendAnimations(mainScene, fscenes.Skip(1).Select(LoadCbor));
        SaveAuto(fmergedScene, mainScene);
    }

    static void SaveAuto(string fname, CBORObject obj)
    {
        string ext = Path.GetExtension(fname).ToLower();
        switch (ext)
        {
            case ".glb":
            case ".gltf":
                SaveGl(fname, obj);
                break;
            case ".slimfbx":
            case ".cbor":
                SaveSlimFbx(fname, obj);
                break;
            default:
                throw new NotSupportedException($"Unsupported file extension: {ext}");
        }
    }

    static void SaveGl(string fname, CBORObject obj)
    {
        var scene = LoadScene(obj);
        GlExporter exporter = new (scene);
        //var mesh = scene.GetMeshes().MaxBy(m => m.VertexCount); //todo: currently we just add the biggest mesh
        //if(mesh != null)
        //    exporter.AddMeshNode(mesh);
        exporter.AddAllMeshes();
        foreach (var stack in scene.AnimStacks)
            exporter.AddAnimStack(stack);
        exporter.SaveAuto(fname);
    }

    static void SaveSlimFbx(string fname, CBORObject obj)
        => File.WriteAllBytes(fname, obj.EncodeToBytes());

    public static void PrintCompareFileNodes(string fcbor1, string fcbor2)
    {
        var node1 = LoadCbor(fcbor1);
        var node2 = LoadCbor(fcbor2);
        var sb = new StringBuilder();
        if (!CborUtil.CompareNodeStructures(node1["nodes"], node2["nodes"], sb))
        {
            Console.WriteLine(sb.ToString());
        }
        else
        {
            Console.WriteLine("Nodes are identical.");
        }
    }
}
