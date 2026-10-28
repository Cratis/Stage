// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class without_a_handler : given.a_described_handler
{
    void Establish() => _command = _command with { Handler = null };
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_classify_as_none() => _classification.Kind.ShouldEqual(HandlerKind.None);
    [Fact] void should_not_select_an_orphan_requirement() => _classification.Requirement.ShouldBeNull();
    [Fact] void should_have_no_refusal_reason() => _classification.Reason.ShouldBeNull();
}
