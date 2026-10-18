// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_optional_collection_reaction_values : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    [Theory]
    [InlineData("null-collection", true)]
    [InlineData("populated", true)]
    [InlineData("null-element", false)]
    public void should_apply_optionality_to_the_collection_only(string form, bool admitted)
    {
        SetContext(Compile(Source.Replace("note String optional", "note String[] optional", StringComparison.Ordinal)));
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive");
        var trigger = reaction.Triggers.Single();
        var invocation = trigger.Invokes.Single();
        var note = _context.Commands[invocation.Command].Properties.Single(property => property.Name == "note");
        SemanticValue value = form switch
        {
            "null-collection" => SemanticValue.Null,
            "populated" => SemanticValue.Array([SemanticValue.Text("one")]),
            _ => SemanticValue.Array([SemanticValue.Null])
        };
        var slice = _located.Slice with
        {
            Reactions = [reaction with
            {
                Triggers = [trigger with
                {
                    Invokes = [invocation with { Mappings = [.. invocation.Mappings, new(note.Id, new SemanticValueExpression(value))] }]
                }]
            }]
        };
        // Exercise Stage's defensive admission without rebuilding a model that Screenplay rejects first.
        var diagnostics = SemanticCratisAdmission.Evaluate(_context, [_located with { Slice = slice }]);
        diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-039").ShouldEqual(!admitted);
    }
}
