// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_context_invocation_mapping : given.an_application_trigger_reaction
{
    void Establish()
    {
        InspectSource(InvocationSource);
        var invocation = _trigger.Invokes.Single();
        var mapping = invocation.Mappings.Single();
        _trigger = _trigger with { Invokes = [invocation with { Mappings = [mapping with { Source = new SemanticEventContextExpression(SemanticEventContextValueKind.Occurred, new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false)) }] }] };
    }

    void Because() => Inspect();

    [Fact] void should_refuse_the_context_mapping() => _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024").Message.ShouldContain("maps '$context.occurred'");
}
