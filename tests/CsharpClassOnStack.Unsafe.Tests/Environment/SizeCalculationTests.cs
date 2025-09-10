using System;
using System.Collections.Generic;
using CsharpClassOnStack.Unsafe.Override;
using FluentAssertions;
using Xunit;

namespace CsharpClassOnStack.Unsafe.Tests.Environment;

public class SizeCalculationTests
{
    [Theory]
    [MemberData(nameof(SizeOf_ShouldReturnCorrectSize_SampleData))]
    public void SizeOf_ShouldReturnCorrectSize(Type type, int expectedSize)
    {
        // Arrange
        var makeGenericType = typeof(Unsafe<>).MakeGenericType(type);

        // Act
        var size = (int?)makeGenericType.GetMethod("SizeOf")?.Invoke(null, []);

        // Assert
        size.Should().NotBeNull().And.BeGreaterOrEqualTo(expectedSize);
    }

    public static IEnumerable<object[]> SizeOf_ShouldReturnCorrectSize_SampleData()
    {
        var headerSize = IntPtr.Size * 2;

        yield return [typeof(OnlyOneInt), headerSize + sizeof(int)];
        yield return [typeof(TwoInt), headerSize + sizeof(int) * 2];
        yield return [typeof(TwoDouble), headerSize + sizeof(double) * 2];
        yield return [typeof(OneString), headerSize + IntPtr.Size];
        yield return [typeof(Complex), headerSize + IntPtr.Size + sizeof(double) + sizeof(int)];
    }

    private sealed class OnlyOneInt
    {
        public int Field1 { get; set; }
    }

    private sealed class TwoInt
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
    }

    private sealed class TwoDouble
    {
        public double Field1 { get; set; }
        public double Field2 { get; set; }
    }

    private sealed class OneString
    {
        public string Field1 { get; set; }
    }

    private sealed class Complex
    {
        public string Field1 { get; set; }
        public int Field2 { get; set; }
        public double Field3 { get; set; }
    }
}
