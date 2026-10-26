// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_member_has_the_name_of_a_type_named_in_an_expression : given.a_framework_catalog
{
    readonly TypeReference _parse = new("System", "Guid", 0, ReferenceContext.Expression);
    readonly TypeReference _parameter = new("System", "Guid", 0, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(Request(_parse, _parameter) with { ScopeValueNames = ["Guid"] }, _catalog);

    [Fact] void should_qualify_the_expression() => _result.Spell(_parse).ShouldEqual("global::System.Guid");
    [Fact] void should_spell_the_type_position_short() => _result.Spell(_parameter).ShouldEqual("Guid");
}
