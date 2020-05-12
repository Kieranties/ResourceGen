// Licensed under the MIT license. See https://kieranties.mit-license.org/ for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Xml;
using FluentAssertions;
using Kieranties.ResourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NSubstitute;
using Xunit;

namespace Kieranties.ResoruceGen.Tests
{
    public class ResourceModelFixture
    {
        private static readonly Assembly _asm = typeof(ResourceModel).Assembly;
        private readonly AdditionalText _source;
        private readonly SourceText _sourceText;

        public ResourceModelFixture()
        {
            _source = Substitute.For<AdditionalText>();
            _sourceText = SourceText.From("<root></root>");
            _source.GetText().Returns(_sourceText);
            _source.Path.Returns("/my/MyClass.resx");
        }

        [Fact]
        public void Ctor_NullSource_Throws()
        {
            // Arrange / Act
            Action action = () => new ResourceModel(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("source");
        }

        [Fact]
        public void Ctor_NullSourceText_DoesNotThrow()
        {
            // Arrange
            _source.GetText().Returns((SourceText)null);

            // Act
            Action action = () => new ResourceModel(_source);

            // Assert
            action.Should().NotThrow();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\t\t  ")]
        public void Ctor_SourcePath_Throws(string path)
        {
            // Arrange
            _source.Path.Returns(path);

            // Act
            Action action = () => new ResourceModel(_source);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .And.Message.Should().Be(@$"Cannot derive class name from invalid path ""{path}""");
        }

        [Fact]
        public void Ctor_Namespace_SetsDefault()
        {
            // Act
            var result = new ResourceModel(_source);

            // Assert
            result.Namespace.Should().Be($"{nameof(Kieranties)}.GeneratedResource");
        }

        [Theory]
        [InlineData("alpha")]
        [InlineData("alpha.beta.")]
        [InlineData("system.text")]
        public void Namespace_IsSettable(string @namespace)
        {
            // Act
            var result = new ResourceModel(_source)
            {
                Namespace = @namespace
            };

            // Assert
            result.Namespace.Should().Be(@namespace);
        }

        [Theory]
        [InlineData("/my/resources.resx", "resources")]
        [InlineData("/my/resources.en.resx", "resources.en")]
        [InlineData("/my/resources.custom.resx", "resources.custom")]
        public void Ctor_ClassName_SetsFromPath(string path, string expected)
        {
            // Arrange
            _source.Path.Returns(path);

            // Act
            var result = new ResourceModel(_source);

            // Assert
            result.ClassName.Should().Be(expected);
        }

        [Theory]
        [InlineData("<root></mismatch>")]
        [InlineData("<root><data><value>test</value></data></root>")]
        [InlineData("<root><data name=\"test\"><x>test</x></data></root>")]
        [InlineData("<root><data name=\"test\"></data></root>")]
        public void Ctor_InvalidXml_Throws(string xml)
        {
            // Arrange
            _source.GetText().Returns(SourceText.From(xml));

            // Act
            Action action = () => new ResourceModel(_source);

            // Assert
            action.Should().Throw<XmlException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\t\t  ")]
        public void Ctor_InvalidDataName_Throws(string name)
        {
            // Arrange
            _source.GetText().Returns(SourceText.From($@"<root><data name=""{name}""><value>test</value></data></root>"));

            // Act
            Action action = () => new ResourceModel(_source);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Resource name must be a non-empty string");
        }

        [Fact]
        public void ToSourceText_NoProperties_ReturnsExpected()
        {
            // Arrange
            var expected = GetExpectation("NoProperties");
            var sut = new ResourceModel(_source)
            {
                Namespace = "MyNamespace"
            };

            // Act
            var result = sut.ToSourceText();

            // Assert
            result.ToString().Should().Be(expected);
        }

        [Fact]
        public void ToSourceText_WithSingleProperty_ReturnsExpected()
        {
            // Arrange
            var expected = GetExpectation("WithSingleProperty");
            _source.GetText().Returns(SourceText.From(@"<root>
    <data name=""Single"">
        <value>Single Value</value>
    </data>
</root>"));
            var sut = new ResourceModel(_source)
            {
                Namespace = "MyNamespace"
            };

            // Act
            var result = sut.ToSourceText();

            // Assert
            result.ToString().Should().Be(expected);
        }

        private string GetExpectation(string name)
        {
            var basePath = Directory.GetParent(_asm.Location).FullName;
            var sourcePath = Path.Combine(basePath, "Expectations", $"{name}.txt");
            var content = File.ReadAllText(sourcePath);
            var asmName = _asm.GetName();

            return content
                .Replace("{0}", asmName.Name)
                .Replace("{1}", asmName.Version.ToString())
                .Trim();
        }
    }
}
