// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_the_effective_global_imports_drift : given.a_framework_catalog
{
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);

    [Fact]
    public void should_accept_the_complete_known_inventory() => ImportResolver.Resolve(
        Request(_guid) with { EffectiveGlobalUsings = _catalog.Content.Usings }, _catalog).Spell(_guid).ShouldEqual("Guid");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_references_qualified_when_the_known_catalog_contains_static_or_alias_imports(bool alias)
    {
        var @using = new CatalogUsing(
            GeneratedProjects.Application,
            alias ? CatalogUsingKind.Alias : CatalogUsingKind.Static,
            alias ? "System.String" : "System.Math",
            alias ? "Guid" : string.Empty,
            "compile:Imports.cs");
        var catalog = new FrameworkNameCatalog(_catalog.Content with { Usings = [.. _catalog.Content.Usings, @using] });
        var result = ImportResolver.Resolve(Request(_guid) with { EffectiveGlobalUsings = catalog.Content.Usings }, catalog);
        result.Spell(_guid).ShouldEqual(_guid.Qualified);
        result.Usings.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("additional")]
    [InlineData("missing")]
    [InlineData("alias")]
    [InlineData("static")]
    [InlineData("wrong-project")]
    [InlineData("package")]
    public void should_refuse_an_unmodeled_effective_import_set(string drift)
    {
        var effective = _catalog.Content.Usings.ToList();
        switch (drift)
        {
            case "missing": effective.RemoveAt(0); break;
            case "alias": effective.Add(new(GeneratedProjects.Application, CatalogUsingKind.Alias, "System.String", "Guid", "compile:Customizations/Imports.cs")); break;
            case "static": effective.Add(new(GeneratedProjects.Application, CatalogUsingKind.Static, "System.Math", string.Empty, "compile:Customizations/Imports.cs")); break;
            case "wrong-project": effective[0] = effective[0] with { Projects = GeneratedProjects.Specifications }; break;
            case "package": effective.Add(new(GeneratedProjects.Application, CatalogUsingKind.Namespace, "System.Text", string.Empty, "package")); break;
            default: effective.Add(new(GeneratedProjects.Application, CatalogUsingKind.Namespace, "System.Text", string.Empty, "compile:Customizations/Imports.cs")); break;
        }

        var exception = Assert.Throws<InvalidOperationException>(() => ImportResolver.Resolve(
            Request(_guid) with { EffectiveGlobalUsings = effective }, _catalog));
        exception.Message.ShouldEqual("The effective global usings do not match the restored scaffold catalog.");
    }
}
