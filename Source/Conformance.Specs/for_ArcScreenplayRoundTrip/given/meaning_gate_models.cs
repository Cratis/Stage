// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class meaning_gate_models
{
    const string Source = """
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                id String identifier
                text String
                produces event Deposited
                  id String = id
                  text String = text
              specification Once
                when Deposit
                  id = "one"
                  text = "x"
                then Deposited
                  id = "one"
                  text = "x"
        """;

    public static ExecutableSemanticModel Action => comparison_profile_models.Explicit(round_trip_fixtures.Compile(Source));

    public static ExecutableSemanticModel Response(string primitive = "String", string value = "\"x\"")
    {
        var model = round_trip_fixtures.Compile(Source.Replace("text String", $"text {primitive}", StringComparison.Ordinal).Replace("\"x\"", value, StringComparison.Ordinal));
        var application = comparison_profile_models.Commands(model.Application, command => command with
        {
            Response = new SemanticScalarCommandResponse(command.Properties.Single(property => property.Name == "text").Id, command.Properties.Single(property => property.Name == "text").Type)
        });
        return ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, application);
    }

    public static ExecutableSemanticModel Generated => round_trip_fixtures.Compile("""
        concept GeneratedId : Uuid
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                id GeneratedId generated identifier
                text String
                returns text
        """);

    public static ExecutableSemanticModel WithoutEvents(ExecutableSemanticModel model) => ExecutableSemanticModel.Create(
        model.LanguageVersion,
        model.SemanticVersion,
        model.Application with { Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(WithoutEvents)] })] });

    static SemanticFeature WithoutEvents(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(WithoutEvents)],
        Slices = [.. feature.Slices.Select(WithoutEvents)]
    };

    static SemanticSlice WithoutEvents(SemanticSlice slice) => slice with
    {
        Events = [],
        Commands = [.. slice.Commands.Select(WithoutEvents)],
        Specifications = [.. slice.Specifications.Select(specification => WithoutEvents(specification, slice.Commands.Single(command => command.Id == specification.When!.Command)))]
    };

    static SemanticCommand WithoutEvents(SemanticCommand command)
    {
        var identity = command.Properties.Single(property => property.IsIdentifier);
        return command with { Produces = [], Destination = new(identity.Type, new SemanticResolvedExpression(SemanticExpressionRootKind.Command, SemanticExpressionSourceKind.Property, identity.Id)) };
    }

    static SemanticSpecification WithoutEvents(SemanticSpecification specification, SemanticCommand command)
    {
        var response = (SemanticScalarCommandResponse)command.Response!;
        return specification with
        {
            ThenEvents = [],
            ThenReturns = specification.ThenErrors.IsEmpty ? new SemanticScalarSpecificationResponse(specification.When!.Values.Single(value => value.TargetProperty == response.Source).Value) : null
        };
    }

    public static ExecutableSemanticModel Specifications(ExecutableSemanticModel model, Func<SemanticSpecification, SemanticSpecification> change) =>
        ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(feature => Specifications(feature, change))] })]
        });

    static SemanticFeature Specifications(SemanticFeature feature, Func<SemanticSpecification, SemanticSpecification> change) => feature with
    {
        Features = [.. feature.Features.Select(child => Specifications(child, change))],
        Slices = [.. feature.Slices.Select(slice => slice with { Specifications = [.. slice.Specifications.Select(change)] })]
    };
}
#endif
