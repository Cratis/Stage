// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_a_generator_contributes_global_usings : Specification
{
    DirectoryInfo _directory = null!;
    ScaffoldedProject _application = null!;
    ScaffoldedProject _specifications = null!;
    FrameworkNameCatalogContent _catalog = null!;

    async Task Establish()
    {
        _directory = Directory.CreateDirectory(Path.Combine(given.a_restored_scaffold.Workspace(), $"generator-imports-{Guid.NewGuid():N}"));
        var path = Path.Combine(_directory.FullName, "Imports.g.cs");
        await File.WriteAllTextAsync(path,
            """
            global using System.Text;
            global using static global::System.Math;
            global using Value = global::System.String;
            """);
        _application = new(GeneratedProjects.Application, _directory.FullName, [], [], [], [path], []);
        _specifications = new(GeneratedProjects.Specifications, _directory.FullName, [], [], [], [], []);
    }

    void Because() => _catalog = FrameworkNameCatalogGenerator.Generate(_application, _specifications);

    [Fact] void should_record_the_namespace_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Namespace, "System.Text", string.Empty, "generator:Imports.g.cs"));
    [Fact] void should_record_the_static_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Static, "System.Math", string.Empty, "generator:Imports.g.cs"));
    [Fact] void should_record_the_alias_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Alias, "System.String", "Value", "generator:Imports.g.cs"));
    [Fact] void should_not_leak_imports_into_the_other_project() => _catalog.Usings.Where(@using => @using.Projects != GeneratedProjects.Application).ShouldBeEmpty();

    void Destroy() => _directory.Delete(recursive: true);
}
