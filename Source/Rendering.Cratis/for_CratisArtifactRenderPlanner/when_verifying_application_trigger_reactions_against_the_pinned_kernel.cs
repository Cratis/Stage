// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Xml.Linq;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_application_trigger_reactions_against_the_pinned_kernel : a_generated_application
{
    const string InnerTest = "Reactions.KernelProbe.ApplicationTriggerProbe.should_append_once_per_namespace_and_skip_a_receipted_redelivery";
    protected override bool DeferPlan => true;

    [MongoFact]
    public async Task should_append_once_per_namespace_and_skip_a_receipted_redelivery_when_configured()
    {
        try
        {
            Install();
            var build = await Run("stage343-build.log", "build", "Reactions.csproj", "-c", "Debug", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var output = await Run("stage343-probe.log", "test", "Reactions.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName={InnerTest}", "--logger", "trx;LogFileName=stage343.trx", "--results-directory", "stage343-results", "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
            var trx = ReadGeneratedFile(Assert.Single(GeneratedFiles("stage343.trx")));
            RequireOneExecutedProbe(trx);
            var root = Environment.GetEnvironmentVariable("AI_WORK_KEEP");
            Assert.False(string.IsNullOrWhiteSpace(root), "AI_WORK_KEEP is required to retain the inner occurrence proof.");
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "stage343-inner.trx"), trx);
        }
        finally
        {
            Cleanup();
        }
    }

    void Install()
    {
        var model = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(an_application_trigger_reaction.Source);
        var reaction = model.Application.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        RenderApplication(plan);
        AddGeneratedHostSupport("Stage343Host.cs", ApplicationTriggerProbeSource.Host);
        AddGeneratedSpecification("Stage343Probe.cs", ApplicationTriggerProbeSource.Probe
            .Replace("__EFFECT_NAME__", $"stage:reaction:{reaction.Id}:productions", StringComparison.Ordinal)
            .Replace("__TRIGGER_ID__", model.Application.Triggers.Single().Id.ToString(), StringComparison.Ordinal));
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
}
#endif
