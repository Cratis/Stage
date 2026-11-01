// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_nested_type_has_the_name : given.a_framework_catalog
{
    readonly TypeReference _string = new("System", "String", 0, ReferenceContext.Type);
    readonly TypeReference _guid = new("System", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_string, _guid) with { ScopeTypeNames = ["String", "Guid"] }, _catalog);

    [Fact] void should_qualify_the_type() => _result.Spell(_guid).ShouldEqual("StageImport0::Guid");
    [Fact] void should_use_the_keyword_that_no_nested_type_can_hide() => _result.Spell(_string).ShouldEqual("string");
}
