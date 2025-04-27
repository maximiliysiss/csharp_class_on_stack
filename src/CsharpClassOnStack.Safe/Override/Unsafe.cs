using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace CsharpClassOnStack.Safe.Override;

internal static class Unsafe
{
    private delegate ref IntPtr UnsafeRefCastDelegate(ref Span<IntPtr> from);

    private static readonly UnsafeRefCastDelegate _delegate;

    static Unsafe()
    {
        var tFrom = typeof(Span<IntPtr>);
        var tTo = typeof(IntPtr);

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
    public static ref IntPtr As(ref Span<IntPtr> source) => ref _delegate(ref source);
}
