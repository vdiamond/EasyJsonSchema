# EasyJsonSchema

EasyJsonSchema is a .NET class library that validates a JSON object against the EasyJsonSchema format. It uses `System.Text.Json` and the BCL only. Behavior follows EasyJsonSchema specification v23 (approved 2026-11-22). That specification document is not in this repository. The library project sets `IsPackable` to false. There is no application to run: `src/EasyJsonSchema/EasyJsonSchema.csproj` is a class library (`OutputType` is `Library`).

Development happens only through Victor and Cursor agents.

## Build, run, and test

`global.json` pins SDK `10.0.404` and rolls forward to the latest 10.0 feature band.

```
dotnet build EasyJsonSchema.slnx
dotnet test EasyJsonSchema.slnx
```

Both commands succeed. `dotnet test` runs the xUnit project `tests/EasyJsonSchema.Tests` (129 tests). There is no runnable project, so there is no `dotnet run` command.

## Layout

- `src/EasyJsonSchema/` — class library, `net10.0`, assembly and root namespace `EasyJsonSchema`. Public types: `IEasyJsonSchema`, `EasyJsonSchemaService`, `EasyJsonSchema`, `PropertyDef`. Other types in this project are `internal`.
- `tests/EasyJsonSchema.Tests/` — xUnit tests. `Traceability.md` maps specification T-IDs to test methods. `Models/` holds sample types for schema generation.
- `EasyJsonSchema.slnx` — solution (`src` and `tests`).
- `Directory.Build.props` — `LangVersion` latest, nullable reference types, implicit usings, warnings as errors.
- `global.json` — SDK pin.
- `.editorconfig` — charset, line endings, indentation, and C# brace style.

## Dependencies on other vdiamond repos

This repository does not depend on any other vdiamond repository. Nothing here is a git submodule, an HTTP client of another vdiamond service, or a `ProjectReference` outside this tree. `src/EasyJsonSchema/EasyJsonSchema.csproj` has no package references. `tests/EasyJsonSchema.Tests/EasyJsonSchema.Tests.csproj` references the local library plus `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`. No sibling clone is required to build or test.

Known dependent: [AgentConnect](https://github.com/vdiamond/AgentConnect) consumes this repository as the git submodule `external/EasyJsonSchema`. A change to the public API of this library affects that submodule.

## Conventions

`.cursor/rules/coding-standards.mdc` is the governing standard. Existing code may not yet comply. Bring it into compliance when you touch it.

What the code does today:

- `net10.0`, latest C# language version, nullable reference types, implicit usings, and warnings as errors on every build (`Directory.Build.props`).
- File-scoped namespaces. Braces on every control-flow block (`.editorconfig` sets `csharp_prefer_braces`).
- Public classes are `sealed`. Public APIs have XML documentation (`GenerateDocumentationFile` is true).
- Private members are wrapped in `#region private` when a type has them. `EasyJsonSchema` and `PropertyDef` are property bags and have no private region.
- The library stays on the BCL and `System.Text.Json`. Validation and parse failures return `false` and an error string of the form `<path>: <problem>`.
- The class and the namespace share the name `EasyJsonSchema`. Tests alias them as `SchemaDocument` and `SchemaService` in `tests/EasyJsonSchema.Tests/GlobalUsings.cs`.
- Tests are xUnit `[Fact]` methods whose names start with `Tnn_`, matching `tests/EasyJsonSchema.Tests/Traceability.md`.
- `EasyJsonSchema` and `PropertyDef` are mutable types with public setters and `List<T>` properties so `System.Text.Json` can deserialize them.

## What not to touch

- Secrets. Do not put secrets in source or in committed configuration. This repository has none.
- Generated output: `bin/`, `obj/`, `TestResults/`, and the XML documentation file written next to the assembly.
- The public API (`IEasyJsonSchema`, `EasyJsonSchemaService`, `EasyJsonSchema`, `PropertyDef`) unless Victor asks. AgentConnect consumes this repository as a submodule.
- Specification behavior. The v23 document is not in this repository.
- Package references on the library. It is limited to the BCL and `System.Text.Json`.
