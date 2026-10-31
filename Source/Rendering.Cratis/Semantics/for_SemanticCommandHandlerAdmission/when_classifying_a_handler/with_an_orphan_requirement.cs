// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class with_an_orphan_requirement : given.a_described_handler
{
    void Establish() => SetRequirement(_requirement with { Source = _requirement.Source with { SemanticId = _request.Model.Application.Id } });
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_classify_as_invalid() => _classification.Kind.ShouldEqual(HandlerKind.Invalid);
    [Fact] void should_explain_the_wrong_owner() => _classification.Reason.ShouldContain("command");
}
