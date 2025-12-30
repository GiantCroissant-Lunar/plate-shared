# General.AutoProperties

This project contains a source generator for creating C# properties from backing fields using attributes.

It is designed to work well with other generators in this repo, especially:

- `General.AutoToString` – for generating `ToString()` output
- `DI.ConstructorInjection` – for generating DI-friendly constructors

Below is a quick reference for the key usage patterns.

---

## Core attributes

### `[AutoProperty]` (type-level)

Apply to a partial class/struct to enable property generation.

```csharp
[AutoProperty]
public partial class Person
{
}
```

Configuration properties:

- `DefaultAccessibility` – default `PropertyAccessibility` for generated properties.
- `DefaultPropertyKind` – default `PropertyKind` for generated properties.
- `GenerateForAllFields` – if `true`, all private instance fields are considered unless explicitly skipped.
- `FieldPrefix` – prefix stripped from field names when creating property names (e.g. `_` or `m_`).

### `[GenerateProperty]` (field-level)

Apply to private fields to generate corresponding properties.

```csharp
[AutoProperty]
public partial class Person
{
    [GenerateProperty]
    private string _name;
}
```

Key options:

- `PropertyName` – override the generated property name.
- `Accessibility` – override the default accessibility.
- `PropertyKind` – shape of the property (see below).
- `IsRequired` – when `true`, emits the C# `required` modifier on the property type.
- `IncludeInToString` – when `true`, marks this field to be included in `ToString()` when used with `General.AutoToString`.
- `ResolveInConstructor` – when `true`, marks this field as a DI constructor parameter when used with `DI.ConstructorInjection`.
- `NotifyPropertyChanged` – reserved for future use.

### `PropertyKind`

```csharp
public enum PropertyKind
{
    GetterSetter,
    GetterOnly,
    GetterPrivateSetter,
    GetterProtectedSetter,
    GetterInternalSetter,
    GetterInitOnly
}
```

- `GetterSetter` – `get` and `set`.
- `GetterOnly` – `get` only.
- `GetterPrivateSetter` – `get` and `private set`.
- `GetterProtectedSetter` – `get` and `protected set`.
- `GetterInternalSetter` – `get` and `internal set`.
- `GetterInitOnly` – `get` and `init` (C# 9 init-only).

---

## Common patterns

### Mutable model

```csharp
[AutoProperty(DefaultPropertyKind = PropertyKind.GetterSetter)]
public partial class Person
{
    [GenerateProperty]
    private string _name;

    [GenerateProperty]
    private int _age;
}
```

Generates standard read/write properties for typical mutable models.

### Init-only and required

```csharp
[AutoProperty]
public partial class Person
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterInitOnly, IsRequired = true)]
    private string _name;
}
```

Generates a property like:

```csharp
public required string Name
{
    get => _name;
    init => _name = value;
}
```

This works well for immutable/value-like types that should be initialized via object initializers.

### Readonly fields

Readonly fields are always treated as **getter-only** properties, regardless of the requested `PropertyKind`. This avoids generating illegal setters or init accessors.

```csharp
[AutoProperty(DefaultPropertyKind = PropertyKind.GetterSetter)]
public partial class Person
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterPrivateSetter)]
    private readonly string _id;
}
```

Generates:

```csharp
public string Id
{
    get => _id;
}
```

---

## Integration with General.AutoToString

When combined with `General.AutoToString`:

```csharp
using Plate.SCG.General.AutoToString.Attributes;
using Plate.General.AutoProperties.Attributes;

[AutoProperty(FieldPrefix = "_")]
[AutoToString]
public partial class Person
{
    [GenerateProperty(IncludeInToString = true)]
    private string _name;
}
```

- `IncludeInToString = true` marks the backing field for inclusion in the generated `ToString()`.
- AutoToString uses the field name and `FieldPrefix` to produce a **property-style label**:
  - Label: `Name`
  - Value source: `_name`

The resulting `ToString()` is conceptually:

```csharp
Person Name = { _name }
```

---

## Integration with DI.ConstructorInjection

When combined with `DI.ConstructorInjection`:

```csharp
using Plate.SCG.DI.ConstructorInjection.Attributes;
using Plate.General.AutoProperties.Attributes;

[ConstructorInjection]
[AutoProperty]
public partial class SomeService
{
    [GenerateProperty(ResolveInConstructor = true)]
    private readonly IFoo _foo;

    [GenerateProperty(ResolveInConstructor = true)]
    private readonly IBar _bar;
}
```

- Fields with `[ResolveInConstructor]` **or** `[GenerateProperty(ResolveInConstructor = true)]` are treated as constructor parameters.
- The DI generator creates a constructor like:

```csharp
public SomeService(IFoo foo, IBar bar)
{
    ConstructorBegin();

    _foo = foo;
    _bar = bar;

    ConstructorEnd();
}
```

This lets you declare dependencies once on the backing fields and get both properties and DI wiring.
