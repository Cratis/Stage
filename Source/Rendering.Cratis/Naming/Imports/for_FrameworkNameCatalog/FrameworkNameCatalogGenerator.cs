// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Cratis.Scaffolding;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Generates the framework name catalog from the two restored scaffold projects. Everything is read as metadata or
/// syntax; nothing from the scaffold's dependency graph is loaded into the generating process.
/// </summary>
static class FrameworkNameCatalogGenerator
{
    /// <summary>
    /// The namespace prefixes whose types are recorded in full, besides every imported namespace and the global namespace.
    /// They cover what generated code names: Arc, Chronicle and the rest of Cratis, the BCL, FluentValidation, the
    /// MongoDB driver and the test libraries of the specifications project.
    /// </summary>
    public static readonly ImmutableArray<string> Scopes = ["Cratis", "FluentValidation", "MongoDB", "NSubstitute", "System", "Xunit"];

    /// <summary>
    /// Generates the catalog content.
    /// </summary>
    /// <param name="application">The restored application project.</param>
    /// <param name="specifications">The restored specifications project.</param>
    /// <returns>The catalog content.</returns>
    public static FrameworkNameCatalogContent Generate(ScaffoldedProject application, ScaffoldedProject specifications)
    {
        ScaffoldedProject[] projects = [application, specifications];
        var profile = CratisBackendApplicationScaffoldProfile.Current;
        var usings = Usings(projects);
        var imported = usings.Where(@using => @using.Kind == CatalogUsingKind.Namespace).Select(@using => @using.Target).ToHashSet(StringComparer.Ordinal);
        bool Records(string @namespace) => @namespace.Length == 0 || imported.Contains(@namespace) ||
            Scopes.Any(scope => @namespace == scope || @namespace.StartsWith($"{scope}.", StringComparison.Ordinal));

        var types = new CatalogTypes(Records);
        foreach (var assembly in projects.SelectMany(project => project.References.Select(reference => (reference.Path, project.Project)))
            .GroupBy(reference => reference.Path, StringComparer.Ordinal))
        {
            var (namespaces, metadata) = MetadataAssembly.Read(assembly.Key);
            types.Add(assembly.Aggregate(GeneratedProjects.None, (all, reference) => all | reference.Project), namespaces, metadata);
        }

        var sourceTypes = SourceTypes(projects, types).Where(type => Records(type.Namespace)).ToArray();
        return new(
            profile.Version,
            ScaffoldPins.Of(profile),
            [.. Merge(
                projects.SelectMany(project => project.Packages.Select(package => (project.Project, package))),
                package => package,
                (tags, package) => new CatalogPackage(tags, package.Id, package.Version))],
            [.. Merge(
                projects.SelectMany(project => project.References.Where(reference => reference.Framework.Length > 0).Select(reference => (project.Project, reference.Framework))),
                framework => framework,
                (tags, framework) => new CatalogFramework(tags, framework, profile.TargetFramework))],
            [.. usings],
            Scopes,
            [.. types.Namespaces()],
            [.. Merge(
                types.Types().Select(type => (type.Projects, type)).Concat(sourceTypes.Select(type => (type.Projects, type))),
                type => (type.Namespace, type.MetadataName, type.Kind),
                (tags, type) => type with { Projects = tags })],
            [.. types.Extensions()],
            [.. types.BaseTypes()]);
    }

    static IReadOnlyList<CatalogUsing> Usings(ScaffoldedProject[] projects) =>
    [
        .. Merge(
            projects.SelectMany(project => project.Usings.Concat(project.PackageSources.SelectMany(source => CatalogSources.Usings(project.Project, source.Package, source.Path))))
                .Select(@using => (@using.Projects, @using)),
            @using => (@using.Kind, @using.Target, @using.Alias, @using.Source),
            (tags, @using) => @using with { Projects = tags })
    ];

    static IEnumerable<CatalogedType> SourceTypes(ScaffoldedProject[] projects, CatalogTypes types)
    {
        foreach (var project in projects)
        {
            // The application grants the specifications project its internals, so its generated declarations are in scope there too.
            var generatedFor = project.Project == GeneratedProjects.Application ? GeneratedProjects.Both : project.Project;
            var sources = project.PackageSources.Select(source => (project.Project, source.Path))
                .Concat(project.GeneratedSources.Select(path => (Project: generatedFor, Path: path)));
            foreach (var (projectsSeeing, path) in sources)
            {
                foreach (var (@namespace, metadataName, kind) in CatalogSources.Types(path))
                {
                    var named = @namespace.Split('.').TakeWhile(segment => !segment.Contains(CatalogScaffold.ProjectName, StringComparison.Ordinal)).ToArray();
                    types.AddNamespace(projectsSeeing, string.Join('.', named));
                    if (named.Length == @namespace.Split('.').Length || @namespace.Length == 0)
                    {
                        yield return new(projectsSeeing, @namespace, metadataName, kind);
                    }
                }
            }
        }
    }

    static IEnumerable<TResult> Merge<TItem, TKey, TResult>(
        IEnumerable<(GeneratedProjects Projects, TItem Item)> items,
        Func<TItem, TKey> key,
        Func<GeneratedProjects, TItem, TResult> create)
        where TKey : notnull =>
        items.GroupBy(entry => key(entry.Item))
            .Select(group => create(group.Aggregate(GeneratedProjects.None, (all, entry) => all | entry.Projects), group.First().Item));
}
