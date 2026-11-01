// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_global_usings : a_current_scaffold
{
    string _content = null!;

    void Because() => _content = Content("GlobalUsings.cs");

    [Fact] void should_emit_exactly_the_four_global_usings_in_ordinal_order() => _content.ShouldEqual(
        "global using Cratis.Arc.MongoDB;\nglobal using Cratis.Chronicle.Keys;\nglobal using MongoDB.Driver;\nglobal using System.Reactive.Subjects;\n");
    [Fact] void should_declare_no_namespace() => _content.ShouldNotContain("namespace");
    [Fact] void should_not_depend_on_the_root_namespace() => Content("GlobalUsings.cs").ShouldEqual(
        Text(new Scaffolding.CratisBackendApplicationScaffold().Create(Scaffolding.CratisBackendApplicationScaffoldRequest.Create("Other", "Other", "Acme.Other", _profile)).Single(input => PathOf(input) == "GlobalUsings.cs")));
}
