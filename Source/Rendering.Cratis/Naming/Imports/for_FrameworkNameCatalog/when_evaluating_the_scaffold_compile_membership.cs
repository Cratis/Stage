// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_evaluating_the_scaffold_compile_membership : given.a_restored_scaffold
{
    string[] _application = null!;
    string[] _specifications = null!;
    FrameworkNameCatalog _catalog = null!;

    async Task Because()
    {
        var projects = await Projects();
        // The shared fixture captures the catalog before deleting its disposable scaffold.
        _catalog = new(FrameworkNameCatalogText.Read(_generatedText));
        _application = [.. projects.Application.ProjectSources.Select(path => Path.GetRelativePath(projects.Application.Directory, path).Replace('\\', '/'))];
        _specifications = [.. projects.Specifications.ProjectSources.Select(path => Path.GetRelativePath(projects.Specifications.Directory, path).Replace('\\', '/'))];
    }

    [Theory]
    [InlineData("Cratis.Arc.Chronicle.ReadModels")]
    [InlineData("Cratis.Chronicle.Keys")]
    [InlineData("System.Collections.Immutable")]
    [InlineData("System.Reactive.Subjects")]
    public void should_catalog_each_shared_global_for_both_projects(string @namespace)
    {
        _catalog.GlobalUsings(GeneratedProjects.Application).ShouldContain(@namespace);
        _catalog.GlobalUsings(GeneratedProjects.Specifications).ShouldContain(@namespace);
    }

    [Theory]
    [InlineData("Cratis.Specifications")]
    [InlineData("NSubstitute")]
    [InlineData("Xunit")]
    public void should_catalog_testing_globals_only_for_specifications(string @namespace)
    {
        _catalog.GlobalUsings(GeneratedProjects.Application).ShouldNotContain(@namespace);
        _catalog.GlobalUsings(GeneratedProjects.Specifications).ShouldContain(@namespace);
    }

    [Fact] void should_compile_application_globals_in_the_application() => _application.ShouldContain("GlobalUsings.cs");
    [Fact] void should_not_compile_testing_globals_in_the_application() => _application.ShouldNotContain("GlobalUsings.Specs.cs");
    [Fact] void should_not_compile_when_specifications_in_the_application() => _application.ShouldNotContain("when_probe.cs");
    [Fact] void should_keep_for_helpers_in_the_application() => _application.ShouldContain("for_Helper/Helper.cs");
    [Fact] void should_keep_given_helpers_in_the_application() => _application.ShouldContain("given_Helper/Helper.cs");
    [Fact] void should_keep_dot_specs_files_in_the_application() => _application.ShouldContain("Feature.Specs.cs");
    [Fact]
    void should_link_only_the_two_globals_and_when_specifications() => _specifications
        .Where(path => !path.StartsWith("obj/", StringComparison.Ordinal)).ShouldContainOnly("../GlobalUsings.cs", "../GlobalUsings.Specs.cs", "../when_probe.cs");
}
