// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies.given;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies;

public class when_resolving_an_inline_body : a_compiled_document_set
{
    ImmutableDictionary<string, string> _bodies = null!;

    void Establish() => Compile(InlineSource);

    void Because() => _bodies = SemanticImplementationBodies.Resolve(_documents, _requirements);

    [Fact] void should_resolve_one_body_for_the_requirement() => _bodies.Count.ShouldEqual(1);
    [Fact] void should_key_the_body_by_the_requirement_identity() => _bodies.ContainsKey(_requirements.Single().RequirementId).ShouldBeTrue();
    [Fact] void should_resolve_the_code_between_the_fences() => _bodies.Values.Single().ShouldContain("Nothing to order");
}
