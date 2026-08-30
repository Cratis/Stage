// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_a_zoo_project : Specification
{
    string[] _paths = null!;

    void Because()
    {
        var inputs = new CratisBackendApplicationScaffold().Create(CratisBackendApplicationScaffoldRequest.Create("Zoo"));
        _paths = [.. inputs.Select(input => input.Name["cratis-scaffold:text:".Length..])];
    }

    [Fact] void should_order_all_actual_relative_paths_ordinally() => _paths.SequenceEqual(
        [
            "Directory.Build.props",
            "Directory.Build.targets",
            "Directory.Packages.props",
            "Program.cs",
            "Zoo.csproj",
            "Zoo.slnx",
            "appsettings.json",
            "docker-compose.yml"
        ]).ShouldBeTrue();
}
