// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.Semantics.SemanticCommandHandlerAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandHandlerAdmission.when_classifying_a_handler;

public class with_an_unresolved_file : given.a_described_handler
{
    void Establish() => SetRequirement(_requirement with { AttachmentResolution = SemanticAttachmentResolution.UnresolvedFile, File = "Handlers/ApplyLateFee.cs" });
    void Because() => _classification = Classify(_command, _request);

    [Fact] void should_not_mistake_the_file_for_a_resolved_body() => _classification.Kind.ShouldEqual(HandlerKind.Invalid);
    [Fact] void should_explain_the_unresolved_attachment() => _classification.Reason.ShouldContain("resolved");
}
