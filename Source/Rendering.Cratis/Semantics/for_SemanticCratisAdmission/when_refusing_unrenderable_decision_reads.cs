// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_refusing_unrenderable_decision_reads
{
    const string Generation = "concept Receipt : Uuid\n";

    static string Corpus => ProtectedCommandReadsCorpus.V25.SourceForms.Single(form => form.Name == "single").Documents.Single().Text;

    [Theory]
    [InlineData("generated-value freshness scopes", "freshness")]
    public void should_name_the_reason_with_the_decision_read_code(string reason, string shape)
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Variant(shape)));
        var refusals = plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-044").ToArray();

        plan.Success.ShouldBeFalse();
        refusals.ShouldNotBeEmpty();
        refusals.ShouldEachConformTo(diagnostic => diagnostic.Message.Contains("Command '", StringComparison.Ordinal) && diagnostic.Message.Contains(reason, StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("condition")]
    [InlineData("destination")]
    [InlineData("optional")]
    [InlineData("type")]
    public void should_refuse_hand_built_read_paths(string shape)
    {
        var model = invoice_model.Compile(Corpus);
        var command = Slices(model).SelectMany(slice => slice.Commands).Single(candidate => candidate.Name == "Consume");
        var produced = command.Produces.Single();
        var read = command.Reads.Single();
        var view = Slices(model).SelectMany(slice => slice.ReadModels).Single(candidate => candidate.Id == read.ReadModel);
        var value = view.Properties.Single(property => property.Name == "value");
        var path = new SemanticConditionOperand(value.Id, null) { Read = read.Name };
        var changedCommand = shape switch
        {
            "condition" => command with { Produces = [produced with { When = new SemanticComparison(path, SemanticComparisonOperator.NotEqual, new(default, SemanticValue.Text(""))) }] },
            "destination" => command with { Produces = [produced with { Destination = new SemanticReadPathExpression(read.Name, value.Id) }] },
            _ => command
        };
        var changedValue = shape switch
        {
            "optional" => value with { Type = value.Type with { IsOptional = true } },
            "type" => value with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber) },
            _ => value
        };
        var changed = Rebuild(model, slice => slice with
        {
            Commands = [.. slice.Commands.Select(candidate => candidate.Id == command.Id ? changedCommand : candidate)],
            ReadModels = [.. slice.ReadModels.Select(candidate => candidate.Id == view.Id
                ? candidate with { Properties = [.. candidate.Properties.Select(property => property.Id == value.Id ? changedValue : property)] }
                : candidate)]
        });
        var plan = CratisRendering.Plan(changed, SemanticExecutionPlan.Compile(changed).Plan!, new(ArtifactRenderScopeKind.Application, changed.Application.Id), new("InvoiceApp", "Invoices"));

        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-044" && diagnostic.Artifact == command.Id);
    }

    [Fact]
    public void should_admit_a_decision_without_an_outcome()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Corpus));

        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Message.Contains("RequireApproval", StringComparison.Ordinal) && diagnostic.Message.Contains("no outcome", StringComparison.Ordinal));
    }

    static IEnumerable<SemanticSlice> Slices(ExecutableSemanticModel model) => SemanticVersionFeatures.Slices(model.Application);

    // Fault injection: Screenplay's compiler refuses these shapes, but an ESM document can still carry them.
    static ExecutableSemanticModel Rebuild(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> change)
    {
        SemanticFeature Feature(SemanticFeature feature) => feature with { Features = [.. feature.Features.Select(Feature)], Slices = [.. feature.Slices.Select(change)] };
        var application = model.Application with { Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(Feature)] })] };
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);

        return model;
    }

    static string Variant(string shape) => shape == "freshness"
        ? Corpus.Replace(Generation, "concept Receipt : Uuid\n  generate uuidv7\n", StringComparison.Ordinal)
            .Replace("        sourceId String\n        reads Current by sourceId\n        validate\n", "        sourceId String\n        receipt Receipt generated\n        reads Current by sourceId\n        validate\n", StringComparison.Ordinal)
        : Corpus;
}
