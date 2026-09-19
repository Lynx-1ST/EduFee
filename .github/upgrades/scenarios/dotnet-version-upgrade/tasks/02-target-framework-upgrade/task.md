# 02-target-framework-upgrade: Update target framework to .NET 8

Change the project's target framework from `net48` to `net8.0` in the SDK-style `.csproj` file. This activates the .NET 8 runtime and enables API version checks.

Assessment indicates:
- Binary incompatibilities with .NET 8 (certain classes/members removed or changed)
- Source-level API changes requiring code adjustments
- Package updates needed for .NET 8 compatibility

**Done when**:
- `.csproj` specifies `<TargetFramework>net8.0</TargetFramework>`
- Project loads in Visual Studio
- Verify with: `dotnet build` succeeds (may have warnings to fix in next task)
