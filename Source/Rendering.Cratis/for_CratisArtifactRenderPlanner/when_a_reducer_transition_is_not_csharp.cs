// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_reducer_transition_is_not_csharp
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_preserve_a_throwing_transition_and_independent_artifacts(bool file, bool self)
    {
        const string source = when_rendering_a_pure_reducer.Source;
        var loaded = file ? await when_performer_languages_are_not_supported.LoadFile(source, "java") :
            await when_rendering_a_pure_reducer.Load(source.Replace("```csharp", "```java", StringComparison.Ordinal) + when_performer_languages_are_not_supported.Independent);
        when_performer_languages_are_not_supported.AssertLanguageGap(when_rendering_a_pure_reducer.Plan(loaded, self: self), loaded, "java");
    }
}
#endif
