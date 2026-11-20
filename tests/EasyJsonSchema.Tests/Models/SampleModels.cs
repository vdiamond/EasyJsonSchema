using System.Text.Json.Serialization;

namespace EasyJsonSchema.Tests.Models;

internal sealed class T10User
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

internal sealed class WithCreated
{
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }
}

internal sealed class NullableAgeUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }
}

internal sealed class IntNameHolder
{
    public int Name { get; set; }
}

internal sealed class StreamHolder
{
    public Stream? Data { get; set; }
}

internal enum T27Color
{
    Red,
    Blue,
}

internal sealed class T27Address
{
    public string City { get; set; } = "";
}

internal sealed class T27Sample
{
    public T27Color Color { get; set; }

    public List<string> Tags { get; set; } = new();

    public T27Address Address { get; set; } = new();

    public int? Age { get; set; }
}

internal sealed class FirstNameHolder
{
    public string FirstName { get; set; } = "";
}

internal sealed class JsonNameIdHolder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

internal sealed class NullableOnStringHolder
{
    public string? Name { get; set; }
}

internal sealed class EmptyClass
{
}

internal sealed class EmptyNestedHolder
{
    public EmptyClass Address { get; set; } = new();
}

internal enum EmptyEnum
{
}

internal sealed class EmptyEnumHolder
{
    public EmptyEnum Status { get; set; }
}

internal sealed class DuplicateNameHolder
{
    public string Id { get; set; } = "";

    [JsonPropertyName("Id")]
    public string Other { get; set; } = "";
}

internal sealed class StreamMemberHolder
{
    public Stream Data { get; set; } = Stream.Null;
}

internal sealed class PointType
{
    public int X { get; set; }
}

internal sealed class GeoType
{
    public PointType Point { get; set; } = new();
}

internal sealed class AddressType
{
    public GeoType Geo { get; set; } = new();
}

internal sealed class AddressRoot
{
    public AddressType Address { get; set; } = new();
}

internal sealed class Node
{
    public Node Next { get; set; } = null!;
}

internal enum MyEnum
{
    A,
    B,
}

internal sealed class ListEnumHolder
{
    public List<MyEnum> Items { get; set; } = new();
}

internal sealed class NullableListHolder
{
    public List<int?> Numbers { get; set; } = new();

    public List<MyEnum?> Enums { get; set; } = new();
}

internal enum ProgressNamed
{
    Todo,

    [JsonStringEnumMemberName("in-progress")]
    InProgress,

    Done,
}

internal sealed class ProgressHolder
{
    public ProgressNamed Progress { get; set; }
}

internal sealed class ProgressListHolder
{
    public List<ProgressNamed> History { get; set; } = new();
}

internal enum DuplicateProgress
{
    Done,

    [JsonStringEnumMemberName("Done")]
    Finished,
}

internal sealed class DuplicateProgressHolder
{
    public DuplicateProgress Progress { get; set; }
}

internal sealed class DuplicateProgressListHolder
{
    public List<DuplicateProgress> History { get; set; } = new();
}

internal sealed class InvalidNameHolder
{
    [JsonPropertyName("a.b")]
    public string X { get; set; } = "";
}

internal sealed class ScalarBag
{
    public bool Flag { get; set; }

    public byte Small { get; set; }

    public short ShortValue { get; set; }

    public int Count { get; set; }

    public long Big { get; set; }

    public float SingleValue { get; set; }

    public double DoubleValue { get; set; }

    public decimal Money { get; set; }

    public DateTime Created { get; set; }

    public string Name { get; set; } = "";

    public List<int> Tags { get; set; } = new();

    public bool[] Flags { get; set; } = [];
}
