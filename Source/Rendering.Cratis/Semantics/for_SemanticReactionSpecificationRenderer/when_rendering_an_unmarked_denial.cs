// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_an_unmarked_denial : given.a_reaction_specification
{
    void Establish() => PrepareIdentity("NoIdentityDenies");
    void Because() => Render();
    [Fact] void should_execute_the_unmarked_reactor() => _source.ShouldContain("ReactorScenario<UnmarkedWorker>");
    [Fact] void should_not_forward_the_fixture_caller() => _source.ShouldNotContain("BeginScope(principal)");
    [Fact] void should_open_no_system_scope_if_the_attribute_is_absent() => _source.ShouldContain("identity is null ? null");
    [Fact] void should_assert_denial_and_no_appends() => _source.ShouldContain("_result.IsAuthorized.ShouldBeFalse()");
}
