namespace KeeperData.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StepOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}