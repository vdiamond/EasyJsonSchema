# EasyJsonSchema

EasyJsonSchema is a .NET class library that validates a JSON object against a small schema format. It is aimed at the request path that checks a body before storing the original bytes.

The behavior is defined by **EasyJsonSchema specification v23** (approved 2026-11-22). That document is not in this repository.

Agent guidance is in [AGENTS.md](AGENTS.md).

## Layout

- `src/EasyJsonSchema/` — class library
- `tests/EasyJsonSchema.Tests/` — xUnit tests (`Traceability.md` maps specification tests)
- `EasyJsonSchema.slnx` — solution

## Build and test

Uses the .NET 10 SDK named in `global.json`.

```
dotnet build EasyJsonSchema.slnx
dotnet test EasyJsonSchema.slnx
```

## Target

- `net10.0`
- `System.Text.Json` and the BCL only
- Assembly and root namespace: `EasyJsonSchema`

## Use

```csharp
var schemas = new EasyJsonSchema.EasyJsonSchemaService();

if (!schemas.ValidateEasyJsonSchema(schemaJson, out var schemaError))
{
    // schemaError is "<path>: <problem>"
    return;
}

if (!schemas.DeserializeEasyJsonSchema(schemaJson, out var schema, out schemaError) || schema is null)
{
    return;
}

if (!schemas.ValidateJsonObject(schema, utf8Body, out var dataError))
{
    // return dataError to the caller
    return;
}

// store the original utf8Body unchanged
```

The class and the namespace share the name `EasyJsonSchema`. Refer to the document type as `EasyJsonSchema.EasyJsonSchema`, or add a using alias.

`ValidateJsonObject` reads the JSON once and returns the first error. It does not throw for a schema that has already passed `ValidateEasyJsonSchema`. `Deserialize<T>` is for stored JSON and does not validate.

`GenerateEasyJsonSchema` writes compact JSON unless `writeIndented` is true. `Serialize<T>` writes a .NET object as JSON, with C# enums as names, and returns it only when that JSON passes the schema generated for the type.
