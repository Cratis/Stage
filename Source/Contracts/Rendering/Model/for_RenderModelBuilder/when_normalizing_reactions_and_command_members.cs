// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelBuilder;

public class when_normalizing_reactions_and_command_members : given.a_response_only_command
{
    RenderPayloadNormalizer _normalizer = null!;
    RenderReactionPayload _reaction = null!;
    SemanticReactionTrigger _trigger = null!;

    void Establish()
    {
        _normalizer = new(_request.Model.Application);
        var property = _command.Properties[0].Id;
        var produced = new SemanticProducedEvent(Id("event"), null, null, [new(property, new SemanticValueExpression(new SemanticTextValue("mapped")))])
        {
            Tags = ["tag"],
            Destination = new SemanticRefusalExpression("source", _command.Properties[0].Type),
            DestinationType = _command.Properties[0].Type
        };
        _trigger = new(SemanticReactionTriggerKind.Event)
        {
            Source = Id("event"),
            Produces = [produced],
            RequirementId = "body",
            Invokes = [new(_command.Id, []) { OnRefused = [new("any", null, [produced])] }]
        };
    }

    void Because() => _reaction = _normalizer.Reaction(new(Id("reaction"), "EchoReaction", [_trigger])
    {
        From = new(Id("source"), null),
        RunsAs = new(SemanticReactionIdentityKind.System, ["operator"])
    });

    [Fact] void should_keep_observer_filter() => _reaction.From!.Source.ShouldEqual(Id("source"));
    [Fact] void should_keep_runs_as_roles() => _reaction.RunsAs!.Roles.ShouldEqual(["operator"]);
    [Fact] void should_keep_trigger_order_and_kind() => _reaction.Triggers.Single().Kind.ShouldEqual(SemanticReactionTriggerKind.Event);
    [Fact] void should_keep_trigger_body_reference() => _reaction.Triggers.Single().RequirementId.ShouldEqual("body");
    [Fact] void should_keep_invocation_command() => _reaction.Triggers.Single().Invokes.Single().Command.ShouldEqual(_command.Id);
    [Fact] void should_keep_refusal_selector() => _reaction.Triggers.Single().Invokes.Single().OnRefused.Single().Selector.ShouldEqual("any");
    [Fact] void should_keep_nested_refusal_productions() => _reaction.Triggers.Single().Invokes.Single().OnRefused.Single().Produces.Single().EventContract.ShouldEqual(Id("event"));
    [Fact] void should_keep_production_tags() => _reaction.Triggers.Single().Produces.Single().Tags.ShouldEqual(["tag"]);
    [Fact] void should_keep_destination_type() => _reaction.Triggers.Single().Produces.Single().DestinationType!.Primitive.ShouldEqual(SemanticPrimitiveType.Text);
    [Fact] void should_keep_mapping_target() => _reaction.Triggers.Single().Produces.Single().Mappings.Single().TargetProperty.ShouldEqual(_command.Properties[0].Id);
    [Fact] void should_reject_unknown_trigger_kinds() => Assert.Throws<InvalidArtifactRenderContract>(() => _normalizer.Reaction(new(Id("reaction"), "Unknown", [_trigger with { Kind = SemanticReactionTriggerKind.Unknown }])));

    [Fact]
    void should_keep_schedule_fields()
    {
        var result = _normalizer.Reaction(new(Id("schedule"), "Schedule", [_trigger with { Kind = SemanticReactionTriggerKind.Interval, Source = default, Every = 30, At = 9, OnDayOfWeek = 2, OnDayOfMonth = 12 }])).Triggers.Single();
        result.Source.ShouldBeNull();
        result.Every.ShouldEqual(30L);
        result.At.ShouldEqual(9);
        result.OnDayOfWeek.ShouldEqual(2);
        result.OnDayOfMonth.ShouldEqual(12);
    }

    [Fact]
    void should_keep_command_reads_and_requirements()
    {
        var property = _command.Properties[0].Id;
        var condition = new SemanticComparison(new(property, null), SemanticComparisonOperator.NotEqual, new(default, new SemanticTextValue("")));
        var command = _command with { Reads = [new("current", Id("view"), property, SemanticDecisionReadPresence.Required)], Requirements = [new(condition, "required")], Produces = _trigger.Produces, Route = new(Id("source"), Id("stream")), Destination = new(_command.Properties[0].Type, null) };
        var payload = _normalizer.Command(command, _slice);
        payload.Reads.Single().Name.ShouldEqual("current");
        payload.Requirements.Single().Message.ShouldEqual("required");
        payload.Route!.Stream.ShouldEqual(Id("stream"));
        payload.Destination!.Value.ShouldBeNull();
        payload.Produces.Single().Mappings.Single().Source.TypeState.ShouldEqual(RenderTypeState.Resolved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    void should_normalize_authorization_trees(int kind)
    {
        SemanticPolicyCondition condition = kind switch
        {
            0 => new SemanticAuthenticatedCondition(),
            1 => new SemanticAnonymousCondition(),
            2 => new SemanticRoleCondition("operator"),
            3 => new SemanticClaimCondition("permission", SemanticClaimTargetKind.Literal, "read"),
            4 => new SemanticOpaquePolicyCondition("policy-body"),
            5 => new SemanticNotPolicyCondition(new SemanticRoleCondition("blocked")),
            _ => new SemanticLogicalPolicyCondition(new SemanticAuthenticatedCondition(), SemanticLogicalOperator.And, new SemanticRoleCondition("operator"))
        };
        RenderAuthorization expected = kind switch
        {
            0 => new RenderAuthorization.Authenticated(),
            1 => new RenderAuthorization.Anonymous(),
            2 => new RenderAuthorization.Role("operator"),
            3 => new RenderAuthorization.Claim("permission", SemanticClaimTargetKind.Literal, "read"),
            4 => new RenderAuthorization.OpaquePolicy("policy-body"),
            5 => new RenderAuthorization.NotPolicy(new RenderAuthorization.Role("blocked")),
            _ => new RenderAuthorization.Logical(new RenderAuthorization.Authenticated(), SemanticLogicalOperator.And, new RenderAuthorization.Role("operator"))
        };
        var application = _request.Model.Application with { Policies = [new("access", condition)] };
        var result = new RenderPayloadNormalizer(application).Command(_command with { Authorization = new SemanticPolicyReference("access") }, _slice);
        Assert.IsType<RenderAuthorization.Policy>(result.Authorization).Condition.ShouldEqual(expected);
    }
}
