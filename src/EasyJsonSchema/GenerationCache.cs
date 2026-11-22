using System.Collections.Concurrent;

namespace EasyJsonSchema;

internal static class GenerationCache
{
    internal static bool TryGet(Type type, out EasyJsonSchema? schema, out string error)
    {
        var entry = Cache.GetOrAdd(type, static key => Create(key));
        schema = entry.Schema;
        error = entry.Error;
        return entry.Success;
    }

    #region private

    private static readonly ConcurrentDictionary<Type, Entry> Cache = new();

    private static Entry Create(Type type)
    {
        if (!SchemaGenerator.TryCreate(type, out var schema, out var error) || schema is null)
        {
            return new Entry(false, null, error);
        }

        return new Entry(true, schema, string.Empty);
    }

    private sealed record Entry(bool Success, EasyJsonSchema? Schema, string Error);

    #endregion
}
