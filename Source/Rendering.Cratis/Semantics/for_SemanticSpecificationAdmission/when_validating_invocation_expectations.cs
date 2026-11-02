// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_validating_invocation_expectations : for_SemanticReactionSpecificationRenderer.given.an_invocation_expectation
{
    [Theory]
    [InlineData("native")]
    [InlineData("fan-out")]
    [InlineData("multiple-invocations")]
    [InlineData("mixed-effects")]
    [InlineData("refusal")]
    [InlineData("unordered")]
    [InlineData("no-observer")]
    public void should_refuse_unobservable_shapes_on_the_original_specification(string shape)
    {
        var reaction = _located.Slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var expected = _specification.ThenInvocations.Single();
        switch (shape)
        {
            case "native": ChangeTrigger(candidate => candidate with { RequirementId = "native" }); break;
            case "fan-out": ReplaceAutomation(_located.Slice with { Reactions = [reaction, reaction with { Id = SemanticId.Create(SemanticKind.Reaction, "second"), Name = "Second" }] }); break;
            case "multiple-invocations": ChangeTrigger(candidate => candidate with { Invokes = [.. trigger.Invokes, .. trigger.Invokes] }); break;
            case "mixed-effects":
                var produced = _context.Commands[expected.Command].Produces.Single();
                var command = _context.Commands[expected.Command];
                var properties = _context.Events[produced.EventContract].Properties;
                var mappings = produced.Mappings.Select(mapping =>
                {
                    var name = properties.Single(property => property.Id == mapping.TargetProperty).Name;
                    var target = command.Properties.Single(property => property.Name == name);
                    return mapping with { Source = new SemanticValueExpression(expected.Values.Single(value => value.TargetProperty == target.Id).Value) };
                });
                var destination = trigger.Invokes.Single().Mappings.Single(mapping => command.Properties.Single(property => property.Id == mapping.TargetProperty).Name == "invoiceId").Source;
                ChangeTrigger(candidate => candidate with { Produces = [produced with { Destination = destination, DestinationType = command.Properties.Single(property => property.Name == "invoiceId").Type, Mappings = [.. mappings] }] });
                break;
            case "refusal": ChangeTrigger(candidate => candidate with { Invokes = [trigger.Invokes.Single() with { OnRefused = [new("validation", null, [])] }] }); break;
            case "unordered": _specification = _specification with { ThenEventsInAnyOrder = true }; break;
            case "no-observer": ReplaceAutomation(_located.Slice with { Reactions = [] }); break;
        }
        SetExpectations(_specification.ThenInvocations);
        ValidateExpectations();
        var diagnostic = Assert.Single(_diagnostics);
        Assert.Equal("STAGE-ESM-016", diagnostic.Code);
        Assert.Equal(_specification.Id, diagnostic.Artifact);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_defensively_refuse_incompatible_or_duplicate_values(bool duplicate)
    {
        var expected = _specification.ThenInvocations.Single();
        SetExpectations([expected with { Values = duplicate ? [expected.Values[0], expected.Values[0]] : [expected.Values[0] with { Value = SemanticValue.Array([]) }] }]);
        ValidateExpectations();
        Assert.Equal("STAGE-ESM-016", Assert.Single(_diagnostics).Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_admit_mismatches_for_generated_assertions_rather_than_hide_them(bool wrongCommand)
    {
        var expected = _specification.ThenInvocations.Single();
        var other = _context.Commands.Values.Single(command => command.Name == "ArchiveInvoice");
        SetExpectations(wrongCommand ? [new(other.Id, [])] : [expected, expected]);
        ValidateExpectations();
        Assert.Empty(_diagnostics);
    }

    [Fact]
    public void should_leave_absent_invocation_expectations_on_the_common_path()
    {
        ReplaceAutomation(_located.Slice with { Reactions = [] });
        SetExpectations([]);
        ValidateExpectations();
        Assert.Empty(_diagnostics);
    }
}
#endif
