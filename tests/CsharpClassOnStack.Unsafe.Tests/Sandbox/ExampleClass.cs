namespace CsharpClassOnStack.Unsafe.Tests.Sandbox;

public sealed class ExampleClass
{
    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString() => $"X: {X}, Y: {Y}";
}
