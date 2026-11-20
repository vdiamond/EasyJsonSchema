namespace EasyJsonSchema.Tests.Models.Enums;

internal enum Status
{
    Active,
    Pending,
    Disabled,
}

internal sealed class User
{
    public Status Status { get; set; }

    public List<Status> History { get; set; } = new();
}
