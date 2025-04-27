namespace CsharpClassOnStack.Safe.Tests.Sandbox;

public abstract class BaseClass
{
    public int X { get; set; }

    public override string ToString() => $"X: {X}";
}
