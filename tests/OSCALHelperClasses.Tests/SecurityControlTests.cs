using System.Xml;
using FluentAssertions;
using OSCALHelperClasses;
using Xunit;

namespace OSCALHelperClasses.Tests;

public class SecurityControlTests
{
    private const string XMLNamespace = "http://csrc.nist.gov/ns/oscal/1.0";

    private XmlNode LoadControlNode(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return doc.DocumentElement!;
    }

    [Fact]
    public void Constructor_WithControlIdAttribute_ShouldParseControlId()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <prop name=""implementation-status"" value=""implemented""/>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.ControlId.Should().Be("ac-1");
    }

    [Fact]
    public void Constructor_ShouldParseProperties()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <prop name=""implementation-status"" value=""implemented""/>
            <prop name=""control-origination"" value=""service-provider-corporate""/>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.Properties.Should().HaveCount(2);
        control.Properties[0].Name.Should().Be("implementation-status");
        control.Properties[0].Value.Should().Be("implemented");
        control.Properties[1].Name.Should().Be("control-origination");
        control.Properties[1].Value.Should().Be("service-provider-corporate");
    }

    [Fact]
    public void Constructor_ShouldParseStatements()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <statement statement-id=""ac-1_smt.a"" uuid=""stmt-001"">
                <description><p>Test statement content.</p></description>
            </statement>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.Statements.Should().HaveCount(1);
        control.Statements[0].StatementID.Should().Be("ac-1_smt.a");
        control.Statements[0].Value.Should().Contain("Test statement content");
    }

    [Fact]
    public void Constructor_ShouldParseResponsibleRoles()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <responsible-role role-id=""system-owner"">
                <party-uuid>party-001</party-uuid>
            </responsible-role>
            <responsible-role role-id=""authorizing-official"">
                <party-uuid>party-002</party-uuid>
            </responsible-role>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.ResponsibleRoles.Should().HaveCount(2);
        control.ResponsibleRoles[0].Name.Should().Be("system-owner");
        control.ResponsibleRoles[1].Name.Should().Be("authorizing-official");
    }

    [Fact]
    public void Constructor_ShouldParseParameters()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-2"" uuid=""test"">
            <set-parameter param-id=""ac-2_prm_1"">
                <value>30 days</value>
            </set-parameter>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.Parameters.Should().HaveCount(1);
        control.Parameters[0].ParamID.Should().Be("ac-2_prm_1");
        control.Parameters[0].Value.Should().Contain("30 days");
    }

    [Fact]
    public void HasMultipleResponsibleRoles_ShouldReturnTrue_WhenMultipleRoles()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <responsible-role role-id=""system-owner""/>
            <responsible-role role-id=""authorizing-official""/>
        </implemented-requirement>";

        var node = LoadControlNode(xml);
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.HasMultipleResponsibleRoles.Should().BeTrue();
    }

    [Fact]
    public void HasMultipleResponsibleRoles_ShouldReturnFalse_WhenSingleRole()
    {
        // Arrange
        var xml = @"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <responsible-role role-id=""system-owner""/>
        </implemented-requirement>";

        var node = LoadControlNode(xml);
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.HasMultipleResponsibleRoles.Should().BeFalse();
    }

    [Theory]
    [InlineData("implemented")]
    [InlineData("partially-implemented")]
    [InlineData("planned")]
    [InlineData("not-applicable")]
    public void Constructor_ShouldParseVariousImplementationStatuses(string status)
    {
        // Arrange
        var xml = $@"<implemented-requirement xmlns=""http://csrc.nist.gov/ns/oscal/1.0"" control-id=""ac-1"" uuid=""test"">
            <prop name=""implementation-status"" value=""{status}""/>
        </implemented-requirement>";

        var node = LoadControlNode(xml);

        // Act
        var control = new SecurityControl(node, 0, XMLNamespace);

        // Assert
        control.Properties.Should().Contain(p => p.Name == "implementation-status" && p.Value == status);
    }
}
