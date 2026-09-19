# 01-project-format-conversion: Convert to SDK-style project format

The project currently uses the legacy .NET Framework `.csproj` format with `ToolsVersion` and requires manual reference declarations. This must be converted to modern SDK-style format as a prerequisite for .NET 8 targeting.

SDK-style projects enable:
- Central Package Management (CPM) for simplified dependency management
- Direct `<ItemGroup>` package references instead of `packages.config`
- Implicit SDK package includes reducing boilerplate
- Modern tooling and MSBuild integration

**Done when**: 
- Project file uses modern SDK format (first line: `<Project Sdk="Microsoft.NET.Sdk"`)
- All package references converted to `<ItemGroup>` in `.csproj`
- `packages.config` removed (if present)
- Project loads without errors in Visual Studio
- Verify with: `dotnet build` succeeds with no warnings
