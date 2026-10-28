// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_an_import_would_bring_an_extension_member_the_file_accesses : given.a_framework_catalog
{
    readonly TypeReference _scenario = new("Cratis.Chronicle.Testing", "EventScenario", 1, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_scenario) with { MemberAccessNames = ["ShouldHaveAppended"] }, _catalog);

    [Fact] void should_not_import_the_namespace() => _result.Usings.ShouldBeEmpty();
    [Fact] void should_qualify_the_type() => _result.Spell(_scenario).ShouldEqual("StageImport0::EventScenario");
}
