// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal sealed record RenderCommandPayload(
    RenderSequence<RenderProperty> Properties,
    RenderSequence<RenderValidation> Validations,
    RenderSequence<RenderRequirement> Requirements,
    RenderSequence<RenderCodeValidation> CodeValidations,
    RenderSequence<RenderProduction> Produces,
    RenderSequence<RenderRead> Reads,
    RenderResponse Response,
    RenderHandler? Handler,
    RenderAuthorization? Authorization,
    RenderRoute? Route,
    RenderDestination? Destination,
    RenderSequence<RenderConstraintRef> Constraints) : RenderMember;

internal sealed record RenderProperty(SemanticId Id, string Name, RenderSequence<string> NameWords, RenderTypeRef Type, bool IsIdentifier, bool IsGenerated) : RenderMember;
internal sealed record RenderValidation(SemanticId Property, SemanticValidationRuleKind Kind, RenderLiteral? Operand, string? Message, string? Name, string? RequirementId, SemanticValidationSeverity Severity) : RenderMember;
internal sealed record RenderRequirement(RenderExpression Condition, string? Message, SemanticValidationSeverity Severity) : RenderMember;
internal sealed record RenderCodeValidation(string RequirementId) : RenderMember;
internal sealed record RenderProduction(SemanticId EventContract, RenderExpression? When, RenderExpression? Condition, RenderExpression? Destination, RenderTypeRef? DestinationType, RenderSequence<RenderMapping> Mappings, RenderSequence<string> Tags, RenderRoute? Route) : RenderMember;
internal sealed record RenderMapping(SemanticId TargetProperty, RenderExpression Source) : RenderMember;
internal sealed record RenderRead(string Name, SemanticId ReadModel, SemanticId Key, SemanticDecisionReadPresence Presence) : RenderMember;
internal sealed record RenderHandler(string RequirementId, uint ResultVersion) : RenderMember;
internal sealed record RenderRoute(SemanticId Source, SemanticId Stream, RenderExpression? StreamId, RenderSequence<RenderRoutePart> StreamIdParts) : RenderMember;
internal sealed record RenderRoutePart(string Part, RenderExpression Value) : RenderMember;
internal sealed record RenderDestination(RenderTypeRef Type, RenderExpression? Value) : RenderMember;

// Constraints have names, not semantic ids, in Screenplay 4.240.2. Ownership remains slice-level.
internal sealed record RenderConstraintRef(string Name) : RenderMember;

internal abstract record RenderResponse : RenderMember
{
    RenderResponse()
    {
    }
    internal sealed record Absent : RenderResponse;
    internal sealed record Scalar(SemanticId Source, RenderTypeRef Type) : RenderResponse;
    internal sealed record Record(RenderSequence<RenderResponseField> Fields) : RenderResponse;
}

internal sealed record RenderResponseField(string Name, SemanticId Source, RenderTypeRef Type) : RenderMember;
internal sealed record RenderReactionPayload(RenderObserverFilter? From, RenderIdentity? RunsAs, RenderSequence<RenderTrigger> Triggers) : RenderMember;
internal sealed record RenderObserverFilter(SemanticId Source, SemanticId? Stream) : RenderMember;
internal sealed record RenderIdentity(SemanticReactionIdentityKind Kind, RenderSequence<string> Roles) : RenderMember;
internal sealed record RenderTrigger(SemanticReactionTriggerKind Kind, SemanticId? Source, long? Every, int? At, int? OnDayOfWeek, int? OnDayOfMonth, RenderExpression? Where, RenderSequence<RenderProduction> Produces, RenderSequence<RenderInvocation> Invokes, string? RequirementId) : RenderMember;
internal sealed record RenderInvocation(SemanticId Command, RenderSequence<RenderMapping> Mappings, RenderSequence<RenderInvocationRefusal> OnRefused) : RenderMember;
internal sealed record RenderInvocationRefusal(string Selector, string? Constraint, RenderSequence<RenderProduction> Produces) : RenderMember;
