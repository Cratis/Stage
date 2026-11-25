// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCommandClockRendering;

public class when_admitting_clock_validations : Specification
{
    public enum FindingCase
    {
        ForeignOwner,
        MissingCommand,
        MissingValidation,
        ExtraLeaf,
        WrongMemberRoot,
        ConceptRule,
        ReactionWhere,
        UnknownMember,
        MissingPath
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_admit_validations_owned_by_the_indexed_slice_or_command(bool commandOwned)
    {
        var context = Context();
        var slice = context.SelectedSlices().Single().Slice;
        var owner = commandOwned ? slice.Commands.Single().Id : slice.Id;
        var member = new SemanticUnknownMember(owner, commandOwned ? "command" : "slice", "SemanticValidationRule.ClockOperand", "Application.Modules[0].Features[0].Slices[0].Commands[0].Validations[0].ClockOperand", true);
        var standing = new EsmSupportStanding(EsmVersionStanding.Audited, [member]);
        SemanticCommandClockAdmission.Known(standing, context.Request.Model).UnknownMembers.ShouldBeEmpty();
        var plan = invoice_model.Plan(context.Request.Model);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
    }

    [Theory]
    [InlineData(FindingCase.ForeignOwner)]
    [InlineData(FindingCase.MissingCommand)]
    [InlineData(FindingCase.MissingValidation)]
    [InlineData(FindingCase.ExtraLeaf)]
    [InlineData(FindingCase.WrongMemberRoot)]
    [InlineData(FindingCase.ConceptRule)]
    [InlineData(FindingCase.ReactionWhere)]
    [InlineData(FindingCase.UnknownMember)]
    [InlineData(FindingCase.MissingPath)]
    public void should_preserve_unmatched_clock_findings(FindingCase scenario)
    {
        const string Prefix = "Application.Modules[0].Features[0].Slices[0].Commands[0]";
        var context = Context();
        var path = scenario switch
        {
            FindingCase.MissingCommand => "Application.Modules[0].Features[0].Slices[0].Commands[1].Validations[0].ClockOperand.Kind",
            FindingCase.MissingValidation => Prefix + ".Validations[1].ClockOperand.Kind",
            FindingCase.ExtraLeaf => Prefix + ".Validations[0].ClockOperand.Kind.Extra",
            FindingCase.WrongMemberRoot => Prefix + ".Validations[0].ClockOperand",
            FindingCase.ConceptRule => "Application.Concepts[0].Validations[0].ClockOperand.Kind",
            FindingCase.ReactionWhere => "Application.Modules[0].Features[0].Slices[0].Reactions[0].Where.Right.Clock.Kind",
            FindingCase.MissingPath => string.Empty,
            _ => Prefix + ".Validations[0].ClockOperand.Kind"
        };
        var name = scenario switch
        {
            FindingCase.WrongMemberRoot => "SemanticConditionOperand.Clock",
            FindingCase.UnknownMember => "SemanticClockOperand.Unknown",
            _ => "SemanticClockKind.Now"
        };
        var owner = scenario == FindingCase.ForeignOwner ? context.Application.Id : context.Commands.Values.Single().Id;
        var member = new SemanticUnknownMember(owner, "command", name, path, true);
        var standing = new EsmSupportStanding(EsmVersionStanding.Audited, [member]);
        SemanticCommandClockAdmission.Known(standing, context.Request.Model).UnknownMembers.Single().ShouldEqual(member);
    }

    [Fact]
    public void should_refuse_collection_rules()
    {
        var context = Context();
        var command = context.Commands.Values.Single();
        var properties = command.Properties.Select(property => property.Name == "at" ? property with { Type = property.Type with { IsCollection = true } } : property);
        SemanticCommandClockSupport.Supports(context.Application, command with { Properties = [.. properties] }).ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_unknown_rule_kinds()
    {
        var context = Context();
        var command = context.Commands.Values.Single();
        var rule = command.Validations.Single() with { Kind = (SemanticValidationRuleKind)int.MaxValue };
        SemanticCommandClockSupport.Supports(context.Application, command with { Validations = [rule] }).ShouldBeFalse();
    }

    [Fact]
    public void should_refuse_unknown_clock_units()
    {
        var context = Context();
        var command = context.Commands.Values.Single();
        var rule = command.Validations.Single();
        rule = rule with { ClockOperand = rule.ClockOperand! with { Unit = (SemanticClockUnit)int.MaxValue } };
        SemanticCommandClockSupport.Supports(context.Application, command with { Validations = [rule] }).ShouldBeFalse();
    }

    static SemanticApplicationContext Context()
    {
        var model = invoice_model.Compile("""
            concept Moment : DateTime
            module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    at DateTime
                    validate
                      at >= now
                    produces Kept
                      for id
                  event Kept
            """);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, CratisRendering.CreateProfile("InvoiceApp", options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        return new(request, options);
    }
}
