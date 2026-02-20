using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SlimFbx;

internal class MemoryWriter
{
    byte[] buffer = [];
    int length = 0;

    void ReserveNew(int cap)
    {
        int cap1 = length + cap;
        if(buffer.Length < cap1)
        {
            var newBuffer = new byte[Math.Max(cap1, buffer.Length * 2)];
            if(length > 0)
                Array.Copy(buffer, newBuffer, length);
            buffer = newBuffer;
        }
    }

    public void Write<T>(T value) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        if (size == 0) return;
        ReserveNew(size);
        Unsafe.WriteUnaligned(ref buffer[length], value);
        length += size;
    }

    public void WriteSpan<T>(ReadOnlySpan<T> value) where T : unmanaged
    {
        if (value.Length == 0) return;
        int size = Unsafe.SizeOf<T>() * value.Length;
        ReserveNew(size);
        value.CopyTo(MemoryMarshal.Cast<byte, T>(buffer.AsSpan(length)));
        length += size;
    }

    public int Length => length;

    public ReadOnlySpan<byte> AsSpan() => buffer.AsSpan(0, length);
}
