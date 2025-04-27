using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CsharpClassOnStack.Safe;

public static class Stack<T>
{
    public static readonly int Size = IntPtr.Size * 2 + Unsafe.SizeOf<T>();

    private static readonly IntPtr _typeHandle = typeof(T).TypeHandle.Value;
    private static readonly IntPtr _arrayTypeHandle = typeof(T[]).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Safe(Span<byte> stackBuffer)
    {
        var buffer = MemoryMarshal.Cast<byte, IntPtr>(stackBuffer);

        buffer[0] = IntPtr.Zero;
        buffer[1] = _typeHandle;

        var objBuffer = buffer[1..];

#if NET9_0_OR_GREATER
        var ptr = Unsafe.As<Span<IntPtr>, IntPtr>(ref objBuffer);
#else
        var ptr = Override.Unsafe.As(ref objBuffer);
#endif

        return Unsafe.As<IntPtr, T>(ref ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Safe(Span<byte> stackBuffer, int length)
    {
        var buffer = MemoryMarshal.Cast<byte, IntPtr>(stackBuffer);

        buffer[0] = IntPtr.Zero;
        buffer[1] = _arrayTypeHandle;
        buffer[2] = length;

        var objBuffer = buffer[1..];

#if NET9_0_OR_GREATER
        var ptr = Unsafe.As<Span<IntPtr>, IntPtr>(ref objBuffer);
#else
        var ptr = Override.Unsafe.As(ref objBuffer);
#endif

        return Unsafe.As<IntPtr, T[]>(ref ptr);
    }
}
