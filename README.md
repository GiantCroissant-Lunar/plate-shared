# Plate Shared

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com)

A collection of C# source generators and shared libraries for the Plate project ecosystem.

## Features

### Source Generators

- **DI.ConstructorInjection** - Automatic constructor-based dependency injection code generation
- **General.AutoProperties** - Generate properties from fields automatically
- **General.AutoToString** - Automatic `ToString()` method generation
- **General.DisposePattern** - Implement IDisposable pattern correctly
- **General.NamespaceUsingScope** - Namespace and using directive management
- **Resilience.Decorator** - Generate resilience decorator patterns

### Libraries

- **Plate.Resilience.Abstractions** - Core abstractions for resilience patterns
- **Plate.Resilience.Polly** - Integration with Polly for resilience strategies

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or JetBrains Rider

### Building the Project

```bash
# Using NUKE build system
./build/nuke/build.ps1

# Or using dotnet CLI
dotnet build dotnet/source-generators/SourceGenerators.sln
```

### Running Tests

```bash
dotnet test dotnet/source-generators/SourceGenerators.sln
```

## Project Structure

```
plate-shared/
├── dotnet/
│   ├── source-generators/    # C# source generators
│   └── src/                   # Shared libraries
├── build/                     # NUKE build configuration
└── .editorconfig             # Code style rules
```

## Source Generator Usage

Each source generator provides attributes you can use in your code:

```csharp
// Constructor Injection
[ConstructorInjection]
public partial class MyService
{
    [ResolveInConstructor] private readonly ILogger _logger;
}

// Auto Properties
[AutoProperty]
public partial class MyClass
{
    [GenerateProperty] private string _name;
}

// Auto ToString
[AutoToString]
public partial class Person
{
    [AddToString] private string _firstName;
    [AddToString] private string _lastName;
}

// Dispose Pattern
[DisposePattern]
public partial class ResourceManager
{
    [ToBeDisposed] private readonly Stream _stream;
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with:
- [Roslyn](https://github.com/dotnet/roslyn) - .NET Compiler Platform
- [Polly](https://github.com/App-vNext/Polly) - Resilience and transient-fault-handling library
- [NUKE](https://nuke.build/) - Cross-platform build automation system
