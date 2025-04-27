using System;
using AutoFixture.Xunit2;
using CsharpClassOnStack.Safe.Tests.Sandbox;
using FluentAssertions;
using Xunit;

namespace CsharpClassOnStack.Safe.Tests.Safe;

public class StackTests
{
    [Theory, AutoData]
    public void Unsafe_ShouldAllocate_WhenItIsSimpleClass(ExampleClass expected)
    {
        // Arrange

        // Act
        var obj = Stack<ExampleClass>.Safe(stackalloc byte[Stack<ExampleClass>.Size]);

        obj.X = expected.X;
        obj.Y = expected.Y;

        // Assert
        obj.Should().BeEquivalentTo(expected);
        obj.ToString().Should().Be(expected.ToString());
    }

    [Theory, AutoData]
    public void Unsafe_ShouldAllocate_WhenItIsArray(ExampleClass[] expected)
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

    [Theory, AutoData]
    public void Unsafe_ShouldAllocate_WhenItIsDerivedClass(DerivedClass expected)
    {
        // Arrange

        // Act
        var obj = Stack<DerivedClass>.Safe(stackalloc byte[Stack<DerivedClass>.Size]);

        obj.X = expected.X;
        obj.Y = expected.Y;

        // Assert
        BaseClass based = obj;

        based.Should().BeEquivalentTo(expected);
        based.ToString().Should().Be(expected.ToString());
    }

    [Theory, AutoData]
    public void Unsafe_ShouldAllocateAndChange_WhenItIsDerivedClass(DerivedClass expected)
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
}
