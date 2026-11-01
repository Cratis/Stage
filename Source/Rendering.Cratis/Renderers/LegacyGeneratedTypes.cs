// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Lists the declarations whose names legacy rendering can bring into scope.
/// </summary>
internal static class LegacyGeneratedTypes
{
    /// <summary>
    /// Lists the application declarations with their absolute generated namespaces.
    /// </summary>
    /// <param name="application">The declarations and their placements.</param>
    /// <param name="root">The generated application's root namespace.</param>
    /// <returns>The distinct generated type names.</returns>
    internal static IReadOnlyList<GeneratedTypeName> For(ApplicationSet application, string root)
    {
        var types = new List<GeneratedTypeName>
        {
            new(string.Empty, "Program"),
            new($"{root}.GeneratedTenancy", "PortableTenantValues"),
            new($"{root}.GeneratedTenancy", "AmbiguousTenant"),
            new($"{root}.GeneratedCommands", "CommandReceiptTime"),
            new($"{root}.GeneratedCommands", "CommandReceiptTimeUnavailable")
        };

        types.AddRange(application.Concepts.Values.SelectMany(concept => ConceptTypes(concept, application, root)));
        types.AddRange(application.Types.Keys.Select(name => new GeneratedTypeName(
            ReferencedNamespaces.ForPlacement(root, application.ConceptPlacements.GetValueOrDefault(name, [])),
            Identifiers.ToPascalCase(name))));
        types.AddRange(application.DeclarationPlacements.Select(declaration => new GeneratedTypeName(
            SliceNaming.Namespace(root, declaration.Value), Identifiers.ToPascalCase(declaration.Key))));

        types.AddRange(application.Slices.SelectMany(slice => SliceTypes(slice, root)));

        return [.. types.Distinct()];
    }

    static IEnumerable<GeneratedTypeName> ConceptTypes(ConceptSyntax concept, ApplicationSet application, string root)
    {
        var ownNamespace = ReferencedNamespaces.ForPlacement(root, application.ConceptPlacements.GetValueOrDefault(concept.Name, []));
        var name = Identifiers.ToPascalCase(concept.Name);
        yield return new(ownNamespace, name);
        if (!concept.IsEnum && HasConceptValidator(concept))
        {
            yield return new(ownNamespace, $"{name}Validator");
        }
    }

    static IEnumerable<GeneratedTypeName> SliceTypes(LocatedSlice slice, string root)
    {
        var ownNamespace = SliceNaming.Namespace(root, slice.FullPath);
        if (slice.Slice.Type == SliceType.StateChange && slice.Slice.Commands.FirstOrDefault() is { } command)
        {
            return CommandValidators(command, ownNamespace);
        }

        if (slice.Slice.Type == SliceType.StateView && slice.Slice.Projections.FirstOrDefault() is { } projection)
        {
            return ProjectionRecords(projection.Blocks, new(ownNamespace, Identifiers.ToPascalCase(projection.ReadModel ?? projection.Name)));
        }

        return [];
    }

    static IEnumerable<GeneratedTypeName> CommandValidators(CommandSyntax command, string ownNamespace)
    {
        if (command.Validations.OfType<DeclarativeValidateSyntax>().SelectMany(validation => validation.Rules).Any())
        {
            yield return new(ownNamespace, $"{Identifiers.ToPascalCase(command.Name)}Validator");
        }
    }

    static bool HasConceptValidator(ConceptSyntax concept) =>
        (concept.Validations ?? []).OfType<DeclarativeValidateSyntax>().SelectMany(validation => validation.Rules).Any() ||
        (concept.Validations ?? []).OfType<CodeValidateSyntax>().Any();

    static IEnumerable<GeneratedTypeName> ProjectionRecords(IEnumerable<ProjectionBlockSyntax> blocks, GeneratedTypeName parent)
    {
        foreach (var block in blocks)
        {
            var child = ProjectionChild(block);
            if (child.Name.Length == 0)
            {
                continue;
            }

            var type = new GeneratedTypeName(parent.Namespace, $"{parent.Name}{Identifiers.ToPascalCase(child.Name)}");
            yield return type;
            foreach (var descendant in ProjectionRecords(child.Blocks, type))
            {
                yield return descendant;
            }
        }
    }

    static (string Name, IEnumerable<ProjectionBlockSyntax> Blocks) ProjectionChild(ProjectionBlockSyntax block) => block switch
    {
        NestedSyntax nested => (nested.Property, nested.Blocks),
        ChildrenSyntax children => (children.Property, children.Blocks),
        _ => (string.Empty, [])
    };
}
