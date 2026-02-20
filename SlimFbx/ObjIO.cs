using Stride.Core.Mathematics;

public static class ObjIO
{
    public static void SaveObj(string filePath, Vector3[] vertices, int[] indices)
    {
        using (var writer = new StreamWriter(filePath))
        {
            // Write vertices
            foreach (var vertex in vertices)
            {
                writer.WriteLine($"v {vertex.X} {vertex.Y} {vertex.Z}");
            }

            // Write faces (assuming triangles)
            for (int i = 0; i < indices.Length; i += 3)
            {
                // OBJ format uses 1-based indexing
                writer.WriteLine($"f {indices[i] + 1} {indices[i + 1] + 1} {indices[i + 2] + 1}");
            }
        }
    }
}
