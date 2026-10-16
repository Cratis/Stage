// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_a_compile_item_contributes_global_usings : Specification
{
    DirectoryInfo _directory = null!;
    ScaffoldedProject _application = null!;
    ScaffoldedProject _specifications = null!;
    FrameworkNameCatalogContent _catalog = null!;

    async Task Establish()
    {
        _directory = Directory.CreateDirectory(Path.Combine(given.a_restored_scaffold.Workspace(), $"compile-imports-{Guid.NewGuid():N}"));
        var path = Path.Combine(_directory.FullName, "Imports.cs");
        await File.WriteAllTextAsync(path, "global using System.Text;\nglobal using static System.Math;\nglobal using Value = System.String;\n");
        _application = new(GeneratedProjects.Application, _directory.FullName, [], [], [], [], []) { ProjectSources = [path] };
        _specifications = new(GeneratedProjects.Specifications, _directory.FullName, [], [], [], [], []);
    }

    void Because() => _catalog = FrameworkNameCatalogGenerator.Generate(_application, _specifications);

    [Fact] void should_record_the_namespace_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Namespace, "System.Text", string.Empty, "compile:Imports.cs"));
    [Fact] void should_record_the_static_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Static, "System.Math", string.Empty, "compile:Imports.cs"));
    [Fact] void should_record_the_alias_import() => _catalog.Usings.ShouldContain(new CatalogUsing(GeneratedProjects.Application, CatalogUsingKind.Alias, "System.String", "Value", "compile:Imports.cs"));

    void Destroy() => _directory.Delete(recursive: true);
}
