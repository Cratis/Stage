// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_generation_body_is_not_csharp
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task should_preserve_a_throwing_factory_and_independent_artifacts(bool file, bool whitespace)
    {
        var source = generation_rule_model.Source(whitespace ? " \n" : generation_rule_model.PortableBody);
        var loaded = file ? await when_performer_languages_are_not_supported.LoadFile(source, "java") :
            await when_rendering_a_pure_reducer.Load(source.Replace("```csharp", "```java", StringComparison.Ordinal) + when_performer_languages_are_not_supported.Independent);
        when_performer_languages_are_not_supported.AssertLanguageGap(opaque_policy_model.Plan(loaded), loaded, "java");
    }
}
#endif
