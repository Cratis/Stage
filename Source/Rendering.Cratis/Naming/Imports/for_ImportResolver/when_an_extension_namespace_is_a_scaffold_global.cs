// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_extension_namespace_is_a_scaffold_global : Specification
{
    ImportResolution _application = null!;
    ImportResolution _scoped = null!;

    void Because()
    {
        var request = new ImportRequest("Shop.Orders.Placing", GeneratedProjects.Application, [], []) { RequiredImports = ["MongoDB.Driver"], MemberAccessNames = ["Count"] };
        _application = ImportResolver.Resolve(request, FrameworkNameCatalog.Current);
        _scoped = ImportResolver.Resolve(request with { HasScaffoldGlobalUsings = false }, FrameworkNameCatalog.Current);
    }

    [Fact] void should_drop_the_import_with_the_scaffold_file() => _application.TopLevelUsings.ShouldBeEmpty();
    [Fact] void should_keep_the_import_without_it() => string.Join(',', _scoped.TopLevelUsings).ShouldEqual("MongoDB.Driver");
}
