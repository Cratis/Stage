// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class with_a_resolved_csharp_inline_body : given.a_described_handler
{
    void Establish() => Resolve("csharp");
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_classify_as_csharp() => _classification.Kind.ShouldEqual(HandlerKind.CSharp);
    [Fact] void should_preserve_the_requirement() => _classification.Requirement.ShouldBeSame(_requirement);
    [Fact] void should_have_no_refusal_reason() => _classification.Reason.ShouldBeNull();
}
