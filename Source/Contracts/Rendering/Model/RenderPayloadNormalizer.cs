// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal sealed class RenderPayloadNormalizer(SemanticApplication application, SemanticExpressionRootKind root = SemanticExpressionRootKind.Command)
{
    readonly RenderTypeResolver _types = new(application);
    readonly RenderExpressionNormalizer _expressions = new(new(application), root);
    readonly RenderAuthorizationNormalizer _authorization = new(application);

    internal RenderCommandPayload Command(SemanticCommand command, SemanticSlice slice) => new(
        new(command.Properties.OrderBy(value => value.Id.ToString(), StringComparer.Ordinal).Select(Property)),
        new(command.Validations.Select(Validation)),
        new(command.Requirements.Select(value => new RenderRequirement(_expressions.Condition(value.Condition), value.Message, value.Severity))),
        new(command.CodeValidations.Select(value => new RenderCodeValidation(value.RequirementId))),
        new(command.Produces.Select(Production)),
        new(command.Reads.OrderBy(value => value.Name, StringComparer.Ordinal).Select(value => new RenderRead(value.Name, value.ReadModel, value.Key, value.Presence))),
        Response(command.Response),
        command.Handler is { } handler ? new(handler.RequirementId, handler.ResultVersion) : null,
        command.Authorization is { } authorization ? _authorization.Normalize(authorization) : null,
        Route(command.Route),
        command.Destination is { } destination ? new(_types.Resolve(destination.Type), Expression(destination.Value)) : null,
        new(slice.Constraints.Select(value => new RenderConstraintRef(value.Name))));

    internal RenderReactionPayload Reaction(SemanticReaction reaction) => new(
        reaction.From is { } from ? new(from.Source, from.Stream) : null,
        reaction.RunsAs is { } identity ? new(identity.Kind, new(identity.Roles)) : null,
        new(reaction.Triggers.Select(Trigger)));

    RenderProperty Property(SemanticProperty property) => new(property.Id, property.Name, RenderModelBuilder.Words(property.Name), _types.Resolve(property.Type), property.IsIdentifier, property.IsGenerated);

    RenderValidation Validation(SemanticValidationRule validation) => new(validation.Property, validation.Kind, validation.Operand is { } operand ? _expressions.Literal(operand) : null, validation.Message, validation.Name, validation.RequirementId, validation.Severity);

    RenderResponse Response(SemanticCommandResponse? response) => response switch
    {
        null => new RenderResponse.Absent(),
        SemanticScalarCommandResponse scalar => new RenderResponse.Scalar(scalar.Source, _types.Resolve(scalar.Type)),
        SemanticRecordCommandResponse record => new RenderResponse.Record(new(record.Fields.OrderBy(value => value.Source.ToString(), StringComparer.Ordinal).ThenBy(value => value.Name, StringComparer.Ordinal).Select(value => new RenderResponseField(value.Name, value.Source, _types.Resolve(value.Type))))),
        _ => throw new InvalidArtifactRenderContract($"Unclassified response '{response.GetType().Name}'.")
    };

    RenderProduction Production(SemanticProducedEvent produced) => new(
        produced.EventContract, produced.When is { } when ? _expressions.Condition(when) : null, Expression(produced.Condition), Expression(produced.Destination), produced.DestinationType is { } type ? _types.Resolve(type) : null, Mappings(produced.Mappings), new(produced.Tags), Route(produced.Route));

    RenderRoute? Route(SemanticCommandRoute? route) => route is null ? null : new(route.Source, route.Stream, Expression(route.StreamId), new(route.StreamIdParts.Select(value => new RenderRoutePart(value.Part, _expressions.Normalize(value.Value)))));

    RenderTrigger Trigger(SemanticReactionTrigger trigger)
    {
        if (trigger.Kind == SemanticReactionTriggerKind.Unknown || !Enum.IsDefined(trigger.Kind))
        {
            throw new InvalidArtifactRenderContract($"Unclassified trigger '{trigger.Kind}' with source '{trigger.Source}'.");
        }

        var normalizer = new RenderPayloadNormalizer(application, trigger.Kind == SemanticReactionTriggerKind.Event ? SemanticExpressionRootKind.Event : SemanticExpressionRootKind.Trigger);
        return new(trigger.Kind, trigger.Source.IsSet ? trigger.Source : null, trigger.Every, trigger.At, trigger.OnDayOfWeek, trigger.OnDayOfMonth, trigger.Where is { } where ? normalizer._expressions.Condition(where) : null, new(trigger.Produces.Select(normalizer.Production)), new(trigger.Invokes.Select(normalizer.Invocation)), trigger.RequirementId);
    }

    RenderInvocation Invocation(SemanticInvocation invocation) => new(invocation.Command, Mappings(invocation.Mappings), new(invocation.OnRefused.Select(refusal => new RenderInvocationRefusal(refusal.Selector, refusal.Constraint, new(refusal.Produces.Select(Production))))));

    RenderSequence<RenderMapping> Mappings(ImmutableArray<SemanticPropertyMapping> mappings)
    {
        if (mappings.Select(value => value.TargetProperty).Distinct().Count() != mappings.Length)
        {
            throw new InvalidArtifactRenderContract($"Duplicate mapping target '{mappings.GroupBy(value => value.TargetProperty).First(group => group.Count() > 1).Key}'.");
        }

        return new(mappings.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value => new RenderMapping(value.TargetProperty, _expressions.Normalize(value.Source, _types.Property(value.TargetProperty)))));
    }

    RenderExpression? Expression(SemanticExpression? expression) => expression is null ? null : _expressions.Normalize(expression);
}
