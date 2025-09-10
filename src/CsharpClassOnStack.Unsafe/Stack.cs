using System;
using System.Runtime.CompilerServices;
using CsharpClassOnStack.Unsafe.Override;

namespace CsharpClassOnStack.Unsafe;

public static class Stack<T> where T : class
{
    public static readonly int Size = Unsafe<T>.SizeOf();

    private static readonly IntPtr _typeHandle = typeof(T).TypeHandle.Value;
    private static readonly IntPtr _arrayTypeHandle = typeof(T[]).TypeHandle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Unsafe(byte* stackBuffer)
    {
        var pointer = (IntPtr*)stackBuffer;

        var mtPtr = pointer + 1;
        *mtPtr = _typeHandle;

        var ptr = (IntPtr)mtPtr;

        return System.Runtime.CompilerServices.Unsafe.As<IntPtr, T>(ref ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T[] Unsafe(byte* stackBuffer, int length)
    {
        var pointer = (IntPtr*)stackBuffer;

        var mtPtr = pointer + 1;
        *mtPtr = _arrayTypeHandle;
        *(pointer + 2) = length;

        var ptr = (IntPtr)mtPtr;

        return System.Runtime.CompilerServices.Unsafe.As<IntPtr, T[]>(ref ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T Unsafe(Span<byte> buffer) => Unsafe(*(byte**)&buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T[] Unsafe(Span<byte> buffer, int length) => Unsafe(*(byte**)&buffer, length);
}
