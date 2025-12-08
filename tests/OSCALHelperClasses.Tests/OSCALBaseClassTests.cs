using System.Xml;
using FluentAssertions;
using OSCALHelperClasses;
using Xunit;

namespace OSCALHelperClasses.Tests;

public class OSCALBaseClassTests
{
    [Fact]
    public void RemoveTag_WithNoTags_ShouldReturnOriginal()
    {
        // Act
        var result = OSCALBaseClass.RemoveTag("No tags here");

        // Assert
        result.Should().Be("No tags here");
    }

    [Fact]
    public void RemoveTag_ShouldRemoveAngleBrackets()
    {
        // The existing RemoveTag implementation removes < and > characters
        // Note: This is quirky legacy behavior - it doesn't perfectly strip HTML
        var result = OSCALBaseClass.RemoveTag("Test < Value > End");

        // Assert - should at least remove < and >
        result.Should().NotContain("<");
        result.Should().NotContain(">");
    }

    [Fact]
    public void FindXPath_ShouldReturnCorrectPath_ForElement()
    {
        // Arrange
        var xml = @"<root xmlns=""http://csrc.nist.gov/ns/oscal/1.0"">
            <child>
                <grandchild>Value</grandchild>
            </child>
        </root>";
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var grandchild = doc.SelectSingleNode("//*[local-name()='grandchild']");

        // Act
        var xpath = OSCALBaseClass.FindXPath(grandchild!);

        // Assert
        xpath.Should().Contain("grandchild");
        xpath.Should().Contain("child");
    }

    [Fact]
    public void FindElementIndex_ShouldReturnOneBasedIndex()
    {
        // Arrange
        var xml = @"<root>
            <item>First</item>
            <item>Second</item>
            <item>Third</item>
        </root>";
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var items = doc.SelectNodes("//item");

        // Act & Assert
        OSCALBaseClass.FindElementIndex((XmlElement)items![0]).Should().Be(1);
        OSCALBaseClass.FindElementIndex((XmlElement)items![1]).Should().Be(2);
        OSCALBaseClass.FindElementIndex((XmlElement)items![2]).Should().Be(3);
    }

    [Fact]
    public void FindXPathWithoutIndex_ShouldReturnPathWithoutBrackets()
    {
        // Arrange
        var xml = @"<root>
            <child>
                <grandchild>Value</grandchild>
            </child>
        </root>";
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var grandchild = doc.SelectSingleNode("//grandchild");

        // Act
        var xpath = OSCALBaseClass.FindXPathWithoutIndex(grandchild!);

        // Assert
        xpath.Should().NotContain("[");
        xpath.Should().NotContain("]");
        xpath.Should().Contain("grandchild");
    }

    [Fact]
    public void Components_ShouldSplitXPathIntoComponents()
    {
        // Arrange
        var xpath = "/root/child/grandchild";

        // Act
        var components = OSCALBaseClass.Components(xpath);

        // Assert
        components.Should().Contain("grandchild");
        components.Should().Contain("child");
        components.Should().Contain("root");
    }

    [Fact]
    public void Components_ShouldHandleEmptyPath()
    {
        // Arrange
        var xpath = "";

        // Act
        var components = OSCALBaseClass.Components(xpath);

        // Assert
        components.Should().BeEmpty();
    }
}
