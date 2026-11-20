namespace EasyJsonSchema.Tests.Models.Fr19;

internal enum Status
{
    Active,
    Pending,
    Disabled,
}

internal sealed class User
{
    public string Email { get; set; } = "";

    public Status Status { get; set; }

    public Status? PreviousStatus { get; set; }

    public List<Status> History { get; set; } = new();
}
