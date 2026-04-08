namespace Avachat.Tests.API.Support;

[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }

    public TestPriorityAttribute(int priority)
    {
        Priority = priority;
    }
}
