// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_comparing_scaffold_global_usings_with_the_catalog : Specification
{
    const string RootNamespace = "Acme.Shop";
    string[] _scaffold = [];
    string[] _application = [];
    string[] _specifications = [];
    string[] _repeatedElsewhere = [];

    void Because()
    {
        var profile = CratisBackendApplicationScaffoldProfile.Current;
        var file = new CratisBackendApplicationScaffold().Create(CratisBackendApplicationScaffoldRequest.Create("Shop", "Shop", RootNamespace, profile))
            .Single(input => input.Name.EndsWith("GlobalUsings.cs", StringComparison.Ordinal));
        _scaffold =
        [
            .. Encoding.UTF8.GetString(file.Bytes.AsSpan()).Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().TrimEnd(';')).Where(line => line.StartsWith("global using ", StringComparison.Ordinal))
                .Select(line => line["global using ".Length..])
        ];
        var catalog = FrameworkNameCatalog.Current;
        _application = [.. catalog.ScaffoldGlobalUsings(GeneratedProjects.Application)];
        _specifications = [.. catalog.ScaffoldGlobalUsings(GeneratedProjects.Specifications)];
        _repeatedElsewhere =
        [
            .. catalog.Content.Usings.Where(@using => @using.Source != FrameworkNameCatalog.ScaffoldGlobalUsingsSource && _scaffold.Contains(@using.Target))
                .Select(@using => @using.Target)
        ];
    }

    [Fact] void should_list_the_namespaces_the_catalog_records_for_the_application() => string.Join(',', _scaffold.Order(StringComparer.Ordinal)).ShouldEqual(string.Join(',', _application));
    [Fact] void should_list_the_namespaces_the_catalog_records_for_the_specifications() => string.Join(',', _scaffold.Order(StringComparer.Ordinal)).ShouldEqual(string.Join(',', _specifications));
    [Fact] void should_not_repeat_a_namespace_a_package_or_the_sdk_already_imports() => _repeatedElsewhere.ShouldBeEmpty();
    [Fact] void should_not_list_an_application_namespace() => _scaffold.Any(@namespace => @namespace == RootNamespace || @namespace.StartsWith($"{RootNamespace}.", StringComparison.Ordinal)).ShouldBeFalse();
}
