// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The domain blueprint samples (CRM, Ticketing, Approvals, Inventory, HR, Project tracker, Administration and Orders) are the Screenplay
/// repository's samples, kept beside the live blueprint verification. Each has to plan exactly as authored: a sample that only renders once a
/// feature is taken out of it is a defect of the sample or of this renderer, and is found here rather than by a user (Cratis/Stage#457).
/// </summary>
public class when_planning_the_domain_blueprint_samples
{
    public static TheoryData<string> Samples => new() { "crm", "ticketing", "approvals", "inventory", "hr", "projects", "administration", "orders" };

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task should_plan_the_sample_as_authored(string sample)
    {
        var folder = Path.Combine(Root(), "Verification", "live-blueprints", "models", sample);
        var compiled = await SemanticModelLoader.LoadFromPathAsync(folder, null, "Sample");

        var plan = CratisRendering.PlanFrom(compiled, new PlanSelection([.. compiled.Model.Application.Modules.Select(module => PlanSelectionEntry.Module(module.Name))]), new CratisPlanOptions("Sample", "Sample", "Sample"));

        Assert.Equal(
            string.Empty,
            string.Join(Environment.NewLine, plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        Assert.True(plan.Success);
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Verification")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
