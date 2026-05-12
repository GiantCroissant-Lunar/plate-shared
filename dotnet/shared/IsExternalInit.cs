// Polyfill for init-only setters and record types under .NET Standard 2.0 / 2.1.
// .NET 5+ provides this marker type in the BCL; the guard prevents a duplicate
// definition when a csproj multi-targets a net5+ TFM.
//
// Linked into every plate-projects csproj via each repo's Directory.Build.props:
//
//   <ItemGroup>
//     <Compile Include="$(PlateSharedRoot)dotnet\shared\IsExternalInit.cs"
//              Link="Polyfills\IsExternalInit.cs" />
//   </ItemGroup>
//
// Replace `PlateSharedRoot` with the per-repo property that points at the
// plate-shared checkout (see existing Directory.Build.props for the pattern).

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
