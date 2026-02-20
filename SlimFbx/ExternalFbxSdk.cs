using System.Runtime.InteropServices;
using System.Text;

namespace SlimFbx;

public static partial class ExternalFbxSdk
{
    [ThreadStatic]
    private static byte[]? slimFbxContent;

    static ExternalFbxSdk()
    {
        // Ensure the native library is loaded
        NativeLibrary.SetDllImportResolver(typeof(ExternalFbxSdk).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == "fbx2slim.dll")
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "lib/fbx2slim.dll");
                if (NativeLibrary.TryLoad(dllPath, out IntPtr handle))
                {
                    return handle;
                }
            }
            return IntPtr.Zero;
        });
    }

    public static byte[] LoadAndConvertToSlimFbx(string path)
    {
        try
        {
            slimFbxContent = null;
            bool ok = ConvertFbxSlim(path, WriteCallbackImpl);
            if (!ok)
            {
                string message = slimFbxContent != null ? Encoding.UTF8.GetString(slimFbxContent)
                    : "Unknown error during FBX to SlimFbx conversion.";
                throw new Exception(message);
            }
            return slimFbxContent ?? throw new IOException("Failed to convert FBX to SlimFbx: No data received.");
        }
        finally
        {
            slimFbxContent = null;
        }
    }

    // Delegate for the callback function that receives bytecode data
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WriteCallback(IntPtr data, int length);

    [LibraryImport("fbx2slim.dll", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)] // For a boolean return type
    private static partial bool ConvertFbxSlim(string sourcePath, WriteCallback func_write);

    // Callback implementation that receives bytecode chunks
    private unsafe static void WriteCallbackImpl(IntPtr data, int length)
    {
        if (data == IntPtr.Zero || length <= 0)
            return;

        slimFbxContent = new byte[length];

        // Copy the data from unmanaged memory to our buffer
        Span<byte> chunk = new((byte*)data, length);

        chunk.CopyTo(slimFbxContent);
    }
}
