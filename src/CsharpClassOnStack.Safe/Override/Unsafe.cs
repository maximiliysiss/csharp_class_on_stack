using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CsharpClassOnStack.Safe.Override;

internal static class Unsafe<T> where T : class
{
    private delegate ref T UnsafeRefCastDelegate(ref Span<IntPtr> from);

    private static readonly UnsafeRefCastDelegate _delegate;
    private static readonly int _size;

    static Unsafe()
    {
        var tFrom = typeof(Span<IntPtr>);
        var tTo = typeof(T);

        _size = Marshal.ReadInt32(ptr: tTo.TypeHandle.Value, ofs: 4);

        var method = new DynamicMethod(
            name: "UnsafeRefAs",
            returnType: tTo.MakeByRefType(),
            parameterTypes: [tFrom.MakeByRefType()],
            m: typeof(Unsafe).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);

        _delegate = (UnsafeRefCastDelegate)method.CreateDelegate(typeof(UnsafeRefCastDelegate));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T As(ref Span<IntPtr> source) => ref _delegate(ref source);

    public static int SizeOf() => _size;
}
