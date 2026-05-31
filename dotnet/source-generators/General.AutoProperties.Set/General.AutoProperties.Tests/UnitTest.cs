using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PlateShared.SCG.General.AutoProperties;
using Xunit;

namespace PlateShared.General.AutoProperties.Tests;

public class UnitTest
{
    [Fact]
    public Task CheckBasicPropertyGeneration()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty]
    private string _name;

    [GenerateProperty]
    private int _age;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckCustomPropertyNames()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(PropertyName = "FullName")]
    private string _name;

    [GenerateProperty(PropertyName = "YearsOld")]
    private int _age;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckDifferentPropertyKinds()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterSetter)]
    private string _readWrite;

    [GenerateProperty(PropertyKind = PropertyKind.GetterOnly)]
    private string _readOnly;

    [GenerateProperty(PropertyKind = PropertyKind.GetterPrivateSetter)]
    private string _privateSetter;

    [GenerateProperty(PropertyKind = PropertyKind.GetterInitOnly)]
    private string _initOnly;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckDifferentAccessibilityLevels()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(Accessibility = PropertyAccessibility.Public)]
    private string _publicField;

    [GenerateProperty(Accessibility = PropertyAccessibility.Internal)]
    private string _internalField;

    [GenerateProperty(Accessibility = PropertyAccessibility.Protected)]
    private string _protectedField;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckGenerateForAllFields()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(GenerateForAllFields = true)]
public partial class TestClass
{
    private string _name;
    private int _age;

    [SkipProperty(Reason = "Internal use only")]
    private string _internal;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckCustomFieldPrefix()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(FieldPrefix = "m_")]
public partial class TestClass
{
    [GenerateProperty]
    private string m_name;

    [GenerateProperty]
    private int m_age;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckDefaultInitOnlyPropertyKind()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(DefaultPropertyKind = PropertyKind.GetterInitOnly)]
public partial class TestClass
{
    [GenerateProperty]
    private string _name;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckRequiredInitOnlyProperty()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty]
public partial class TestClass
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterInitOnly, IsRequired = true)]
    private string _name;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckAutoPropertyWithAutoToStringIntegration()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;
using PlateShared.SCG.General.AutoToString.Attributes;

namespace TestNamespace;

[AutoProperty]
[AutoToString]
public partial class TestClass
{
    [GenerateProperty(IncludeInToString = true)]
    private string _name;
}
""";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CheckReadonlyFieldGeneratesGetterOnly()
    {
        var source = """
using PlateShared.General.AutoProperties.Attributes;

namespace TestNamespace;

[AutoProperty(DefaultPropertyKind = PropertyKind.GetterSetter)]
public partial class TestClass
{
    [GenerateProperty(PropertyKind = PropertyKind.GetterPrivateSetter)]
    private readonly string _name;
}
""";

        return TestHelper.Verify(source);
    }
}
