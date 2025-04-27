using System;
using AutoFixture.Xunit2;
using CsharpClassOnStack.Unsafe.Tests.Sandbox;
using FluentAssertions;
using Xunit;

namespace CsharpClassOnStack.Unsafe.Tests.Unsafe;

public class StackTests
{
    [Theory, AutoData]
    public unsafe void Unsafe_ShouldAllocate_WhenItIsSimpleClass(ExampleClass expected)
    {
        // Arrange

        // Act
        byte* buffer = stackalloc byte[Stack<ExampleClass>.Size];
        var obj = Stack<ExampleClass>.Unsafe(buffer);

        obj.X = expected.X;
        obj.Y = expected.Y;

        // Assert
        obj.Should().BeEquivalentTo(expected);
        obj.ToString().Should().Be(expected.ToString());
    }

    [Theory, AutoData]
    public unsafe void Unsafe_ShouldAllocate_WhenItIsArray(ExampleClass[] expected)
    {
        // Arrange

        // Act
        byte* buffer = stackalloc byte[Stack<ExampleClass>.Size * expected.Length + 4];
        var obj = Stack<ExampleClass>.Unsafe(buffer, expected.Length);

        for (var i = 0; i < obj.Length; i++)
        {
            byte* elementBuffer = stackalloc byte[Stack<ExampleClass>.Size];
            var element = Stack<ExampleClass>.Unsafe(elementBuffer);

            element.X = expected[i].X;
            element.Y = expected[i].Y;

            obj[i] = element;
        }

        // Assert
        obj.Should().BeEquivalentTo(expected);
    }

    [Theory, AutoData]
    public unsafe void Unsafe_ShouldAllocate_WhenItIsDerivedClass(DerivedClass expected)
    {
        // Arrange

        // Act
        byte* buffer = stackalloc byte[Stack<DerivedClass>.Size];
        var obj = Stack<DerivedClass>.Unsafe(buffer);

        obj.X = expected.X;
        obj.Y = expected.Y;

        // Assert
        BaseClass based = obj;

        based.Should().BeEquivalentTo(expected);
        based.ToString().Should().Be(expected.ToString());
    }

    [Theory, AutoData]
    public unsafe void Unsafe_ShouldAllocateAndChange_WhenItIsDerivedClass(DerivedClass expected)
    {
        // Arrange

        // Act
        byte* buffer = stackalloc byte[Stack<DerivedClass>.Size];
        var obj = Stack<DerivedClass>.Unsafe(buffer);

        obj.X = expected.X - 1;
        obj.Y = expected.Y;

        BaseClass based = obj;

        based.X = expected.X;

        // Assert

        based.Should().BeEquivalentTo(expected);
        based.ToString().Should().Be(expected.ToString());
    }
}
