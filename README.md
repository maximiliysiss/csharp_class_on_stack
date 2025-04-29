# C# allocating class object on stack

## Description

This repository explores several unconventional techniques for creating **class instances on the stack** in C#,
bypassing the default behavior of heap allocation.

The goal is to push the boundaries of what's possible in modern .NET, demonstrating how developers can gain more control
over memory layout and allocation — either for experimentation, performance tuning, or educational purposes.

Two distinct approaches are presented:

1. **Unsafe Approach** – leveraging `unsafe` blocks, pointer arithmetic, and low-level memory manipulation
2. **Safe Approach** – using only standard C# language features, without leaving the bounds of memory safety

> Tested on .NET 7, .NET 8, and .NET 9

> Stack allocation is performed using `stackalloc`

## Project structure

1. **`benchmark/`** — contains benchmarks that compare different approaches in the `SingleAllocate` and `ManyAllocate`
   scenarios
2. **`src/`** — includes the source code for the library implementing both safe and unsafe allocation strategies
3. **`tests/`** — provides unit tests that validate the core functionality and ensure the main scenarios work as
   expected

## Theory

Before diving into the demonstration, it’s helpful to refresh how class objects are represented in memory.

The key components are:

1. **Sync Block Index** — 8 bytes used internally by the runtime for features like locking and thread synchronization
2. **Method Table Pointer** — 8 bytes pointing to metadata about the object, including information about the type and
   the layout of its methods. This is how C# knows which methods to call and where they are located
3. **Field Data** — the remaining bytes are used to store the actual values of the class's fields

If, for example, we have a class:

```csharp
class MyClass
{
    int    a;    // 4 bytes
    byte   b;    // 1 byte
    bool   c;    // 1 byte
}
```

then its display will be akin to

| Offset | Size    | Description                         |
|--------|---------|-------------------------------------|
| 0x00   | 8 bytes | Sync block (reserved by CLR)        |
| 0x08   | 8 bytes | MethodTable pointer (type metadata) |
| 0x10   | 4 bytes | `int a = 42`                        |
| 0x14   | 1 byte  | `byte b = 0xAB`                     |
| 0x15   | 1 byte  | `bool c = true` (0x01)              |
| 0x16   | 2 bytes | Padding for alignment (optional)    |

> The structure described above corresponds to a 64-bit operating system. On a 32-bit system, pointer sizes are
> different: instead of 8 bytes, pointers occupy 4 bytes.

It’s important to note that `struct` types do not include the initial 16 bytes used in class objects.
The field data starts right at offset `0x00`, which will be an important detail later on.

## [Unsafe](./src/CsharpClassOnStack.Unsafe/)

This is probably the simplest way to understand how an object can be manually constructed (allocated).
If we break it down step by step, the process looks like this:

1. **Allocate memory** for the future object — this can be done using `stackalloc byte[size]`
2. **Initialize the first 8 bytes to zero**, which typically represent the sync block index
3. **Set the next 8 bytes** to a pointer referencing the MethodTable — metadata that describes the object's type
4. **Convert the MethodTable pointer** into a managed reference to a C# class instance. Up to this point, everything was
   treated as raw pointers, similar to how you would work in C++

An important step in this process is calculating the total size of the object to allocate. This can be expressed as
`IntPtr.Size * 2 + Unsafe.SizeOf<T>()`, which accounts for the size of the object's actual data plus two pointer-sized
fields: one for the sync block and one for the MethodTable reference.

Another critical detail is how to obtain a pointer to the MethodTable. This can be done using the following construct:
`typeof(T).TypeHandle.Value`, which gives you a raw pointer to the runtime type information associated with the target
class.

Putting all the pieces together, the final code looks like this:

> Of course, most of these operations will require you to work within an `unsafe` context

```csharp
byte* buffer = stackalloc byte[IntPtr.Size * 2 + Unsafe.SizeOf<T>()]; // Buffer for storage on the stack

var pointer = (IntPtr*)stackBuffer; // Transform it for ease of use

*(pointer + 0) = IntPtr.Zero; // Optional
*(pointer + 1) = typeof(T).TypeHandle.Value; // Set MethodTable pointer

var ptr = (IntPtr)(pointer + 1); // Get a pointer to MethodTable and cast it into a managed IntPtr

var obj = Unsafe.As<IntPtr, T>(ref ptr); // Convert IntPtr to T as if we had done *(T*)ptr
```

As a result, we obtain an `obj` that behaves like a fully functional object. However, it’s crucial to keep in mind the
following important points:

1. The object exists **outside the visibility of the Garbage Collector (GC)** — it is not tracked or managed by the GC
2. When the method where `stackalloc` was called exits, the **stack frame is torn down**, and any attempt to use or
   return the object afterward can lead to **undefined behavior**

### Links

1. [Link to example](./src/CsharpClassOnStack.Unsafe/)

## [Safe](./src/CsharpClassOnStack.Safe/)

Now let’s move on to something even more interesting — constructing an object using pure C#, without resorting to raw
pointers.

The sequence of steps remains essentially the same as in the unsafe approach. We’ll still rely on the same techniques to
calculate the required size and to retrieve the pointer to the `MethodTable`.

In the end, the resulting code looks like this:

```csharp
Span<byte> buffer = stackalloc byte[IntPtr.Size * 2 + Unsafe.SizeOf<T>()]; // Buffer for storage on the stack

var buffer = MemoryMarshal.Cast<byte, IntPtr>(stackBuffer); // Transform it for ease of use

buffer[0] = IntPtr.Zero; // Optional
buffer[1] = typeof(T).TypeHandle.Value; // Set MethodTable pointer

var objBuffer = buffer[1..]; // Get a Span<IntPtr> that starts with a MethodTable pointer

var obj = Unsafe.As<Span<IntPtr>, T>(ref objBuffer); // Convert Span<IntPtr> to T bytes to bytes
```

Both approaches are generally quite similar, but there’s an interesting distinction — the line
`Unsafe.As<Span<IntPtr>, T>(ref objBuffer)`, where we convert a `Span<IntPtr>` into `T`.

To understand what’s happening, let’s recall how `struct` types are laid out in memory. It doesn’t have an object
header — its field data starts right at the beginning of the memory block.

So, what does the `Span<>` structure actually look like under the hood?

```csharp
public readonly ref struct Span<T>
{
    internal readonly ref T _reference; // Reference to array of elements
    private readonly int _length; // Element array length

    /* Another code */
}
```

In other words, the first 8 bytes of the `Span<>` structure represent a pointer — and in our case, that memory contains
the `MethodTable`. So when we cast a `Span<T>` into `T`, we’re effectively interpreting its first field as a reference
to our object. C# then treats that pointer as a managed reference to a class instance — which is precisely what we need
in this scenario.

It’s also important to mention some limitations. This approach only works on **.NET 9.0 and later**, because
`Unsafe.As<,>` started supporting `allows ref struct` as arguments only from that version onward. In earlier versions,
such usage is not allowed, which means you can’t simply pass a `Span<>` this way.

To work around this restriction, I wrote a custom implementation using IL emit. [You can find an example
here](src/CsharpClassOnStack.Safe/Override/Unsafe.cs).

### Links

1. [Link to example](./src/CsharpClassOnStack.Safe/)

## A few more fun tricks.

### Inheritance

One of the reasons we love classes is for inheritance and all the powerful features it brings.
And nothing stops us from leveraging that power — even on the stack.

As a result, we can write a test that looks something like this:

```csharp
[Theory, AutoData]
public void Safe_ShouldAllocateAndChange_WhenItIsDerivedClass(DerivedClass expected)
{
    // Arrange

    // Act
    var obj = Stack<DerivedClass>.Safe(stackalloc byte[Stack<DerivedClass>.Size]);

    obj.X = expected.X - 1;
    obj.Y = expected.Y;

    BaseClass based = obj;

    based.X = expected.X;

    // Assert

    based.Should().BeEquivalentTo(expected);
    based.ToString().Should().Be(expected.ToString());
}
```

### The entire array is on the stack

How about placing an array on the stack, with all of its elements stored nearby — also on the stack?
Turns out, it’s surprisingly straightforward.

There are a couple of important points to keep in mind:

1. The total size is calculated as `Unsafe.SizeOf<T>() * length + 4`, which includes space for the array elements plus 4
   bytes reserved for the array’s length field
2. Initializing the array is slightly more involved (just one extra line compared to creating a single object), and
   essentially boils down to the following:

```csharp
buffer[0] = IntPtr.Zero; // Optional
buffer[1] = typeof(T[]).TypeHandle.Value; // MethodTable pointer
buffer[2] = length; // Array length
```

```csharp
[Theory, AutoData]
public void Safe_ShouldAllocate_WhenItIsArray(ExampleClass[] expected)
{
    // Arrange

    // Act
    var obj = Stack<ExampleClass>.Safe(stackalloc byte[Stack<ExampleClass>.Size * expected.Length + 4], expected.Length);

    for (var i = 0; i < obj.Length; i++)
    {
        var element = Stack<ExampleClass>.Safe(stackalloc byte[Stack<ExampleClass>.Size]);

        element.X = expected[i].X;
        element.Y = expected[i].Y;

        obj[i] = element;
    }

    // Assert
    obj.Should().BeEquivalentTo(expected);
}
```

## Testing

### [Unit](./tests/)

The project also includes several tests that cover a range of scenarios, such as:

1. Creating a simple class instance
2. Allocating an array of class instances directly on the stack
3. Creating an instance of a derived class
4. Creating a derived object and modifying it through a base class reference
5. In the unsafe variant, additional cases are covered using `Span<byte>`, which provides a safer way to interact with
   otherwise unsafe memory operations.

### [Benchmark](./benchmark/)

In addition, a set of benchmarks has been implemented to showcase the full power and performance potential of
stack-based allocation.
These benchmarks help illustrate just how fast and efficient working with the stack can be compared to traditional heap
allocation.

#### Safe

| Method              | Job      | Runtime  |          Mean |      Error |     StdDev |        Median |   Gen0 | Allocated |
|---------------------|----------|----------|--------------:|-----------:|-----------:|--------------:|-------:|----------:|
| SingleAllocateHeap  | .NET 7.0 | .NET 7.0 |     1.9396 ns |  0.0553 ns |  0.0517 ns |     1.9181 ns | 0.0029 |      24 B |
| SingleAllocateStack | .NET 7.0 | .NET 7.0 |     8.6831 ns |  0.1932 ns |  0.3677 ns |     8.7082 ns |      - |         - |
| ManyAllocateHeap    | .NET 7.0 | .NET 7.0 | 2,452.9738 ns | 22.3611 ns | 20.9166 ns | 2,454.1597 ns | 2.8687 |   24000 B |
| ManyAllocateStack   | .NET 7.0 | .NET 7.0 | 2,723.5996 ns |  0.8523 ns |  0.6654 ns | 2,723.6259 ns |      - |         - |
| SingleAllocateHeap  | .NET 8.0 | .NET 8.0 |     1.7152 ns |  0.0029 ns |  0.0025 ns |     1.7155 ns | 0.0029 |      24 B |
| SingleAllocateStack | .NET 8.0 | .NET 8.0 |     8.7965 ns |  0.1961 ns |  0.3382 ns |     8.6271 ns |      - |         - |
| ManyAllocateHeap    | .NET 8.0 | .NET 8.0 | 2,478.6933 ns | 24.0770 ns | 22.5216 ns | 2,480.5584 ns | 2.8687 |   24000 B |
| ManyAllocateStack   | .NET 8.0 | .NET 8.0 | 2,684.2186 ns |  1.9118 ns |  1.4926 ns | 2,684.7157 ns |      - |         - |
| SingleAllocateHeap  | .NET 9.0 | .NET 9.0 |     2.2176 ns |  0.0048 ns |  0.0045 ns |     2.2161 ns | 0.0029 |      24 B |
| SingleAllocateStack | .NET 9.0 | .NET 9.0 |     0.7736 ns |  0.0076 ns |  0.0064 ns |     0.7749 ns |      - |         - |
| ManyAllocateHeap    | .NET 9.0 | .NET 9.0 | 2,705.4946 ns | 11.6700 ns | 10.9161 ns | 2,705.4497 ns | 2.8687 |   24000 B |
| ManyAllocateStack   | .NET 9.0 | .NET 9.0 | 1,273.6940 ns |  0.3620 ns |  0.3023 ns | 1,273.7439 ns |      - |         - |

The results clearly show that stack-based allocation outperforms heap allocation — not only in terms of memory usage (
which is expected), but also in raw execution speed when running on .NET 9.0.
However, for .NET 8.0 and 7.0, there’s a noticeable slowdown compared to heap allocation.
This is primarily due to the overhead of generating `Unsafe.As<,>` dynamically via IL emit at runtime.

#### Unsafe

| Method                    | Job      | Runtime  |          Mean |      Error |     StdDev |   Gen0 | Allocated |
|---------------------------|----------|----------|--------------:|-----------:|-----------:|-------:|----------:|
| SingleAllocateHeap        | .NET 7.0 | .NET 7.0 |     1.6922 ns |  0.0486 ns |  0.0455 ns | 0.0029 |      24 B |
| SingleAllocateStack       | .NET 7.0 | .NET 7.0 |     0.4787 ns |  0.0159 ns |  0.0149 ns |      - |         - |
| SingleAllocateStackOnSpan | .NET 7.0 | .NET 7.0 |     0.9192 ns |  0.0174 ns |  0.0162 ns |      - |         - |
| ManyAllocateHeap          | .NET 7.0 | .NET 7.0 | 2,560.6623 ns | 18.9084 ns | 17.6869 ns | 2.8687 |   24000 B |
| ManyAllocateStack         | .NET 7.0 | .NET 7.0 |   944.7791 ns |  5.6593 ns |  4.7257 ns |      - |         - |
| ManyAllocateStackOnSpan   | .NET 7.0 | .NET 7.0 | 1,296.3634 ns |  9.2856 ns |  8.6857 ns |      - |         - |
| SingleAllocateHeap        | .NET 8.0 | .NET 8.0 |     2.0119 ns |  0.0159 ns |  0.0133 ns | 0.0029 |      24 B |
| SingleAllocateStack       | .NET 8.0 | .NET 8.0 |     0.3722 ns |  0.0141 ns |  0.0118 ns |      - |         - |
| SingleAllocateStackOnSpan | .NET 8.0 | .NET 8.0 |     0.5997 ns |  0.0182 ns |  0.0170 ns |      - |         - |
| ManyAllocateHeap          | .NET 8.0 | .NET 8.0 | 2,515.6083 ns |  8.5975 ns |  7.6214 ns | 2.8687 |   24000 B |
| ManyAllocateStack         | .NET 8.0 | .NET 8.0 |   948.0459 ns |  7.3097 ns |  6.1039 ns |      - |         - |
| ManyAllocateStackOnSpan   | .NET 8.0 | .NET 8.0 | 1,132.9332 ns |  4.7512 ns |  4.4443 ns |      - |         - |
| SingleAllocateHeap        | .NET 9.0 | .NET 9.0 |     2.2379 ns |  0.0386 ns |  0.0361 ns | 0.0029 |      24 B |
| SingleAllocateStack       | .NET 9.0 | .NET 9.0 |     0.2845 ns |  0.0182 ns |  0.0161 ns |      - |         - |
| SingleAllocateStackOnSpan | .NET 9.0 | .NET 9.0 |     0.4485 ns |  0.0133 ns |  0.0118 ns |      - |         - |
| ManyAllocateHeap          | .NET 9.0 | .NET 9.0 | 2,786.9785 ns | 23.1452 ns | 19.3273 ns | 2.8687 |   24000 B |
| ManyAllocateStack         | .NET 9.0 | .NET 9.0 |   824.4629 ns |  1.3196 ns |  1.0303 ns |      - |         - |
| ManyAllocateStackOnSpan   | .NET 9.0 | .NET 9.0 |   962.2158 ns |  1.0145 ns |  0.8472 ns |      - |         - |

In some cases, the stack-based approach achieved up to a 10x performance boost, clearly demonstrating the raw power and
efficiency of using the stack over the heap — especially when compared to the fully safe alternative.

These results highlight how, with the right setup, stack allocation can significantly outperform more traditional
approaches.

## Conclusion

This exploration demonstrates that allocating class instances on the stack in C#—while unconventional—is not only
technically feasible, but can also yield substantial performance gains in certain scenarios.

We’ve shown that:

1. **Unsafe techniques** provide maximum control and efficiency, though they require deep knowledge of memory layout and
   careful handling to avoid undefined behavior
2. **Safe techniques**, made possible in .NET 9 (or emulated in previous versions), offer a surprising degree of
   flexibility without stepping outside the boundaries of memory safety

These methods aren’t meant to replace the heap in everyday development—but they open up exciting opportunities for
advanced use cases, such as high-performance systems, embedded scenarios, or low-level experimentation.

> In the end, it’s less about circumventing the runtime and more about understanding it deeply—so you know exactly when,
> why, and how to bend the rules
