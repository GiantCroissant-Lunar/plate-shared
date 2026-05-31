# Source Code Generators

This directory contains source code generators integrated from the eco-shared repository.

## DisposePattern Generator

The DisposePattern source generator automatically implements the `IDisposable` pattern for classes, eliminating boilerplate code and ensuring correct disposal implementations.

### Installation

The generator is already installed in the following projects:

- `PigeonPea.Console`
- `PigeonPea.Windows`
- `PigeonPea.Shared`

Quick add via MSBuild props (recommended):

- Use in-repo analyzer (preferred during development):

  ```xml
  <!-- Adjust relative path as needed from your project folder -->
  <Import Project="..\source-generators\DisposePattern.Local.props" />
  ```

- Or consume from local NuGet packages:

  ```xml
  <!-- Adjust the relative path from your project folder to dotnet/source-generators/DisposePattern.props -->
  <Import Project="..\source-generators\DisposePattern.props" />
  ```

- Or add direct references explicitly:

  ```xml
  <ItemGroup>
    <!-- In-repo analyzer -->
    <ProjectReference Include="..\source-generators\General.DisposePattern.Set\General.DisposePattern\General.DisposePattern.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <!-- Attributes package -->
    <PackageReference Include="PlateShared.SCG.Shared.Attributes" Version="0.1.0" />
  </ItemGroup>
  ```

### Usage

1. **Mark your class with the `[DisposePattern]` attribute**:

   ```csharp
   using PlateShared.SCG.General.DisposePattern.Attributes;

   [DisposePattern]
   public partial class MyClass
   {
       // Your code here
   }
   ```

2. **Mark fields that need disposal with `[ToBeDisposed]`**:

   ```csharp
   [DisposePattern]
   public partial class MyClass
   {
       [ToBeDisposed]
       private Timer? _timer;

       [ToBeDisposed]
       private SKBitmap? _bitmap;

       // This field will NOT be disposed (no attribute)
       private int _someValue;
   }
   ```

3. **The generator creates**:
   - Public `Dispose()` method implementing `IDisposable`
   - Protected virtual `Dispose(bool disposing)` for inheritance support
   - Automatic null-checking before disposal
   - Automatic nulling of non-readonly reference fields after disposal
   - Four partial method hooks for custom cleanup logic:
     - `BeforeDisposeManagedResources()`
     - `DisposeManagedResources()`
     - `BeforeDisposeUnmanagedResources()`
     - `DisposeUnmanagedResources()`

### Example

**Before (manual disposal)**:

```csharp
public class GameCanvas : Image
{
    private SKBitmap? _bitmap;

    public void Cleanup()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
```

**After (with DisposePattern generator)**:

```csharp
[DisposePattern]
public partial class GameCanvas : Image
{
    [ToBeDisposed]
    private SKBitmap? _bitmap;

    // Dispose() method is auto-generated!
    // No manual cleanup code needed
}
```

### Benefits

- ✅ **Correctness**: Implements the full dispose pattern correctly
- ✅ **Consistency**: All disposal follows the same pattern
- ✅ **Less Boilerplate**: No need to write repetitive disposal code
- ✅ **Inheritance Support**: Generated code supports class hierarchies
- ✅ **Null Safety**: Automatic null-checking
- ✅ **Extensibility**: Partial methods for custom cleanup logic

### Policy and Adoption (RFC-044)

For guidance on when and where to use the DisposePattern generator across the codebase, see:

- [RFC-044: Adopt DisposePattern Source Code Generator](../../docs/rfcs/044-adopt-dispose-pattern-generator.md)

### Package Source

The DisposePattern generator packages are sourced from the [eco-shared repository](https://github.com/GiantCroissant-Lunar/eco-shared) and stored locally in `../.local-packages/`:

- `PlateShared.SCG.General.DisposePattern.0.1.0.nupkg` - The source generator
- `PlateShared.SCG.Shared.Attributes.0.1.0.nupkg` - Required attributes
- `PlateShared.SCG.Shared.Abstractions.0.1.0.nupkg` - Transitive dependency
- `Plate.Shared.Abstractions.0.1.0.nupkg` - Transitive dependency

**Note**: Only the first two packages are directly referenced in project files. The remaining packages are transitive dependencies required by the generator but don't need explicit project references.

### NuGet Configuration

The local package source is configured in `dotnet/NuGet.Config`:

```xml
<packageSources>
  <add key="local-eco-shared" value="../.local-packages" />
</packageSources>
```

### Current Usage in Codebase

The DisposePattern is currently applied to:

- **GameCanvas** (`PigeonPea.Windows`): Disposes `SKBitmap` and `WriteableBitmap` graphics resources properly

**Important**: Dispose must be called explicitly when the control is no longer needed (e.g., when the parent window closes). Do not call Dispose() in `OnDetachedFromVisualTree` as Avalonia controls can be detached and reattached to the visual tree.

### Future Considerations

- Check if `Arch.Core.World` (used in `GameWorld`) implements `IDisposable`
- Apply pattern to other classes with disposable resources as they're identified
- Consider applying to renderer classes if they manage unmanaged resources

## Adding More Generators

To add additional source generators from eco-shared:

1. Download the `.nupkg` file from eco-shared's `build/packages/` directory
2. Copy it to `.local-packages/`
3. Add a `PackageReference` to your `.csproj`:
   ```xml
   <PackageReference Include="Package.Name" Version="0.1.0"
                     PrivateAssets="all"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
   ```

## AutoProperties Generator

The AutoProperties source generator automatically creates public properties from private fields based on attributes.

### Files (in-repo Set)

- Generator project
  `dotnet/source-generators/General.AutoProperties.Set/General.AutoProperties/General.AutoProperties.csproj`
- Generator implementation
  `dotnet/source-generators/General.AutoProperties.Set/General.AutoProperties/SourceGenerator.cs`
- Local props for consumers
  `dotnet/source-generators/AutoProperties.Local.props`

```xml
<Project>
  <ItemGroup>
    <ProjectReference Include="General.AutoProperties.Set\General.AutoProperties\General.AutoProperties.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <PackageReference Include="PlateShared.General.AutoProperties.Attributes" Version="0.1.0" />
  </ItemGroup>
</Project>
```

### Using AutoProperties in a project

1. Import the local props from your `.csproj` (adjust the path as needed):

   ```xml
   <Import Project="..\..\..\..\source-generators\AutoProperties.Local.props" />
   ```

2. Add the attributes namespace and mark a partial type:

   ```csharp
   using PlateShared.General.AutoProperties.Attributes;

   [AutoProperty]
   public partial class Example
   {
       [GenerateProperty]
       private int _value;
   }
   ```

3. Optionally configure class-level behavior:

   ```csharp
   [AutoProperty(GenerateForAllFields = true, FieldPrefix = "_")]
   public partial class Example
   {
       private string _name;

       [SkipProperty]
       private string _internalId;
   }
   ```

The generator emits `*.AutoProperties.g.cs` files under `obj/` – do not edit them directly.

## NamespaceUsingScope Analyzer (Prototype)

`PlateShared.SCG.General.NamespaceUsingScope` enforces where `using` directives should live relative to namespaces, based on a simple JSON config. It is currently consumed directly via `ProjectReference` from consumer repos.

### Configuration

Each consumer project that opts in supplies a JSON config file (added as an `AdditionalFile`):

```json
{
  "insideNamespacePrefixes": [
    "MungBean."
  ]
}
```

- Any `using` whose target namespace starts with one of the prefixes is treated as "inside-namespace".
- Others are treated as file-scoped/top-level usings.

### Behavior

- **Block-scoped namespaces** (`namespace Foo.Bar { ... }`):
  - `NSUSG001`: `using` should move **inside** the namespace body (code fix provided).
  - `NSUSG002`: `using` should move to **file scope** (code fix provided).
  - `NSUSG003`: ordering/grouping preference of usings (code fix provided).
- **File-scoped namespaces** (`namespace Foo.Bar;`):
  - `NSUSG003`: enforces ordering/grouping of usings (code fix provided).
  - `NSUSG004`: advisory only – the config prefers inside-namespace placement but file-scoped syntax cannot express that. No code fix is offered.

Severity for each diagnostic is controlled via `.editorconfig` in consumer repos, for example:

```ini
dotnet_diagnostic.NSUSG001.severity = warning
dotnet_diagnostic.NSUSG002.severity = warning
dotnet_diagnostic.NSUSG003.severity = warning
dotnet_diagnostic.NSUSG004.severity = warning
```

### Workflow Notes

- **Intended use today**: run `dotnet build` (or the IDE) to surface `NSUSG00x` diagnostics, then apply fixes via IDE quick actions (per file or "Fix all" where supported).
- **CLI auto-fix**: `dotnet format analyzers` can see these diagnostics, but in practice does not reliably apply all NamespaceUsingScope fixes across projects. Treat CLI bulk fixing for this analyzer as experimental/use-at-own-risk for now; the IDE experience is the primary path.

## Agent Checklist: Adding a New Generator Set

When integrating another shared source generator into this repo:

1. **Create a Set folder** under `dotnet/source-generators`
   Example: `General.AutoProperties.Set`, `DI.ConstructorInjection.Set`.
2. **Add the generator project and source**:
   - `*.csproj` targeting `netstandard2.0`, marked as analyzer (`IsRoslynAnalyzer`, `IsRoslynComponent`).
   - `SourceGenerator.cs` that uses `PlateShared.SCG.Shared.Abstractions.Utility` helpers.
3. **Create a `*.Local.props` file** in `dotnet/source-generators` that:
   - Adds a `ProjectReference` to the in-repo generator project with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.
   - Adds any required **attributes packages** (e.g., `PlateShared.SCG.Shared.Attributes`, `PlateShared.General.AutoProperties.Attributes`).
4. **Import the local props** from each consuming project via `<Import Project="..\..\..\..\source-generators\Xxx.Local.props" />`, adjusting the relative path.
5. **Use the documented attributes** in code (e.g., `[DisposePattern]`, `[AutoProperty]`, `[GenerateProperty]`).
6. **Build and verify** that the analyzer DLL is referenced and the expected `*.g.cs` files are generated without errors.
