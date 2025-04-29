using System;
using System.Runtime.CompilerServices;

namespace CsharpClassOnStack.Unsafe;

public static class Stack<T>
{
    public static readonly int Size = IntPtr.Size * 2 + System.Runtime.CompilerServices.Unsafe.SizeOf<T>();

    private static readonly IntPtr _typeHandle = typeof(T).TypeHandle.Value;
    private static readonly IntPtr _arrayTypeHandle = typeof(T[]).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Unsafe(byte* stackBuffer)
    {
        var pointer = (IntPtr*)stackBuffer;

        *(pointer + 0) = IntPtr.Zero;
        *(pointer + 1) = _typeHandle;

        var ptr = (IntPtr)(pointer + 1);

        return System.Runtime.CompilerServices.Unsafe.As<IntPtr, T>(ref ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T[] Unsafe(byte* stackBuffer, int length)
    {
        var pointer = (IntPtr*)stackBuffer;

        *(pointer + 0) = IntPtr.Zero;
        *(pointer + 1) = _arrayTypeHandle;
        *(pointer + 2) = length;

        var ptr = (IntPtr)(pointer + 1);

        return System.Runtime.CompilerServices.Unsafe.As<IntPtr, T[]>(ref ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Unsafe(Span<byte> buffer) => Unsafe(*(byte**)&buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T[] Unsafe(Span<byte> buffer, int length) => Unsafe(*(byte**)&buffer, length);
}
