using System.Linq;
using System.Threading.Tasks;
using PlateShared.SCG.General.NamespaceUsingScope;
using Xunit;

namespace PlateShared.General.NamespaceUsingScope.Tests;

public class UnitTest
{
    [Fact]
    public async Task FileScopedNamespace_MisorderedUsings_ProducesShouldBeInsideDiagnostic()
    {
        // MungBean.Input is at file scope but should be inside the file-scoped namespace
        var source = """
using MungBean.Input;
using System;

namespace TestNamespace;

public class TestClass {}
""";

        var config = """{ "insideNamespacePrefixes": [ "MungBean." ] }""";

        var diagnostics = await TestHelper.GetDiagnosticsAsync(source, config);

        // Expect NSUSG001 because MungBean.* usings should be inside namespace
        Assert.Contains(diagnostics, d => d.Id == NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId);
    }

    [Fact]
    public async Task BlockScopedNamespace_UsingOutsideShouldBeInside_ProducesDiagnostic()
    {
        var source = """
using MungBean.Input;
using System;

namespace TestNamespace
{
    public class TestClass {}
}
""";

        var config = """{ "insideNamespacePrefixes": [ "MungBean." ] }""";

        var diagnostics = await TestHelper.GetDiagnosticsAsync(source, config);

        Assert.Contains(diagnostics, d => d.Id == NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId);
    }

    [Fact]
    public async Task FileScopedNamespace_UsingOutsideShouldBeInside_ProducesDiagnostic()
    {
        var source = """
using MungBean.Plugins.Contracts;

namespace MungBean.Perception;

public interface IPlugin : IMungBeanPlugin
{
}
""";

        var config = """{ "insideNamespacePrefixes": [ "MungBean." ] }""";

        var diagnostics = await TestHelper.GetDiagnosticsAsync(source, config);

        Assert.Contains(diagnostics, d => d.Id == NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId);
    }

    [Fact]
    public async Task Fallback_NoConfig_UsesMungBeanAndPlatePrefixes()
    {
        // When no config is provided, the analyzer should use the hardcoded fallback
        // prefixes: MungBean. and Plate.
        var source = """
using MungBean.Input;

namespace TestNamespace;

public class TestClass {}
""";

        // Empty config - no prefixes defined
        var config = """{ }""";

        var diagnostics = await TestHelper.GetDiagnosticsAsync(source, config);

        // Should still produce NSUSG001 due to fallback prefixes
        Assert.Contains(diagnostics, d => d.Id == NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId);
    }

    [Fact]
    public async Task Fallback_PlatePrefix_ProducesDiagnostic()
    {
        // Verify Plate. prefix also works with fallback
        var source = """
using Plate.Core;

namespace TestNamespace;

public class TestClass {}
""";

        // Empty config - no prefixes defined
        var config = """{ }""";

        var diagnostics = await TestHelper.GetDiagnosticsAsync(source, config);

        // Should produce NSUSG001 due to fallback Plate. prefix
        Assert.Contains(diagnostics, d => d.Id == NamespaceUsingScopeAnalyzer.UsingShouldBeInsideNamespaceId);
    }
}
