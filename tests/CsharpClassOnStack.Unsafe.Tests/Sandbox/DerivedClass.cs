namespace CsharpClassOnStack.Unsafe.Tests.Sandbox;

public sealed class DerivedClass : BaseClass
{
    public int Y { get; set; }

    public override string ToString() => $"{base.ToString()}, Y: {Y}";
}
