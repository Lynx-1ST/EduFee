# 03-api-compatibility-fixes: Resolve API incompatibilities and breaking changes

Fix all API errors and warnings introduced by .NET 8 targeting. Common areas:
- Removed or obsolete types and methods
- Changed signatures or behavior
- Updated namespaces or package locations
- Deprecated patterns replaced with modern equivalents

Research in task.md will identify specific incompatibilities from assessment data and prioritize high-impact areas.

**Done when**:
- Solution builds without errors
- Solution builds without warnings (or warnings are understood and deliberately suppressed per user preference)
- Core functionality tests pass
- Verify with: `dotnet build` and `dotnet test` succeed
