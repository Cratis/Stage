// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class with_a_missing_requirement_envelope : given.a_described_handler
{
    void Establish() => _request = _request with { ImplementationRequirements = default };
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_classify_as_invalid_without_throwing() => _classification.Kind.ShouldEqual(HandlerKind.Invalid);
    [Fact] void should_explain_the_missing_envelope() => _classification.Reason!.ShouldContain("envelope");
}
