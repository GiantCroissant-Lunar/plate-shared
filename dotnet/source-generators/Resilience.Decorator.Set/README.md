# Resilience.Decorator (Source Generator)

This Set adds a Roslyn incremental source generator that produces **decorator classes** for interfaces, applying *resilience policies* around async method calls.

- **Generator**: `Plate.SCG.Resilience.Decorator` (`netstandard2.0` analyzer)
- **Attributes**: `Plate.SCG.Resilience.Decorator.Attributes` (`netstandard2.0` library)
- **Runtime abstractions**: `Plate.Resilience.Abstractions` (`netstandard2.1`)
- **Runtime implementation (Polly)**: `Plate.Resilience.Polly` (`netstandard2.1`, Polly `7.2.4`)
- **Tests**: `Plate.SCG.Resilience.Decorator.Tests` (`net9.0`)

All projects are included in `dotnet/source-generators/SourceGenerators.sln`.

## Status / Progress Summary

- **Implemented**:
  - `Plate.Resilience.Abstractions`
    - `IResilienceService` for executing async operations.
    - `IResiliencePolicyProvider` for *policy-by-name* lookup.
    - `ResiliencePolicyRegistry` (in-memory, thread-safe) implementation of `IResiliencePolicyProvider`.
    - `ResilienceServiceExtensions.ExecuteAsync(...)` overloads that resolve a policy by name and call the service.
    - `ResiliencePolicyOptions` for basic retry + circuit-breaker knobs.
  - `Plate.Resilience.Polly`
    - `PollyResilienceService` implementing `IResilienceService` using Polly (retry + circuit breaker).
  - `Plate.SCG.Resilience.Decorator`
    - Generates a `{InterfaceName}ResilienceDecorator` implementing the interface.
    - Wraps *async* methods (`Task` / `Task<T>`) using `Plate.Resilience.ResilienceServiceExtensions.ExecuteAsync(...)`.
    - Supports *policy-by-name* from attributes.
    - Forwards properties and events to the wrapped `_inner` instance.
    - Emits generated code with `GeneratedCode` + `DebuggerNonUserCode` attributes.
  - `Plate.SCG.Resilience.Decorator.Tests`
    - Verifies that generated code compiles and contains key expected output (decorator name + ExecuteAsync call + policy name).

- **Intentionally not implemented yet / limitations**:
  - No automatic DI container registration wiring (i.e., no `IServiceCollection` extensions generated).
  - No `ValueTask` / `ValueTask<T>` wrapping.
  - Methods with `ref/out/in` parameters are **not wrapped** (they are forwarded directly), because capturing `ref/out` in a lambda is unsafe.
  - `void` return methods are forwarded directly.
  - `CancellationToken` support is limited to detecting a parameter *typed as* `System.Threading.CancellationToken` and passing it through to the lambda token parameter.
    - If an interface method has no token parameter, the wrapper uses `default` for the token.

## How it works

### Attribute trigger

The generator runs on **interfaces** annotated with `[Resilient]` / `[ResilientAttribute]`.

Attribute definition:

- `Plate.SCG.Resilience.Decorator.Attributes.ResilientAttribute`
  - Allowed targets: `Interface` and `Method`
  - Optional constructor argument: `policyName`

### Policy name resolution

For a given interface method, the policy name is resolved as:

1. Method attribute: `[Resilient("some-policy")]` (if present)
2. Interface attribute: `[Resilient("some-policy")]` (if present)
3. Fallback: `iface.ToDisplayString()` (the interface display string)

### Generated decorator shape

For an interface `IFoo`, the generator emits:

- Class name: `IFooResilienceDecorator`
- Fields:
  - `_inner : IFoo`
  - `_resilience : Plate.Resilience.IResilienceService`
  - `_policyProvider : Plate.Resilience.IResiliencePolicyProvider`
- Constructor:
  - `IFooResilienceDecorator(IFoo inner, IResilienceService resilience, IResiliencePolicyProvider policyProvider)`

Method handling:

- **Task / Task<T>**: wrapped via
  - `Plate.Resilience.ResilienceServiceExtensions.ExecuteAsync(_resilience, _policyProvider, "policy", token => _inner.Method(...), ct)`
- **Sync return types**: forwarded directly.
- **void**: forwarded directly.
- **ref/out/in parameters**: forwarded directly.

## Consuming the generator (current pattern)

There is currently **no** `Resilience.Decorator.Local.props` convenience import like `AutoProperties.Local.props`.

To use it in a consuming `.csproj`, add references like:

- Analyzer project reference (in-repo development):

```xml
<ItemGroup>
  <ProjectReference Include="..\..\dotnet\source-generators\Resilience.Decorator.Set\Resilience.Decorator\Resilience.Decorator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />

  <ProjectReference Include="..\..\dotnet\source-generators\Resilience.Decorator.Set\Resilience.Decorator.Attributes\Resilience.Decorator.Attributes.csproj" />

  <ProjectReference Include="..\..\dotnet\src\Plate.Resilience.Abstractions\Plate.Resilience.Abstractions.csproj" />

  <!-- Optional runtime implementation -->
  <ProjectReference Include="..\..\dotnet\src\Plate.Resilience.Polly\Plate.Resilience.Polly.csproj" />
</ItemGroup>
```

Adjust paths as needed.

## Usage example

```csharp
using System.Threading;
using System.Threading.Tasks;
using Plate.SCG.Resilience.Decorator.Attributes;

namespace Demo;

[Resilient("http")]
public interface IFoo
{
    Task PingAsync(CancellationToken ct = default);

    [Resilient("http-fast")]
    Task<int> AddAsync(int a, int b, CancellationToken ct = default);

    int AddSync(int a, int b);
}
```

The generator emits `IFooResilienceDecorator` in the same namespace as `IFoo`.

## Runtime wiring example (manual)

The generated decorator requires:

- An `IFoo` implementation (`inner`)
- An `IResilienceService` implementation
- An `IResiliencePolicyProvider` implementation

Example (conceptual):

```csharp
var policies = new ResiliencePolicyRegistry();
policies.SetPolicy("http", new ResiliencePolicyOptions { RetryCount = 3 });
policies.SetPolicy("http-fast", new ResiliencePolicyOptions { RetryCount = 1 });

IResilienceService resilience = new Plate.Resilience.Polly.PollyResilienceService();

IFoo inner = new Foo();
IFoo foo = new IFooResilienceDecorator(inner, resilience, policies);
```

## Tests / verification

- The generator test `GeneratesDecoratorAndCompiles`:
  - Runs the generator against an interface containing `Task`, `Task<T>`, and sync methods.
  - Asserts the output compilation contains **no errors**.
  - Asserts generated code contains:
    - `ResilienceDecorator`
    - `ResilienceServiceExtensions.ExecuteAsync`
    - the policy name string literal

## Next steps (recommended)

- Add a `Resilience.Decorator.Local.props` file (matching the pattern used by other generator Sets) to simplify consumption.
- Decide whether to support:
  - `ValueTask` / `ValueTask<T>`
  - sync wrapping (non-async methods)
  - interface inheritance / explicit implementation edge cases
  - more robust `CancellationToken` handling (e.g., token in the middle, optional tokens, etc.)
- Add snapshot-style verification (like the other generator tests) to assert the full generated output, not just string contains.
- Add DI helper(s) (optional) if desired: e.g. user-authored `IServiceCollection` extensions that register decorator + inner.
