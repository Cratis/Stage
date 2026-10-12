// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_protected_identities
{
    [Theory]
    [InlineData("command identifier", true)]
    [InlineData("command identifier", false)]
    [InlineData("read-model identifier", true)]
    [InlineData("read-model identifier", false)]
    [InlineData("destination", true)]
    [InlineData("destination", false)]
    [InlineData("projection key", true)]
    [InlineData("projection key", false)]
    [InlineData("removal key", true)]
    [InlineData("removal key", false)]
    [InlineData("event-source identifier", true)]
    [InlineData("event-source identifier", false)]
    [InlineData("stream identity", true)]
    [InlineData("stream identity", false)]
    [InlineData("enum", true)]
    [InlineData("enum", false)]
    public void should_refuse_without_partial_artifacts(string use, bool personal)
    {
        var model = invoice_model.Compile(Source(use));
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var application = ProtectedUse(model.Application, use, personal);

        // Upstream validation rejects these shapes too. A producer bypassing it must still
        // receive a target refusal, independently of other malformed-shape diagnostics.
        Set(model, nameof(model.Application), application);
        Set(model, nameof(model.LanguageVersion), LanguageVersion.V13);
        Set(model, nameof(model.SemanticVersion), SemanticVersion.V13);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var request = new ArtifactRenderRequest(model, execution, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var result = new CratisArtifactRenderPlanner().Plan(request);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-040");
        result.Artifacts.ShouldBeEmpty();
    }

    static void Set(ExecutableSemanticModel model, string property, object value) =>
        typeof(ExecutableSemanticModel).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, value);

    static SemanticApplication ProtectedUse(SemanticApplication application, string use, bool personal)
    {
        var name = use switch
        {
            "projection key" or "removal key" => "HealthInformation",
            "enum" => "InvoiceStatus",
            _ => "InvoiceId"
        };

        return application with
        {
            Concepts = [.. application.Concepts.Select(concept => concept.Name == name ? concept with { Compliance = personal ? new(new(), null) : new(null, new()) } : concept)],
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => ChangeIdentityUse(application, slice, use))]
                })]
            })]
        };
    }

    static SemanticSlice ChangeIdentityUse(SemanticApplication application, SemanticSlice slice, string use) => slice with
    {
        Commands = [.. slice.Commands.Select(command => use == "destination" ? command with
        {
            Properties = [.. command.Properties.Select(property => property with { IsIdentifier = false })]
        } : command)],
        Projections = [.. slice.Projections.Select(projection => ChangeKey(application, projection, use))]
    };

    static SemanticProjection ChangeKey(SemanticApplication application, SemanticProjection projection, string use)
    {
        if (use != "projection key" && use != "removal key") return projection;
        var contract = application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Events).Single();
        var property = contract.Properties.Single(property => property.Name == "health");
        var key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([property.Id]));
        var scope = projection.Scope!;

        return projection with
        {
            Scope = use == "projection key"
                ? scope with { From = [.. scope.From.Select(block => block with { Key = key })] }
                : scope with { Removals = [new(contract.Id, key, null)] }
        };
    }

    static string Source(string use)
    {
        var source = a_concept_compliance_model.Unmarked(a_concept_compliance_model.Source);
        if (use == "event-source identifier") return source.Replace("id InvoiceId identifier", "id Uuid identifier", StringComparison.Ordinal) + "\neventsource Invoice\n  identifier InvoiceId\n";
        if (use == "stream identity") return source.Replace("id InvoiceId identifier", "id Uuid identifier", StringComparison.Ordinal) + "\neventsource Invoice\n  identifier String\n  stream Details\n    streamId InvoiceId\n";
        if (use == "command identifier" || use == "destination" || use == "enum") return source;
        if (use == "read-model identifier") source = source.Replace("id InvoiceId identifier", "id Uuid identifier", StringComparison.Ordinal);

        return source + "\n" + a_concept_compliance_model.Lookup;
    }
}
