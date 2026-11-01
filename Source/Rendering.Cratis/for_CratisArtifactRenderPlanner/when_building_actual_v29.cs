// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_building_actual_v29 : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var model = SemanticModelSerializer.Deserialize(AcmeAdmissionsCorpus.V29.EsmBytes.AsSpan());
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return CratisRendering.Plan(
            model,
            execution.Plan!,
            new(ArtifactRenderScopeKind.Application, model.Application.Id),
            new("AcmeAdmissions", "AcmeAdmissions"));
    }

    [Fact]
    async Task should_build_the_canonical_application_and_its_specifications_in_debug()
    {
        try
        {
            var build = await Run("actual-v29-debug-build.log", "build", "AcmeAdmissions.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
