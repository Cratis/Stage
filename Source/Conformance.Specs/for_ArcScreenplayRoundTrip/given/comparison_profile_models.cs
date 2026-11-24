// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;

internal static class comparison_profile_models
{
    public static ExecutableSemanticModel Legacy
    {
        get
        {
            var model = round_trip_fixtures.Compile("""
                module Banking
                  feature Accounts
                    feature Deposits
                      slice StateChange Deposit
                        command Deposit
                          accountId Uuid identifier
                          amount Decimal
                          produces event Deposited
                            amount Decimal = amount
                """);

            // Today's binder infers an explicit v2 destination. Author the historical v1 graph with its implicit default instead.
            return ExecutableSemanticModel.Create(
                LanguageVersion.V1,
                SemanticVersion.V1,
                Commands(model.Application, command => command with { Destination = null }));
        }
    }

    public static ExecutableSemanticModel Golden(int version) => SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(version).Bytes.AsSpan());

    // Deliberately malformed input for source-revalidation specs only; production conversion must reject it before promotion.
    public static ExecutableSemanticModel Unchecked(ExecutableSemanticModel model, LanguageVersion language, SemanticVersion semantic, SemanticApplication application) =>
        (ExecutableSemanticModel)typeof(ExecutableSemanticModel).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(LanguageVersion), typeof(SemanticVersion), typeof(SemanticRevision), typeof(SemanticApplication)],
            null)!.Invoke([language, semantic, model.Revision, application]);

    // An independently authored explicit destination, rather than obtaining the expected graph from the conversion under test.
    public static ExecutableSemanticModel Explicit(ExecutableSemanticModel model) => ExecutableSemanticModel.Create(
        LanguageVersion.V2,
        SemanticVersion.V2,
        Commands(model.Application, command => command with
        {
            Destination = new(
                command.Properties.Single(property => property.IsIdentifier).Type,
                new SemanticResolvedExpression(
                    SemanticExpressionRootKind.Command,
                    SemanticExpressionSourceKind.Property,
                    command.Properties.Single(property => property.IsIdentifier).Id))
        }));

    public static ExecutableSemanticModel Calendar(ExecutableSemanticModel model, string zone = "UTC", bool configurable = false) =>
        ExecutableSemanticModel.Create(LanguageVersion.V37, SemanticVersion.V37, model.Application with { TimeZone = new(zone, configurable) });

    public static SemanticApplication Commands(SemanticApplication application, Func<SemanticCommand, SemanticCommand> change) =>
        application with { Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(feature => Commands(feature, change))] })] };

    public static IEnumerable<SemanticCommand> Commands(ExecutableSemanticModel model) => model.Application.Modules.SelectMany(module => module.Features).SelectMany(Commands);

    static IEnumerable<SemanticCommand> Commands(SemanticFeature feature) => feature.Slices.SelectMany(slice => slice.Commands).Concat(feature.Features.SelectMany(Commands));

    static SemanticFeature Commands(SemanticFeature feature, Func<SemanticCommand, SemanticCommand> change) => feature with
    {
        Features = [.. feature.Features.Select(child => Commands(child, change))],
        Slices = [.. feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Select(change)] })]
    };
}
#endif
