// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Types;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Renders a Screenplay composite <see cref="TypeSyntax"/> — a plain value-object record, placed at the folder
/// level computed by <see cref="ApplicationSet.ConceptPlacements"/>, the same rule concepts use.
/// </summary>
public static class TypeRenderer
{
    /// <summary>
    /// Renders a composite type into its generated file.
    /// </summary>
    /// <param name="type">The composite type to render.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the type was declared in.</param>
    /// <param name="rootNamespace">The root namespace of the target application.</param>
    /// <returns>The <see cref="RenderedFile"/>.</returns>
    public static RenderedFile Render(TypeSyntax type, ApplicationSet applicationSet, string rootNamespace)
    {
        var typeName = Identifiers.ToPascalCase(type.Name);
        var placement = applicationSet.ConceptPlacements.GetValueOrDefault(type.Name, []);
        var folderSegments = placement.Count == 0 ? ["Common"] : SliceNaming.FolderPath(placement);
        var ownNamespace = placement.Count == 0 ? $"{rootNamespace}.Common" : SliceNaming.Namespace(rootNamespace, placement);

        var builder = new CSharpCodeBuilder().Namespace(ownNamespace);
        foreach (var @namespace in ReferencedNamespaces(type, applicationSet, rootNamespace, ownNamespace))
        {
            builder.Using(@namespace);
        }

        var parameters = string.Join(", ", type.Properties.Select(property => RenderParameter(property, applicationSet)));
        var summary = type.Description ?? $"Represents {Identifiers.ToWords(type.Name)}.";

        builder.Summary(summary).Line($"public record {typeName}({parameters});");

        var path = new List<string>(folderSegments) { $"{typeName}.cs" };
        return new RenderedFile(Path.Combine([.. path]), builder.ToString());
    }

    static string RenderParameter(PropertySyntax property, ApplicationSet applicationSet)
    {
        var resolved = TypeResolver.Resolve(property.Type, applicationSet);
        return $"{resolved.ToTypeSyntax()} {Identifiers.ToPascalCase(property.Name)}";
    }

    static IReadOnlyList<string> ReferencedNamespaces(TypeSyntax type, ApplicationSet applicationSet, string rootNamespace, string ownNamespace)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        var referencedNames = type.Properties.Select(property => property.Type.Name)
            .Where(name => applicationSet.Concepts.ContainsKey(name) || applicationSet.Types.ContainsKey(name));

        foreach (var name in referencedNames)
        {
            var placement = applicationSet.ConceptPlacements.GetValueOrDefault(name, []);
            var @namespace = placement.Count == 0 ? $"{rootNamespace}.Common" : SliceNaming.Namespace(rootNamespace, placement);
            if (!string.Equals(@namespace, ownNamespace, StringComparison.Ordinal))
            {
                namespaces.Add(@namespace);
            }
        }

        return [.. namespaces];
    }
}
