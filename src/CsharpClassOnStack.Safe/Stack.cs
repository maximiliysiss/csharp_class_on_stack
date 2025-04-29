using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CsharpClassOnStack.Safe;

public static class Stack<T> where T : class
{
    public static readonly int Size = IntPtr.Size * 2 + Unsafe.SizeOf<T>();

    private static readonly IntPtr _typeHandle = typeof(T).TypeHandle.Value;
    private static readonly IntPtr _arrayTypeHandle = typeof(T[]).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Safe(Span<byte> stackBuffer)
    {
        var buffer = MemoryMarshal.Cast<byte, IntPtr>(stackBuffer);

        buffer[1] = _typeHandle;

        var objBuffer = buffer[1..];

#if NET9_0_OR_GREATER
        return Unsafe.As<Span<IntPtr>, T>(ref objBuffer);
#else
        return Override.Unsafe<T>.As(ref objBuffer);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Safe(Span<byte> stackBuffer, int length)
    {
        var buffer = MemoryMarshal.Cast<byte, IntPtr>(stackBuffer);

        buffer[1] = _arrayTypeHandle;
        buffer[2] = length;

        var objBuffer = buffer[1..];

#if NET9_0_OR_GREATER
        return Unsafe.As<Span<IntPtr>, T[]>(ref objBuffer);
#else
        return Override.Unsafe<T[]>.As(ref objBuffer);
#endif
    }
}
