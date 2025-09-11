using System;
using System.Runtime.InteropServices;
using AutoFixture.Xunit2;
using FluentAssertions;
using Xunit;

namespace CsharpClassOnStack.Unsafe.Tests.Unsafe;

public class StringTests
{
    [Theory, AutoData]
    public void Unsafe_ShouldAllocateStringOnStack(string expected)
    {
        // Arrange
        var header = IntPtr.Size * 2;
        var fullHeader = header + sizeof(int);

        var size = fullHeader + sizeof(char) + sizeof(char) * expected.Length;

        // Act
        Span<byte> memory = stackalloc byte[size];

        var ints = MemoryMarshal.Cast<byte, int>(memory[header..]);
        var chars = MemoryMarshal.Cast<byte, char>(memory[fullHeader..]);

        ints[0] = expected.Length;
        expected.CopyTo(chars);

        var str = Stack<string>.Unsafe(memory);

        // Assert
        str.Should().Be(expected);
    }
}
