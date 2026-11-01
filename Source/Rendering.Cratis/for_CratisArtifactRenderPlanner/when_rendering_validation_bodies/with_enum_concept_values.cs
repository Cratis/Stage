// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

public class with_enum_concept_values
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_enum_validation_before_emitting_an_unwrap(bool iterator)
    {
        var validation = iterator
            ? "  validate\n    ```csharp\n    yield return \"Invalid status\";\n    ```"
            : "  validate\n    rule Valid message \"Invalid status\"\n      ```csharp\n      return true;\n      ```";
        var source = an_enumeration_model.Source.Replace("  issued", "  issued\n" + validation, StringComparison.Ordinal);
        var loaded = validation_body_model.Load(source);
        var concept = loaded.Model.Application.Concepts.Single(concept => concept.Name == "Status");
        Assert.NotEmpty(concept.Values);
        Assert.NotEmpty(concept.Validations);

        var plan = opaque_policy_model.Plan(loaded);

        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-002" && diagnostic.Artifact == concept.Id);
        Assert.Empty(plan.Artifacts);
    }
}
#endif
