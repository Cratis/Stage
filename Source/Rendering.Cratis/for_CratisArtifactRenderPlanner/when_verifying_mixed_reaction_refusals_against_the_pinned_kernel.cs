// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using System.Xml.Linq;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_mixed_reaction_refusals_against_the_pinned_kernel : a_generated_application
{
    const string InnerTest = "Reactions.KernelProbe.MixedRefusalProbe.should_prove_retention_acknowledgement_and_durable_redelivery";
    protected override bool DeferPlan => true;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_compile_both_probe_modes_without_running_the_framework(bool atomic)
    {
        try
        {
            Install(atomic);
            await Build(atomic);
        }
        finally
        {
            Cleanup();
        }
    }

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    public Task should_match_default_refusal_and_redelivery_when_configured() => Verify(false);

    [MongoFact]
    public Task should_match_atomic_refusal_and_redelivery_when_configured() => Verify(true);
#pragma warning restore CRSPEC0004

    async Task Verify(bool atomic)
    {
        try
        {
            Install(atomic);
            await Build(atomic);
            var mode = atomic ? "atomic" : "default";
            var output = await Run($"stage342-{mode}-probe.log", "test", "Reactions.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName={InnerTest}", "--logger", $"trx;LogFileName=stage342-{mode}.trx", "--results-directory", "stage342-results", "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
            var path = Assert.Single(GeneratedFiles($"stage342-{mode}.trx"));
            RequireOneExecutedProbe(ReadGeneratedFile(path));
            AddEvidence(mode, ReadGeneratedFile(path));
        }
        finally
        {
            Cleanup();
        }
    }

    async Task Build(bool atomic)
    {
        var output = await Run($"stage342-{(atomic ? "atomic" : "default")}-build.log", "build", "Reactions.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        Assert.Equal(string.Empty, BuildWarnings(output));
    }

    void Install(bool atomic)
    {
        var referenceModel = an_automation.Compile((atomic ? "reaction batches atomic\n" : string.Empty) + MixedReactionRefusalProbeSource.Model);
        var reference = ReferenceWorlds(referenceModel);
        var module = referenceModel.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var model = ExecutableSemanticModel.Create(referenceModel.LanguageVersion, referenceModel.SemanticVersion, referenceModel.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [] }] }] }]
        });
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        RenderApplication(plan);
        AddGeneratedHostSupport("Stage342Host.cs", MixedReactionRefusalProbeSource.Host);
        AddGeneratedSpecification("Stage342Probe.cs", MixedReactionRefusalProbeSource.Probe.Replace("__REFERENCE_LITERAL__", JsonSerializer.Serialize(JsonSerializer.Serialize(reference)), StringComparison.Ordinal));
    }

    static Dictionary<string, string[]> ReferenceWorlds(ExecutableSemanticModel model)
    {
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        Assert.Equal(3, plan.Specifications.Count);
        var worlds = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var specification in plan.Specifications.Values)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            Assert.True(reference.Passed, $"Reference/{specification.Name}: {reference.Execution}");
            Assert.Equal(specification.Name == "Unhandled" ? SemanticExecutionOutcomeKind.Rejected : SemanticExecutionOutcomeKind.Accepted, reference.Execution.Kind);
            if (reference.Execution is SemanticRejected rejected) Assert.Equal("Positive amount required", rejected.Details);
            worlds.Add(specification.Name, [.. reference.Execution.World.Facts.Select(fact => Shape(plan, fact))]);
        }
        Assert.Equal(3, worlds["Unhandled"].Length);
        Assert.Equal(3, worlds["Acknowledged"].Length);
        Assert.Equal(5, worlds["ValidControl"].Length);
        return worlds;
    }

    static string Shape(SemanticExecutionPlan plan, SemanticFact fact)
    {
        var contract = plan.Events[fact.EventContract];
        var markerProperty = contract.Properties.SingleOrDefault(property => property.Name == "marker");
        var marker = markerProperty is null ? string.Empty : Assert.IsType<SemanticTextValue>(fact.Values.Single(value => value.TargetProperty == markerProperty.Id).Value).Value;
        return $"{contract.Name}|{Assert.IsType<SemanticTextValue>(fact.Destination).Value}|{marker}";
    }

    static void RequireOneExecutedProbe(string trx)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var document = XDocument.Parse(trx);
        var result = Assert.Single(document.Descendants(ns + "UnitTestResult"));
        Assert.Equal(InnerTest, (string?)result.Attribute("testName"));
        Assert.Equal("Passed", (string?)result.Attribute("outcome"));
        var counters = Assert.Single(document.Descendants(ns + "Counters"));
        Assert.Equal("1", (string?)counters.Attribute("total"));
        Assert.Equal("1", (string?)counters.Attribute("executed"));
        Assert.Equal("1", (string?)counters.Attribute("passed"));
        Assert.Equal("0", (string?)counters.Attribute("notExecuted"));
    }

    static void AddEvidence(string mode, string trx)
    {
        var root = Environment.GetEnvironmentVariable("AI_WORK_KEEP");
        Assert.False(string.IsNullOrWhiteSpace(root), "AI_WORK_KEEP is required to retain the inner runtime proof.");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, $"stage342-{mode}-inner.trx"), trx);
    }
}
#endif
