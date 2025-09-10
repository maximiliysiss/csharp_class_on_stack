using System.Runtime.InteropServices;

namespace CsharpClassOnStack.Unsafe.Override;

internal static class Unsafe<T> where T : class
{
    private static readonly int _size = Marshal.ReadInt32(ptr: typeof(T).TypeHandle.Value, ofs: 4);
    public static int SizeOf() => _size;
}
