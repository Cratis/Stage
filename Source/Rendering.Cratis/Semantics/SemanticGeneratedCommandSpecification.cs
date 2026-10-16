// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders pins and relational expectations for a command's generated values and response.
/// </summary>
/// <param name="specification">The specification to render.</param>
/// <param name="command">The command under specification.</param>
/// <param name="types">The generated type and value expressions.</param>
/// <param name="context">The indexed semantic application.</param>
internal sealed class SemanticGeneratedCommandSpecification(SemanticSpecification specification, SemanticCommand command, SemanticTypeSystem types, SemanticApplicationContext context)
{
    static readonly DateTimeOffset _receiptTime = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);

    internal bool RequiresEstablish => specification.GivenClock is not null || !specification.GivenEvents.IsEmpty || specification.GivenCaller is not null || GeneratedProperties.Any();

    DateTimeOffset ReceiptTime => SemanticSpecificationClock.TryGet(specification, out var instant) ? instant : _receiptTime;

    IEnumerable<SemanticProperty> GeneratedProperties => command.Properties.Where(property => property.IsGenerated);
    IEnumerable<SemanticProperty> UnpinnedProperties => GeneratedProperties.Where(property => !IsPinned(property.Id));

    internal bool NeedsCommon(SemanticProperty property) => property.IsGenerated || SemanticTypeSystem.ValueNeedsCommon(
        specification.When!.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type);

    internal IEnumerable<string> Arguments() => command.Properties.Where(property => !property.IsGenerated).Select(property =>
        types.Value(specification.When!.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));

    internal void RenderAccepted(CSharpCodeBuilder builder, Action renderEvents)
    {
        renderEvents();
        RenderResponse(builder);
    }

    internal void RenderClock(CSharpCodeBuilder builder)
    {
        if (SemanticSpecificationClock.TryGet(specification, out var instant))
        {
            builder.Line($"_scenario.Clock(new global::System.DateTimeOffset({instant.Ticks}L, global::System.TimeSpan.Zero));");
        }
        else if (UnpinnedProperties.Any(property => SemanticGenerationRendering.UsesContext(context.Concepts[property.Type.Target].Generation)))
        {
            builder.Line("_scenario.Clock(new global::System.DateTimeOffset(2026, 1, 2, 3, 4, 5, 678, global::System.TimeSpan.Zero));");
        }
    }

    internal void RenderFixtures(CSharpCodeBuilder builder)
    {
        foreach (var property in GeneratedProperties)
        {
            var pin = specification.When!.GeneratedValues.SingleOrDefault(value => value.TargetProperty == property.Id);
            if (pin is not null)
            {
                builder.Line($"_scenario.PinGeneratedValue({CSharpCodeBuilder.StringLiteral(property.Name)}, {types.Value(pin.Value, property.Type)});");
            }
        }
        if (UnpinnedProperties.Any()) builder.Line("_scenario.AllowGeneration();");
    }

    internal SemanticEventSourceIdentity Destination(SemanticProducedEvent produced) => UnpinnedDestination(produced) is { } property
        ? new(property.Type, SemanticValue.Null)
        : SemanticDestinations.ForSpecification(specification, command, produced);

    internal bool DestinationNeedsCommon(SemanticProducedEvent produced)
    {
        if (UnpinnedDestination(produced) is not null) return true;
        var source = Destination(produced);

        return SemanticTypeSystem.ValueNeedsCommon(source.Value, source.Type);
    }

    internal string DestinationExpression(SemanticProducedEvent produced)
    {
        var source = Destination(produced);
        var value = UnpinnedDestination(produced) is { } property ? Captured(property) : types.Value(source.Value, source.Type);

        return types.EventSourceExpression(value, source.Type);
    }

    internal string EventPredicate(SemanticProducedEvent produced, SemanticProperty property, string literal)
    {
        if (produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == property.Id)?.Source is not SemanticResolvedExpression source) return literal;
        var generated = UnpinnedProperties.SingleOrDefault(candidate => candidate.Id == source.Target);

        return generated is null ? literal : $"@event.{Identifiers.ToPascalCase(property.Name)} == {Captured(generated)}";
    }

    internal void RenderResponse(CSharpCodeBuilder builder)
    {
        switch (specification.ThenReturns, command.Response)
        {
            case (SemanticScalarSpecificationResponse scalar, SemanticScalarCommandResponse response):
                builder.Line($"[global::Xunit.FactAttribute] void should_return_the_expected_response() => {ScalarResponseValue(response.Type)}.ShouldEqual({ExpectedResponse(response.Source, response.Type, scalar.Value)});");
                break;
            case (SemanticRecordSpecificationResponse record, SemanticRecordCommandResponse contract):
                RenderResponseFields(builder, record, contract);
                break;
        }
        foreach (var property in UnpinnedProperties)
        {
            var expectation = StrategyExpectation(property);
            builder.Line($"[global::Xunit.FactAttribute] void should_generate_{Identifiers.ToSnakeCase(property.Name)}() => {expectation};");
            if (IsTimeOrdered(property))
            {
                var prefix = SemanticGenerationTime.Prefix(context.Concepts[property.Type.Target], ReceiptTime);
                builder.Line($"[global::Xunit.FactAttribute] void should_generate_{Identifiers.ToSnakeCase(property.Name)}_at_the_receipt_time() => {Captured(property)}.Value.ToString()!.StartsWith({CSharpCodeBuilder.StringLiteral(prefix)}, global::System.StringComparison.Ordinal).ShouldBeTrue();");
            }
        }
    }

    string StrategyExpectation(SemanticProperty property)
    {
        var concept = context.Concepts[property.Type.Target];
        if (concept.Generation is { } range && string.Equals(range.Kind, "random", StringComparison.Ordinal))
        {
            return $"{Captured(property)}.Value.ShouldBeInRange({range.Minimum!.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)}L, {range.Maximum!.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)}L)";
        }

        return context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15)
            ? $"global::System.Text.RegularExpressions.Regex.IsMatch({Captured(property)}.Value.ToString()!, {CSharpCodeBuilder.StringLiteral(SemanticGenerationShapes.Pattern(concept, ReceiptTime))}, global::System.Text.RegularExpressions.RegexOptions.CultureInvariant, global::System.TimeSpan.FromSeconds(1)).ShouldBeTrue()"
            : $"{Captured(property)}.Value.ShouldNotEqual(global::System.Guid.Empty)";
    }

    bool IsTimeOrdered(SemanticProperty property) => context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) &&
        SemanticGenerationRendering.IsTimeOrdered(context.Concepts[property.Type.Target].Generation);

    // Arc 22.68 wraps a present optional scalar by its boxed runtime type, and a null response is a plain
    // CommandResult. ResponseValue is protected internal; read the public Response property without assuming
    // the result's generic type, then cast the boxed value (or null) to the model's declared optional type.
    string ScalarResponseValue(SemanticTypeReference type) => type.IsOptional
        ? $"(({types.Type(type)})_result.GetType().GetProperty(nameof(global::Cratis.Arc.Commands.CommandResult<object>.Response))?.GetValue(_result))"
        : $"((global::Cratis.Arc.Commands.CommandResult<{types.Type(type)}>)_result).Response";

    void RenderResponseFields(CSharpCodeBuilder builder, SemanticRecordSpecificationResponse record, SemanticRecordCommandResponse contract)
    {
        var responseType = types.SliceType(command.Id, command.Name + "Response");
        foreach (var field in record.Fields)
        {
            var declared = contract.Fields.Single(candidate => candidate.Name == field.Name);
            builder.Line($"[global::Xunit.FactAttribute] void should_return_{Identifiers.ToSnakeCase(field.Name)}() => ((global::Cratis.Arc.Commands.CommandResult<{responseType}>)_result).Response!.{Identifiers.ToPascalCase(field.Name)}.ShouldEqual({ExpectedResponse(declared.Source, declared.Type, field.Value)});");
        }
    }

    SemanticProperty? UnpinnedDestination(SemanticProducedEvent produced)
    {
        var destination = (SemanticResolvedExpression)SemanticDestinations.Of(command, produced)!;

        return UnpinnedProperties.SingleOrDefault(property => property.Id == destination.Target);
    }

    string ExpectedResponse(SemanticId source, SemanticTypeReference type, SemanticValue value)
    {
        var property = command.Properties.Single(property => property.Id == source);

        return IsUnpinned(property) ? Captured(property) : types.Value(value, type);
    }

    bool IsUnpinned(SemanticProperty property) => property.IsGenerated && !IsPinned(property.Id);

    bool IsPinned(SemanticId property) => specification.When!.GeneratedValues.Any(pin => pin.TargetProperty == property);

    string Captured(SemanticProperty property) =>
        $"_scenario.GeneratedValues.Last<{types.Type(property.Type)}>({CSharpCodeBuilder.StringLiteral(property.Name)})";
}
