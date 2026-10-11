// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_mixed_exact_number_versions
{
    [Theory]
    [InlineData(10, 11)]
    [InlineData(11, 10)]
    [InlineData(11, 12)]
    [InlineData(12, 11)]
    public void should_refuse_without_emitting_artifacts(int language, int semantic)
    {
        var model = invoice_model.Compile(an_exact_number_model.Source);
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion((uint)language, 0));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion((uint)semantic, 0));
        var plan = invoice_model.Plan(model);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly("STAGE-ESM-016");
        plan.Artifacts.ShouldBeEmpty();
    }
}
