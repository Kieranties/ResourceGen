// Licensed under the MIT license. See https://kieranties.mit-license.org/ for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Kieranties.ResourceGen
{
    /// <summary>
    /// Models for a resource generated from a .resx file.
    /// </summary>
    public class ResourceModel
    {
        private static readonly Assembly _asm = Assembly.GetExecutingAssembly();
        private readonly StringBuilder _propertiesCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceModel"/> class.
        /// </summary>
        /// <param name="source">The source information.</param>
        public ResourceModel(AdditionalText source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrWhiteSpace(source.Path))
            {
                throw new InvalidOperationException(@$"Cannot derive class name from invalid path ""{source.Path}""");
            }

            _propertiesCode = ProcessProperties(source.GetText()?.ToString());
            ClassName = Path.GetFileNameWithoutExtension(source.Path);
        }

        /// <summary>
        /// Gets or sets the namespace for the generated class.
        /// </summary>
        public string Namespace { get; set; } = $"{nameof(Kieranties)}.GeneratedResource";

        /// <summary>
        /// Gets the clas name for the generated class.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// Returns a <see cref="SourceText"/> representation of the class.
        /// </summary>
        /// <returns>The <see cref="SourceText"/> result.</returns>
        public SourceText ToSourceText()
        {
            var code = $@"namespace {Namespace} {{

    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{_asm.GetName().Name}"", ""{_asm.GetName().Version}"")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute()]
    internal class {ClassName} {{

        private static global::System.Resources.ResourceManager resourceMan;

        /// <summary>
        ///   Initializes a new instance of the <see cref=""{ClassName}""/> class.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(""Microsoft.Performance"", ""CA1811:AvoidUncalledPrivateCode"")]
        internal {ClassName}() {{ }}

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {{
            get
            {{
                if (object.ReferenceEquals(resourceMan, null))
                {{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(""{Namespace}.{ClassName}"", typeof({ClassName}).Assembly);
                    resourceMan = temp;
                }}
                return resourceMan;
            }}
        }}

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {{ get; set; }}

        /// <summary>
        ///    Returns the resource string for the given key
        /// </summary>
        private static string GetString(string key) => ResourceManager.GetString(key, Culture);

        {_propertiesCode}
    }}
}}";
            return SourceText.From(code, Encoding.UTF8);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "TODO")]
        private static StringBuilder ProcessProperties(string content)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(content))
            {
                var xdoc = XDocument.Parse(content);
                var dataNodes = xdoc.Descendants("data");
                foreach (var node in dataNodes)
                {
                    var nameAttr = node.Attribute("name") ?? throw new XmlException("'data' nodes must have a 'name' attribute");
                    var name = nameAttr.Value;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        throw new InvalidOperationException("Resource name must be a non-empty string");
                    }

                    var valueElement = node.Element("value") ?? throw new XmlException("'data' nodes must have a 'value' descendant node");

                    builder.Append($@"/// <summary>
        /// Looks up a localized string similar to: {valueElement.Value}
        /// </summary>
        internal static string {name} => GetString(nameof({name}));");
                }
            }

            return builder;
        }
    }
}
