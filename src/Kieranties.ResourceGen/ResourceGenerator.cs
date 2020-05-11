// Licensed under the MIT license. See https://kieranties.mit-license.org/ for full license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Kieranties.ResourceGen
{
    /// <summary>
    /// Generates code for '.resx' files in the source compilation.
    /// </summary>
    [Generator]
    public class ResourceGenerator : ISourceGenerator
    {
        /// <inheritdoc/>
        public void Execute(SourceGeneratorContext context)
        {
            var resources = context.AdditionalFiles.Where(f => f.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase));

            foreach (var resource in resources)
            {
                var model = new ResourceModel(resource)
                {
                    Namespace = context.Compilation.AssemblyName
                };

                var ctxName = $"{model.Namespace}.{model.ClassName}";
                context.AddSource(ctxName, model.ToSourceText());
            }
        }

        /// <inheritdoc/>
        public void Initialize(InitializationContext context)
        {
            // TODO: Investigate event hooks for changes in source files.
        }
    }
}
