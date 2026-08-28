// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_with_distinct_names : Specification
{
    string[] _paths = null!;
    string _project = null!;
    string _settings = null!;

    void Because()
    {
        var request = CratisBackendApplicationScaffoldRequest.Create("Orders", "Orders.Backend", "Company.Orders");
        var inputs = new CratisBackendApplicationScaffold().Create(request);
        _paths = [.. inputs.Select(input => input.Name["cratis-scaffold:text:".Length..])];
        _project = Encoding.UTF8.GetString(inputs.Single(input => input.Name.EndsWith(".csproj", StringComparison.Ordinal)).Bytes.AsSpan());
        _settings = Encoding.UTF8.GetString(inputs.Single(input => input.Name.EndsWith("appsettings.json", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_use_only_the_explicit_project_name_for_project_paths() => _paths.Count(path => path == "Orders.Backend.csproj").ShouldEqual(1);
    [Fact] void should_use_only_the_explicit_project_name_for_solution_paths() => _paths.Count(path => path == "Orders.Backend.slnx").ShouldEqual(1);
    [Fact] void should_use_the_explicit_root_namespace() => _project.ShouldContain("<RootNamespace>Company.Orders</RootNamespace>");
    [Fact] void should_use_the_explicit_application_name_for_persistent_stores() => _settings.Split("\"Orders\"", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_not_derive_a_name_from_a_destination_path() => string.Join('|', _paths).ShouldNotContain("Company.Orders");
}
