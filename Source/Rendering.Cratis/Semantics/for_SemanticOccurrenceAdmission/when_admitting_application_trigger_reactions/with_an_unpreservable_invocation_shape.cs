// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_an_unpreservable_invocation_shape : given.an_application_trigger_reaction
{
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("unknown-target")]
    [InlineData("unsafe-value")]
    public void should_refuse_instead_of_emitting_an_unsafe_argument(string shape)
    {
        InspectSource(InvocationSource);
        var invocation = _trigger.Invokes.Single();
        var mapping = invocation.Mappings.Single();
        _trigger = _trigger with
        {
            Invokes = [invocation with
            {
                Mappings = shape switch
                {
                    "missing" => [],
                    "duplicate" => [mapping, mapping],
                    "unknown-target" => [mapping with { TargetProperty = default }],
                    _ => [mapping with { Source = new SemanticValueExpression(SemanticValue.Number(42)) }]
                }
            }]
        };
        Inspect();
        _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024").Message.ShouldContain("cannot preserve for an occurrence");
    }
}
